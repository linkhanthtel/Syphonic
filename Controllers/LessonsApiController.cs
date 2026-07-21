using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Syphonic.Data;

namespace Syphonic.Controllers;

[ApiController]
[Route("api/lessons")]
public class LessonsApiController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public LessonsApiController(ApplicationDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Public catalog for integrations, SPAs, or mobile clients.
    /// </summary>
    [HttpGet("published")]
    public async Task<ActionResult<IReadOnlyList<LessonPublicDto>>> Published(CancellationToken cancellationToken)
    {
        var lessons = await _db.Lessons.AsNoTracking()
            .Where(l => l.Published)
            .OrderBy(l => l.OrderIndex).ThenBy(l => l.Title)
            .Select(l => new LessonPublicDto(l.Id, l.Title, l.Slug, l.Summary, l.OrderIndex))
            .ToListAsync(cancellationToken);

        return Ok(lessons);
    }

    public sealed record LessonPublicDto(int Id, string Title, string Slug, string? Summary, int OrderIndex);
}
