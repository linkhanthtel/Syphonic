using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Syphonic.Data;
using Syphonic.Models;
using Syphonic.Services;

namespace Syphonic.Pages.Practice;

/// <summary>
/// A mixed practice round: random questions drawn from every published lesson's quiz.
/// Rounds count towards the activity log and streak, but not towards per-lesson quiz scores.
/// </summary>
public class IndexModel : PageModel
{
    public const int DefaultCount = 5;
    public const int MaxCount = 10;

    /// <summary>Round sizes offered on the page.</summary>
    public static readonly int[] CountChoices = [3, 5, 10];

    private readonly ApplicationDbContext _db;

    public IndexModel(ApplicationDbContext db)
    {
        _db = db;
    }

    public IReadOnlyList<QuizQuestion> Questions { get; private set; } = Array.Empty<QuizQuestion>();

    /// <summary>
    /// The questions in this round, sent back by hidden form fields so the same
    /// (randomly chosen) questions can be graded after the learner submits.
    /// </summary>
    [BindProperty]
    public List<int> QuestionIds { get; set; } = new();

    /// <summary>Filled from form fields named "Answers[questionId]" whose value is the chosen option index.</summary>
    [BindProperty]
    public Dictionary<int, int> Answers { get; set; } = new();

    /// <summary>Set only after a submission has been graded.</summary>
    public QuizResult? Result { get; private set; }

    public async Task OnGetAsync(int count = DefaultCount, CancellationToken cancellationToken = default)
    {
        count = Math.Clamp(count, 1, MaxCount);

        var allIds = await _db.QuizQuestions.AsNoTracking()
            .Where(q => q.Lesson.Published)
            .Select(q => q.Id)
            .ToListAsync(cancellationToken);

        var picked = allIds.OrderBy(_ => Random.Shared.Next()).Take(count).ToList();
        Questions = await LoadQuestionsAsync(picked, cancellationToken);
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        // Never trust the posted list: cap its size, and LoadQuestionsAsync drops unpublished or unknown ids.
        var ids = QuestionIds.Distinct().Take(MaxCount).ToList();
        Questions = await LoadQuestionsAsync(ids, cancellationToken);
        if (Questions.Count == 0)
            return RedirectToPage();

        var unanswered = Questions.Count(q => !Answers.ContainsKey(q.Id));
        if (unanswered > 0)
        {
            ModelState.AddModelError(string.Empty,
                unanswered == 1 ? "Please answer the last question." : $"Please answer all questions ({unanswered} left).");
            return Page();
        }

        Result = QuizGrader.Grade(Questions, Answers);

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is not null)
        {
            _db.UserActivities.Add(new UserActivity
            {
                UserId = userId,
                Kind = "practice_completed",
                Description = $"Scored {Result.Score}/{Result.Total} in a mixed practice round."
            });
            await _db.SaveChangesAsync(cancellationToken);
        }

        return Page();
    }

    /// <summary>Loads the given questions (published lessons only), keeping the order of <paramref name="ids"/>.</summary>
    private async Task<IReadOnlyList<QuizQuestion>> LoadQuestionsAsync(List<int> ids, CancellationToken cancellationToken)
    {
        if (ids.Count == 0)
            return Array.Empty<QuizQuestion>();

        var questions = await _db.QuizQuestions.AsNoTracking()
            .Include(q => q.Lesson)
            .Where(q => ids.Contains(q.Id) && q.Lesson.Published)
            .ToListAsync(cancellationToken);

        return questions.OrderBy(q => ids.IndexOf(q.Id)).ToList();
    }
}
