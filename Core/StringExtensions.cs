using System.Globalization;

namespace CueComplete.Core;

public static class StringExtensions
{
    private static readonly CompareInfo InvariantCompareInfo = CultureInfo.InvariantCulture.CompareInfo;
    private const CompareOptions DiacriticAndCaseInsensitiveOptions = CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace;

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
    /// ignoring casing and diacritics/accents (including Cyrillic 'ё' and 'е').
    /// </summary>
    public static bool MatchesArtist(string? candidate, string? target)
    {
        if (string.IsNullOrWhiteSpace(candidate) || string.IsNullOrWhiteSpace(target))
            return false;

        var candTrimmed = candidate.Trim();
        var targetTrimmed = target.Trim();

        return InvariantCompareInfo.IndexOf(candTrimmed, targetTrimmed, DiacriticAndCaseInsensitiveOptions) >= 0
            || InvariantCompareInfo.IndexOf(targetTrimmed, candTrimmed, DiacriticAndCaseInsensitiveOptions) >= 0;
    }
}
