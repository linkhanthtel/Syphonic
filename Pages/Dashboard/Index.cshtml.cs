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

    public int InProgressCount { get; private set; }

    public IReadOnlyList<ActivityVm> RecentActivities { get; private set; } = Array.Empty<ActivityVm>();

    public sealed record ActivityVm(string Kind, string Description, DateTimeOffset CreatedAt);

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

        InProgressCount = await _db.LessonProgress
            .Join(
                _db.Lessons.Where(l => l.Published),
                progress => progress.LessonId,
                lesson => lesson.Id,
                (progress, _) => progress)
            .CountAsync(
                p => p.UserId == userId && p.CompletedAt == null && p.StartedAt != null,
                cancellationToken);

        RecentActivities = await _db.UserActivities.AsNoTracking()
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.CreatedAt)
            .Take(12)
            .Select(a => new ActivityVm(a.Kind, a.Description, a.CreatedAt))
            .ToListAsync(cancellationToken);
    }
}
