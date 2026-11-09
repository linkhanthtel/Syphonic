using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Syphonic.Data;
using Syphonic.Models;
using Syphonic.Services;

namespace Syphonic.Pages.Lessons;

public class QuizModel : PageModel
{
    private readonly ApplicationDbContext _db;

    public QuizModel(ApplicationDbContext db)
    {
        _db = db;
    }

    public Lesson Lesson { get; private set; } = null!;

    public IReadOnlyList<QuizQuestion> Questions { get; private set; } = Array.Empty<QuizQuestion>();

    /// <summary>
    /// Filled from form fields named "Answers[questionId]" whose value is the chosen option index.
    /// </summary>
    [BindProperty]
    public Dictionary<int, int> Answers { get; set; } = new();

    /// <summary>Set only after a submission has been graded.</summary>
    public QuizResult? Result { get; private set; }

    public int? BestPercent { get; private set; }

    public int AttemptCount { get; private set; }

    public async Task<IActionResult> OnGetAsync(string slug, CancellationToken cancellationToken)
    {
        if (!await LoadAsync(slug, cancellationToken))
            return NotFound();

        await LoadHistoryAsync(cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string slug, CancellationToken cancellationToken)
    {
        if (!await LoadAsync(slug, cancellationToken))
            return NotFound();

        if (Questions.Count == 0)
            return RedirectToPage();

        var unanswered = Questions.Count(q => !Answers.ContainsKey(q.Id));
        if (unanswered > 0)
        {
            ModelState.AddModelError(string.Empty,
                unanswered == 1 ? "Please answer the last question." : $"Please answer all questions ({unanswered} left).");
            await LoadHistoryAsync(cancellationToken);
            return Page();
        }

        Result = QuizGrader.Grade(Questions, Answers);

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is not null)
        {
            _db.QuizAttempts.Add(new QuizAttempt
            {
                UserId = userId,
                LessonId = Lesson.Id,
                Score = Result.Score,
                TotalQuestions = Result.Total
            });

            _db.UserActivities.Add(new UserActivity
            {
                UserId = userId,
                Kind = "quiz_completed",
                Description = $"Scored {Result.Score}/{Result.Total} on the “{Lesson.Title}” quiz."
            });

            await _db.SaveChangesAsync(cancellationToken);
        }

        await LoadHistoryAsync(cancellationToken);
        return Page();
    }

    private async Task<bool> LoadAsync(string slug, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(slug))
            return false;

        var query = _db.Lessons.AsNoTracking();
        if (!User.IsInRole("Admin"))
            query = query.Where(l => l.Published);

        var lesson = await query.FirstOrDefaultAsync(l => l.Slug == slug, cancellationToken);
        if (lesson is null)
            return false;

        Lesson = lesson;
        Questions = await _db.QuizQuestions.AsNoTracking()
            .Where(q => q.LessonId == lesson.Id)
            .OrderBy(q => q.OrderIndex).ThenBy(q => q.Id)
            .ToListAsync(cancellationToken);

        return true;
    }

    private async Task LoadHistoryAsync(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return;

        var attempts = await _db.QuizAttempts.AsNoTracking()
            .Where(a => a.UserId == userId && a.LessonId == Lesson.Id)
            .Select(a => new { a.Score, a.TotalQuestions })
            .ToListAsync(cancellationToken);

        AttemptCount = attempts.Count;
        BestPercent = attempts.Count == 0
            ? null
            : attempts.Max(a => a.TotalQuestions == 0 ? 0 : (int)Math.Round(a.Score * 100m / a.TotalQuestions, MidpointRounding.AwayFromZero));
    }
}
