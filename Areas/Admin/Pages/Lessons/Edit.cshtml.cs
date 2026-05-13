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

public class EditModel : PageModel
{
    private readonly ApplicationDbContext _db;

    public EditModel(ApplicationDbContext db)
    {
        _db = db;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id, CancellationToken cancellationToken)
    {
        var lesson = await _db.Lessons.AsNoTracking().FirstOrDefaultAsync(l => l.Id == id, cancellationToken);
        if (lesson is null)
            return NotFound();

        Input = new InputModel
        {
            Id = lesson.Id,
            Title = lesson.Title,
            Slug = lesson.Slug,
            Summary = lesson.Summary,
            Content = lesson.Content,
            OrderIndex = lesson.OrderIndex,
            Published = lesson.Published
        };

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return Page();

        try
        {
            NormalizeSlug(Input);
        }
        catch (ArgumentException ex)
        {
            ModelState.AddModelError(nameof(Input.Slug), ex.Message);
            return Page();
        }

        await EnsureSlugAvailableAsync(Input.Id, Input.Slug, cancellationToken);
        if (!ModelState.IsValid)
            return Page();

        var lesson = await _db.Lessons.FirstOrDefaultAsync(l => l.Id == Input.Id, cancellationToken);
        if (lesson is null)
            return NotFound();

        lesson.Title = Input.Title.Trim();
        lesson.Slug = Input.Slug;
        lesson.Summary = NormalizeOptional(Input.Summary);
        lesson.Content = Input.Content.Trim();
        lesson.OrderIndex = Input.OrderIndex;
        lesson.Published = Input.Published;
        lesson.UpdatedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        TempData["Message"] = $"{lesson.Title} updated.";
        return RedirectToPage("/Lessons/Index");
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id, CancellationToken cancellationToken)
    {
        var lesson = await _db.Lessons.FirstOrDefaultAsync(l => l.Id == id, cancellationToken);
        if (lesson is null)
            return NotFound();

        _db.Lessons.Remove(lesson);
        await _db.SaveChangesAsync(cancellationToken);

        TempData["Message"] = $"{lesson.Title} removed.";
        return RedirectToPage("/Lessons/Index");
    }

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private async Task EnsureSlugAvailableAsync(int lessonId, string slug, CancellationToken cancellationToken)
    {
        if (await _db.Lessons.AnyAsync(l => l.Slug == slug && l.Id != lessonId, cancellationToken))
            ModelState.AddModelError(nameof(Input.Slug), "Slug already in use. Pick another slug.");
    }

    private static void NormalizeSlug(InputModel input)
    {
        if (string.IsNullOrWhiteSpace(input.Slug))
            input.Slug = SlugHelper.CreateSlug(input.Title);
        else
            input.Slug = input.Slug.Trim().ToLowerInvariant();

        ValidateSlug(input.Slug);
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
        public int Id { get; set; }

        [Required]
        [MaxLength(256)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [MaxLength(256)]
        public string Slug { get; set; } = string.Empty;

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
