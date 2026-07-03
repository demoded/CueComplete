namespace CueComplete.Core;

public class CueData
{
    public string? Artist { get; set; }
    public string? Album { get; set; }
    public string? Genre { get; set; }
    public string? Date { get; set; }
    public string? Label { get; set; }
    public string? CatalogNumber { get; set; }
    public string? Country { get; set; }
    public string? Barcode { get; set; }
    public string? ReleaseDate { get; set; }
    public string? DiscId { get; set; }
    public List<string> OriginalLines { get; set; } = new();
}
