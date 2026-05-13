using Syphonic.Data;

namespace Syphonic.Models;

public class LessonProgress
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    public ApplicationUser User { get; set; } = null!;

    public int LessonId { get; set; }

    public Lesson Lesson { get; set; } = null!;

    public DateTimeOffset? StartedAt { get; set; }

    public DateTimeOffset? CompletedAt { get; set; }
}
