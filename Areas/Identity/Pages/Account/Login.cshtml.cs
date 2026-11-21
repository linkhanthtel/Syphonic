using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Syphonic.Data;

namespace Syphonic.Areas.Identity.Pages.Account;

/// <summary>
/// Log-in page. Replaces the built-in Identity page, which links to password-reset and
/// email-confirmation features this app does not have (it sends no email).
/// </summary>
[AllowAnonymous]
public class LoginModel : PageModel
{
    private readonly SignInManager<ApplicationUser> _signInManager;

    public LoginModel(SignInManager<ApplicationUser> signInManager)
    {
        _signInManager = signInManager;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string ReturnUrl { get; private set; } = "/";

    /// <summary>True when the learner was sent here by a page that needs an account.</summary>
    public bool CameFromProtectedPage { get; private set; }

    public sealed class InputModel
    {
        [Required(ErrorMessage = "Enter your email address.")]
        [EmailAddress(ErrorMessage = "That does not look like an email address.")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Enter your password.")]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Keep me signed in on this device")]
        public bool RememberMe { get; set; }
    }

    public async Task<IActionResult> OnGetAsync(string? returnUrl = null)
    {
        ReturnUrl = SafeReturnUrl(returnUrl);
        if (_signInManager.IsSignedIn(User))
            return LocalRedirect(ReturnUrl);

        CameFromProtectedPage = ReturnUrl != Url.Content("~/");

        // Clear any half-finished external sign-in so the form starts clean.
        await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        ReturnUrl = SafeReturnUrl(returnUrl);
        if (!ModelState.IsValid)
            return Page();

        // Accounts use the email address as the user name.
        var result = await _signInManager.PasswordSignInAsync(
            Input.Email.Trim(), Input.Password, Input.RememberMe, lockoutOnFailure: true);

        if (result.Succeeded)
            return LocalRedirect(ReturnUrl);

        if (result.IsLockedOut)
        {
            ModelState.AddModelError(string.Empty, "Too many wrong passwords. For your safety this account is locked for a few minutes; please try again later.");
            return Page();
        }

        // The same message whether the email or the password was wrong, so nobody can test which emails have accounts.
        ModelState.AddModelError(string.Empty, "The email or password is not correct. Check both and try again.");
        return Page();
    }

    private string SafeReturnUrl(string? returnUrl) =>
        !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl) ? returnUrl : Url.Content("~/");
}
