namespace CueComplete.Core;

public enum DiffKind
{
    Unchanged,
    Added,
    Modified,
    Removed
}

public record HeaderFieldDiff(
    string FieldName,
    string? BackupValue,
    string? CurrentValue,
    DiffKind Kind
);

public class HeaderDiffResult
{
    public string CueFilePath { get; set; } = string.Empty;
    public string BackupFilePath { get; set; } = string.Empty;
    public bool HasBackup { get; set; }
    public List<HeaderFieldDiff> Fields { get; set; } = new();
    public int TotalChanges => Fields.Count(f => f.Kind != DiffKind.Unchanged);
    public bool HasChanges => TotalChanges > 0;
}

public static class HeaderDiffService
{
    public static HeaderDiffResult CompareFiles(string cueFilePath)
    {
        var backupPath = cueFilePath + ".bak";
        if (!File.Exists(backupPath))
        {
            return new HeaderDiffResult
            {
                CueFilePath = cueFilePath,
                BackupFilePath = backupPath,
                HasBackup = false
            };
        }

        var backupData = CueFileParser.Parse(backupPath);
        var currentData = CueFileParser.Parse(cueFilePath);

        var result = Compare(backupData, currentData);
        result.CueFilePath = cueFilePath;
        result.BackupFilePath = backupPath;
        result.HasBackup = true;
        return result;
    }

    public static HeaderDiffResult Compare(CueData? backupData, CueData? currentData)
    {
        var result = new HeaderDiffResult
        {
            HasBackup = backupData != null
        };

        var fields = new List<HeaderFieldDiff>();

        void CheckField(string fieldName, string? backupVal, string? currentVal)
        {
            string? b = string.IsNullOrWhiteSpace(backupVal) ? null : backupVal.Trim();
            string? c = string.IsNullOrWhiteSpace(currentVal) ? null : currentVal.Trim();

            DiffKind kind;
            if (b == null && c == null)
            {
                kind = DiffKind.Unchanged;
            }
            else if (b == null && c != null)
            {
                kind = DiffKind.Added;
            }
            else if (b != null && c == null)
            {
                kind = DiffKind.Removed;
            }
            else if (string.Equals(b, c, StringComparison.Ordinal))
            {
                kind = DiffKind.Unchanged;
            }
            else
            {
                kind = DiffKind.Modified;
            }

            fields.Add(new HeaderFieldDiff(fieldName, b ?? string.Empty, c ?? string.Empty, kind));
        }

        string FormatDiscs(CueData? data)
        {
            if (data == null) return string.Empty;
            if (data.DiscNumber.HasValue && data.Discs.HasValue && data.Discs > 1)
                return $"{data.DiscNumber} of {data.Discs}";
            return data.Discs.HasValue ? $"{data.Discs}" : string.Empty;
        }

        CheckField("Artist", backupData?.Artist, currentData?.Artist);
        CheckField("Album", backupData?.Album, currentData?.Album);
        CheckField("Genre", backupData?.Genre, currentData?.Genre);
        CheckField("Date", backupData?.Date, currentData?.Date);
        CheckField("Label", backupData?.Label, currentData?.Label);
        CheckField("Cat No", backupData?.CatalogNumber, currentData?.CatalogNumber);
        CheckField("Country", backupData?.Country, currentData?.Country);
        CheckField("Barcode", backupData?.Barcode, currentData?.Barcode);
        CheckField("Rel Date", backupData?.ReleaseDate, currentData?.ReleaseDate);
        CheckField("Discs", FormatDiscs(backupData), FormatDiscs(currentData));

        // Comment field: include if either has a comment
        if (!string.IsNullOrWhiteSpace(backupData?.Comment) || !string.IsNullOrWhiteSpace(currentData?.Comment))
        {
            CheckField("Comment", backupData?.Comment, currentData?.Comment);
        }

        result.Fields = fields;
        return result;
    }
}
