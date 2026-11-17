using CueComplete.Core;
using CueComplete.UI;
using Xunit;

namespace CueComplete.Tests;

public class HeaderDiffServiceTests
{
    [Fact]
    public void Compare_AllHeaderFieldsIdentical_ReturnsZeroChanges()
    {
        var backup = new CueData
        {
            Artist = "Test Artist",
            Album = "Test Album",
            Genre = "Heavy Metal",
            Date = "1986",
            Label = "Roadrunner",
            CatalogNumber = "RR 9786",
            Country = "US",
            Barcode = "016861978628",
            ReleaseDate = "1986-10-15",
            DiscNumber = 1,
            Discs = 1,
            Comment = "Original release"
        };

        var current = new CueData
        {
            Artist = "Test Artist",
            Album = "Test Album",
            Genre = "Heavy Metal",
            Date = "1986",
            Label = "Roadrunner",
            CatalogNumber = "RR 9786",
            Country = "US",
            Barcode = "016861978628",
            ReleaseDate = "1986-10-15",
            DiscNumber = 1,
            Discs = 1,
            Comment = "Original release"
        };

        var diff = HeaderDiffService.Compare(backup, current);

        Assert.Equal(0, diff.TotalChanges);
        Assert.False(diff.HasChanges);
        Assert.All(diff.Fields, f => Assert.Equal(DiffKind.Unchanged, f.Kind));
    }

    [Fact]
    public void Compare_TrackInformationDifferences_AreIgnored()
    {
        var backup = new CueData
        {
            Artist = "Test Artist",
            Album = "Test Album",
            Date = "1990",
            Tracks = 5
        };

        var current = new CueData
        {
            Artist = "Test Artist",
            Album = "Test Album",
            Date = "1990",
            Tracks = 14
        };

        var diff = HeaderDiffService.Compare(backup, current);

        Assert.Equal(0, diff.TotalChanges);
        Assert.False(diff.HasChanges);
    }

    [Fact]
    public void Compare_ModifiedField_IdentifiesModifiedKind()
    {
        var backup = new CueData
        {
            Artist = "Old Artist",
            Album = "Album Name"
        };

        var current = new CueData
        {
            Artist = "New Artist",
            Album = "Album Name"
        };

        var diff = HeaderDiffService.Compare(backup, current);

        Assert.Equal(1, diff.TotalChanges);
        var artistDiff = diff.Fields.First(f => f.FieldName == "Artist");
        Assert.Equal(DiffKind.Modified, artistDiff.Kind);
        Assert.Equal("Old Artist", artistDiff.BackupValue);
        Assert.Equal("New Artist", artistDiff.CurrentValue);
    }

    [Fact]
    public void Compare_AddedField_IdentifiesAddedKind()
    {
        var backup = new CueData
        {
            Artist = "Artist",
            Album = "Album",
            Barcode = null
        };

        var current = new CueData
        {
            Artist = "Artist",
            Album = "Album",
            Barcode = "012345678901"
        };

        var diff = HeaderDiffService.Compare(backup, current);

        Assert.Equal(1, diff.TotalChanges);
        var barcodeDiff = diff.Fields.First(f => f.FieldName == "Barcode");
        Assert.Equal(DiffKind.Added, barcodeDiff.Kind);
        Assert.Equal(string.Empty, barcodeDiff.BackupValue);
        Assert.Equal("012345678901", barcodeDiff.CurrentValue);
    }

    [Fact]
    public void Compare_RemovedField_IdentifiesRemovedKind()
    {
        var backup = new CueData
        {
            Artist = "Artist",
            Album = "Album",
            Label = "Epic Records"
        };

        var current = new CueData
        {
            Artist = "Artist",
            Album = "Album",
            Label = null
        };

        var diff = HeaderDiffService.Compare(backup, current);

        Assert.Equal(1, diff.TotalChanges);
        var labelDiff = diff.Fields.First(f => f.FieldName == "Label");
        Assert.Equal(DiffKind.Removed, labelDiff.Kind);
        Assert.Equal("Epic Records", labelDiff.BackupValue);
        Assert.Equal(string.Empty, labelDiff.CurrentValue);
    }

    [Fact]
    public void Compare_DiscsFormatting_MultiDisc()
    {
        var backup = new CueData
        {
            DiscNumber = 1,
            Discs = 1
        };

        var current = new CueData
        {
            DiscNumber = 1,
            Discs = 2
        };

        var diff = HeaderDiffService.Compare(backup, current);

        var discDiff = diff.Fields.First(f => f.FieldName == "Discs");
        Assert.Equal(DiffKind.Modified, discDiff.Kind);
        Assert.Equal("1", discDiff.BackupValue);
        Assert.Equal("1 of 2", discDiff.CurrentValue);
    }

    [Fact]
    public void Compare_WhitespaceAndNull_TreatedAsUnchanged()
    {
        var backup = new CueData
        {
            Genre = "   "
        };

        var current = new CueData
        {
            Genre = null
        };

        var diff = HeaderDiffService.Compare(backup, current);

        var genreDiff = diff.Fields.First(f => f.FieldName == "Genre");
        Assert.Equal(DiffKind.Unchanged, genreDiff.Kind);
    }

    [Fact]
    public void CompareFiles_MissingBackup_ReturnsHasBackupFalse()
    {
        var tempCue = Path.GetTempFileName() + ".cue";
        try
        {
            File.WriteAllText(tempCue, "TITLE \"Test\"\n");
            var result = HeaderDiffService.CompareFiles(tempCue);

            Assert.False(result.HasBackup);
            Assert.Empty(result.Fields);
        }
        finally
        {
            if (File.Exists(tempCue)) File.Delete(tempCue);
        }
    }

    [Fact]
    public void CompareFiles_ExistingBackup_ParsesAndCompares()
    {
        var tempCue = Path.GetTempFileName() + ".cue";
        var tempBak = tempCue + ".bak";
        try
        {
            File.WriteAllText(tempBak, "REM GENRE \"Rock\"\nPERFORMER \"Old Artist\"\nTITLE \"Album\"\n");
            File.WriteAllText(tempCue, "REM GENRE \"Hard Rock\"\nPERFORMER \"New Artist\"\nTITLE \"Album\"\n");

            var result = HeaderDiffService.CompareFiles(tempCue);

            Assert.True(result.HasBackup);
            Assert.True(result.HasChanges);
            Assert.Equal(2, result.TotalChanges);

            var artistDiff = result.Fields.First(f => f.FieldName == "Artist");
            Assert.Equal(DiffKind.Modified, artistDiff.Kind);
            Assert.Equal("Old Artist", artistDiff.BackupValue);
            Assert.Equal("New Artist", artistDiff.CurrentValue);

            var genreDiff = result.Fields.First(f => f.FieldName == "Genre");
            Assert.Equal(DiffKind.Modified, genreDiff.Kind);
            Assert.Equal("Rock", genreDiff.BackupValue);
            Assert.Equal("Hard Rock", genreDiff.CurrentValue);
        }
        finally
        {
            if (File.Exists(tempCue)) File.Delete(tempCue);
            if (File.Exists(tempBak)) File.Delete(tempBak);
        }
    }

    [Fact]
    public void FormatDiffText_ContainsExpectedHeadersAndStatus()
    {
        var diff = new HeaderDiffResult
        {
            CueFilePath = "test.cue",
            BackupFilePath = "test.cue.bak",
            Fields = new List<HeaderFieldDiff>
            {
                new("Artist", "Old", "New", DiffKind.Modified),
                new("Album", "Same", "Same", DiffKind.Unchanged),
                new("Barcode", "", "12345", DiffKind.Added)
            }
        };

        var text = DiffDialog.FormatDiffText(diff);

        Assert.Contains("Field", text);
        Assert.Contains("Backup (.bak)", text);
        Assert.Contains("Current (.cue)", text);
        Assert.Contains("[MODIFIED]", text);
        Assert.Contains("[ADDED]", text);
        Assert.Contains("Total differences: 2 field(s) changed.", text);
    }
}
