using System.Text;

namespace CueComplete.Core;

public class CueFileWriter
{
    public static void Save(string filePath, CueData updatedData)
    {
        if (File.Exists(filePath))
        {
            var backupPath = filePath + ".bak";
            File.Copy(filePath, backupPath, overwrite: true);
        }

        var outputLines = new List<string>();
        bool inTrack = false;
        
        // Track which fields we've already written so we don't duplicate them
        var writtenKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Define a helper to format fields
        string FormatField(string key, string? value, bool isRem)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            if (isRem) return $"REM {key} \"{value}\"";
            return $"{key} \"{value}\"";
        }

        // Prepare the new global header block
        var newHeaders = new List<string>();
        Action<string, string?, bool> addHeader = (key, value, isRem) =>
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                newHeaders.Add(FormatField(key, value, isRem));
                writtenKeys.Add(isRem ? "REM " + key : key);
            }
        };

        addHeader("GENRE", updatedData.Genre, true);
        addHeader("DATE", updatedData.Date, true);
        addHeader("LABEL", updatedData.Label, true);
        addHeader("CATALOGNUMBER", updatedData.CatalogNumber, true);
        addHeader("COUNTRY", updatedData.Country, true);
        addHeader("RELEASEDATE", updatedData.ReleaseDate, true);
        addHeader("COMMENT", updatedData.Comment, true);
        addHeader("DISCNUMBER", (updatedData.DiscNumber ?? 1).ToString(), true);
        addHeader("TOTALDISCS", (updatedData.Discs ?? 1).ToString(), true);
        addHeader("PERFORMER", updatedData.Artist, false);
        addHeader("TITLE", updatedData.Album, false);
        addHeader("CATALOG", updatedData.Barcode?.Replace(" ", ""), false);

        bool headersInjected = false;

        foreach (var line in updatedData.OriginalLines)
        {
            var trimmedLine = line.Trim();
            
            if (trimmedLine.StartsWith("FILE", StringComparison.OrdinalIgnoreCase) || 
                trimmedLine.StartsWith("TRACK", StringComparison.OrdinalIgnoreCase))
            {
                if (!headersInjected)
                {
                    outputLines.AddRange(newHeaders);
                    headersInjected = true;
                }
                inTrack = true;
            }

            if (!inTrack)
            {
                // We are in the global section. We need to skip lines that we are overriding.
                bool skipLine = false;
                
                // Helper to check and skip
                bool CheckSkip(string key, bool isRem)
                {
                    var prefix = isRem ? $"REM {key} " : $"{key} ";
                    if (trimmedLine.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                        return true;
                    return false;
                }

                if (CheckSkip("GENRE", true) || CheckSkip("DATE", true) || 
                    CheckSkip("LABEL", true) || CheckSkip("CATALOGNUMBER", true) || CheckSkip("CATALOG NUMBER", true) || 
                    CheckSkip("COUNTRY", true) || CheckSkip("RELEASEDATE", true) || CheckSkip("RELEASE DATE", true) ||
                    CheckSkip("COMMENT", true) ||
                    CheckSkip("DISCNUMBER", true) || CheckSkip("TOTALDISCS", true) ||
                    CheckSkip("PERFORMER", false) || CheckSkip("TITLE", false) || 
                    CheckSkip("CATALOG", false))
                {
                    skipLine = true;
                }

                if (!skipLine && !string.IsNullOrWhiteSpace(line))
                {
                    outputLines.Add(line);
                }
            }
            else
            {
                outputLines.Add(line);
            }
        }

        // If for some reason the cue file had no FILE/TRACK, inject at the end
        if (!headersInjected)
        {
            outputLines.AddRange(newHeaders);
        }

        var encodingToUse = updatedData.OriginalEncoding;
        var fullText = string.Join(Environment.NewLine, outputLines);
        
        try 
        {
            var encodingWithFallback = Encoding.GetEncoding(
                encodingToUse.CodePage, 
                new EncoderExceptionFallback(), 
                new DecoderExceptionFallback()
            );
            encodingWithFallback.GetBytes(fullText);
        }
        catch (EncoderFallbackException)
        {
            encodingToUse = new UTF8Encoding(true);
        }

        File.WriteAllLines(filePath, outputLines, encodingToUse);
    }
}
