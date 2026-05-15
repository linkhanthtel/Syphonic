using System.Security.Claims;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Syphonic.Data;

namespace Syphonic.Pages.Lessons;

public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _db;

    public IndexModel(ApplicationDbContext db)
    {
        _db = db;
    }

    public IReadOnlyList<LessonListItem> Lessons { get; private set; } = Array.Empty<LessonListItem>();

    public sealed record LessonListItem(
        int Id,
        string Title,
        string Slug,
        string? Summary,
        int OrderIndex,
        bool HasProgress,
        bool IsCompleted);

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var query = _db.Lessons.AsNoTracking();
        if (!User.IsInRole("Admin"))
            query = query.Where(l => l.Published);

        var projected = await query
            .OrderBy(l => l.OrderIndex).ThenBy(l => l.Title)
            .Select(l => new { l.Id, l.Title, l.Slug, l.Summary, l.OrderIndex })
            .ToListAsync(cancellationToken);

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        Dictionary<int, (bool HasProgress, bool IsCompleted)> state = [];
        if (userId is not null && projected.Count != 0)
        {
            var lessonIds = projected.Select(entry => entry.Id).ToHashSet();
            state = await _db.LessonProgress.AsNoTracking()
                .Where(p => p.UserId == userId && lessonIds.Contains(p.LessonId))
                .ToDictionaryAsync(
                    p => p.LessonId,
                    p => (HasProgress: true, IsCompleted: p.CompletedAt.HasValue),
                    cancellationToken);
        }

        Lessons = projected.ConvertAll(entry =>
        {
            state.TryGetValue(entry.Id, out var snapshot);
            return new LessonListItem(
                entry.Id,
                entry.Title,
                entry.Slug,
                entry.Summary,
                entry.OrderIndex,
                snapshot.HasProgress,
                snapshot.IsCompleted);
        });
    }
}
