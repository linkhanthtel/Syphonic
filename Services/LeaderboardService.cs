using Microsoft.EntityFrameworkCore;
using Syphonic.Data;

namespace Syphonic.Services;

/// <summary>
/// Ranks learners by points earned from data the app already records, so no extra table is needed.
/// Points = 100 per completed published lesson + the learner's best quiz percent on each lesson.
/// </summary>
public class LeaderboardService
{
    public const int PointsPerLesson = 100;

    private readonly ApplicationDbContext _db;

    public LeaderboardService(ApplicationDbContext db)
    {
        _db = db;
    }

    /// <summary>Every learner with at least one point, best first.</summary>
    public async Task<IReadOnlyList<LeaderboardEntry>> GetAsync(CancellationToken cancellationToken)
    {
        var completions = await _db.LessonProgress.AsNoTracking()
            .Where(p => p.CompletedAt != null && p.Lesson.Published)
            .Select(p => p.UserId)
            .ToListAsync(cancellationToken);

        var attempts = await _db.QuizAttempts.AsNoTracking()
            .Where(a => a.Lesson.Published && a.TotalQuestions > 0)
            .Select(a => new { a.UserId, a.LessonId, a.Score, a.TotalQuestions })
            .ToListAsync(cancellationToken);

        var completedByUser = completions
            .GroupBy(id => id)
            .ToDictionary(g => g.Key, g => g.Count());

        // Best percent per (user, lesson): retaking a quiz can only raise a score, never farm points.
        var bestByUser = attempts
            .GroupBy(a => a.UserId)
            .ToDictionary(
                g => g.Key,
                g => g.GroupBy(a => a.LessonId)
                    .Select(l => l.Max(a => (int)Math.Round(a.Score * 100m / a.TotalQuestions, MidpointRounding.AwayFromZero)))
                    .ToList());

        var userIds = completedByUser.Keys.Union(bestByUser.Keys).ToList();
        if (userIds.Count == 0)
            return Array.Empty<LeaderboardEntry>();

        var names = await _db.Users.AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .Select(u => new { u.Id, u.DisplayName })
            .ToDictionaryAsync(u => u.Id, u => u.DisplayName, cancellationToken);

        var scored = userIds
            .Select(id =>
            {
                completedByUser.TryGetValue(id, out var lessons);
                var best = bestByUser.GetValueOrDefault(id) ?? [];
                return new
                {
                    UserId = id,
                    Name = PublicName(id, names.GetValueOrDefault(id)),
                    Points = lessons * PointsPerLesson + best.Sum(),
                    Lessons = lessons,
                    Passed = best.Count(p => p >= QuizResult.PassPercent),
                    Average = best.Count == 0 ? (int?)null : (int)Math.Round(best.Average(), MidpointRounding.AwayFromZero)
                };
            })
            .Where(e => e.Points > 0)
            .OrderByDescending(e => e.Points)
            .ThenByDescending(e => e.Lessons)
            .ThenBy(e => e.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return scored
            .Select((e, index) => new LeaderboardEntry(index + 1, e.UserId, e.Name, e.Points, e.Lessons, e.Passed, e.Average))
            .ToList();
    }

    /// <summary>
    /// The name shown to other learners. Email addresses are never shown publicly: learners
    /// without a display name appear under an anonymous label derived from their id.
    /// </summary>
    private static string PublicName(string userId, string? displayName)
    {
        if (!string.IsNullOrWhiteSpace(displayName))
            return displayName.Trim();

        return $"Explorer {userId[..Math.Min(4, userId.Length)].ToUpperInvariant()}";
    }
}

public sealed record LeaderboardEntry(
    int Rank,
    string UserId,
    string Name,
    int Points,
    int LessonsCompleted,
    int QuizzesPassed,
    int? AverageBestPercent);
