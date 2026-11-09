using Syphonic.Models;

namespace Syphonic.Services;

/// <summary>
/// Grades quiz answers on the server. Correct answers never reach the browser
/// before submission, so learners can't peek at them in the page source.
/// </summary>
public static class QuizGrader
{
    /// <param name="questions">The lesson's questions, in display order.</param>
    /// <param name="answers">Question id → chosen option index, as posted by the form.</param>
    public static QuizResult Grade(IReadOnlyList<QuizQuestion> questions, IReadOnlyDictionary<int, int> answers)
    {
        var items = new List<QuizResultItem>(questions.Count);

        foreach (var question in questions)
        {
            int? chosen = answers.TryGetValue(question.Id, out var value) ? value : null;
            items.Add(new QuizResultItem(question, chosen, chosen == question.CorrectOption));
        }

        return new QuizResult(items);
    }
}

public sealed record QuizResultItem(QuizQuestion Question, int? ChosenOption, bool IsCorrect);

public sealed record QuizResult(IReadOnlyList<QuizResultItem> Items)
{
    public int Score => Items.Count(i => i.IsCorrect);

    public int Total => Items.Count;

    public int Percent => Total == 0 ? 0 : (int)Math.Round(Score * 100m / Total, MidpointRounding.AwayFromZero);

    public bool Passed => Percent >= PassPercent;

    public const int PassPercent = 70;
}
