using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Syphonic.Data;

namespace Syphonic.Areas.Admin.Pages.Users;

public class IndexModel : PageModel
{
    private const string AdministratorRoleName = "Admin";

    private readonly UserManager<ApplicationUser> _userManager;

    public IndexModel(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public IReadOnlyList<UserSummary> Rows { get; private set; } = Array.Empty<UserSummary>();

    public sealed record UserSummary(string Id, string Email, string? DisplayName, bool IsAdmin, bool CanToggle);

    public async Task OnGetAsync(CancellationToken cancellationToken)
        => Rows = await BuildRowsAsync(cancellationToken);

    public async Task<IActionResult> OnPostToggleAdminAsync(string userId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            Rows = await BuildRowsAsync(cancellationToken);
            return Page();
        }

        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (currentUserId == userId)
        {
            ModelState.AddModelError(string.Empty, "You cannot alter your own admin role from this panel.");
            Rows = await BuildRowsAsync(cancellationToken);
            return Page();
        }

        var subject = await _userManager.FindByIdAsync(userId);
        if (subject is null)
            return NotFound();

        if (await _userManager.IsInRoleAsync(subject, AdministratorRoleName))
        {
            var removed = await _userManager.RemoveFromRoleAsync(subject, AdministratorRoleName);
            if (!removed.Succeeded)
            {
                foreach (var error in removed.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);
            }
        }
        else
        {
            var added = await _userManager.AddToRoleAsync(subject, AdministratorRoleName);
            if (!added.Succeeded)
            {
                foreach (var error in added.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);
            }
        }

        if (!ModelState.IsValid)
        {
            Rows = await BuildRowsAsync(cancellationToken);
            return Page();
        }

        return RedirectToPage();
    }

    private async Task<IReadOnlyList<UserSummary>> BuildRowsAsync(CancellationToken cancellationToken)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var admins = await _userManager.GetUsersInRoleAsync(AdministratorRoleName);
        var adminIds = admins.Select(u => u.Id).ToHashSet(StringComparer.Ordinal);

        var learners = await _userManager.Users
            .AsNoTracking()
            .OrderBy(u => u.Email ?? u.UserName ?? "")
            .ToListAsync(cancellationToken);

        return learners.ConvertAll(user =>
        {
            var isAdmin = adminIds.Contains(user.Id);
            var email = user.Email ?? user.UserName ?? string.Empty;
            var canToggle = currentUserId is not null && currentUserId != user.Id;

            return new UserSummary(user.Id, email, user.DisplayName, isAdmin, canToggle);
        });
    }
}
