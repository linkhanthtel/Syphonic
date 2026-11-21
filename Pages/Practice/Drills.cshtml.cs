using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Syphonic.Data;
using Syphonic.Models;
using Syphonic.Services;

namespace Syphonic.Pages.Practice;

/// <summary>
/// Country drills: short exercises generated from the country data (capitals, continents, population).
/// Open to everyone; signed-in learners get each finished drill added to their own activity log.
/// </summary>
public class DrillsModel : PageModel
{
    public const int QuestionCount = 10;
    private const string AllRegions = "all";

    /// <summary>Drill key → name shown to learners.</summary>
    public static readonly IReadOnlyDictionary<string, string> Topics = new Dictionary<string, string>
    {
        ["capitals"] = "Capitals: pick the answer",
        ["capitals-typed"] = "Capitals: type the answer",
        ["continents"] = "Which continent?",
        ["population"] = "Which has more people?"
    };

    private readonly ApplicationDbContext _db;
    private readonly CountryData _data;

    public DrillsModel(ApplicationDbContext db, CountryData data)
    {
        _db = db;
        _data = data;
    }

    /// <summary>Chosen drill, or null while the learner is still choosing.</summary>
    [BindProperty(SupportsGet = true)]
    public string? Topic { get; set; }

    [BindProperty(SupportsGet = true)]
    public string Region { get; set; } = AllRegions;

    /// <summary>One entry per question, posted back by the form so the same questions can be graded.</summary>
    [BindProperty]
    public List<PostedQuestion> Q { get; set; } = new();

    public IReadOnlyList<string> Regions => _data.Regions;

    public IReadOnlyList<QuestionVm> Questions { get; private set; } = Array.Empty<QuestionVm>();

    public IReadOnlyList<ResultVm>? Results { get; private set; }

    public int Score => Results?.Count(r => r.IsCorrect) ?? 0;

    /// <summary>The signed-in learner's own latest drills, newest first.</summary>
    public IReadOnlyList<RecentDrill> Recent { get; private set; } = Array.Empty<RecentDrill>();

    public string TopicName => Topic is not null && Topics.TryGetValue(Topic, out var name) ? name : string.Empty;

    public string RegionName => Region == AllRegions ? "Whole world" : Region;

    public bool IsTyped => Topic == "capitals-typed";

    public sealed class PostedQuestion
    {
        public string? CountryId { get; set; }

        /// <summary>The second country in a population question.</summary>
        public string? OtherId { get; set; }

        /// <summary>Comma-separated option values in the order they were shown.</summary>
        public string? Options { get; set; }

        public string? Answer { get; set; }
    }

    public sealed record OptionVm(string Value, string Label);

    public sealed record QuestionVm(string Prompt, string CountryId, string? OtherId, IReadOnlyList<OptionVm> Options);

    public sealed record ResultVm(string Prompt, string YourAnswer, string CorrectAnswer, bool IsCorrect, string? Note);

    public sealed record RecentDrill(string Description, DateTimeOffset When);

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        NormaliseChoices();
        if (Topic is not null)
            Questions = BuildQuestions();

        await LoadRecentAsync(cancellationToken);
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        NormaliseChoices();
        if (Topic is null)
            return RedirectToPage();

        // Never trust the posted questions: cap the count and skip anything that is not a known country.
        var results = new List<ResultVm>();
        foreach (var posted in Q.Take(QuestionCount))
        {
            var result = Grade(posted);
            if (result is not null)
                results.Add(result);
        }

        if (results.Count == 0)
            return RedirectToPage(new { topic = Topic, region = Region == AllRegions ? null : Region });

        Results = results;

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is not null)
        {
            _db.UserActivities.Add(new UserActivity
            {
                UserId = userId,
                Kind = "drill_completed",
                Description = $"Scored {Score}/{results.Count} in the “{TopicName}” drill ({RegionName})."
            });
            await _db.SaveChangesAsync(cancellationToken);
        }

        await LoadRecentAsync(cancellationToken);
        return Page();
    }

    /// <summary>Unknown topic or region values (for example from a hand-edited link) fall back to safe defaults.</summary>
    private void NormaliseChoices()
    {
        if (Topic is not null && !Topics.ContainsKey(Topic))
            Topic = null;

        // Asking "which continent?" inside one continent would give the answer away.
        if (Topic == "continents" || !_data.Regions.Contains(Region))
            Region = AllRegions;
    }

    private IReadOnlyList<CountryData.Country> Pool() =>
        Region == AllRegions ? _data.Countries : _data.Countries.Where(c => c.Region == Region).ToList();

    private IReadOnlyList<QuestionVm> BuildQuestions()
    {
        var pool = Pool();
        var picked = pool.OrderBy(_ => Random.Shared.Next()).Take(QuestionCount).ToList();
        var questions = new List<QuestionVm>();

        foreach (var country in picked)
        {
            switch (Topic)
            {
                case "capitals":
                    // Wrong answers come from the same part of the world, so they are plausible.
                    var others = pool.Where(c => c.Id != country.Id && c.Capitals[0] != country.Capitals[0])
                        .OrderBy(_ => Random.Shared.Next()).Take(3);
                    var options = others.Append(country).OrderBy(_ => Random.Shared.Next())
                        .Select(c => new OptionVm(c.Id, c.Capitals[0])).ToList();
                    questions.Add(new QuestionVm($"What is the capital of {country.Name}?", country.Id, null, options));
                    break;

                case "capitals-typed":
                    questions.Add(new QuestionVm($"What is the capital of {country.Name}?", country.Id, null, Array.Empty<OptionVm>()));
                    break;

                case "continents":
                    questions.Add(new QuestionVm($"Which continent is {country.Name} in?", country.Id, null,
                        _data.Regions.Select(r => new OptionVm(r, r)).ToList()));
                    break;

                case "population":
                    // Skip near-ties: the figures are estimates, so only clearly different pairs are fair.
                    var rival = pool.Where(c => c.Id != country.Id && IsClearlyDifferent(c.Population, country.Population))
                        .OrderBy(_ => Random.Shared.Next()).FirstOrDefault();
                    if (rival is null)
                        break;
                    questions.Add(new QuestionVm("Which country has more people?", country.Id, rival.Id,
                        new[] { new OptionVm(country.Id, country.Name), new OptionVm(rival.Id, rival.Name) }));
                    break;
            }
        }

        return questions;
    }

    private static bool IsClearlyDifferent(long a, long b) =>
        Math.Max(a, b) >= Math.Min(a, b) * 1.3;

    /// <summary>Works out the right answer again from the country data, so a tampered form cannot mark itself correct.</summary>
    private ResultVm? Grade(PostedQuestion posted)
    {
        var country = _data.Find(posted.CountryId);
        if (country is null)
            return null;

        var answer = posted.Answer?.Trim() ?? string.Empty;
        const string noAnswer = "No answer";

        switch (Topic)
        {
            case "capitals":
            {
                var chosen = _data.Find(answer);
                return new ResultVm($"What is the capital of {country.Name}?",
                    chosen is null ? noAnswer : chosen.Capitals[0], string.Join(" / ", country.Capitals), chosen?.Id == country.Id, null);
            }

            case "capitals-typed":
            {
                var match = CountryData.MatchTyped(answer, country.Capitals);
                return new ResultVm($"What is the capital of {country.Name}?",
                    answer.Length == 0 ? noAnswer : answer, string.Join(" / ", country.Capitals), match != TypedMatch.Wrong,
                    match == TypedMatch.Close ? "Counted as correct. Check the spelling." : null);
            }

            case "continents":
                return new ResultVm($"Which continent is {country.Name} in?",
                    answer.Length == 0 ? noAnswer : answer, country.Region, answer == country.Region, null);

            case "population":
            {
                var rival = _data.Find(posted.OtherId);
                if (rival is null || rival.Id == country.Id)
                    return null;

                var larger = country.Population >= rival.Population ? country : rival;
                var chosen = answer == country.Id ? country : answer == rival.Id ? rival : null;
                return new ResultVm($"Which has more people: {country.Name} or {rival.Name}?",
                    chosen?.Name ?? noAnswer, larger.Name, chosen?.Id == larger.Id,
                    $"{country.Name}: about {Round(country.Population)}. {rival.Name}: about {Round(rival.Population)}.");
            }

            default:
                return null;
        }
    }

    /// <summary>"126 million", "5.4 million" or "340,000": precise enough for estimates.</summary>
    private static string Round(long people) =>
        people switch
        {
            >= 1_000_000_000 => $"{people / 1_000_000_000.0:0.0#} billion",
            >= 10_000_000 => $"{people / 1_000_000.0:0} million",
            >= 1_000_000 => $"{people / 1_000_000.0:0.0} million",
            _ => $"{Math.Round(people / 1000.0) * 1000:N0}"
        };

    private async Task LoadRecentAsync(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return;

        // Only this learner's rows. Ordered by id because SQLite cannot sort DateTimeOffset.
        var rows = await _db.UserActivities.AsNoTracking()
            .Where(a => a.UserId == userId && a.Kind == "drill_completed")
            .OrderByDescending(a => a.Id)
            .Take(5)
            .Select(a => new { a.Description, a.CreatedAt })
            .ToListAsync(cancellationToken);

        Recent = rows.Select(r => new RecentDrill(r.Description, r.CreatedAt)).ToList();
    }
}
