using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Syphonic.Data;
using Syphonic.Models;
using Syphonic.Services;

namespace Syphonic.Pages.Lessons;

public class DetailModel : PageModel
{
    private readonly ApplicationDbContext _db;

    public DetailModel(ApplicationDbContext db)
    {
        _db = db;
    }

    public Lesson? Lesson { get; private set; }

    public string LessonHtml { get; private set; } = string.Empty;

    public LessonProgress? Progress { get; private set; }

    public bool IsCompleted => Progress?.CompletedAt is not null;

    public int QuestionCount { get; private set; }

    public int? BestQuizPercent { get; private set; }

    public LessonLink? PreviousLesson { get; private set; }

    public LessonLink? NextLesson { get; private set; }

    public sealed record LessonLink(string Title, string Slug);

    [TempData]
    public string? StatusMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(string slug, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(slug))
            return NotFound();

        var query = _db.Lessons.AsQueryable();
        if (!User.IsInRole("Admin"))
            query = query.Where(l => l.Published);

        Lesson = await query.AsNoTracking().FirstOrDefaultAsync(l => l.Slug == slug, cancellationToken);

        if (Lesson is null)
            return NotFound();

        LessonHtml = LessonMarkdown.ToHtml(Lesson.Content);
        QuestionCount = await _db.QuizQuestions.CountAsync(q => q.LessonId == Lesson.Id, cancellationToken);
        await LoadNeighboursAsync(Lesson, cancellationToken);

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return Page();

        await EnsureLessonStartedAsync(userId, Lesson, cancellationToken);
        Progress = await _db.LessonProgress.AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == userId && p.LessonId == Lesson.Id, cancellationToken);

        var attempts = await _db.QuizAttempts.AsNoTracking()
            .Where(a => a.UserId == userId && a.LessonId == Lesson.Id)
            .ToListAsync(cancellationToken);
        BestQuizPercent = attempts.Count == 0 ? null : attempts.Max(a => a.Percent);

        return Page();
    }

    /// <summary>
    /// Finds the lessons just before and after this one in the course order, so
    /// learners can move through the course without going back to the list.
    /// </summary>
    private async Task LoadNeighboursAsync(Lesson current, CancellationToken cancellationToken)
    {
        var query = _db.Lessons.AsNoTracking();
        if (!User.IsInRole("Admin"))
            query = query.Where(l => l.Published);

        var ordered = await query
            .OrderBy(l => l.OrderIndex).ThenBy(l => l.Title)
            .Select(l => new { l.Id, l.Title, l.Slug })
            .ToListAsync(cancellationToken);

        var index = ordered.FindIndex(l => l.Id == current.Id);
        if (index > 0)
            PreviousLesson = new LessonLink(ordered[index - 1].Title, ordered[index - 1].Slug);
        if (index >= 0 && index < ordered.Count - 1)
            NextLesson = new LessonLink(ordered[index + 1].Title, ordered[index + 1].Slug);
    }

    public async Task<IActionResult> OnPostCompleteAsync(string slug, CancellationToken cancellationToken)
    {
        if (User.Identity?.IsAuthenticated != true)
            return Challenge();

        if (string.IsNullOrWhiteSpace(slug))
            return NotFound();

        var query = _db.Lessons.AsQueryable();
        if (!User.IsInRole("Admin"))
            query = query.Where(l => l.Published);

        var lesson = await query.FirstOrDefaultAsync(l => l.Slug == slug, cancellationToken);
        if (lesson is null)
            return NotFound();

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return Challenge();

        var progress = await _db.LessonProgress.FirstOrDefaultAsync(
            p => p.UserId == userId && p.LessonId == lesson.Id, cancellationToken);

        if (progress is null)
        {
            progress = new LessonProgress
            {
                UserId = userId,
                LessonId = lesson.Id,
                StartedAt = DateTimeOffset.UtcNow
            };
            _db.LessonProgress.Add(progress);
        }

        if (progress.CompletedAt is null)
        {
            progress.CompletedAt = DateTimeOffset.UtcNow;
            progress.StartedAt ??= progress.CompletedAt;

            _db.UserActivities.Add(new UserActivity
            {
                UserId = userId,
                Kind = "lesson_completed",
                Description = $"Completed “{lesson.Title}”."
            });

            await _db.SaveChangesAsync(cancellationToken);
            StatusMessage = "Lesson marked complete—nice work!";
        }

        return RedirectToPage(new { slug });
    }

    private async Task EnsureLessonStartedAsync(string userId, Lesson lesson, CancellationToken cancellationToken)
    {
        var tracked = await _db.LessonProgress.FirstOrDefaultAsync(
            p => p.UserId == userId && p.LessonId == lesson.Id, cancellationToken);

        if (tracked is null)
        {
            _db.LessonProgress.Add(new LessonProgress
            {
                UserId = userId,
                LessonId = lesson.Id,
                StartedAt = DateTimeOffset.UtcNow
            });

            _db.UserActivities.Add(new UserActivity
            {
                UserId = userId,
                Kind = "lesson_started",
                Description = $"Opened “{lesson.Title}”."
            });

            await _db.SaveChangesAsync(cancellationToken);
            return;
        }

        if (tracked.StartedAt is null)
        {
            tracked.StartedAt = DateTimeOffset.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
        }
    }
}
