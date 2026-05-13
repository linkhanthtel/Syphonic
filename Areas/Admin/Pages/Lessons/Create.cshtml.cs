using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Syphonic.Data;
using Syphonic.Helpers;
using Syphonic.Models;

namespace Syphonic.Areas.Admin.Pages.Lessons;

public class CreateModel : PageModel
{
    private readonly ApplicationDbContext _db;

    public CreateModel(ApplicationDbContext db)
    {
        _db = db;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public IActionResult OnGet()
    {
        Input.OrderIndex = 100;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return Page();

        try
        {
            var slug = ResolveSlug(Input);
            Input.SlugOverride = slug;
        }
        catch (ArgumentException ex)
        {
            ModelState.AddModelError(nameof(Input.SlugOverride), ex.Message);
        }

        if (!ModelState.IsValid)
            return Page();

        await EnsureSlugAvailableAsync(Input.SlugOverride, null, cancellationToken);
        if (!ModelState.IsValid)
            return Page();

        var now = DateTimeOffset.UtcNow;
        var lesson = new Lesson
        {
            Title = Input.Title.Trim(),
            Slug = Input.SlugOverride,
            Summary = NormalizeOptional(Input.Summary),
            Content = Input.Content.Trim(),
            OrderIndex = Input.OrderIndex,
            Published = Input.Published,
            CreatedAt = now,
            UpdatedAt = now
        };

        _db.Lessons.Add(lesson);
        await _db.SaveChangesAsync(cancellationToken);

        TempData["Message"] = $"{lesson.Title} saved.";
        return RedirectToPage("/Lessons/Index");
    }

    private static string ResolveSlug(InputModel input)
    {
        if (string.IsNullOrWhiteSpace(input.SlugOverride))
            return SlugHelper.CreateSlug(input.Title);

        var candidate = input.SlugOverride.Trim().ToLowerInvariant();
        ValidateSlug(candidate);
        return candidate;
    }

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private async Task EnsureSlugAvailableAsync(string slug, int? excludingId, CancellationToken cancellationToken)
    {
        var query = _db.Lessons.AsQueryable();
        if (excludingId is not null)
            query = query.Where(l => l.Id != excludingId);

        if (await query.AnyAsync(l => l.Slug == slug, cancellationToken))
            ModelState.AddModelError(nameof(Input.SlugOverride), "Slug already in use. Choose another slug.");
    }

    private static void ValidateSlug(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
            throw new ArgumentException("Slug cannot be empty.");

        if (!Regex.IsMatch(slug, @"^[a-z0-9]+(?:-[a-z0-9]+)*$", RegexOptions.CultureInvariant))
            throw new ArgumentException("Slug may only contain lowercase letters, digits, and hyphens.");

        if (Encoding.UTF8.GetByteCount(slug) > 256)
            throw new ArgumentException("Slug is too long.");
    }

    public sealed class InputModel
    {
        [Required]
        [MaxLength(256)]
        public string Title { get; set; } = string.Empty;

        [Display(Name = "Slug (optional)")]
        [MaxLength(256)]
        public string? SlugOverride { get; set; }

        [MaxLength(2000)]
        [Display(Name = "Summary")]
        public string? Summary { get; set; }

        [Required]
        [Display(Name = "Lesson body")]
        public string Content { get; set; } = string.Empty;

        [Display(Name = "Order index")]
        public int OrderIndex { get; set; }

        public bool Published { get; set; }
    }
}
