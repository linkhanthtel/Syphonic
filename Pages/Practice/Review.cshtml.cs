using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Syphonic.Data;
using Syphonic.Models;
using Syphonic.Services;

namespace Syphonic.Pages.Practice;

/// <summary>
/// A review session: the questions on the learner's review list that are due now.
/// Questions get onto the list by being answered in a lesson quiz or a mixed round.
/// </summary>
[Authorize]
public class ReviewModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly ReviewService _reviews;

    public ReviewModel(ApplicationDbContext db, ReviewService reviews)
    {
        _db = db;
        _reviews = reviews;
    }

    public IReadOnlyList<QuizQuestion> Questions { get; private set; } = Array.Empty<QuizQuestion>();

    public ReviewSummary Summary { get; private set; } = new(0, 0, 0, 0, 0, null);

    /// <summary>Sent back by hidden form fields so the same questions are graded after submitting.</summary>
    [BindProperty]
    public List<int> QuestionIds { get; set; } = new();

    /// <summary>Filled from form fields named "Answers[questionId]" whose value is the chosen option index.</summary>
    [BindProperty]
    public Dictionary<int, int> Answers { get; set; } = new();

    /// <summary>Set only after a submission has been graded.</summary>
    public QuizResult? Result { get; private set; }

    /// <summary>Question id → its new schedule, shown beside each graded answer.</summary>
    public IReadOnlyDictionary<int, ReviewOutcome> Outcomes { get; private set; } = new Dictionary<int, ReviewOutcome>();

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var ids = await _reviews.GetDueQuestionIdsAsync(userId, ReviewService.SessionSize, cancellationToken);
        Questions = await LoadQuestionsAsync(userId, ids, cancellationToken);
        Summary = await _reviews.GetSummaryAsync(userId, cancellationToken);
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        // Never trust the posted list: cap its size, and LoadQuestionsAsync keeps only this learner's own review questions.
        var ids = QuestionIds.Distinct().Take(ReviewService.SessionSize).ToList();
        Questions = await LoadQuestionsAsync(userId, ids, cancellationToken);
        if (Questions.Count == 0)
            return RedirectToPage();

        var unanswered = Questions.Count(q => !Answers.ContainsKey(q.Id));
        if (unanswered > 0)
        {
            ModelState.AddModelError(string.Empty,
                unanswered == 1 ? "Please answer the last question." : $"Please answer all questions ({unanswered} left).");
            Summary = await _reviews.GetSummaryAsync(userId, cancellationToken);
            return Page();
        }

        Result = QuizGrader.Grade(Questions, Answers);
        Outcomes = await _reviews.RecordAsync(userId, Result.Items, cancellationToken);

        _db.UserActivities.Add(new UserActivity
        {
            UserId = userId,
            Kind = "review_completed",
            Description = $"Reviewed {Result.Total} question{(Result.Total == 1 ? "" : "s")} and got {Result.Score} right."
        });
        await _db.SaveChangesAsync(cancellationToken);

        Summary = await _reviews.GetSummaryAsync(userId, cancellationToken);
        return Page();
    }

    /// <summary>Loads the given questions if they are on this learner's review list, keeping the order of <paramref name="ids"/>.</summary>
    private async Task<IReadOnlyList<QuizQuestion>> LoadQuestionsAsync(string userId, List<int> ids, CancellationToken cancellationToken)
    {
        if (ids.Count == 0)
            return Array.Empty<QuizQuestion>();

        var questions = await _db.QuizQuestions.AsNoTracking()
            .Include(q => q.Lesson)
            .Where(q => ids.Contains(q.Id) && q.Lesson.Published
                && _db.QuestionReviews.Any(r => r.UserId == userId && r.QuizQuestionId == q.Id))
            .ToListAsync(cancellationToken);

        return questions.OrderBy(q => ids.IndexOf(q.Id)).ToList();
    }
}
