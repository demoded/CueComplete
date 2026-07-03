using System.Text.RegularExpressions;

namespace CueComplete.Core;

public class CueFileParser
{
    public static CueData Parse(string filePath)
    {
        var data = new CueData();
        if (!File.Exists(filePath))
            return data;

        data.OriginalLines = File.ReadAllLines(filePath).ToList();
        
        bool inTrack = false;

        foreach (var line in data.OriginalLines)
        {
            var trimmedLine = line.Trim();
            
            // If we hit a TRACK or FILE, we're out of the global scope
            if (trimmedLine.StartsWith("FILE", StringComparison.OrdinalIgnoreCase) || 
                trimmedLine.StartsWith("TRACK", StringComparison.OrdinalIgnoreCase))
            {
                inTrack = true;
                // We only care about global metadata for now, though TRACK artist/title could be useful later.
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
                    data.CatalogNumber = ExtractRemValue(trimmedLine, "CATALOG NUMBER") ?? data.CatalogNumber;
                    data.Country = ExtractRemValue(trimmedLine, "COUNTRY") ?? data.Country;
                    data.ReleaseDate = ExtractRemValue(trimmedLine, "RELEASE DATE") ?? data.ReleaseDate;
                    data.DiscId = ExtractRemValue(trimmedLine, "DISCID") ?? data.DiscId;
                }
            }
        }

        if (string.IsNullOrWhiteSpace(data.CatalogNumber))
        {
            var folderName = Path.GetFileName(Path.GetDirectoryName(filePath));
            if (!string.IsNullOrWhiteSpace(folderName))
            {
                data.CatalogNumber = ExtractCatalogNumberFromFolderName(folderName);
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
}
