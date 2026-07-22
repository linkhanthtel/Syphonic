using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Syphonic.Data;

namespace Syphonic.Pages.Dashboard;

[Authorize]
public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _db;

    public IndexModel(ApplicationDbContext db)
    {
        _db = db;
    }

    public int CompletedLessons { get; private set; }

    public int PublishedLessonCount { get; private set; }

    public int CompletionPercent { get; private set; }

    public int InProgressCount { get; private set; }

    public int DistinctPracticeDaysRecent { get; private set; }

    public DateTimeOffset? LastLessonCompletedAt { get; private set; }

    public IReadOnlyList<ActivityVm> RecentActivities { get; private set; } = Array.Empty<ActivityVm>();

    public IReadOnlyList<SuggestedLessonVm> SuggestedLessons { get; private set; } = Array.Empty<SuggestedLessonVm>();

    public sealed record ActivityVm(string Kind, string Description, DateTimeOffset CreatedAt);

    public sealed record SuggestedLessonVm(string Title, string Slug, string? Summary);

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return;

        PublishedLessonCount = await _db.Lessons.CountAsync(l => l.Published, cancellationToken);

        CompletedLessons = await _db.LessonProgress
            .Join(
                _db.Lessons.Where(l => l.Published),
                progress => progress.LessonId,
                lesson => lesson.Id,
                (progress, _) => progress)
            .CountAsync(p => p.UserId == userId && p.CompletedAt != null, cancellationToken);

        CompletionPercent = PublishedLessonCount == 0
            ? 0
            : (int)Math.Round(CompletedLessons * 100m / PublishedLessonCount, MidpointRounding.AwayFromZero);

        InProgressCount = await _db.LessonProgress
            .Join(
                _db.Lessons.Where(l => l.Published),
                progress => progress.LessonId,
                lesson => lesson.Id,
                (progress, _) => progress)
            .CountAsync(
                p => p.UserId == userId && p.CompletedAt == null && p.StartedAt != null,
                cancellationToken);

        // SQLite cannot translate DateTimeOffset range filters; filter in memory after fetch.
        var weekAgo = DateTimeOffset.UtcNow.AddDays(-7);
        var activityTimestamps = await _db.UserActivities.AsNoTracking()
            .Where(a => a.UserId == userId)
            .Select(a => a.CreatedAt)
            .ToListAsync(cancellationToken);

        DistinctPracticeDaysRecent = activityTimestamps
            .Where(ts => ts >= weekAgo)
            .Select(ts => ts.UtcDateTime.Date)
            .Distinct()
            .Count();

        // SQLite cannot ORDER BY DateTimeOffset; sort in memory after fetch.
        var completionTimestamps = await _db.LessonProgress.AsNoTracking()
            .Join(
                _db.Lessons.Where(l => l.Published),
                progress => progress.LessonId,
                lesson => lesson.Id,
                (progress, _) => progress)
            .Where(p => p.UserId == userId && p.CompletedAt != null)
            .Select(p => p.CompletedAt)
            .ToListAsync(cancellationToken);

        LastLessonCompletedAt = completionTimestamps
            .OrderByDescending(ts => ts)
            .FirstOrDefault();

        var activities = await _db.UserActivities.AsNoTracking()
            .Where(a => a.UserId == userId)
            .Select(a => new ActivityVm(a.Kind, a.Description, a.CreatedAt))
            .ToListAsync(cancellationToken);

        RecentActivities = activities
            .OrderByDescending(a => a.CreatedAt)
            .Take(12)
            .ToList();

        var finishedLessonIds = await _db.LessonProgress.AsNoTracking()
            .Join(
                _db.Lessons.Where(l => l.Published),
                progress => progress.LessonId,
                lesson => lesson.Id,
                (progress, _) => progress)
            .Where(p => p.UserId == userId && p.CompletedAt != null)
            .Select(p => p.LessonId)
            .ToListAsync(cancellationToken);

        var finishedLookup = finishedLessonIds.ToHashSet();

        SuggestedLessons = await _db.Lessons.AsNoTracking()
            .Where(l => l.Published && !finishedLookup.Contains(l.Id))
            .OrderBy(l => l.OrderIndex).ThenBy(l => l.Title)
            .Take(5)
            .Select(l => new SuggestedLessonVm(l.Title, l.Slug, l.Summary))
            .ToListAsync(cancellationToken);
    }
}
