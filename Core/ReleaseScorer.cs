namespace CueComplete.Core;

public static class ReleaseScorer
{
    public const int ScoreTracks = 35;
    public const int ScoreBarcode = 35;
    public const int ScoreCatNoExact = 25;
    public const int ScoreCatNoPartial = 15;
    public const int ScoreCountry = 15;
    public const int ScoreYear = 10;
    public const int ScoreArtist = 10;
    public const int ScoreAlbum = 10;
    public const int ScoreDiscNumber = 5;
    public const int ScoreDiscs = 5;

    public static int CalculateScore(CueData candidate, CueData source)
    {
        if (candidate == null || source == null)
            return 0;

        int score = 0;

        // 1. Track Count (+35)
        if (source.Tracks.HasValue && source.Tracks.Value > 0 &&
            candidate.Tracks.HasValue && candidate.Tracks.Value == source.Tracks.Value)
        {
            score += ScoreTracks;
        }

        // 2. Barcode (+35)
        if (!string.IsNullOrWhiteSpace(source.Barcode) && !string.IsNullOrWhiteSpace(candidate.Barcode))
        {
            var cleanSourceBarcode = NormalizeBarcode(source.Barcode);
            var cleanCandidateBarcode = NormalizeBarcode(candidate.Barcode);
            if (!string.IsNullOrEmpty(cleanSourceBarcode) && cleanSourceBarcode.Equals(cleanCandidateBarcode, StringComparison.OrdinalIgnoreCase))
            {
                score += ScoreBarcode;
            }
        }

        // 3. Catalog Number (+25 exact, +15 partial)
        if (!string.IsNullOrWhiteSpace(source.CatalogNumber) && !string.IsNullOrWhiteSpace(candidate.CatalogNumber))
        {
            var cleanSourceCat = NormalizeCatalogNumber(source.CatalogNumber);
            var cleanCandidateCat = NormalizeCatalogNumber(candidate.CatalogNumber);

            if (!string.IsNullOrEmpty(cleanSourceCat) && !string.IsNullOrEmpty(cleanCandidateCat))
            {
                if (cleanSourceCat.Equals(cleanCandidateCat, StringComparison.OrdinalIgnoreCase))
                {
                    score += ScoreCatNoExact;
                }
                else if (cleanSourceCat.Length >= 3 && cleanCandidateCat.Length >= 3 &&
                         (cleanCandidateCat.Contains(cleanSourceCat, StringComparison.OrdinalIgnoreCase) ||
                          cleanSourceCat.Contains(cleanCandidateCat, StringComparison.OrdinalIgnoreCase)))
                {
                    score += ScoreCatNoPartial;
                }
            }
        }

        // 4. Country (+15)
        if (StringExtensions.MatchesSourceCountry(candidate.Country, source))
        {
            score += ScoreCountry;
        }

        // 5. Date / Year (+10)
        var sourceYear = CueData.SanitizeYear(source.Date) ?? CueData.SanitizeYear(source.ReleaseDate);
        var candidateYear = CueData.SanitizeYear(candidate.Date) ?? CueData.SanitizeYear(candidate.ReleaseDate);
        if (!string.IsNullOrEmpty(sourceYear) && !string.IsNullOrEmpty(candidateYear) &&
            sourceYear.Equals(candidateYear, StringComparison.OrdinalIgnoreCase))
        {
            score += ScoreYear;
        }

        // 6. Artist (+10)
        if (StringExtensions.MatchesArtist(candidate.Artist, source.Artist))
        {
            score += ScoreArtist;
        }

        // 7. Album (+10)
        if (StringExtensions.MatchesAlbum(candidate.Album, source.Album))
        {
            score += ScoreAlbum;
        }

        // 8. Discs / Disc Number (+5 each)
        if (source.DiscNumber.HasValue && candidate.DiscNumber.HasValue && source.DiscNumber.Value == candidate.DiscNumber.Value)
        {
            score += ScoreDiscNumber;
        }

        if (source.Discs.HasValue && candidate.Discs.HasValue && source.Discs.Value == candidate.Discs.Value)
        {
            score += ScoreDiscs;
        }

        return score;
    }

    public static string NormalizeBarcode(string barcode)
    {
        if (string.IsNullOrWhiteSpace(barcode))
            return string.Empty;

        var sb = new System.Text.StringBuilder(barcode.Length);
        foreach (char c in barcode)
        {
            if (char.IsLetterOrDigit(c))
                sb.Append(c);
        }
        return sb.ToString();
    }

    public static string NormalizeCatalogNumber(string catNo)
    {
        if (string.IsNullOrWhiteSpace(catNo))
            return string.Empty;

        var sb = new System.Text.StringBuilder(catNo.Length);
        foreach (char c in catNo)
        {
            if (char.IsLetterOrDigit(c))
                sb.Append(char.ToLowerInvariant(c));
        }
        return sb.ToString();
    }
}
