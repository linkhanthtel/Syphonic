using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Syphonic.Services;

namespace Syphonic.Pages.Achievements;

/// <summary>Streaks and badges. URL: /Achievements</summary>
[Authorize]
public class IndexModel : PageModel
{
    private readonly AchievementService _achievements;

    // AchievementService arrives through dependency injection (registered in Program.cs).
    public IndexModel(AchievementService achievements)
    {
        _achievements = achievements;
    }

    public AchievementSummary Summary { get; private set; } = null!;

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        Summary = await _achievements.GetSummaryAsync(userId, cancellationToken);
    }
}
