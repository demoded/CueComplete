using System.IO;
using CueComplete.Core;
using Xunit;

namespace CueComplete.Tests;

public class DateValidationTests
{
    [Theory]
    [InlineData("1988", "1988")]
    [InlineData("1988-09", "1988")]
    [InlineData("1988-09-15", "1988")]
    [InlineData("1988/09/15", "1988")]
    [InlineData("\"1988\"", "1988")]
    [InlineData("\"1988-09\"", "1988")]
    [InlineData("September 1988", "1988")]
    [InlineData("2024", "2024")]
    [InlineData("2024-12-31", "2024")]
    [InlineData("1899", "1899")]
    [InlineData(null, null)]
    [InlineData("", null)]
    [InlineData("   ", null)]
    [InlineData("invalid", null)]
    [InlineData("123", null)]
    [InlineData("12345", null)]
    public void Test_SanitizeYear(string? input, string? expected)
    {
        var result = CueData.SanitizeYear(input);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Test_CueData_DateSetter_SanitizesToYearOnly()
    {
        var data = new CueData();
        data.Date = "1988-09";
        Assert.Equal("1988", data.Date);

        data.Date = "2001-11-20";
        Assert.Equal("2001", data.Date);

        data.Date = "1995";
        Assert.Equal("1995", data.Date);

        data.Date = "invalid";
        Assert.Null(data.Date);
    }

    [Fact]
    public void Test_CueFileWriter_WritesOnlyYearInRemDate()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            var cueData = new CueData
            {
                Artist = "Destiny",
                Album = "Atomic Winter",
                Date = "1988-09",
                ReleaseDate = "1988-09",
                OriginalLines = new List<string>
                {
                    "FILE \"Destiny - Atomic Winter.flac\" WAVE",
                    "  TRACK 01 AUDIO",
                    "    INDEX 01 00:00:00"
                }
            };

            CueFileWriter.Save(tempFile, cueData);

            var lines = File.ReadAllLines(tempFile);
            Assert.Contains(lines, l => l == "REM DATE \"1988\"");
            Assert.DoesNotContain(lines, l => l.Contains("1988-09") && l.StartsWith("REM DATE"));
            Assert.Contains(lines, l => l == "REM RELEASEDATE \"1988-09\"");
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
            if (File.Exists(tempFile + ".bak")) File.Delete(tempFile + ".bak");
        }
    }

    [Fact]
    public void Test_CueFileWriter_FallbackToReleaseDateForYearIfDateNull()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            var cueData = new CueData
            {
                Artist = "Destiny",
                Album = "Atomic Winter",
                ReleaseDate = "1988-09",
                OriginalLines = new List<string>
                {
                    "FILE \"Destiny - Atomic Winter.flac\" WAVE",
                    "  TRACK 01 AUDIO",
                    "    INDEX 01 00:00:00"
                }
            };

            CueFileWriter.Save(tempFile, cueData);

            var lines = File.ReadAllLines(tempFile);
            Assert.Contains(lines, l => l == "REM DATE \"1988\"");
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
            if (File.Exists(tempFile + ".bak")) File.Delete(tempFile + ".bak");
        }
    }

    [Fact]
    public void Test_CueFileParser_ParsesAndSanitizesRemDate()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllLines(tempFile, new[]
            {
                "REM DATE \"1988-09\"",
                "REM RELEASEDATE \"1988-09\"",
                "PERFORMER \"Destiny\"",
                "TITLE \"Atomic Winter\"",
                "FILE \"test.flac\" WAVE",
                "  TRACK 01 AUDIO",
                "    INDEX 01 00:00:00"
            });

            var cueData = CueFileParser.Parse(tempFile);
            Assert.Equal("1988", cueData.Date);
            Assert.Equal("1988-09", cueData.ReleaseDate);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }
}
