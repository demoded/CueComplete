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
                data.Barcode = ExtractValue(trimmedLine, "CATALOG") ?? data.Barcode;
                
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
            if (string.IsNullOrWhiteSpace(data.CatalogNumber))
            {
                data.CatalogNumber = ExtractCatalogNumberFromFolderName(folderName);
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
}
