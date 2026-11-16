using Syphonic.Data;

namespace Syphonic.Models;

/// <summary>
/// How well one learner knows one quiz question. One row per (user, question).
/// A question moves up a box each time it is answered correctly when due, and drops back
/// to box 1 when answered wrongly. Higher boxes wait longer before the question comes back.
/// </summary>
public class QuestionReview
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    public ApplicationUser User { get; set; } = null!;

    public int QuizQuestionId { get; set; }

    public QuizQuestion Question { get; set; } = null!;

    /// <summary>1 = just missed or brand new, up to <c>ReviewService.MaxBox</c> = well known.</summary>
    public int Box { get; set; } = 1;

    /// <summary>
    /// When the question should be asked again. Stored as a UTC DateTime (not DateTimeOffset)
    /// so SQLite can compare and sort it inside the query.
    /// </summary>
    public DateTime DueAtUtc { get; set; } = DateTime.UtcNow;

    public int TimesSeen { get; set; }

    public int TimesCorrect { get; set; }

    public DateTime LastAnsweredAtUtc { get; set; } = DateTime.UtcNow;
}
