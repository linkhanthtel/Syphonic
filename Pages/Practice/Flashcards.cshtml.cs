using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Syphonic.Data;
using Syphonic.Models;

namespace Syphonic.Pages.Practice;

/// <summary>
/// Flashcards built from quiz questions: the question on the front, the right answer and its
/// explanation on the back. Nothing is graded or saved, so answers are sent to the browser on purpose.
/// </summary>
public class FlashcardsModel : PageModel
{
    /// <summary>Largest deck when studying every lesson at once.</summary>
    public const int MaxCards = 40;

    private readonly ApplicationDbContext _db;

    public FlashcardsModel(ApplicationDbContext db)
    {
        _db = db;
    }

    /// <summary>Slug of the lesson to study, or empty for a deck mixed from every lesson.</summary>
    [BindProperty(SupportsGet = true)]
    public string? Lesson { get; set; }

    public IReadOnlyList<DeckChoice> Decks { get; private set; } = Array.Empty<DeckChoice>();

    public IReadOnlyList<Card> Cards { get; private set; } = Array.Empty<Card>();

    /// <summary>Title of the chosen lesson, or null for the mixed deck.</summary>
    public string? LessonTitle { get; private set; }

    public sealed record DeckChoice(string Title, string Slug, int CardCount);

    public sealed record Card(string Question, string Answer, string? Explanation, string Lesson);

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        Decks = await _db.Lessons.AsNoTracking()
            .Where(l => l.Published && l.Questions.Any())
            .OrderBy(l => l.OrderIndex).ThenBy(l => l.Title)
            .Select(l => new DeckChoice(l.Title, l.Slug, l.Questions.Count))
            .ToListAsync(cancellationToken);

        var query = _db.QuizQuestions.AsNoTracking().Include(q => q.Lesson).Where(q => q.Lesson.Published);

        if (!string.IsNullOrWhiteSpace(Lesson))
        {
            var deck = Decks.FirstOrDefault(d => d.Slug == Lesson);
            if (deck is null)
                return RedirectToPage(new { lesson = (string?)null });

            LessonTitle = deck.Title;
            query = query.Where(q => q.Lesson.Slug == Lesson);
        }

        var questions = await query
            .OrderBy(q => q.Lesson.OrderIndex).ThenBy(q => q.OrderIndex).ThenBy(q => q.Id)
            .ToListAsync(cancellationToken);

        // The mixed deck is a random sample so it stays a manageable size as the course grows.
        if (LessonTitle is null && questions.Count > MaxCards)
            questions = questions.OrderBy(_ => Random.Shared.Next()).Take(MaxCards).ToList();

        Cards = questions
            .Select(q => new Card(q.Prompt, AnswerText(q), q.Explanation, q.Lesson.Title))
            .ToList();

        return Page();
    }

    private static string AnswerText(QuizQuestion question) =>
        question.GetOptions().FirstOrDefault(o => o.Index == question.CorrectOption)?.Text ?? string.Empty;
}
