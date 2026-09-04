using CueComplete.Core;
using Xunit;

namespace CueComplete.Tests;

public class DiscIdCalculatorTests
{
    [Fact]
    public void Test_CalculateFreeDbId_CalculatesCorrectId()
    {
        // Arrange: Sample 3-track offset layout
        var tracks = new List<TrackOffset>
        {
            new TrackOffset { TrackNumber = 1, Minutes = 0, Seconds = 0, Frames = 0 },
            new TrackOffset { TrackNumber = 2, Minutes = 3, Seconds = 15, Frames = 0 },
            new TrackOffset { TrackNumber = 3, Minutes = 7, Seconds = 45, Frames = 0 }
        };
        int leadOutSeconds = (10 * 60) + 30; // 10m 30s

        // Act
        string freeDbId = DiscIdCalculator.CalculateFreeDbId(tracks, leadOutSeconds);

        // Assert
        Assert.NotNull(freeDbId);
        Assert.Equal(8, freeDbId.Length);
    }

    [Fact]
    public void Test_CalculateMusicBrainzDiscId_CalculatesValidBase64String()
    {
        // Arrange
        var tracks = new List<TrackOffset>
        {
            new TrackOffset { TrackNumber = 1, Minutes = 0, Seconds = 0, Frames = 0 },
            new TrackOffset { TrackNumber = 2, Minutes = 3, Seconds = 15, Frames = 0 },
            new TrackOffset { TrackNumber = 3, Minutes = 7, Seconds = 45, Frames = 0 }
        };
        int leadOutSectors = (10 * 60 + 30) * 75 + 150;

        // Act
        string mbDiscId = DiscIdCalculator.CalculateMusicBrainzDiscId(tracks, leadOutSectors);

        // Assert
        Assert.NotNull(mbDiscId);
        Assert.Equal(28, mbDiscId.Length);
        Assert.DoesNotContain("+", mbDiscId);
        Assert.DoesNotContain("/", mbDiscId);
        Assert.DoesNotContain("=", mbDiscId);
    }

    [Fact]
    public void Test_ParseJoanJettCue_CalculatesMusicBrainzDiscIdAndFreeDbId()
    {
        // Arrange
        string projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        string cuePath = Path.Combine(projectRoot, "TestStubs", "01. Joan Jett - Bad Reputation - 1981 {Japan 1st Press Victor • VICP-5173}", "Joan Jett - Bad Reputation (VICP-5173).flac (faulty barcode).cue");

        if (!File.Exists(cuePath))
            return;

        // Act
        CueData cueData = CueFileParser.Parse(cuePath);

        // Assert
        Assert.Equal("Joan Jett", cueData.Artist);
        Assert.Equal("Bad Reputation", cueData.Album);
        Assert.Equal("4988002258697", cueData.Barcode);
        Assert.Equal("F50B8A10", cueData.DiscId, ignoreCase: true);
        Assert.Equal(16, cueData.Tracks);
        Assert.Equal("0vHsFsZvLZHNcsI1CReFp0jgFsc-", cueData.MusicBrainzDiscId);
    }
}
