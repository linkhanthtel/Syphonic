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

    /// <summary>The lessons to show, after search and the status filter are applied.</summary>
    public IReadOnlyList<LessonListItem> Lessons { get; private set; } = Array.Empty<LessonListItem>();

    /// <summary>
    /// Search text from the query string (/Lessons?q=ocean). SupportsGet lets a GET
    /// request fill this property; by default [BindProperty] only binds on POST.
    /// </summary>
    [BindProperty(SupportsGet = true, Name = "q")]
    public string? Query { get; set; }

    /// <summary>Status filter from the query string (/Lessons?show=todo). See <see cref="Filters"/>.</summary>
    [BindProperty(SupportsGet = true, Name = "show")]
    public string? Show { get; set; }

    public bool IsSearching => !string.IsNullOrWhiteSpace(Query);

    public bool IsSignedIn { get; private set; }

    /// <summary>Filter tabs with how many lessons each would show (counted before the filter is applied).</summary>
    public IReadOnlyList<FilterTab> Filters { get; private set; } = Array.Empty<FilterTab>();

    // Course-wide numbers for the progress header (ignore search and filter).
    public int TotalLessons { get; private set; }

    public int CompletedLessons { get; private set; }

    public int CompletionPercent => TotalLessons == 0 ? 0 : CompletedLessons * 100 / TotalLessons;

    /// <summary>The first unfinished lesson in course order, for the "Continue" button.</summary>
    public LessonListItem? ContinueLesson { get; private set; }

    public sealed record FilterTab(string Key, string Label, int Count, bool IsActive);

    public sealed record LessonListItem(
        int Id,
        int Number,
        string Title,
        string Slug,
        string? Summary,
        bool Published,
        int QuestionCount,
        bool HasProgress,
        bool IsCompleted,
        bool IsBookmarked,
        int? BestQuizPercent);

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        IsSignedIn = userId is not null;

        // 1. The whole course, in order. Admins also see drafts.
        var courseQuery = _db.Lessons.AsNoTracking();
        if (!User.IsInRole("Admin"))
            courseQuery = courseQuery.Where(l => l.Published);

        var course = await courseQuery
            .OrderBy(l => l.OrderIndex).ThenBy(l => l.Title)
            .Select(l => new { l.Id, l.Title, l.Slug, l.Summary, l.Published, QuestionCount = l.Questions.Count })
            .ToListAsync(cancellationToken);

        // 2. This learner's state for each lesson.
        Dictionary<int, bool> completedByLesson = [];
        HashSet<int> bookmarked = [];
        Dictionary<int, int> bestQuiz = [];
        if (userId is not null)
        {
            completedByLesson = await _db.LessonProgress.AsNoTracking()
                .Where(p => p.UserId == userId)
                .ToDictionaryAsync(p => p.LessonId, p => p.CompletedAt.HasValue, cancellationToken);

            bookmarked = (await _db.Bookmarks.AsNoTracking()
                .Where(b => b.UserId == userId)
                .Select(b => b.LessonId)
                .ToListAsync(cancellationToken)).ToHashSet();

            // Percent is calculated in C#, so group in memory rather than in SQL.
            bestQuiz = (await _db.QuizAttempts.AsNoTracking()
                    .Where(a => a.UserId == userId)
                    .ToListAsync(cancellationToken))
                .GroupBy(a => a.LessonId)
                .ToDictionary(g => g.Key, g => g.Max(a => a.Percent));
        }

        var all = course
            .Select((l, index) => new LessonListItem(
                l.Id,
                index + 1,
                l.Title,
                l.Slug,
                l.Summary,
                l.Published,
                l.QuestionCount,
                completedByLesson.ContainsKey(l.Id),
                completedByLesson.GetValueOrDefault(l.Id),
                bookmarked.Contains(l.Id),
                bestQuiz.TryGetValue(l.Id, out var best) ? best : null))
            .ToList();

        TotalLessons = all.Count;
        CompletedLessons = all.Count(l => l.IsCompleted);
        ContinueLesson = all.FirstOrDefault(l => l.HasProgress && !l.IsCompleted) ?? all.FirstOrDefault(l => !l.IsCompleted);

        // 3. Search (in SQL, so lesson bodies never have to be loaded into memory).
        IEnumerable<LessonListItem> visible = all;
        if (IsSearching)
        {
            Query = Query!.Trim();
            var pattern = $"%{EscapeLike(Query)}%";
            // LIKE is case-insensitive for English letters in SQLite.
            var matchingIds = (await courseQuery
                .Where(l =>
                    EF.Functions.Like(l.Title, pattern, "\\") ||
                    EF.Functions.Like(l.Summary!, pattern, "\\") ||
                    EF.Functions.Like(l.Content, pattern, "\\"))
                .Select(l => l.Id)
                .ToListAsync(cancellationToken)).ToHashSet();

            visible = all.Where(l => matchingIds.Contains(l.Id));
        }

        var searched = visible.ToList();

        // 4. Status filter tabs. Guests have no progress, so they only get "All" and "Has a quiz".
        var tabs = new List<(string Key, string Label, Func<LessonListItem, bool> Matches)>
        {
            ("all", "All", _ => true)
        };
        if (IsSignedIn)
        {
            tabs.Add(("todo", "Not started", l => !l.HasProgress));
            tabs.Add(("doing", "In progress", l => l.HasProgress && !l.IsCompleted));
            tabs.Add(("done", "Completed", l => l.IsCompleted));
            tabs.Add(("saved", "Saved", l => l.IsBookmarked));
        }
        tabs.Add(("quiz", "Has a quiz", l => l.QuestionCount > 0));

        var active = tabs.FirstOrDefault(t => string.Equals(t.Key, Show, StringComparison.OrdinalIgnoreCase));
        if (active.Key is null)
            active = tabs[0];
        Show = active.Key;

        Filters = tabs.ConvertAll(t => new FilterTab(t.Key, t.Label, searched.Count(t.Matches), t.Key == active.Key));
        Lessons = searched.Where(active.Matches).ToList();
    }

    /// <summary>Stops user input like "100%" from acting as a LIKE wildcard.</summary>
    private static string EscapeLike(string value)
        => value.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
}
