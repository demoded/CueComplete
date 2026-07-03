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

        return data;
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
