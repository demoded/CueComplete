using CueComplete.Core;
using CueComplete.UI;
using Terminal.Gui;
using Xunit;

namespace CueComplete.Tests;

public class MainWindowTests
{
    [Fact]
    public void MainWindow_LeftPaneTitle_ReflectsFileCounter()
    {
        Application.Init(new FakeDriver());
        try
        {
            var dummyFiles = new List<string> { "file1.cue", "file2.cue", "file3.cue" };
            var service = new MetadataService(null, null, null);
            var window = new MainWindow(dummyFiles, service);

            Assert.Equal("Cue Files [1/3]", window.LeftPaneTitle);

            window.FileListView.SelectedItem = 1;
            Assert.Equal("Cue Files [2/3]", window.LeftPaneTitle);

            window.FileListView.SelectedItem = 2;
            Assert.Equal("Cue Files [3/3]", window.LeftPaneTitle);
        }
        finally
        {
            Application.Shutdown();
        }
    }

    [Fact]
    public void MainWindow_LeftPaneTitle_ZeroPaddedTwoDigits()
    {
        Application.Init(new FakeDriver());
        try
        {
            var dummyFiles = Enumerable.Range(1, 99).Select(i => $"file_{i}.cue").ToList();
            var service = new MetadataService(null, null, null);
            var window = new MainWindow(dummyFiles, service);

            Assert.Equal("Cue Files [01/99]", window.LeftPaneTitle);

            window.FileListView.SelectedItem = 49;
            Assert.Equal("Cue Files [50/99]", window.LeftPaneTitle);

            window.FileListView.SelectedItem = 98;
            Assert.Equal("Cue Files [99/99]", window.LeftPaneTitle);
        }
        finally
        {
            Application.Shutdown();
        }
    }

    [Fact]
    public void MainWindow_LeftPaneTitle_EmptyFilesList_ShowsZeroOfZero()
    {
        Application.Init(new FakeDriver());
        try
        {
            var dummyFiles = new List<string>();
            var service = new MetadataService(null, null, null);
            var window = new MainWindow(dummyFiles, service);

            Assert.Equal("Cue Files [0/0]", window.LeftPaneTitle);
        }
        finally
        {
            Application.Shutdown();
        }
    }

    [Fact]
    public void MainWindow_SourcePath_UpdatesWithSelectedFile()
    {
        Application.Init(new FakeDriver());
        try
        {
            var dummyFiles = new List<string> { @"C:\Music\Album1\album.cue", @"C:\Music\Album2\album.cue" };
            var service = new MetadataService(null, null, null);
            var window = new MainWindow(dummyFiles, service);

            Assert.Equal(@"C:\Music\Album1\album.cue", window.SourcePath);

            window.FileListView.SelectedItem = 1;
            window.FileListView.SetFocus();
            Assert.Equal(@"C:\Music\Album2\album.cue", window.SourcePath);
        }
        finally
        {
            Application.Shutdown();
        }
    }

    [Fact]
    public void MainWindow_LayoutStructure_ContainsExpectedPanesAndTitles()
    {
        Application.Init(new FakeDriver());
        try
        {
            var dummyFiles = new List<string> { "file1.cue" };
            var service = new MetadataService(null, null, null);
            var window = new MainWindow(dummyFiles, service);

            Assert.Equal("Cue Files [1/1]", window.CueFilesPaneTitle);
            Assert.Equal("Search Results", window.SearchResultsPaneTitle);
            Assert.Equal("Source cue details", window.SourceCueDetailsPaneTitle);
            Assert.Equal("Found cue data", window.FoundCueDataPaneTitle);
            Assert.Equal("file1.cue", window.SourcePath);
            Assert.NotNull(window.FileListView);
            Assert.NotNull(window.ResultsListView);
            Assert.NotNull(window.CueFilesPane);
            Assert.NotNull(window.SearchResultsPane);
            Assert.NotNull(window.SourceCueDetailsPane);
            Assert.NotNull(window.FoundCueDataPane);
            Assert.NotNull(window.SourcePathLabel);
        }
        finally
        {
            Application.Shutdown();
        }
    }

    [Fact]
    public void MainWindow_LayoutDimensions_BottomSectionLockedTo20LinesAndTopFlexible()
    {
        Application.Init(new FakeDriver());
        try
        {
            var dummyFiles = new List<string> { "file1.cue" };
            var service = new MetadataService(null, null, null);
            var window = new MainWindow(dummyFiles, service);

            // Verify bottom panes have Height = 20
            Assert.Equal((Dim)20, window.SourceCueDetailsPane.Height);
            Assert.Equal((Dim)20, window.FoundCueDataPane.Height);

            // Verify source path label has Height = 1
            Assert.Equal((Dim)1, window.SourcePathLabel.Height);

            // Verify top panes have flexible height filling up to 21 lines reserved for bottom and path
            Assert.Equal(Dim.Fill(21), window.CueFilesPane.Height);
            Assert.Equal(Dim.Fill(21), window.SearchResultsPane.Height);
        }
        finally
        {
            Application.Shutdown();
        }
    }
}
