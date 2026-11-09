namespace Syphonic.Models;

/// <summary>
/// One multiple-choice question attached to a lesson. Options C and D are optional,
/// so a question can have two, three, or four answers.
/// </summary>
public class QuizQuestion
{
    public const int MaxOptions = 4;

    public int Id { get; set; }

    public int LessonId { get; set; }

    public Lesson Lesson { get; set; } = null!;

    public string Prompt { get; set; } = string.Empty;

    public string OptionA { get; set; } = string.Empty;

    public string OptionB { get; set; } = string.Empty;

    public string? OptionC { get; set; }

    public string? OptionD { get; set; }

    /// <summary>Zero-based index of the right answer: 0 = A, 1 = B, 2 = C, 3 = D.</summary>
    public int CorrectOption { get; set; }

    /// <summary>Shown after grading to explain why the answer is right.</summary>
    public string? Explanation { get; set; }

    public int OrderIndex { get; set; }

    /// <summary>
    /// Returns only the options that have text, keeping their original index so
    /// "C" is always index 2 even when it is the last option shown.
    /// EF Core ignores methods, so this is not stored in the database.
    /// </summary>
    public IReadOnlyList<QuizOption> GetOptions()
    {
        var all = new[] { OptionA, OptionB, OptionC, OptionD };
        var options = new List<QuizOption>(MaxOptions);
        for (var i = 0; i < all.Length; i++)
        {
            if (!string.IsNullOrWhiteSpace(all[i]))
                options.Add(new QuizOption(i, all[i]!));
        }

        return options;
    }

    public static string LetterFor(int index) => ((char)('A' + index)).ToString();
}

public sealed record QuizOption(int Index, string Text)
{
    public string Letter => QuizQuestion.LetterFor(Index);
}
