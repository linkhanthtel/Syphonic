using Syphonic.Data;

namespace Syphonic.Models;

/// <summary>
/// A graded quiz submission by a signed-in learner. Anonymous visitors can take
/// quizzes too, but their results are not saved.
/// </summary>
public class QuizAttempt
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    public ApplicationUser User { get; set; } = null!;

    public int LessonId { get; set; }

    public Lesson Lesson { get; set; } = null!;

    public int Score { get; set; }

    public int TotalQuestions { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public int Percent => TotalQuestions == 0 ? 0 : (int)Math.Round(Score * 100m / TotalQuestions, MidpointRounding.AwayFromZero);
}
