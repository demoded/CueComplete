using System.Text;
using System.Text.RegularExpressions;

namespace CueComplete.Core;

public class CueData
{
    private string? _date;

    public string? Artist { get; set; }
    public string? Album { get; set; }
    public string? Genre { get; set; }
    public string? Date
    {
        get => _date;
        set => _date = SanitizeYear(value);
    }
    public string? Label { get; set; }
    public string? CatalogNumber { get; set; }
    public string? Country { get; set; }
    public string? Barcode { get; set; }
    public string? ReleaseDate { get; set; }
    public string? DiscId { get; set; }
    public string? MusicBrainzDiscId { get; set; }
    public string? DiscogsId { get; set; }
    public string? Source { get; set; }
    public int? Discs { get; set; }
    public int? DiscNumber { get; set; }
    public int? Tracks { get; set; }
    public string? Comment { get; set; }
    public List<string> OriginalLines { get; set; } = new();
    public Encoding OriginalEncoding { get; set; } = Encoding.UTF8;

    public static string? SanitizeYear(string? date)
    {
        if (string.IsNullOrWhiteSpace(date))
            return null;

        var trimmed = date.Trim().Trim('"');
        var match = Regex.Match(trimmed, @"\b(1[89]\d{2}|20\d{2})\b");
        if (match.Success)
        {
            return match.Groups[1].Value;
        }

        match = Regex.Match(trimmed, @"\b\d{4}\b");
        if (match.Success)
        {
            return match.Groups[1].Value;
        }

        return null;
    }
}
