using System.Security.Claims;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Syphonic.Data;
using Syphonic.Services;

namespace Syphonic.Pages;

/// <summary>
/// Home page: one place that surfaces everything Syphonic offers. Guests see the catalog
/// and site-wide numbers; signed-in learners also see where to pick up and how they are doing.
/// </summary>
public class IndexModel : PageModel
{
    /// <summary>How many lessons the home page previews before linking to the full library.</summary>
    public const int FeaturedLessonLimit = 6;

    private readonly ApplicationDbContext _db;
    private readonly AchievementService _achievements;
    private readonly LeaderboardService _leaderboard;

    public IndexModel(ApplicationDbContext db, AchievementService achievements, LeaderboardService leaderboard)
    {
        _db = db;
        _achievements = achievements;
        _leaderboard = leaderboard;
    }

    public int PublishedLessonCount { get; private set; }

    public int QuizQuestionCount { get; private set; }

    public int LearnerCount { get; private set; }

    public IReadOnlyList<LessonCard> FeaturedLessons { get; private set; } = Array.Empty<LessonCard>();

    public IReadOnlyList<LeaderboardEntry> TopLearners { get; private set; } = Array.Empty<LeaderboardEntry>();

    // ----- Signed-in learners only -----

    public bool IsSignedIn { get; private set; }

    public string? Greeting { get; private set; }

    /// <summary>The lesson to resume (first unfinished one), or null when everything is done.</summary>
    public LessonCard? ContinueLesson { get; private set; }

    public int CompletedLessonCount { get; private set; }

    public int CompletionPercent => PublishedLessonCount == 0 ? 0 : CompletedLessonCount * 100 / PublishedLessonCount;

    public AchievementSummary? Achievements { get; private set; }

    public LeaderboardEntry? MyRank { get; private set; }

    public sealed record LessonCard(string Title, string Slug, string? Summary, int QuestionCount, LessonState State);

    public enum LessonState
    {
        NotStarted,
        InProgress,
        Completed
    }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var lessons = await _db.Lessons.AsNoTracking()
            .Where(l => l.Published)
            .OrderBy(l => l.OrderIndex).ThenBy(l => l.Title)
            .Select(l => new { l.Id, l.Title, l.Slug, l.Summary, QuestionCount = l.Questions.Count })
            .ToListAsync(cancellationToken);

        PublishedLessonCount = lessons.Count;
        QuizQuestionCount = lessons.Sum(l => l.QuestionCount);
        LearnerCount = await _db.Users.CountAsync(cancellationToken);

        var leaderboard = await _leaderboard.GetAsync(cancellationToken);
        TopLearners = leaderboard.Take(3).ToList();

        // Lesson id -> completed? (a missing key means the learner never opened it)
        Dictionary<int, bool> progress = [];
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is not null)
        {
            IsSignedIn = true;
            progress = await _db.LessonProgress.AsNoTracking()
                .Where(p => p.UserId == userId)
                .ToDictionaryAsync(p => p.LessonId, p => p.CompletedAt != null, cancellationToken);

            Greeting = await _db.Users.AsNoTracking()
                .Where(u => u.Id == userId)
                .Select(u => u.DisplayName)
                .FirstOrDefaultAsync(cancellationToken);

            Achievements = await _achievements.GetSummaryAsync(userId, cancellationToken);
            MyRank = leaderboard.FirstOrDefault(e => e.UserId == userId);
        }

        var cards = lessons.ConvertAll(l =>
        {
            var state = !progress.TryGetValue(l.Id, out var completed)
                ? LessonState.NotStarted
                : completed ? LessonState.Completed : LessonState.InProgress;
            return new LessonCard(l.Title, l.Slug, l.Summary, l.QuestionCount, state);
        });

        FeaturedLessons = cards.Take(FeaturedLessonLimit).ToList();

        if (IsSignedIn)
        {
            CompletedLessonCount = cards.Count(c => c.State == LessonState.Completed);
            // Prefer a lesson already under way, otherwise the next unopened one in course order.
            ContinueLesson = cards.FirstOrDefault(c => c.State == LessonState.InProgress)
                ?? cards.FirstOrDefault(c => c.State == LessonState.NotStarted);
        }
    }
}
