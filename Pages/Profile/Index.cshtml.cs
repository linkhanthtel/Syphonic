using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Syphonic.Data;

namespace Syphonic.Pages.Profile;

[Authorize]
public class IndexModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;

    public IndexModel(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public string Email { get; private set; } = string.Empty;

    public string? DisplayName { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return Challenge();

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
            return NotFound();

        Email = user.Email ?? user.UserName ?? string.Empty;
        DisplayName = user.DisplayName;
        CreatedAt = user.CreatedAt;
        return Page();
    }
}
