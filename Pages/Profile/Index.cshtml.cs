using System.ComponentModel.DataAnnotations;
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

    [BindProperty]
    public ProfileInput Input { get; set; } = new();

    public string Email { get; private set; } = string.Empty;

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
        CreatedAt = user.CreatedAt;
        Input.DisplayName = user.DisplayName;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return Challenge();

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
            return NotFound();

        if (!ModelState.IsValid)
        {
            Email = user.Email ?? user.UserName ?? string.Empty;
            CreatedAt = user.CreatedAt;
            return Page();
        }

        user.DisplayName = string.IsNullOrWhiteSpace(Input.DisplayName)
            ? null
            : Input.DisplayName.Trim();

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);

            Email = user.Email ?? user.UserName ?? string.Empty;
            CreatedAt = user.CreatedAt;
            return Page();
        }

        TempData["StatusMessage"] = "Display name updated.";
        return RedirectToPage();
    }

    public sealed class ProfileInput
    {
        [Display(Name = "Display name")]
        [MaxLength(120)]
        public string? DisplayName { get; set; }
    }
}
