using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Syphonic.Data;
using Syphonic.Models;

namespace Syphonic.Areas.Admin.Pages.Lessons;

public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _db;

    public IndexModel(ApplicationDbContext db)
    {
        _db = db;
    }

    public IReadOnlyList<LessonRow> Lessons { get; private set; } = Array.Empty<LessonRow>();

    public sealed record LessonRow(int Id, string Title, string Slug, bool Published, int OrderIndex, DateTimeOffset UpdatedAt);

    public async Task OnGetAsync(CancellationToken cancellationToken)
        => Lessons = await _db.Lessons
            .AsNoTracking()
            .OrderBy(l => l.OrderIndex)
            .ThenBy(l => l.Title)
            .Select(l => new LessonRow(l.Id, l.Title, l.Slug, l.Published, l.OrderIndex, l.UpdatedAt))
            .ToListAsync(cancellationToken);

    public async Task<IActionResult> OnPostTogglePublishAsync(int lessonId, CancellationToken cancellationToken)
    {
        var lesson = await _db.Lessons.FirstOrDefaultAsync(l => l.Id == lessonId, cancellationToken);
        if (lesson is null)
            return NotFound();

        lesson.Published = !lesson.Published;
        lesson.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        TempData["Message"] = lesson.Published ? "Lesson is now visible to learners." : "Lesson is back in drafts.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id, CancellationToken cancellationToken)
    {
        var lesson = await _db.Lessons.FirstOrDefaultAsync(l => l.Id == id, cancellationToken);
        if (lesson is null)
            return NotFound();

        _db.Lessons.Remove(lesson);
        await _db.SaveChangesAsync(cancellationToken);

        TempData["Message"] = $"{lesson.Title} removed.";
        return RedirectToPage();
    }
}
