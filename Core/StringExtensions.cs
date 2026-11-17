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

    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "a", "an", "the", "and", "or", "of", "in", "on", "at", "by", "for", "with", "to",
        "de", "der", "die", "das", "la", "le", "les", "du", "des", "und", "et"
    };

    /// <summary>
    /// Checks whether candidate and target artist names match bidirectionally (one contains the other),
    /// ignoring casing, diacritics/accents (including Cyrillic 'ё' and 'е'), normalizing '&' / 'and',
    /// and checking for acronym/initialism matches (e.g. "MP" and "Metal Priest", "ELO" and "Electric Light Orchestra").
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

        if (InvariantCompareInfo.IndexOf(candNormalized, targetNormalized, DiacriticAndCaseInsensitiveOptions) >= 0
            || InvariantCompareInfo.IndexOf(targetNormalized, candNormalized, DiacriticAndCaseInsensitiveOptions) >= 0)
        {
            return true;
        }

        return IsAcronymMatch(candNormalized, targetNormalized);
    }

    private static string NormalizeArtist(string artist)
    {
        var normalized = Regex.Replace(artist, @"\s*\(\d+\)$", "").Trim();
        normalized = Regex.Replace(normalized, @"\s+&\s+", " and ", RegexOptions.IgnoreCase);
        normalized = Regex.Replace(normalized, @"\s+\+\s+", " and ", RegexOptions.IgnoreCase);
        return normalized;
    }

    /// <summary>
    /// Checks if one string is an acronym/initialism of the other (e.g. "MP" and "Metal Priest", "ELO" and "Electric Light Orchestra").
    /// </summary>
    public static bool IsAcronymMatch(string? str1, string? str2)
    {
        if (string.IsNullOrWhiteSpace(str1) || string.IsNullOrWhiteSpace(str2))
            return false;

        return CheckAcronym(str1, str2) || CheckAcronym(str2, str1);
    }

    private static bool CheckAcronym(string acronymCandidate, string wordsCandidate)
    {
        var cleanAcronym = Regex.Replace(acronymCandidate.Trim(), @"[\.\s\-_/]", "");

        // Acronyms must be between 2 and 6 letters and contain only letters
        if (cleanAcronym.Length < 2 || cleanAcronym.Length > 6 || !cleanAcronym.All(char.IsLetter))
            return false;

        // An acronym candidate must be either 2 characters long (e.g. "MP", "DK"),
        // contain dots (e.g. "M.P.", "U.D.O."), or be entirely uppercase (e.g. "ELO", "BOC", "RHCP").
        bool isLikelyAcronym = cleanAcronym.Length == 2 ||
                               acronymCandidate.Contains('.') ||
                               acronymCandidate.All(c => !char.IsLetter(c) || char.IsUpper(c));
        if (!isLikelyAcronym)
            return false;

        var words = Regex.Split(wordsCandidate.Trim(), @"[\s\-_/,\.]+")
            .Where(w => !string.IsNullOrWhiteSpace(w))
            .Select(w => Regex.Replace(w, @"^[^\p{L}\p{N}]+|[^\p{L}\p{N}]+$", ""))
            .Where(w => !string.IsNullOrWhiteSpace(w) && char.IsLetter(w[0]))
            .ToList();

        if (words.Count < 2)
            return false;

        // 1. Check matching initials of all words
        if (words.Count == cleanAcronym.Length && MatchesInitials(cleanAcronym, words))
            return true;

        // 2. Check matching initials excluding common stopwords
        var nonStopWords = words.Where(w => !StopWords.Contains(w)).ToList();
        if (nonStopWords.Count >= 2 && nonStopWords.Count == cleanAcronym.Length && MatchesInitials(cleanAcronym, nonStopWords))
            return true;

        return false;
    }

    private static bool MatchesInitials(string cleanAcronym, List<string> words)
    {
        for (int i = 0; i < cleanAcronym.Length; i++)
        {
            string acronymChar = cleanAcronym[i].ToString();
            string wordInitial = words[i][0].ToString();

            if (InvariantCompareInfo.Compare(acronymChar, wordInitial, DiacriticAndCaseInsensitiveOptions) != 0)
                return false;
        }

        return true;
    }

    /// <summary>
    /// Generates a standard acronym (initialism) from a multi-word artist name (e.g. "Metal Priest" -> "MP", "Electric Light Orchestra" -> "ELO").
    /// Returns null if artist name does not have 2-6 words.
    /// </summary>
    public static string? GetAcronym(string? artist)
    {
        if (string.IsNullOrWhiteSpace(artist))
            return null;

        var normalized = NormalizeArtist(artist);
        var words = Regex.Split(normalized, @"[\s\-_/,\.]+")
            .Where(w => !string.IsNullOrWhiteSpace(w))
            .Select(w => Regex.Replace(w, @"^[^\p{L}\p{N}]+|[^\p{L}\p{N}]+$", ""))
            .Where(w => !string.IsNullOrWhiteSpace(w) && char.IsLetter(w[0]))
            .ToList();

        if (words.Count < 2)
            return null;

        var nonStopWords = words.Where(w => !StopWords.Contains(w)).ToList();
        var targetWords = (nonStopWords.Count >= 2 && nonStopWords.Count <= 6) ? nonStopWords : words;

        if (targetWords.Count < 2 || targetWords.Count > 6)
            return null;

        return new string(targetWords.Select(w => char.ToUpperInvariant(w[0])).ToArray());
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
