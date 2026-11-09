using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Syphonic.Data;
using Syphonic.Models;

namespace Syphonic.Areas.Admin.Pages.Lessons;

/// <summary>
/// Create or edit one quiz question. A single page handles both cases:
///   /Admin/Lessons/QuestionForm/5      → new question for lesson 5
///   /Admin/Lessons/QuestionForm/5/12   → edit question 12 of lesson 5
/// </summary>
public class QuestionFormModel : PageModel
{
    private readonly ApplicationDbContext _db;

    public QuestionFormModel(ApplicationDbContext db)
    {
        _db = db;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public Lesson Lesson { get; private set; } = null!;

    public bool IsNew => Input.Id == 0;

    public async Task<IActionResult> OnGetAsync(int lessonId, int? id, CancellationToken cancellationToken)
    {
        if (!await LoadLessonAsync(lessonId, cancellationToken))
            return NotFound();

        if (id is null)
        {
            var maxOrder = await _db.QuizQuestions
                .Where(q => q.LessonId == lessonId)
                .MaxAsync(q => (int?)q.OrderIndex, cancellationToken) ?? 0;
            Input = new InputModel { OrderIndex = maxOrder + 10 };
            return Page();
        }

        var question = await _db.QuizQuestions.AsNoTracking()
            .FirstOrDefaultAsync(q => q.Id == id && q.LessonId == lessonId, cancellationToken);
        if (question is null)
            return NotFound();

        Input = new InputModel
        {
            Id = question.Id,
            Prompt = question.Prompt,
            OptionA = question.OptionA,
            OptionB = question.OptionB,
            OptionC = question.OptionC,
            OptionD = question.OptionD,
            CorrectOption = question.CorrectOption,
            Explanation = question.Explanation,
            OrderIndex = question.OrderIndex
        };
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int lessonId, CancellationToken cancellationToken)
    {
        if (!await LoadLessonAsync(lessonId, cancellationToken))
            return NotFound();

        // Rule that data annotations can't express: the correct answer must have text.
        var options = new[] { Input.OptionA, Input.OptionB, Input.OptionC, Input.OptionD };
        var inRange = Input.CorrectOption >= 0 && Input.CorrectOption < options.Length;
        if (inRange && string.IsNullOrWhiteSpace(options[Input.CorrectOption]))
            ModelState.AddModelError(nameof(Input.CorrectOption),
                $"Option {QuizQuestion.LetterFor(Input.CorrectOption)} is empty, so it can't be the correct answer.");

        if (!ModelState.IsValid)
            return Page();

        QuizQuestion question;
        if (IsNew)
        {
            question = new QuizQuestion { LessonId = lessonId };
            _db.QuizQuestions.Add(question);
        }
        else
        {
            var existing = await _db.QuizQuestions
                .FirstOrDefaultAsync(q => q.Id == Input.Id && q.LessonId == lessonId, cancellationToken);
            if (existing is null)
                return NotFound();
            question = existing;
        }

        question.Prompt = Input.Prompt.Trim();
        question.OptionA = Input.OptionA.Trim();
        question.OptionB = Input.OptionB.Trim();
        question.OptionC = NormalizeOptional(Input.OptionC);
        question.OptionD = NormalizeOptional(Input.OptionD);
        question.CorrectOption = Input.CorrectOption;
        question.Explanation = NormalizeOptional(Input.Explanation);
        question.OrderIndex = Input.OrderIndex;

        await _db.SaveChangesAsync(cancellationToken);

        TempData["Message"] = IsNew ? "Question added." : "Question updated.";
        return RedirectToPage("Questions", new { lessonId });
    }

    private async Task<bool> LoadLessonAsync(int lessonId, CancellationToken cancellationToken)
    {
        var lesson = await _db.Lessons.AsNoTracking().FirstOrDefaultAsync(l => l.Id == lessonId, cancellationToken);
        if (lesson is null)
            return false;

        Lesson = lesson;
        return true;
    }

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    public sealed class InputModel
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(1000)]
        [Display(Name = "Question")]
        public string Prompt { get; set; } = string.Empty;

        [Required]
        [MaxLength(300)]
        [Display(Name = "Option A")]
        public string OptionA { get; set; } = string.Empty;

        [Required]
        [MaxLength(300)]
        [Display(Name = "Option B")]
        public string OptionB { get; set; } = string.Empty;

        [MaxLength(300)]
        [Display(Name = "Option C (optional)")]
        public string? OptionC { get; set; }

        [MaxLength(300)]
        [Display(Name = "Option D (optional)")]
        public string? OptionD { get; set; }

        [Range(0, QuizQuestion.MaxOptions - 1)]
        [Display(Name = "Correct answer")]
        public int CorrectOption { get; set; }

        [MaxLength(2000)]
        [Display(Name = "Explanation (shown after grading)")]
        public string? Explanation { get; set; }

        [Display(Name = "Order index")]
        public int OrderIndex { get; set; }
    }
}
