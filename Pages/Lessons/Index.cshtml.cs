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

    public sealed record LessonListItem(int Id, string Title, string Slug, string? Summary, int OrderIndex);

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var query = _db.Lessons.AsNoTracking();
        if (!User.IsInRole("Admin"))
            query = query.Where(l => l.Published);

        Lessons = await query
            .OrderBy(l => l.OrderIndex).ThenBy(l => l.Title)
            .Select(l => new LessonListItem(l.Id, l.Title, l.Slug, l.Summary, l.OrderIndex))
            .ToListAsync(cancellationToken);
    }
}
