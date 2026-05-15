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

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return Page();

        await EnsureLessonStartedAsync(userId, Lesson, cancellationToken);
        Progress = await _db.LessonProgress.AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == userId && p.LessonId == Lesson.Id, cancellationToken);

        return Page();
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
