using CueComplete.Core;
using CueComplete.Core.Metadata;
using Xunit;

namespace CueComplete.Tests;

public class MetadataValidationTests
{
    [Fact]
    public void DummyCatalogBarcode_IsRejectedByIsValidBarcodeCandidate()
    {
        Assert.False(DiscogsProvider.IsValidBarcodeCandidate("0000000000000"));
        Assert.False(DiscogsProvider.IsValidBarcodeCandidate("0000 0000 0000"));
        Assert.False(DiscogsProvider.IsValidBarcodeCandidate("0-0000-0000-0"));
        Assert.True(DiscogsProvider.IsValidBarcodeCandidate("016861245320"));
    }

    [Fact]
    public void CueParser_DoesNotAssignBarcode_WhenCatalogIsAllZeros()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempFile, """
            REM GENRE "Thrash Metal"
            REM DATE 1988
            PERFORMER "Flotsam & Jetsam"
            TITLE "Saturday Night's All Right For Fighting"
            CATALOG 0000000000000
            FILE "Flotsam & Jetsam - Saturday Night's All Right For Fighting.flac" WAVE
              TRACK 01 AUDIO
                TITLE "Saturday Night's All Right For Fighting"
                INDEX 01 00:00:00
              TRACK 02 AUDIO
                TITLE "Hard On You (Live)"
                INDEX 01 04:02:00
              TRACK 03 AUDIO
                TITLE "Misguided Fortune (Live)"
                INDEX 01 08:55:00
              TRACK 04 AUDIO
                TITLE "Dreams Of Death (Live)"
                INDEX 01 14:26:00
            """);

            var data = CueFileParser.Parse(tempFile);
            Assert.Null(data.Barcode);
            Assert.Equal(4, data.Tracks);
            Assert.Equal("Flotsam & Jetsam", data.Artist);
            Assert.Equal("Saturday Night's All Right For Fighting", data.Album);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public void ResultOrdering_PrioritizesCatalogNumberAndTrackCountMatch()
    {
        var list = new List<CueData>
        {
            new CueData
            {
                Album = "Saturday Night's All Right For Fighting",
                Artist = "Flotsam and Jetsam",
                CatalogNumber = "RR 2453 1", // Vinyl single
                Tracks = 4
            },
            new CueData
            {
                Album = "Saturday Night's All Right For Fighting",
                Artist = "Flotsam and Jetsam",
                CatalogNumber = "RR 2453 2", // CD single matching "2453 2"
                Tracks = 4
            }
        };

        var sourceData = new CueData
        {
            Artist = "Flotsam & Jetsam",
            Album = "Saturday Night's All Right For Fighting",
            CatalogNumber = "2453 2",
            Tracks = 4
        };

        var targetCatNo = sourceData.CatalogNumber?.Replace(" ", "").ToLowerInvariant();
        var ordered = list.OrderByDescending(d =>
        {
            int score = 0;
            if (!string.IsNullOrEmpty(targetCatNo) && !string.IsNullOrWhiteSpace(d.CatalogNumber))
            {
                var cleanCat = d.CatalogNumber.Replace(" ", "").ToLowerInvariant();
                if (cleanCat.Contains(targetCatNo) || targetCatNo.Contains(cleanCat))
                    score += 20;
            }
            if (sourceData.Tracks.HasValue && sourceData.Tracks.Value > 0 && d.Tracks == sourceData.Tracks.Value)
            {
                score += 10;
            }
            return score;
        }).ToList();

        Assert.Equal("RR 2453 2", ordered[0].CatalogNumber);
    }
}
