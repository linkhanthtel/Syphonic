using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Syphonic.Data;

namespace Syphonic.Pages.Bookmarks;

/// <summary>The learner's reading list. URL: /Bookmarks</summary>
[Authorize]
public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _db;

    public IndexModel(ApplicationDbContext db)
    {
        _db = db;
    }

    public IReadOnlyList<SavedLesson> Items { get; private set; } = Array.Empty<SavedLesson>();

    [TempData]
    public string? StatusMessage { get; set; }

    public sealed record SavedLesson(int LessonId, string Title, string Slug, string? Summary, DateTimeOffset SavedAt, bool IsCompleted);

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var isAdmin = User.IsInRole("Admin");

        var rows = await _db.Bookmarks.AsNoTracking()
            .Where(b => b.UserId == userId && (isAdmin || b.Lesson.Published))
            .Select(b => new
            {
                b.LessonId,
                b.Lesson.Title,
                b.Lesson.Slug,
                b.Lesson.Summary,
                b.CreatedAt,
                IsCompleted = _db.LessonProgress.Any(p => p.UserId == userId && p.LessonId == b.LessonId && p.CompletedAt != null)
            })
            .ToListAsync(cancellationToken);

        // SQLite cannot ORDER BY DateTimeOffset; sort in memory. Unfinished lessons first, newest saves first.
        Items = rows
            .OrderBy(r => r.IsCompleted)
            .ThenByDescending(r => r.CreatedAt)
            .Select(r => new SavedLesson(r.LessonId, r.Title, r.Slug, r.Summary, r.CreatedAt, r.IsCompleted))
            .ToList();
    }

    public async Task<IActionResult> OnPostRemoveAsync(int lessonId, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        // Filtering by userId means a learner can only ever delete their own bookmarks,
        // even if they edit the lessonId in the form.
        var bookmark = await _db.Bookmarks
            .FirstOrDefaultAsync(b => b.UserId == userId && b.LessonId == lessonId, cancellationToken);

        if (bookmark is not null)
        {
            _db.Bookmarks.Remove(bookmark);
            await _db.SaveChangesAsync(cancellationToken);
            StatusMessage = "Removed from your reading list.";
        }

        return RedirectToPage();
    }
}
