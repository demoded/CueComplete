using CueComplete.Core;
using Xunit;

namespace CueComplete.Tests;

public class ReleaseScorerTests
{
    [Fact]
    public void CalculateScore_MatchingTrackCount_Awards35Points()
    {
        var source = new CueData { Tracks = 10 };
        var candidateMatch = new CueData { Tracks = 10 };
        var candidateMismatch = new CueData { Tracks = 12 };

        int matchScore = ReleaseScorer.CalculateScore(candidateMatch, source);
        int mismatchScore = ReleaseScorer.CalculateScore(candidateMismatch, source);

        Assert.Equal(35, matchScore);
        Assert.Equal(0, mismatchScore);
    }

    [Fact]
    public void CalculateScore_BarcodeMatch_Awards35Points()
    {
        var source = new CueData { Barcode = "075596081428" };
        var candidateMatch = new CueData { Barcode = "0 7559-60814-2 8" };
        var candidateMismatch = new CueData { Barcode = "123456789012" };

        int matchScore = ReleaseScorer.CalculateScore(candidateMatch, source);
        int mismatchScore = ReleaseScorer.CalculateScore(candidateMismatch, source);

        Assert.Equal(35, matchScore);
        Assert.Equal(0, mismatchScore);
    }

    [Fact]
    public void CalculateScore_CatalogNumberMatch_Awards25ForExactAnd15ForPartial()
    {
        var source = new CueData { CatalogNumber = "CD 7559-60814-2" };
        var exactCandidate = new CueData { CatalogNumber = "CD7559608142" };
        var partialCandidate = new CueData { CatalogNumber = "60814-2" };
        var mismatchCandidate = new CueData { CatalogNumber = "XYZ-9999" };

        int exactScore = ReleaseScorer.CalculateScore(exactCandidate, source);
        int partialScore = ReleaseScorer.CalculateScore(partialCandidate, source);
        int mismatchScore = ReleaseScorer.CalculateScore(mismatchCandidate, source);

        Assert.Equal(25, exactScore);
        Assert.Equal(15, partialScore);
        Assert.Equal(0, mismatchScore);
    }

    [Fact]
    public void CalculateScore_CountryMatch_Awards15Points()
    {
        var source = new CueData { Country = "Japan" };
        var matchCandidate = new CueData { Country = "JP" };
        var mismatchCandidate = new CueData { Country = "Germany" };

        int matchScore = ReleaseScorer.CalculateScore(matchCandidate, source);
        int mismatchScore = ReleaseScorer.CalculateScore(mismatchCandidate, source);

        Assert.Equal(15, matchScore);
        Assert.Equal(0, mismatchScore);
    }

    [Fact]
    public void CalculateScore_YearMatch_Awards10Points()
    {
        var source = new CueData { Date = "1991" };
        var candidateMatch = new CueData { Date = "1991-09-24" };
        var candidateMismatch = new CueData { Date = "2011" };

        int matchScore = ReleaseScorer.CalculateScore(candidateMatch, source);
        int mismatchScore = ReleaseScorer.CalculateScore(candidateMismatch, source);

        Assert.Equal(10, matchScore);
        Assert.Equal(0, mismatchScore);
    }

    [Fact]
    public void CalculateScore_ArtistAndAlbumMatch_Awards10PointsEach()
    {
        var source = new CueData { Artist = "Nirvana", Album = "Nevermind" };
        var bothMatch = new CueData { Artist = "Nirvana", Album = "Nevermind" };
        var artistOnly = new CueData { Artist = "Nirvana", Album = "In Utero" };

        int bothScore = ReleaseScorer.CalculateScore(bothMatch, source);
        int artistScore = ReleaseScorer.CalculateScore(artistOnly, source);

        Assert.Equal(20, bothScore);
        Assert.Equal(10, artistScore);
    }

    [Fact]
    public void CalculateScore_DiscsAndDiscNumber_Awards5PointsEach()
    {
        var source = new CueData { Discs = 2, DiscNumber = 1 };
        var bothMatch = new CueData { Discs = 2, DiscNumber = 1 };
        var discNumberOnly = new CueData { Discs = 3, DiscNumber = 1 };

        int bothScore = ReleaseScorer.CalculateScore(bothMatch, source);
        int partialScore = ReleaseScorer.CalculateScore(discNumberOnly, source);

        Assert.Equal(10, bothScore);
        Assert.Equal(5, partialScore);
    }

    [Fact]
    public void ScoreAndOrderResults_PutsReleasesWithSameNumberOfTracksOnTop()
    {
        var source = new CueData
        {
            Artist = "Metallica",
            Album = "Master of Puppets",
            Tracks = 8
        };

        var release1 = new CueData
        {
            Artist = "Metallica",
            Album = "Master of Puppets (Deluxe)",
            Tracks = 30 // Deluxe box set
        };

        var release2 = new CueData
        {
            Artist = "Metallica",
            Album = "Master of Puppets",
            Tracks = 8 // Standard edition
        };

        var release3 = new CueData
        {
            Artist = "Metallica",
            Album = "Master of Puppets (Remastered)",
            Tracks = 8 // Another 8-track edition
        };

        var results = new List<CueData> { release1, release2, release3 };
        var ordered = MetadataService.ScoreAndOrderResults(results, source);

        // release2 and release3 should be ahead of release1 because of track count match (35 pts each)
        Assert.Equal(8, ordered[0].Tracks);
        Assert.Equal(8, ordered[1].Tracks);
        Assert.Equal(30, ordered[2].Tracks);
        Assert.Equal(release1, ordered[2]);
    }

    [Fact]
    public void ScoreAndOrderResults_WhenSourceTracksIsNull_PreservesRelativeOrder()
    {
        var source = new CueData
        {
            Artist = "Test Artist",
            Album = "Test Album"
        };

        var r1 = new CueData { Artist = "Test Artist", Album = "Test Album", Tracks = 10 };
        var r2 = new CueData { Artist = "Test Artist", Album = "Test Album", Tracks = 12 };

        var results = new List<CueData> { r1, r2 };
        var ordered = MetadataService.ScoreAndOrderResults(results, source);

        Assert.Equal(2, ordered.Count);
        Assert.Equal(r1, ordered[0]);
        Assert.Equal(r2, ordered[1]);
    }

    [Fact]
    public void ScoreAndOrderResults_HandlesEmptyAndNullGracefully()
    {
        var source = new CueData { Tracks = 5 };

        var emptyList = new List<CueData>();
        var orderedEmpty = MetadataService.ScoreAndOrderResults(emptyList, source);
        Assert.Empty(orderedEmpty);

        var nullSourceOrdered = MetadataService.ScoreAndOrderResults(new List<CueData> { new CueData() }, null!);
        Assert.Single(nullSourceOrdered);
    }
}
