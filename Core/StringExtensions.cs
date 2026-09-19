using System.Globalization;
using System.Text.RegularExpressions;

namespace CueComplete.Core;

public static class StringExtensions
{
    private static readonly CompareInfo InvariantCompareInfo = CultureInfo.InvariantCulture.CompareInfo;
    private const CompareOptions DiacriticAndCaseInsensitiveOptions = CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace;

    private static readonly Dictionary<string, string[]> CountryAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        { "JP", new[] { "JP", "JPN", "Japan", "Japanese" } },
        { "US", new[] { "US", "USA", "United States", "America" } },
        { "GB", new[] { "GB", "UK", "United Kingdom", "Britain", "Great Britain", "England" } },
        { "XE", new[] { "XE", "EU", "Europe", "European" } },
        { "DE", new[] { "DE", "GER", "Germany", "German" } },
        { "FR", new[] { "FR", "France", "French" } },
        { "RU", new[] { "RU", "RUS", "Russia", "Russian", "USSR" } },
        { "CA", new[] { "CA", "CAN", "Canada", "Canadian" } },
        { "AU", new[] { "AU", "AUS", "Australia", "Australian" } },
        { "IT", new[] { "IT", "ITA", "Italy", "Italian" } },
        { "SE", new[] { "SE", "SWE", "Sweden", "Swedish" } },
        { "NL", new[] { "NL", "NLD", "Netherlands", "Holland", "Dutch" } },
        { "PL", new[] { "PL", "POL", "Poland", "Polish" } },
    };

    /// <summary>
    /// Checks whether the source string contains the target string, ignoring casing and non-spacing diacritics / combining marks (e.g. Cyrillic 'ё'/'е', accents, umlauts).
    /// </summary>
    public static bool ContainsIgnoreCaseAndDiacritics(this string? source, string? target)
    {
        if (string.IsNullOrEmpty(target)) return true;
        if (string.IsNullOrEmpty(source)) return false;

        return InvariantCompareInfo.IndexOf(source, target, DiacriticAndCaseInsensitiveOptions) >= 0;
    }

    /// <summary>
    /// Checks whether candidate and target artist names match bidirectionally (one contains the other),
    /// ignoring casing, diacritics/accents (including Cyrillic 'ё' and 'е'), and normalizing '&' / 'and'.
    /// </summary>
    public static bool MatchesArtist(string? candidate, string? target)
    {
        if (string.IsNullOrWhiteSpace(candidate) || string.IsNullOrWhiteSpace(target))
            return false;

        var candTrimmed = candidate.Trim();
        var targetTrimmed = target.Trim();

        if (InvariantCompareInfo.IndexOf(candTrimmed, targetTrimmed, DiacriticAndCaseInsensitiveOptions) >= 0
            || InvariantCompareInfo.IndexOf(targetTrimmed, candTrimmed, DiacriticAndCaseInsensitiveOptions) >= 0)
        {
            return true;
        }

        var candNormalized = NormalizeArtist(candTrimmed);
        var targetNormalized = NormalizeArtist(targetTrimmed);

        return InvariantCompareInfo.IndexOf(candNormalized, targetNormalized, DiacriticAndCaseInsensitiveOptions) >= 0
            || InvariantCompareInfo.IndexOf(targetNormalized, candNormalized, DiacriticAndCaseInsensitiveOptions) >= 0;
    }

    private static string NormalizeArtist(string artist)
    {
        var normalized = Regex.Replace(artist, @"\s+&\s+", " and ", RegexOptions.IgnoreCase);
        normalized = Regex.Replace(normalized, @"\s+\+\s+", " and ", RegexOptions.IgnoreCase);
        return normalized;
    }

    /// <summary>
    /// Strips trailing parenthetical and bracketed annotations from an album or track title
    /// (e.g. "(Japan)", "[Deluxe Edition]", "(2011 Remaster)", "[Bonus Tracks]").
    /// If stripping removes the entire title, returns the original trimmed title.
    /// </summary>
    public static string StripTitleAnnotations(string? title)
    {
        if (string.IsNullOrWhiteSpace(title))
            return string.Empty;

        var trimmed = title.Trim();
        var stripped = Regex.Replace(trimmed, @"(?:\s*[\(\[][^)\]]*[\)\]]\s*)+$", "").Trim();
        stripped = stripped.TrimEnd(' ', '-', '/', '–', '—').Trim();

        return string.IsNullOrWhiteSpace(stripped) ? trimmed : stripped;
    }

    /// <summary>
    /// Checks whether two country codes or names refer to the same country using common aliases.
    /// </summary>
    public static bool MatchesCountry(string? country1, string? country2)
    {
        if (string.IsNullOrWhiteSpace(country1) || string.IsNullOrWhiteSpace(country2))
            return false;

        var c1 = country1.Trim();
        var c2 = country2.Trim();

        if (string.Equals(c1, c2, StringComparison.OrdinalIgnoreCase))
            return true;

        foreach (var aliases in CountryAliases.Values)
        {
            if (aliases.Any(a => string.Equals(a, c1, StringComparison.OrdinalIgnoreCase))
                && aliases.Any(a => string.Equals(a, c2, StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Checks if a release country matches any country extracted from the source country or album annotations (e.g. "(Japan)").
    /// </summary>
    public static bool MatchesSourceCountry(string? releaseCountry, CueData sourceData)
    {
        if (string.IsNullOrWhiteSpace(releaseCountry))
            return false;

        if (!string.IsNullOrWhiteSpace(sourceData.Country) && MatchesCountry(releaseCountry, sourceData.Country))
            return true;

        if (!string.IsNullOrWhiteSpace(sourceData.Album))
        {
            var match = Regex.Match(sourceData.Album, @"[\(\[]([^)\]]+)[\)\]]");
            while (match.Success)
            {
                var content = match.Groups[1].Value.Trim();
                if (MatchesCountry(releaseCountry, content))
                    return true;
                match = match.NextMatch();
            }
        }

        return false;
    }
}
