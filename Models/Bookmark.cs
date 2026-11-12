using Syphonic.Data;

namespace Syphonic.Models;

/// <summary>A lesson a learner saved to read later. One row per (user, lesson).</summary>
public class Bookmark
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    public ApplicationUser User { get; set; } = null!;

    public int LessonId { get; set; }

    public Lesson Lesson { get; set; } = null!;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
