namespace Syphonic.Models;

public class Lesson
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public string? Summary { get; set; }

    public string Content { get; set; } = string.Empty;

    public int OrderIndex { get; set; }

    public bool Published { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<LessonProgress> Progress { get; set; } = new List<LessonProgress>();
}
