using System.Globalization;
using System.Text;
using System.Text.Json;

namespace Syphonic.Services;

/// <summary>
/// Country facts for the drills, read once from the same wwwroot/data/countries.json file the map uses.
/// Registered as a singleton: the file is small and never changes while the app runs.
/// </summary>
public class CountryData
{
    public sealed record Country(string Id, string Name, IReadOnlyList<string> Capitals, string Region, long Population);

    private readonly Lazy<IReadOnlyList<Country>> _countries;

    public CountryData(IWebHostEnvironment environment)
    {
        _countries = new Lazy<IReadOnlyList<Country>>(() => Load(environment));
    }

    /// <summary>UN member states that have a capital and a population figure: a fair, well-known set to be tested on.</summary>
    public IReadOnlyList<Country> Countries => _countries.Value;

    public IReadOnlyList<string> Regions => Countries.Select(c => c.Region).Distinct().OrderBy(r => r).ToList();

    public Country? Find(string? id) => id is null ? null : Countries.FirstOrDefault(c => c.Id == id);

    private static IReadOnlyList<Country> Load(IWebHostEnvironment environment)
    {
        var file = environment.WebRootFileProvider.GetFileInfo("data/countries.json");
        if (!file.Exists)
            return Array.Empty<Country>();

        using var stream = file.CreateReadStream();
        using var document = JsonDocument.Parse(stream);

        var countries = new List<Country>();
        foreach (var item in document.RootElement.EnumerateArray())
        {
            if (!item.GetProperty("unMember").GetBoolean())
                continue;

            var capitals = item.GetProperty("capital").EnumerateArray()
                .Select(c => c.GetString() ?? string.Empty)
                .Where(c => c.Length > 0)
                .ToList();
            var population = item.TryGetProperty("population", out var p) && p.ValueKind == JsonValueKind.Number ? p.GetInt64() : 0;
            var region = item.GetProperty("region").GetString() ?? string.Empty;

            if (capitals.Count == 0 || population <= 0 || region.Length == 0)
                continue;

            countries.Add(new Country(item.GetProperty("id").GetString()!, item.GetProperty("name").GetString()!, capitals, region, population));
        }

        return countries;
    }

    /// <summary>
    /// Compares a typed answer with the accepted ones, ignoring case, accents, spaces and punctuation,
    /// so "sao tome" matches "São Tomé". One wrong letter is forgiven in longer names.
    /// </summary>
    public static TypedMatch MatchTyped(string? typed, IEnumerable<string> accepted)
    {
        var answer = Normalize(typed);
        if (answer.Length == 0)
            return TypedMatch.Wrong;

        var best = TypedMatch.Wrong;
        foreach (var option in accepted)
        {
            // "Washington D.C." is also accepted as "Washington": try the name without abbreviations such as "D.C.".
            var withoutAbbreviations = string.Join(' ', option.Split(' ', ',').Where(word => word.Length > 0 && !word.Contains('.')));
            foreach (var form in new[] { option, withoutAbbreviations })
            {
                var target = Normalize(form);
                if (target == answer)
                    return TypedMatch.Exact;
                if (target.Length >= 6 && EditDistance(answer, target) == 1)
                    best = TypedMatch.Close;
            }
        }

        return best;
    }

    private static string Normalize(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        var builder = new StringBuilder();
        foreach (var ch in text.Normalize(NormalizationForm.FormD))
        {
            // FormD splits "é" into "e" plus an accent mark; dropping the marks leaves plain letters.
            if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark && char.IsLetterOrDigit(ch))
                builder.Append(char.ToLowerInvariant(ch));
        }

        return builder.ToString();
    }

    /// <summary>Number of single-letter changes (insert, delete, replace) to turn one word into the other.</summary>
    private static int EditDistance(string a, string b)
    {
        var previous = new int[b.Length + 1];
        var current = new int[b.Length + 1];
        for (var j = 0; j <= b.Length; j++)
            previous[j] = j;

        for (var i = 1; i <= a.Length; i++)
        {
            current[0] = i;
            for (var j = 1; j <= b.Length; j++)
            {
                var cost = a[i - 1] == b[j - 1] ? 0 : 1;
                current[j] = Math.Min(Math.Min(current[j - 1] + 1, previous[j] + 1), previous[j - 1] + cost);
            }

            (previous, current) = (current, previous);
        }

        return previous[b.Length];
    }
}

public enum TypedMatch
{
    Wrong,
    /// <summary>One letter off: counted as correct, with the right spelling shown.</summary>
    Close,
    Exact
}
