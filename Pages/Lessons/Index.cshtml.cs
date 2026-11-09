using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
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

    /// <summary>
    /// Search text from the query string (/Lessons?q=ocean). SupportsGet lets a GET
    /// request fill this property; by default [BindProperty] only binds on POST.
    /// </summary>
    [BindProperty(SupportsGet = true, Name = "q")]
    public string? Query { get; set; }

    public bool IsSearching => !string.IsNullOrWhiteSpace(Query);

    public sealed record LessonListItem(
        int Id,
        string Title,
        string Slug,
        string? Summary,
        int OrderIndex,
        int QuestionCount,
        bool HasProgress,
        bool IsCompleted);

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var query = _db.Lessons.AsNoTracking();
        if (!User.IsInRole("Admin"))
            query = query.Where(l => l.Published);

        if (IsSearching)
        {
            Query = Query!.Trim();
            var pattern = $"%{EscapeLike(Query)}%";
            // LIKE is case-insensitive for English letters in SQLite.
            query = query.Where(l =>
                EF.Functions.Like(l.Title, pattern, "\\") ||
                EF.Functions.Like(l.Summary!, pattern, "\\") ||
                EF.Functions.Like(l.Content, pattern, "\\"));
        }

        var projected = await query
            .OrderBy(l => l.OrderIndex).ThenBy(l => l.Title)
            .Select(l => new { l.Id, l.Title, l.Slug, l.Summary, l.OrderIndex, QuestionCount = l.Questions.Count })
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
                entry.QuestionCount,
                snapshot.HasProgress,
                snapshot.IsCompleted);
        });
    }

    /// <summary>Stops user input like "100%" from acting as a LIKE wildcard.</summary>
    private static string EscapeLike(string value)
        => value.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
}
