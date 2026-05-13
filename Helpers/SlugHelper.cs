using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Syphonic.Helpers;

public static class SlugHelper
{
    public static string CreateSlug(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title is required.");

        var lower = title.Trim().ToLowerInvariant();
        var sb = new StringBuilder(lower.Length);

        foreach (var ch in lower.Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark && ch < 127)
                sb.Append(ch == ' ' ? '-' : ch);
        }

        var collapsed = Regex.Replace(sb.ToString(), @"\s*-\s*|\s+", "-");
        var cleaned = Regex.Replace(collapsed.ToLowerInvariant(), @"[^a-z0-9-]", "");
        var slug = Regex.Replace(cleaned, "-{2,}", "-").Trim('-');
        if (string.IsNullOrEmpty(slug))
            throw new ArgumentException("Could not derive a URL slug from the title. Please provide a slug manually.");

        return slug;
    }
}
