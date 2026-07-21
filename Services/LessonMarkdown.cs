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
}
