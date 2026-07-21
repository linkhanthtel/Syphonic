namespace Syphonic.Helpers;

public static class ActivityKindFormatter
{
    public static string ToDisplayName(string kind) =>
        kind switch
        {
            "lesson_started" => "Lesson opened",
            "lesson_completed" => "Lesson finished",
            _ => Capitalize(kind.Replace('_', ' '))
        };

    private static string Capitalize(string value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        return char.ToUpperInvariant(value[0]) + (value.Length > 1 ? value[1..] : string.Empty);
    }
}
