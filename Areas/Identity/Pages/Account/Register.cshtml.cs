using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Syphonic.Data;

namespace Syphonic.Areas.Identity.Pages.Account;

/// <summary>
/// Sign-up page. Replaces the built-in Identity page so the form asks only for what Syphonic
/// needs (name, email, password) and explains problems in plain words.
/// </summary>
[AllowAnonymous]
public class RegisterModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ILogger<RegisterModel> _logger;

    public RegisterModel(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, ILogger<RegisterModel> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _logger = logger;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    /// <summary>Where to send the learner afterwards, e.g. the page that asked them to sign in.</summary>
    public string ReturnUrl { get; private set; } = "/";

    /// <summary>Kept in step with the password options in Program.cs.</summary>
    public const int MinPasswordLength = 8;

    public sealed class InputModel
    {
        [Display(Name = "Display name")]
        [StringLength(40, ErrorMessage = "Use 40 characters or fewer.")]
        public string? DisplayName { get; set; }

        [Required(ErrorMessage = "Enter your email address.")]
        [EmailAddress(ErrorMessage = "That does not look like an email address.")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Choose a password.")]
        [StringLength(100, MinimumLength = MinPasswordLength, ErrorMessage = "Use at least {2} characters.")]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Type the password again.")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare(nameof(Password), ErrorMessage = "The two passwords do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public IActionResult OnGet(string? returnUrl = null)
    {
        ReturnUrl = SafeReturnUrl(returnUrl);
        // Someone already signed in has no use for this form.
        if (_signInManager.IsSignedIn(User))
            return LocalRedirect(ReturnUrl);

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        ReturnUrl = SafeReturnUrl(returnUrl);
        if (!ModelState.IsValid)
            return Page();

        var email = Input.Email.Trim();
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            DisplayName = string.IsNullOrWhiteSpace(Input.DisplayName) ? null : Input.DisplayName.Trim()
        };

        var result = await _userManager.CreateAsync(user, Input.Password);
        if (result.Succeeded)
        {
            _logger.LogInformation("New learner account created.");
            await _signInManager.SignInAsync(user, isPersistent: false);
            return LocalRedirect(ReturnUrl);
        }

        foreach (var error in result.Errors)
        {
            // Identity reports a taken address twice (user name and email); show one clear message instead.
            if (error.Code is "DuplicateUserName" or "DuplicateEmail")
            {
                if (ModelState[$"{nameof(Input)}.{nameof(Input.Email)}"]?.Errors.Count is null or 0)
                    ModelState.AddModelError($"{nameof(Input)}.{nameof(Input.Email)}", "An account with this email already exists. Log in instead.");
            }
            else if (error.Code.StartsWith("Password", StringComparison.Ordinal))
            {
                ModelState.AddModelError($"{nameof(Input)}.{nameof(Input.Password)}", error.Description);
            }
            else
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }

        return Page();
    }

    /// <summary>Only ever redirect inside this site, never to an address supplied by a link from elsewhere.</summary>
    private string SafeReturnUrl(string? returnUrl) =>
        !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl) ? returnUrl : Url.Content("~/");
}
