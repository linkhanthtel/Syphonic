using Microsoft.EntityFrameworkCore;
using Syphonic.Data;

namespace Syphonic.Services;

/// <summary>
/// Works out a learner's practice streak and badges from data the app already records
/// (lesson progress, quiz attempts, activity log), so no extra tables are needed.
///
/// Registered in Program.cs with AddScoped, which means one instance per web request.
/// Any page can receive it through its constructor, like ApplicationDbContext.
/// </summary>
public class AchievementService
{
    private readonly ApplicationDbContext _db;

    public AchievementService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<AchievementSummary> GetSummaryAsync(string userId, CancellationToken cancellationToken)
    {
        var publishedIds = (await _db.Lessons.AsNoTracking()
            .Where(l => l.Published)
            .Select(l => l.Id)
            .ToListAsync(cancellationToken)).ToHashSet();

        var progress = await _db.LessonProgress.AsNoTracking()
            .Where(p => p.UserId == userId)
            .Select(p => new { p.LessonId, Completed = p.CompletedAt != null })
            .ToListAsync(cancellationToken);

        var attempts = await _db.QuizAttempts.AsNoTracking()
            .Where(a => a.UserId == userId)
            .Select(a => new { a.LessonId, a.Score, a.TotalQuestions })
            .ToListAsync(cancellationToken);

        // SQLite can't filter or group DateTimeOffset in SQL, so fetch timestamps and work in memory.
        var activityTimes = await _db.UserActivities.AsNoTracking()
            .Where(a => a.UserId == userId)
            .Select(a => a.CreatedAt)
            .ToListAsync(cancellationToken);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var practiceDays = activityTimes.Select(t => DateOnly.FromDateTime(t.UtcDateTime));
        var streak = StreakCalculator.Calculate(practiceDays, today);

        var lessonsStarted = progress.Count;
        var lessonsCompleted = progress.Count(p => p.Completed);
        var publishedCompleted = progress.Count(p => p.Completed && publishedIds.Contains(p.LessonId));
        var quizzesTaken = attempts.Select(a => a.LessonId).Distinct().Count();
        var hasPerfectScore = attempts.Any(a => a.TotalQuestions > 0 && a.Score == a.TotalQuestions);
        var quizzesPassed = attempts
            .Where(a => a.TotalQuestions > 0 && a.Score * 100 >= QuizResult.PassPercent * a.TotalQuestions)
            .Select(a => a.LessonId)
            .Distinct()
            .Count();

        var badges = new List<Badge>
        {
            new("first-steps",   "👣", "First steps",   "Open your first lesson.",                         lessonsStarted,          1),
            new("bookworm",      "📚", "Bookworm",      "Complete 3 lessons.",                             lessonsCompleted,        3),
            new("explorer",      "🌍", "World explorer", "Complete every published lesson.",                publishedCompleted,      Math.Max(publishedIds.Count, 1)),
            new("quiz-taker",    "📝", "Quiz taker",    "Take your first quiz.",                           quizzesTaken,            1),
            new("perfect-score", "🎯", "Perfect score", "Get 100% on any quiz.",                           hasPerfectScore ? 1 : 0, 1),
            new("quiz-master",   "🏆", "Quiz master",   $"Pass ({QuizResult.PassPercent}%+) 3 different quizzes.", quizzesPassed, 3),
            new("on-a-roll",     "🔥", "On a roll",     "Practice 3 days in a row.",                       streak.Longest,          3),
            new("week-warrior",  "⚡", "Week warrior",  "Practice 7 days in a row.",                       streak.Longest,          7),
        };

        return new AchievementSummary(streak.Current, streak.Longest, streak.PracticedToday, badges);
    }
}

public sealed record Badge(string Key, string Icon, string Title, string Description, int Current, int Target)
{
    public bool Earned => Current >= Target;

    public int ProgressPercent => Target <= 0 ? 100 : Math.Min(100, Current * 100 / Target);

    /// <summary>Progress capped at the target, e.g. "2 / 3".</summary>
    public string ProgressText => $"{Math.Min(Current, Target)} / {Target}";
}

public sealed record AchievementSummary(int CurrentStreak, int LongestStreak, bool PracticedToday, IReadOnlyList<Badge> Badges)
{
    public int EarnedCount => Badges.Count(b => b.Earned);
}

public readonly record struct StreakInfo(int Current, int Longest, bool PracticedToday);

/// <summary>
/// Pure streak maths (no database, no clock) so it is easy to reason about and test.
/// A "practice day" is any UTC calendar day with at least one recorded activity.
/// </summary>
public static class StreakCalculator
{
    public static StreakInfo Calculate(IEnumerable<DateOnly> practiceDays, DateOnly today)
    {
        var days = practiceDays.Distinct().Order().ToList();
        if (days.Count == 0)
            return new StreakInfo(0, 0, false);

        // Longest run of consecutive days anywhere in the history.
        var longest = 1;
        var run = 1;
        for (var i = 1; i < days.Count; i++)
        {
            run = days[i] == days[i - 1].AddDays(1) ? run + 1 : 1;
            longest = Math.Max(longest, run);
        }

        // Current streak counts back from today. If the learner hasn't practiced yet today,
        // the streak is still alive as long as they practiced yesterday.
        var daySet = days.ToHashSet();
        var practicedToday = daySet.Contains(today);
        var cursor = practicedToday ? today : today.AddDays(-1);
        var current = 0;
        while (daySet.Contains(cursor))
        {
            current++;
            cursor = cursor.AddDays(-1);
        }

        return new StreakInfo(current, longest, practicedToday);
    }
}
