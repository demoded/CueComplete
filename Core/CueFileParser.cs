using System.Text.RegularExpressions;
using System.Text;

namespace CueComplete.Core;

public class CueFileParser
{
    public static CueData Parse(string filePath)
    {
        var data = new CueData();
        if (!File.Exists(filePath))
            return data;

        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var encoding = DetectEncoding(filePath);
        data.OriginalEncoding = encoding;
        data.OriginalLines = File.ReadAllLines(filePath, encoding).ToList();
        
        bool inTrack = false;
        int currentTrackNum = 0;
        var trackOffsets = new List<TrackOffset>();

        foreach (var line in data.OriginalLines)
        {
            var trimmedLine = line.Trim();
            
            // If we hit a TRACK or FILE, we're out of the global scope
            if (trimmedLine.StartsWith("FILE", StringComparison.OrdinalIgnoreCase))
            {
                inTrack = true;
            }
            else if (trimmedLine.StartsWith("TRACK", StringComparison.OrdinalIgnoreCase))
            {
                inTrack = true;
                data.Tracks = (data.Tracks ?? 0) + 1;
                var parts = trimmedLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2 && int.TryParse(parts[1], out int tNum))
                {
                    currentTrackNum = tNum;
                }
                else
                {
                    currentTrackNum++;
                }
            }
            else if (trimmedLine.StartsWith("INDEX 01", StringComparison.OrdinalIgnoreCase))
            {
                var timeStr = ExtractValue(trimmedLine, "INDEX 01") ?? trimmedLine.Substring("INDEX 01".Length).Trim();
                var timeParts = timeStr.Split(':');
                if (timeParts.Length == 3 &&
                    int.TryParse(timeParts[0], out int mm) &&
                    int.TryParse(timeParts[1], out int ss) &&
                    int.TryParse(timeParts[2], out int ff))
                {
                    trackOffsets.Add(new TrackOffset
                    {
                        TrackNumber = currentTrackNum > 0 ? currentTrackNum : trackOffsets.Count + 1,
                        Minutes = mm,
                        Seconds = ss,
                        Frames = ff
                    });
                }
            }

            if (!inTrack)
            {
                data.Artist = ExtractValue(trimmedLine, "PERFORMER") ?? data.Artist;
                data.Album = ExtractValue(trimmedLine, "TITLE") ?? data.Album;
                var catalogVal = ExtractValue(trimmedLine, "CATALOG");
                if (!string.IsNullOrWhiteSpace(catalogVal) && Metadata.DiscogsProvider.IsValidBarcodeCandidate(catalogVal))
                {
                    data.Barcode = catalogVal;
                }
                
                // REM fields
                if (trimmedLine.StartsWith("REM", StringComparison.OrdinalIgnoreCase))
                {
                    data.Genre = ExtractRemValue(trimmedLine, "GENRE") ?? data.Genre;
                    data.Date = ExtractRemValue(trimmedLine, "DATE") ?? data.Date;
                    data.Label = ExtractRemValue(trimmedLine, "LABEL") ?? data.Label;
                    data.CatalogNumber = ExtractRemValue(trimmedLine, "CATALOGNUMBER") ?? ExtractRemValue(trimmedLine, "CATALOG NUMBER") ?? data.CatalogNumber;
                    data.Country = ExtractRemValue(trimmedLine, "COUNTRY") ?? data.Country;
                    data.ReleaseDate = ExtractRemValue(trimmedLine, "RELEASEDATE") ?? ExtractRemValue(trimmedLine, "RELEASE DATE") ?? data.ReleaseDate;
                    data.DiscId = ExtractRemValue(trimmedLine, "DISCID") ?? data.DiscId;
                    data.MusicBrainzDiscId = ExtractRemValue(trimmedLine, "MUSICBRAINZ_DISCID") ?? ExtractRemValue(trimmedLine, "MUSICBRAINZ_RELEASEID") ?? ExtractRemValue(trimmedLine, "MBDISCID") ?? data.MusicBrainzDiscId;
                    data.Comment = ExtractRemValue(trimmedLine, "COMMENT") ?? data.Comment;
                    
                    var discNumberStr = ExtractRemValue(trimmedLine, "DISCNUMBER");
                    if (int.TryParse(discNumberStr, out int dn)) data.DiscNumber = dn;

                    var totalDiscsStr = ExtractRemValue(trimmedLine, "TOTALDISCS");
                    if (int.TryParse(totalDiscsStr, out int td)) data.Discs = td;
                }
            }
        }

        if (trackOffsets.Count > 0)
        {
            var lastTrack = trackOffsets[^1];
            int leadOutSectors = lastTrack.TotalSectors + (2 * 60 * 75);
            int leadOutSeconds = lastTrack.TotalSeconds + 120;

            if (!string.IsNullOrWhiteSpace(data.DiscId) && data.DiscId.Length == 8)
            {
                try
                {
                    int freedbSeconds = Convert.ToInt32(data.DiscId.Substring(2, 4), 16);
                    leadOutSeconds = freedbSeconds + trackOffsets[0].TotalSeconds + 2;
                    leadOutSectors = (freedbSeconds + 2) * 75 + trackOffsets[0].Frames + 2;
                }
                catch { }
            }

            if (string.IsNullOrWhiteSpace(data.DiscId))
            {
                data.DiscId = DiscIdCalculator.CalculateFreeDbId(trackOffsets, leadOutSeconds);
            }

            if (string.IsNullOrWhiteSpace(data.MusicBrainzDiscId))
            {
                data.MusicBrainzDiscId = DiscIdCalculator.CalculateMusicBrainzDiscId(trackOffsets, leadOutSectors);
            }
        }

        var folderName = Path.GetFileName(Path.GetDirectoryName(filePath));
        if (!string.IsNullOrWhiteSpace(folderName))
        {
            if (string.IsNullOrWhiteSpace(data.CatalogNumber) && !string.IsNullOrWhiteSpace(folderName))
            {
                data.CatalogNumber = ExtractCatalogNumberFromFolderName(folderName);
            }

            if (string.IsNullOrWhiteSpace(data.Country) && !string.IsNullOrWhiteSpace(folderName))
            {
                data.Country = ExtractCountryFromFolderName(folderName);
            }

            var fileName = Path.GetFileNameWithoutExtension(filePath);
            
            if (string.IsNullOrWhiteSpace(data.Artist) || string.IsNullOrWhiteSpace(data.Album))
            {
                var parts = fileName.Split(new[] { " - " }, 2, StringSplitOptions.None);
                if (parts.Length == 2)
                {
                    data.Artist ??= parts[0].Trim();
                    data.Album ??= parts[1].Trim();
                }
            }

            if (!data.DiscNumber.HasValue)
            {
                var match = Regex.Match(fileName, @"\b(?:CD|Disc)\s*(\d+)\b", RegexOptions.IgnoreCase);
                if (match.Success && int.TryParse(match.Groups[1].Value, out int dn))
                {
                    data.DiscNumber = dn;
                }
            }

            // Strip trailing release metadata bracket (e.g. "[1994, Massacre, MASS CD 033, DE]")
            // so catalog numbers like "MASS CD 033" are not mistakenly parsed as disc numbers
            string folderCleanForDiscs = folderName;
            var metaMatch = Regex.Match(folderName, @"\[([^\]]+)\][^\[\]]*$");
            if (metaMatch.Success && metaMatch.Groups[1].Value.Contains(','))
            {
                folderCleanForDiscs = folderName.Substring(0, metaMatch.Index).Trim();
            }

            if (!data.DiscNumber.HasValue)
            {
                var match = Regex.Match(folderCleanForDiscs, @"\b(?:CD|Disc)\s*(\d+)\b", RegexOptions.IgnoreCase);
                if (match.Success && int.TryParse(match.Groups[1].Value, out int dn))
                {
                    bool isCatNoMatch = !string.IsNullOrWhiteSpace(data.CatalogNumber) &&
                        Regex.IsMatch(data.CatalogNumber, $@"\b(?:CD|Disc)?\s*0*{dn}\b", RegexOptions.IgnoreCase);
                    if (!isCatNoMatch)
                    {
                        data.DiscNumber = dn;
                    }
                }
            }

            if (!data.Discs.HasValue)
            {
                var match = Regex.Match(folderCleanForDiscs, @"\b(\d+)\s*CD\b", RegexOptions.IgnoreCase);
                if (match.Success && int.TryParse(match.Groups[1].Value, out int td))
                {
                    data.Discs = td;
                }
            }
        }

        return data;
    }

    private static string? ExtractCatalogNumberFromFolderName(string folderName)
    {
        var match = Regex.Match(folderName, @"\[([^\]]+)\][^\[\]]*$");
        if (!match.Success) return null;

        var content = match.Groups[1].Value;
        var parts = content.Split(',').Select(p => p.Trim()).ToList();

        parts.RemoveAll(p => Regex.IsMatch(p, @"^\d{4}$"));
        parts.RemoveAll(p => Regex.IsMatch(p, @"^\d+CD$", RegexOptions.IgnoreCase));
        parts.RemoveAll(p => Regex.IsMatch(p, @"^[A-Z]{2,3}$"));

        if (parts.Count == 0) return null;

        var withDigits = parts.Where(p => Regex.IsMatch(p, @"\d")).ToList();
        if (withDigits.Count > 0)
        {
            return withDigits.Last();
        }

        return parts.Last();
    }

    private static string? ExtractCountryFromFolderName(string folderName)
    {
        var match = Regex.Match(folderName, @"\[([^\]]+)\][^\[\]]*$");
        if (!match.Success) return null;

        var content = match.Groups[1].Value;
        var parts = content.Split(',').Select(p => p.Trim()).ToList();
        return parts.FirstOrDefault(p => Regex.IsMatch(p, @"^[A-Z]{2,3}$", RegexOptions.IgnoreCase));
    }

    private static string? ExtractValue(string line, string key)
    {
        if (line.StartsWith(key + " ", StringComparison.OrdinalIgnoreCase))
        {
            var value = line.Substring(key.Length).Trim();
            return value.Trim('"');
        }
        return null;
    }

    private static string? ExtractRemValue(string line, string remKey)
    {
        var prefix = $"REM {remKey} ";
        if (line.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            var value = line.Substring(prefix.Length).Trim();
            return value.Trim('"');
        }
        return null;
    }

    private static Encoding DetectEncoding(string filePath)
    {
        var bytes = File.ReadAllBytes(filePath);
        if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
            return Encoding.UTF8;

        var detector = new Ude.CharsetDetector();
        detector.Feed(bytes, 0, bytes.Length);
        detector.DataEnd();
        
        if (detector.Charset != null)
        {
            try 
            {
                var charset = detector.Charset.ToLowerInvariant();
                if (charset == "maccyrillic" || charset == "x-mac-cyrillic" || charset == "iso-8859-8") 
                {
                    charset = "windows-1251";
                }
                return Encoding.GetEncoding(charset);
            }
            catch { }
        }

        var utf8Strict = new UTF8Encoding(false, true);
        try
        {
            utf8Strict.GetString(bytes);
            return Encoding.UTF8;
        }
        catch (DecoderFallbackException)
        {
            return Encoding.GetEncoding(1252);
        }
    }

    public static CueData ParseFormattedText(string text, CueData? template = null)
    {
        var cue = new CueData();
        if (template != null)
        {
            cue.OriginalLines = new List<string>(template.OriginalLines);
            cue.OriginalEncoding = template.OriginalEncoding;
            cue.DiscId = template.DiscId;
            cue.MusicBrainzDiscId = template.MusicBrainzDiscId;
            cue.DiscogsId = template.DiscogsId;
            cue.Source = template.Source;
        }

        if (string.IsNullOrWhiteSpace(text))
            return cue;

        var lines = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();
            if (string.IsNullOrEmpty(line) || line.StartsWith("(", StringComparison.Ordinal))
                continue;

            int colonIdx = line.IndexOf(':');
            if (colonIdx <= 0)
                continue;

            string key = line.Substring(0, colonIdx).Trim();
            string val = line.Substring(colonIdx + 1).Trim();
            string? nonNullVal = string.IsNullOrWhiteSpace(val) ? null : val;

            if (key.Equals("Artist", StringComparison.OrdinalIgnoreCase))
            {
                cue.Artist = nonNullVal;
            }
            else if (key.Equals("Album", StringComparison.OrdinalIgnoreCase))
            {
                cue.Album = nonNullVal;
            }
            else if (key.Equals("Genre", StringComparison.OrdinalIgnoreCase))
            {
                cue.Genre = nonNullVal;
            }
            else if (key.Equals("Date", StringComparison.OrdinalIgnoreCase))
            {
                cue.Date = nonNullVal;
            }
            else if (key.Equals("Label", StringComparison.OrdinalIgnoreCase))
            {
                cue.Label = nonNullVal;
            }
            else if (key.Equals("Cat No", StringComparison.OrdinalIgnoreCase) ||
                     key.Equals("CatNo", StringComparison.OrdinalIgnoreCase) ||
                     key.Equals("Catalog", StringComparison.OrdinalIgnoreCase) ||
                     key.Equals("Catalog Number", StringComparison.OrdinalIgnoreCase) ||
                     key.Equals("CatalogNumber", StringComparison.OrdinalIgnoreCase))
            {
                cue.CatalogNumber = nonNullVal;
            }
            else if (key.Equals("Country", StringComparison.OrdinalIgnoreCase))
            {
                cue.Country = nonNullVal;
            }
            else if (key.Equals("Barcode", StringComparison.OrdinalIgnoreCase))
            {
                cue.Barcode = nonNullVal;
            }
            else if (key.Equals("Rel Date", StringComparison.OrdinalIgnoreCase) ||
                     key.Equals("Release Date", StringComparison.OrdinalIgnoreCase) ||
                     key.Equals("ReleaseDate", StringComparison.OrdinalIgnoreCase))
            {
                cue.ReleaseDate = nonNullVal;
            }
            else if (key.Equals("Discs", StringComparison.OrdinalIgnoreCase) ||
                     key.Equals("Disc", StringComparison.OrdinalIgnoreCase))
            {
                ParseDiscsAndTracks(line, cue);
            }
            else if (key.Equals("Tracks", StringComparison.OrdinalIgnoreCase) ||
                     key.Equals("Track", StringComparison.OrdinalIgnoreCase))
            {
                if (int.TryParse(val, out int t))
                {
                    cue.Tracks = t;
                }
            }
            else if (key.Equals("Comment", StringComparison.OrdinalIgnoreCase))
            {
                cue.Comment = nonNullVal;
            }
        }

        return cue;
    }

    private static void ParseDiscsAndTracks(string line, CueData cue)
    {
        int tracksIdx = line.IndexOf("Tracks:", StringComparison.OrdinalIgnoreCase);
        if (tracksIdx < 0)
        {
            tracksIdx = line.IndexOf("Track:", StringComparison.OrdinalIgnoreCase);
        }

        string discsPart;
        string? tracksPart = null;

        int discsColonIdx = line.IndexOf(':');
        if (tracksIdx >= 0)
        {
            discsPart = line.Substring(discsColonIdx + 1, tracksIdx - (discsColonIdx + 1)).Trim();
            int trackColon = line.IndexOf(':', tracksIdx);
            if (trackColon >= 0)
            {
                tracksPart = line.Substring(trackColon + 1).Trim();
            }
        }
        else
        {
            discsPart = discsColonIdx >= 0 ? line.Substring(discsColonIdx + 1).Trim() : string.Empty;
        }

        if (!string.IsNullOrWhiteSpace(tracksPart) && int.TryParse(tracksPart, out int t))
        {
            cue.Tracks = t;
        }

        if (!string.IsNullOrWhiteSpace(discsPart))
        {
            var ofMatch = Regex.Match(discsPart, @"^(\d+)\s+of\s+(\d+)$", RegexOptions.IgnoreCase);
            if (ofMatch.Success)
            {
                cue.DiscNumber = int.Parse(ofMatch.Groups[1].Value);
                cue.Discs = int.Parse(ofMatch.Groups[2].Value);
            }
            else if (int.TryParse(discsPart, out int d))
            {
                cue.Discs = d;
                if (d == 1) cue.DiscNumber = 1;
            }
        }
    }
}
