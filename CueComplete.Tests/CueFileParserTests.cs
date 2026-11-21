using CueComplete.Core;
using CueComplete.UI;
using Xunit;

namespace CueComplete.Tests;

public class CueFileParserTests
{
    [Fact]
    public void ParseFormattedText_ValidFullMetadata_ParsesAllFieldsCorrectly()
    {
        string text =
            "Artist: Iron Maiden\n" +
            "Album: Powerslave\n" +
            "Genre: Heavy Metal\n" +
            "Date: 1984\n" +
            "Label: EMI\n" +
            "Cat No: CDP 7 46045 2\n" +
            "Country: UK\n" +
            "Barcode: 5099974604528\n" +
            "Rel Date: 1984-09-03\n" +
            "Discs: 1 of 2  Tracks: 8\n" +
            "Comment: ExactAudioCopy v1.0";

        var cue = CueFileParser.ParseFormattedText(text);

        Assert.Equal("Iron Maiden", cue.Artist);
        Assert.Equal("Powerslave", cue.Album);
        Assert.Equal("Heavy Metal", cue.Genre);
        Assert.Equal("1984", cue.Date);
        Assert.Equal("EMI", cue.Label);
        Assert.Equal("CDP 7 46045 2", cue.CatalogNumber);
        Assert.Equal("UK", cue.Country);
        Assert.Equal("5099974604528", cue.Barcode);
        Assert.Equal("1984-09-03", cue.ReleaseDate);
        Assert.Equal(1, cue.DiscNumber);
        Assert.Equal(2, cue.Discs);
        Assert.Equal(8, cue.Tracks);
        Assert.Equal("ExactAudioCopy v1.0", cue.Comment);
    }

    [Theory]
    [InlineData("Discs: 1 of 2  Tracks: 8", 1, 2, 8)]
    [InlineData("Discs: 2 of 3  Tracks: 14", 2, 3, 14)]
    [InlineData("Discs: 1  Tracks: 10", 1, 1, 10)]
    [InlineData("Discs: 2  Tracks: 12", null, 2, 12)]
    [InlineData("Discs: 3 of 4", 3, 4, null)]
    [InlineData("Discs:   Tracks: 15", null, null, 15)]
    public void ParseFormattedText_DiscsAndTracksFormats_ParsesCorrectly(
        string line, int? expectedDiscNum, int? expectedTotalDiscs, int? expectedTracks)
    {
        var cue = CueFileParser.ParseFormattedText(line);

        Assert.Equal(expectedDiscNum, cue.DiscNumber);
        Assert.Equal(expectedTotalDiscs, cue.Discs);
        Assert.Equal(expectedTracks, cue.Tracks);
    }

    [Fact]
    public void ParseFormattedText_AlternateKeys_ParsesCorrectly()
    {
        string text =
            "CatNo: CAT-123\n" +
            "Release Date: 1999-12-31\n" +
            "Disc: 2 of 2\n" +
            "Track: 9";

        var cue = CueFileParser.ParseFormattedText(text);

        Assert.Equal("CAT-123", cue.CatalogNumber);
        Assert.Equal("1999-12-31", cue.ReleaseDate);
        Assert.Equal(2, cue.DiscNumber);
        Assert.Equal(2, cue.Discs);
        Assert.Equal(9, cue.Tracks);
    }

    [Fact]
    public void ParseFormattedText_EmptyAndWhitespace_HandlesSafely()
    {
        var cueEmpty = CueFileParser.ParseFormattedText(string.Empty);
        Assert.Null(cueEmpty.Artist);
        Assert.Null(cueEmpty.Album);

        var cueBlankLines = CueFileParser.ParseFormattedText("\n\n(No results found)\n\n");
        Assert.Null(cueBlankLines.Artist);
        Assert.Null(cueBlankLines.Album);

        string textWithEmptyValues =
            "Artist: \n" +
            "Album:    \n" +
            "Date:\n" +
            "Comment: ";

        var cueBlankFields = CueFileParser.ParseFormattedText(textWithEmptyValues);
        Assert.Null(cueBlankFields.Artist);
        Assert.Null(cueBlankFields.Album);
        Assert.Null(cueBlankFields.Date);
        Assert.Null(cueBlankFields.Comment);
    }

    [Fact]
    public void ParseFormattedText_PreservesTemplateMetadata()
    {
        var template = new CueData
        {
            Artist = "Old Artist",
            DiscId = "discid123",
            MusicBrainzDiscId = "mbid456",
            DiscogsId = "discogs789",
            Source = "Discogs",
            OriginalLines = new List<string> { "FILE \"test.wav\" WAVE", "  TRACK 01 AUDIO" }
        };

        string text = "Artist: New Artist\nAlbum: New Album";
        var result = CueFileParser.ParseFormattedText(text, template);

        Assert.Equal("New Artist", result.Artist);
        Assert.Equal("New Album", result.Album);
        Assert.Equal("discid123", result.DiscId);
        Assert.Equal("mbid456", result.MusicBrainzDiscId);
        Assert.Equal("discogs789", result.DiscogsId);
        Assert.Equal("Discogs", result.Source);
        Assert.Equal(2, result.OriginalLines.Count);
    }

    [Fact]
    public void ParseFormattedText_RoundTrip_WithFormatCueData()
    {
        var original = new CueData
        {
            Artist = "Judas Priest",
            Album = "Painkiller",
            Genre = "Heavy Metal",
            Date = "1990",
            Label = "Columbia",
            CatalogNumber = "CK 46891",
            Country = "US",
            Barcode = "07464468912",
            ReleaseDate = "1990-09-03",
            DiscNumber = 1,
            Discs = 1,
            Tracks = 10,
            Comment = "Mastered by Audio"
        };

        string formatted = MainWindow.FormatCueData(original);
        var parsed = CueFileParser.ParseFormattedText(formatted);

        Assert.Equal(original.Artist, parsed.Artist);
        Assert.Equal(original.Album, parsed.Album);
        Assert.Equal(original.Genre, parsed.Genre);
        Assert.Equal(original.Date, parsed.Date);
        Assert.Equal(original.Label, parsed.Label);
        Assert.Equal(original.CatalogNumber, parsed.CatalogNumber);
        Assert.Equal(original.Country, parsed.Country);
        Assert.Equal(original.Barcode, parsed.Barcode);
        Assert.Equal(original.ReleaseDate, parsed.ReleaseDate);
        Assert.Equal(original.DiscNumber, parsed.DiscNumber);
        Assert.Equal(original.Discs, parsed.Discs);
        Assert.Equal(original.Tracks, parsed.Tracks);
        Assert.Equal(original.Comment, parsed.Comment);
    }
}
