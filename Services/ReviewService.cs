using Microsoft.EntityFrameworkCore;
using Syphonic.Data;
using Syphonic.Models;

namespace Syphonic.Services;

/// <summary>
/// Spaced review: every question a learner answers is scheduled to come back later.
/// Missed questions come back straight away; questions answered correctly wait longer each time.
/// </summary>
public class ReviewService
{
    public const int MaxBox = 5;

    /// <summary>Most questions asked in one review session.</summary>
    public const int SessionSize = 10;

    /// <summary>Days to wait before asking again, by box number (index 0 is unused).</summary>
    private static readonly int[] DaysUntilDue = [0, 0, 1, 3, 7, 21];

    private readonly ApplicationDbContext _db;

    public ReviewService(ApplicationDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Updates the learner's review schedule from graded answers. Unanswered questions are skipped.
    /// Changes are added to the DbContext but not saved: the caller calls SaveChangesAsync,
    /// so the schedule and the caller's own rows are written together.
    /// </summary>
    /// <returns>Question id → what happened to its schedule.</returns>
    public async Task<IReadOnlyDictionary<int, ReviewOutcome>> RecordAsync(
        string userId, IReadOnlyList<QuizResultItem> items, CancellationToken cancellationToken = default)
    {
        var answered = items.Where(i => i.ChosenOption is not null).ToList();
        var questionIds = answered.Select(i => i.Question.Id).ToList();

        var existing = await _db.QuestionReviews
            .Where(r => r.UserId == userId && questionIds.Contains(r.QuizQuestionId))
            .ToDictionaryAsync(r => r.QuizQuestionId, cancellationToken);

        var now = DateTime.UtcNow;
        var outcomes = new Dictionary<int, ReviewOutcome>();

        foreach (var item in answered)
        {
            var isNew = !existing.TryGetValue(item.Question.Id, out var review);
            if (review is null)
            {
                review = new QuestionReview { UserId = userId, QuizQuestionId = item.Question.Id, Box = 1, DueAtUtc = now };
                _db.QuestionReviews.Add(review);
                existing[item.Question.Id] = review;
            }

            var wasDue = isNew || review.DueAtUtc <= now;

            review.TimesSeen++;
            review.LastAnsweredAtUtc = now;

            if (!item.IsCorrect)
            {
                review.Box = 1;
                review.DueAtUtc = now;
            }
            else
            {
                review.TimesCorrect++;
                // Retaking a quiz several times in one sitting must not fake long-term memory:
                // a question only moves up when it was actually due.
                if (wasDue)
                {
                    review.Box = Math.Min(review.Box + 1, MaxBox);
                    review.DueAtUtc = now.AddDays(DaysUntilDue[review.Box]);
                }
            }

            outcomes[item.Question.Id] = new ReviewOutcome(review.Box, review.DueAtUtc, DaysFromNow(review.DueAtUtc, now));
        }

        return outcomes;
    }

    /// <summary>Ids of the questions due now, longest-waiting first. Unpublished lessons are left out.</summary>
    public async Task<List<int>> GetDueQuestionIdsAsync(string userId, int max, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        return await _db.QuestionReviews.AsNoTracking()
            .Where(r => r.UserId == userId && r.DueAtUtc <= now && r.Question.Lesson.Published)
            .OrderBy(r => r.DueAtUtc).ThenBy(r => r.Id)
            .Take(max)
            .Select(r => r.QuizQuestionId)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountDueAsync(string userId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        return await _db.QuestionReviews.AsNoTracking()
            .CountAsync(r => r.UserId == userId && r.DueAtUtc <= now && r.Question.Lesson.Published, cancellationToken);
    }

    public async Task<ReviewSummary> GetSummaryAsync(string userId, CancellationToken cancellationToken = default)
    {
        var rows = await _db.QuestionReviews.AsNoTracking()
            .Where(r => r.UserId == userId && r.Question.Lesson.Published)
            .Select(r => new { r.Box, r.DueAtUtc })
            .ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;
        var upcoming = rows.Where(r => r.DueAtUtc > now).Select(r => (DateTime?)r.DueAtUtc).Min();

        return new ReviewSummary(
            Total: rows.Count,
            Due: rows.Count(r => r.DueAtUtc <= now),
            Learning: rows.Count(r => r.Box <= 2),
            Familiar: rows.Count(r => r.Box is 3 or 4),
            Mastered: rows.Count(r => r.Box >= MaxBox),
            NextDueInDays: upcoming is null ? null : DaysFromNow(upcoming.Value, now));
    }

    /// <summary>Whole days until a due time, rounded up, so "in 20 hours" reads as 1 day rather than 0.</summary>
    private static int DaysFromNow(DateTime dueAtUtc, DateTime now) =>
        dueAtUtc <= now ? 0 : (int)Math.Ceiling((dueAtUtc - now).TotalDays);

    /// <summary>"today", "tomorrow" or "in N days".</summary>
    public static string DescribeDays(int days) =>
        days switch
        {
            <= 0 => "today",
            1 => "tomorrow",
            _ => $"in {days} days"
        };
}

/// <param name="Box">The question's box after this answer.</param>
/// <param name="DueInDays">0 means it is due again straight away.</param>
public sealed record ReviewOutcome(int Box, DateTime DueAtUtc, int DueInDays);

/// <param name="Learning">Boxes 1 and 2.</param>
/// <param name="Familiar">Boxes 3 and 4.</param>
/// <param name="Mastered">The top box.</param>
/// <param name="NextDueInDays">Days until the next question that is not yet due, or null when none is waiting.</param>
public sealed record ReviewSummary(int Total, int Due, int Learning, int Familiar, int Mastered, int? NextDueInDays);
