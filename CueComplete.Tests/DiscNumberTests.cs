using CueComplete.Core;
using CueComplete.Core.Metadata;
using Xunit;

namespace CueComplete.Tests;

public class DiscNumberTests
{
    [Fact]
    public void CueFileParser_DoesNotParseCatalogNumberAsDiscNumber()
    {
        // Given a path with a catalog number like MASS CD 033 in bracketed metadata
        string fakeDir = Path.Combine(Path.GetTempPath(), "1994 - Blut [1994, Massacre, MASS CD 033, DE]");
        Directory.CreateDirectory(fakeDir);
        string fakeCue = Path.Combine(fakeDir, "Atrocity - Blut.cue");
        File.WriteAllText(fakeCue, "PERFORMER \"Atrocity\"\nTITLE \"Blut\"\nFILE \"test.flac\" WAVE\n  TRACK 01 AUDIO\n    INDEX 01 00:00:00\n");

        try
        {
            var parsed = CueFileParser.Parse(fakeCue);
            Assert.Equal("MASS CD 033", parsed.CatalogNumber);
            Assert.Null(parsed.DiscNumber);
        }
        finally
        {
            if (File.Exists(fakeCue)) File.Delete(fakeCue);
            if (Directory.Exists(fakeDir)) Directory.Delete(fakeDir);
        }
    }

    [Fact]
    public void CueFileParser_ParsesLegitimateDiscNumberFromFolderName()
    {
        string fakeDir = Path.Combine(Path.GetTempPath(), "Atrocity - Blut (CD 2) [1994, Massacre, MASS CD 033, DE]");
        Directory.CreateDirectory(fakeDir);
        string fakeCue = Path.Combine(fakeDir, "Atrocity - Blut.cue");
        File.WriteAllText(fakeCue, "PERFORMER \"Atrocity\"\nTITLE \"Blut\"\nFILE \"test.flac\" WAVE\n  TRACK 01 AUDIO\n    INDEX 01 00:00:00\n");

        try
        {
            var parsed = CueFileParser.Parse(fakeCue);
            Assert.Equal(2, parsed.DiscNumber);
        }
        finally
        {
            if (File.Exists(fakeCue)) File.Delete(fakeCue);
            if (Directory.Exists(fakeDir)) Directory.Delete(fakeDir);
        }
    }

    [Fact]
    public void CueFileWriter_EnforcesDiscNumberOneWhenTotalDiscsIsOne()
    {
        string tempFile = Path.GetTempFileName();
        try
        {
            var cueData = new CueData
            {
                Artist = "Atrocity",
                Album = "Blut",
                DiscNumber = 33, // Erroneous disc number
                Discs = 1,
                OriginalLines = new List<string>
                {
                    "PERFORMER \"Atrocity\"",
                    "TITLE \"Blut\"",
                    "FILE \"Atrocity - Blut.flac\" WAVE",
                    "  TRACK 01 AUDIO",
                    "    INDEX 01 00:00:00"
                }
            };

            CueFileWriter.Save(tempFile, cueData);

            var writtenLines = File.ReadAllLines(tempFile);
            Assert.Contains("REM DISCNUMBER \"1\"", writtenLines);
            Assert.Contains("REM TOTALDISCS \"1\"", writtenLines);
            Assert.DoesNotContain("REM DISCNUMBER \"33\"", writtenLines);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
            if (File.Exists(tempFile + ".bak")) File.Delete(tempFile + ".bak");
        }
    }
}
