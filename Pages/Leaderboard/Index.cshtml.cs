using System.Security.Claims;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Syphonic.Services;

namespace Syphonic.Pages.Leaderboard;

public class IndexModel : PageModel
{
    /// <summary>How many rows the table shows. The signed-in learner's own row is always included.</summary>
    public const int TableLimit = 25;

    private readonly LeaderboardService _leaderboard;

    public IndexModel(LeaderboardService leaderboard)
    {
        _leaderboard = leaderboard;
    }

    public IReadOnlyList<LeaderboardEntry> TopThree { get; private set; } = Array.Empty<LeaderboardEntry>();

    public IReadOnlyList<LeaderboardEntry> Rows { get; private set; } = Array.Empty<LeaderboardEntry>();

    /// <summary>Set when the signed-in learner ranks below the visible table, so their row can be appended.</summary>
    public LeaderboardEntry? MyRowBelowTable { get; private set; }

    public string? MyUserId { get; private set; }

    public int TotalRanked { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var all = await _leaderboard.GetAsync(cancellationToken);
        MyUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        TotalRanked = all.Count;
        TopThree = all.Take(3).ToList();
        Rows = all.Take(TableLimit).ToList();
        MyRowBelowTable = all.Skip(TableLimit).FirstOrDefault(e => e.UserId == MyUserId);
    }
}
