using System.Net;
using System.Text.RegularExpressions;
using Markdig;

namespace Syphonic.Services;

/// <summary>
/// Turns lesson Markdown (authored by staff) into HTML for Razor rendering.
/// </summary>
public static class LessonMarkdown
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .UseSoftlineBreakAsHardlineBreak()
        .Build();

    public static string ToHtml(string? markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
            return string.Empty;

        return Markdown.ToHtml(markdown, Pipeline);
    }

    /// <summary>Average adult reading speed used for the "N min read" estimate.</summary>
    private const int WordsPerMinute = 200;

    /// <summary>Rough reading time in whole minutes, never less than one.</summary>
    public static int EstimateReadingMinutes(string? markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
            return 1;

        var words = markdown.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
        return Math.Max(1, (int)Math.Ceiling(words / (double)WordsPerMinute));
    }

    // Markdig's advanced extensions give every heading an id, which the outline links to.
    private static readonly Regex HeadingPattern = new(
        """<h(?<level>[23]) id="(?<id>[^"]+)"[^>]*>(?<text>.*?)</h\k<level>>""",
        RegexOptions.Singleline | RegexOptions.Compiled);

    private static readonly Regex TagPattern = new("<[^>]+>", RegexOptions.Compiled);

    /// <summary>The second- and third-level headings of rendered lesson HTML, for an "On this page" list.</summary>
    public static IReadOnlyList<LessonHeading> GetOutline(string html)
    {
        var headings = new List<LessonHeading>();
        foreach (Match match in HeadingPattern.Matches(html))
        {
            var text = WebUtility.HtmlDecode(TagPattern.Replace(match.Groups["text"].Value, string.Empty)).Trim();
            if (text.Length > 0)
                headings.Add(new LessonHeading(match.Groups["id"].Value, text, int.Parse(match.Groups["level"].Value)));
        }

        return headings;
    }
}

/// <param name="Id">The heading's HTML id, used as the link target.</param>
/// <param name="Level">2 or 3.</param>
public sealed record LessonHeading(string Id, string Text, int Level);
