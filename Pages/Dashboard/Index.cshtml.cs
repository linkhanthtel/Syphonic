using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Syphonic.Data;
using Syphonic.Services;

namespace Syphonic.Pages.Dashboard;

[Authorize]
public class IndexModel : PageModel
{
    /// <summary>How many days the activity bar chart covers, ending today (UTC).</summary>
    public const int ActivityWindowDays = 14;

    private readonly ApplicationDbContext _db;
    private readonly AchievementService _achievements;
    private readonly LeaderboardService _leaderboard;

    public IndexModel(ApplicationDbContext db, AchievementService achievements, LeaderboardService leaderboard)
    {
        _db = db;
        _achievements = achievements;
        _leaderboard = leaderboard;
    }

    public AchievementSummary? Achievements { get; private set; }

    public int SavedLessonCount { get; private set; }

    // ----- Lesson progress -----

    public int PublishedLessonCount { get; private set; }

    public int CompletedLessons { get; private set; }

    public int InProgressCount { get; private set; }

    public int NotStartedCount => Math.Max(0, PublishedLessonCount - CompletedLessons - InProgressCount);

    public int CompletionPercent { get; private set; }

    public DateTimeOffset? LastLessonCompletedAt { get; private set; }

    // ----- Activity -----

    public int DistinctPracticeDaysRecent { get; private set; }

    /// <summary>One entry per day for the last <see cref="ActivityWindowDays"/> days, oldest first.</summary>
    public IReadOnlyList<ActivityDayVm> ActivityByDay { get; private set; } = Array.Empty<ActivityDayVm>();

    public IReadOnlyList<ActivityVm> RecentActivities { get; private set; } = Array.Empty<ActivityVm>();

    // ----- Quizzes -----

    public int QuizzesTaken { get; private set; }

    /// <summary>Average of the learner's best score on each quiz they have taken, or null if none.</summary>
    public int? AverageBestQuizPercent { get; private set; }

    /// <summary>Every saved quiz attempt in the order it was taken (the scatter plot).</summary>
    public IReadOnlyList<QuizAttemptVm> QuizAttempts { get; private set; } = Array.Empty<QuizAttemptVm>();

    /// <summary>Best score per lesson quiz, in course order (the score bars).</summary>
    public IReadOnlyList<BestScoreVm> BestScores { get; private set; } = Array.Empty<BestScoreVm>();

    // ----- Leaderboard -----

    public IReadOnlyList<LeaderboardEntry> TopLearners { get; private set; } = Array.Empty<LeaderboardEntry>();

    public LeaderboardEntry? MyRank { get; private set; }

    public int RankedLearnerCount { get; private set; }

    public IReadOnlyList<SuggestedLessonVm> SuggestedLessons { get; private set; } = Array.Empty<SuggestedLessonVm>();

    /// <summary>Serialized into the page as JSON and drawn by wwwroot/js/dashboard.js.</summary>
    public object ChartData { get; private set; } = new { };

    public sealed record ActivityVm(string Kind, string Description, DateTimeOffset CreatedAt);

    public sealed record ActivityDayVm(DateOnly Day, int Lessons, int Quizzes)
    {
        public int Total => Lessons + Quizzes;
    }

    public sealed record QuizAttemptVm(int Number, string LessonTitle, int Score, int Total, int Percent, bool Passed, DateTimeOffset TakenAt);

    public sealed record BestScoreVm(string LessonTitle, string Slug, int Percent, int Attempts);

    public sealed record SuggestedLessonVm(string Title, string Slug, string? Summary, bool InProgress);

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return;

        Achievements = await _achievements.GetSummaryAsync(userId, cancellationToken);
        SavedLessonCount = await _db.Bookmarks.CountAsync(b => b.UserId == userId && b.Lesson.Published, cancellationToken);

        // Published lessons in course order; everything below is measured against this list.
        var lessons = await _db.Lessons.AsNoTracking()
            .Where(l => l.Published)
            .OrderBy(l => l.OrderIndex).ThenBy(l => l.Title)
            .Select(l => new { l.Id, l.Title, l.Slug, l.Summary })
            .ToListAsync(cancellationToken);
        var lessonById = lessons.ToDictionary(l => l.Id);
        PublishedLessonCount = lessons.Count;

        // SQLite cannot filter or sort DateTimeOffset in SQL, so date work happens in memory.
        var progress = (await _db.LessonProgress.AsNoTracking()
                .Where(p => p.UserId == userId)
                .Select(p => new { p.LessonId, p.CompletedAt })
                .ToListAsync(cancellationToken))
            .Where(p => lessonById.ContainsKey(p.LessonId))
            .ToList();

        var completedIds = progress.Where(p => p.CompletedAt != null).Select(p => p.LessonId).ToHashSet();
        var inProgressIds = progress.Where(p => p.CompletedAt == null).Select(p => p.LessonId).ToHashSet();

        CompletedLessons = completedIds.Count;
        InProgressCount = inProgressIds.Count;
        CompletionPercent = PublishedLessonCount == 0
            ? 0
            : (int)Math.Round(CompletedLessons * 100m / PublishedLessonCount, MidpointRounding.AwayFromZero);
        LastLessonCompletedAt = progress.Max(p => p.CompletedAt);

        // Lessons under way first, then unopened ones, each in course order.
        SuggestedLessons = lessons
            .Where(l => !completedIds.Contains(l.Id))
            .OrderByDescending(l => inProgressIds.Contains(l.Id))
            .Take(5)
            .Select(l => new SuggestedLessonVm(l.Title, l.Slug, l.Summary, inProgressIds.Contains(l.Id)))
            .ToList();

        await LoadActivityAsync(userId, cancellationToken);

        var attempts = (await _db.QuizAttempts.AsNoTracking()
                .Where(a => a.UserId == userId)
                .ToListAsync(cancellationToken))
            .Where(a => lessonById.ContainsKey(a.LessonId))
            .OrderBy(a => a.CreatedAt)
            .ToList();

        QuizAttempts = attempts
            .Select((a, index) => new QuizAttemptVm(
                index + 1,
                lessonById[a.LessonId].Title,
                a.Score,
                a.TotalQuestions,
                a.Percent,
                a.Percent >= QuizResult.PassPercent,
                a.CreatedAt))
            .ToList();

        var attemptsByLesson = attempts.ToLookup(a => a.LessonId);
        BestScores = lessons
            .Where(l => attemptsByLesson.Contains(l.Id))
            .Select(l => new BestScoreVm(l.Title, l.Slug, attemptsByLesson[l.Id].Max(a => a.Percent), attemptsByLesson[l.Id].Count()))
            .ToList();

        QuizzesTaken = BestScores.Count;
        AverageBestQuizPercent = BestScores.Count == 0
            ? null
            : (int)Math.Round(BestScores.Average(b => b.Percent), MidpointRounding.AwayFromZero);

        var leaderboard = await _leaderboard.GetAsync(cancellationToken);
        TopLearners = leaderboard.Take(3).ToList();
        MyRank = leaderboard.FirstOrDefault(e => e.UserId == userId);
        RankedLearnerCount = leaderboard.Count;

        ChartData = new
        {
            passPercent = QuizResult.PassPercent,
            activity = ActivityByDay.Select(d => new
            {
                label = d.Day.ToString("MMM d"),
                lessons = d.Lessons,
                quizzes = d.Quizzes
            }),
            attempts = QuizAttempts.Select(a => new
            {
                x = a.Number,
                y = a.Percent,
                lesson = a.LessonTitle,
                score = $"{a.Score}/{a.Total}",
                date = a.TakenAt.UtcDateTime.ToString("MMM d, yyyy"),
                passed = a.Passed
            })
        };
    }

    private async Task LoadActivityAsync(string userId, CancellationToken cancellationToken)
    {
        var activities = await _db.UserActivities.AsNoTracking()
            .Where(a => a.UserId == userId)
            .Select(a => new ActivityVm(a.Kind, a.Description, a.CreatedAt))
            .ToListAsync(cancellationToken);

        RecentActivities = activities
            .OrderByDescending(a => a.CreatedAt)
            .Take(8)
            .ToList();

        // Days are UTC calendar days, the same definition the streak uses.
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var byDay = activities.ToLookup(a => DateOnly.FromDateTime(a.CreatedAt.UtcDateTime));

        ActivityByDay = Enumerable.Range(0, ActivityWindowDays)
            .Select(offset => today.AddDays(offset - (ActivityWindowDays - 1)))
            .Select(day => new ActivityDayVm(
                day,
                byDay[day].Count(a => a.Kind.StartsWith("lesson_", StringComparison.Ordinal)),
                byDay[day].Count(a => !a.Kind.StartsWith("lesson_", StringComparison.Ordinal))))
            .ToList();

        DistinctPracticeDaysRecent = ActivityByDay.TakeLast(7).Count(d => d.Total > 0);
    }
}
