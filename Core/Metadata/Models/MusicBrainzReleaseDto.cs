namespace CueComplete.Core.Metadata.Models;

public class MusicBrainzReleaseDto
{
    public string? ReleaseId { get; set; }
    public string? Artist { get; set; }
    public string? Album { get; set; }
    public string? Barcode { get; set; }
    public string? Date { get; set; }
    public string? ReleaseDate { get; set; }
    public string? Country { get; set; }
    public int? Discs { get; set; }
    public int? Tracks { get; set; }
    public string? Label { get; set; }
    public string? CatalogNumber { get; set; }
    public string? Genre { get; set; }
    public string? DiscogsId { get; set; }

    public CueData ToCueData()
    {
        return new CueData
        {
            Source = "[MB]",
            Artist = Artist,
            Album = Album,
            Barcode = Barcode,
            Date = Date,
            ReleaseDate = ReleaseDate,
            Country = Country,
            Discs = Discs,
            Tracks = Tracks,
            Label = Label,
            CatalogNumber = CatalogNumber,
            Genre = Genre,
            DiscogsId = DiscogsId
        };
    }
}
