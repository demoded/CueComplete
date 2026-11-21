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

    [Fact]
    public void MainWindow_ShowDiff_WhenNoFiles_DoesNotThrow()
    {
        Application.Init(new FakeDriver());
        try
        {
            var dummyFiles = new List<string>();
            var service = new MetadataService(null, null, null);
            var window = new MainWindow(dummyFiles, service);

            var ex = Record.Exception(() => window.ShowDiff());
            Assert.Null(ex);
        }
        finally
        {
            Application.Shutdown();
        }
    }

    [Fact]
    public void DiffDialog_InitializesWithFormattedText()
    {
        Application.Init(new FakeDriver());
        try
        {
            var diff = new HeaderDiffResult
            {
                CueFilePath = "album.cue",
                BackupFilePath = "album.cue.bak",
                Fields = new List<HeaderFieldDiff>
                {
                    new("Artist", "Old Artist", "New Artist", DiffKind.Modified)
                }
            };

            var dialog = new DiffDialog(diff);
            Assert.Contains("Old Artist", dialog.DiffTextView.Text.ToString());
            Assert.Contains("New Artist", dialog.DiffTextView.Text.ToString());
            Assert.Contains("[MODIFIED]", dialog.DiffTextView.Text.ToString());
        }
        finally
        {
            Application.Shutdown();
        }
    }

    [Fact]
    public void MainWindow_FoundCueDataPane_IsEditableAndAllowsTabbing()
    {
        Application.Init(new FakeDriver());
        try
        {
            var dummyFiles = new List<string> { "file1.cue" };
            var service = new MetadataService(null, null, null);
            var window = new MainWindow(dummyFiles, service);

            Assert.NotNull(window.FoundDetailsTextView);
            Assert.False(window.FoundDetailsTextView.ReadOnly);
            Assert.False(window.FoundDetailsTextView.AllowsTab);
        }
        finally
        {
            Application.Shutdown();
        }
    }

    [Fact]
    public void MainWindow_Navigation_SourceDetailsCannotFocus_FoundDetailsReceivesFocusAndCursorOnFirstLine()
    {
        Application.Init(new FakeDriver());
        try
        {
            var dummyFiles = new List<string> { "file1.cue" };
            var service = new MetadataService(null, null, null);
            var window = new MainWindow(dummyFiles, service)
            {
                Frame = new Rect(0, 0, 80, 24)
            };
            window.LayoutSubviews();

            // Source details must not be focusable
            Assert.False(window.SourceDetailsTextView.CanFocus);
            Assert.False(window.SourceCueDetailsPane.CanFocus);

            // Found details must be focusable
            Assert.True(window.FoundDetailsTextView.CanFocus);

            bool enterFired = false;
            window.FoundDetailsTextView.Enter += (e) => enterFired = true;

            window.FoundDetailsTextView.Text = "Artist: Megadeth\nAlbum: Rust in Peace\nDate: 1990";
            window.FoundDetailsTextView.SetFocus();

            Assert.True(enterFired, "Expected Enter event to fire on SetFocus");
            Assert.True(window.FoundDetailsTextView.HasFocus);
            Assert.Equal(0, window.FoundDetailsTextView.CursorPosition.Y);
            Assert.Equal(0, window.FoundDetailsTextView.CurrentRow);
            Assert.Equal("Artist: Megadeth".Length, window.FoundDetailsTextView.CursorPosition.X);
            Assert.Equal(0, window.FoundDetailsTextView.TopRow);
        }
        finally
        {
            Application.Shutdown();
        }
    }

    [Fact]
    public void MainWindow_TabNavigation_FlowsFromCueFilesToSearchResultsToFoundCueData()
    {
        Application.Init(new FakeDriver());
        try
        {
            var dummyFiles = new List<string> { "file1.cue" };
            var service = new MetadataService(null, null, null);
            var window = new MainWindow(dummyFiles, service)
            {
                Frame = new Rect(0, 0, 80, 24)
            };
            window.LayoutSubviews();

            // Initial focus should be on FileListView
            window.FileListView.SetFocus();
            Assert.True(window.FileListView.HasFocus);

            // Tab -> Search Results
            window.ProcessKey(new KeyEvent(Key.Tab, new KeyModifiers()));
            Assert.True(window.ResultsListView.HasFocus);

            // Tab -> skips Source Details -> Found Details
            window.ProcessKey(new KeyEvent(Key.Tab, new KeyModifiers()));
            Assert.False(window.SourceDetailsTextView.HasFocus);
            Assert.True(window.FoundDetailsTextView.HasFocus);
            Assert.Equal(0, window.FoundDetailsTextView.CursorPosition.Y);
            Assert.Equal(0, window.FoundDetailsTextView.CurrentRow);

            // Tab -> wraps back to FileListView
            window.ProcessKey(new KeyEvent(Key.Tab, new KeyModifiers()));
            Assert.True(window.FileListView.HasFocus);
        }
        finally
        {
            Application.Shutdown();
        }
    }

    [Fact]
    public void MainWindow_CueFilesPane_SelectedFileRendersYellow()
    {
        Application.Init(new FakeDriver());
        try
        {
            var dummyFiles = new List<string> { "album1.cue", "album2.cue", "album3.cue" };
            var service = new MetadataService(null, null, null);
            var window = new MainWindow(dummyFiles, service)
            {
                Frame = new Rect(0, 0, 80, 24)
            };
            window.LayoutSubviews();

            var rowRenderDelegate = (Action<ListViewRowEventArgs>?)typeof(ListView)
                .GetField("RowRender", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?
                .GetValue(window.FileListView);
            Assert.NotNull(rowRenderDelegate);

            // Row 0 is selected by default
            window.FileListView.SelectedItem = 0;

            // When FileListView is focused
            window.FileListView.SetFocus();
            Assert.True(window.FileListView.HasFocus);

            var row0Args = new ListViewRowEventArgs(0);
            rowRenderDelegate(row0Args);
            var row0AttrFocused = row0Args.RowAttribute;

            var row1Args = new ListViewRowEventArgs(1);
            rowRenderDelegate(row1Args);
            var row1AttrFocused = row1Args.RowAttribute;

            Assert.NotNull(row0AttrFocused);
            Assert.Equal(Color.BrightYellow, row0AttrFocused.Value.Foreground);
            Assert.Null(row1AttrFocused);

            // When FileListView loses focus (e.g. user moved focus to FoundDetailsTextView)
            window.FoundDetailsTextView.SetFocus();
            Assert.False(window.FileListView.HasFocus);

            var row0ArgsUnfocused = new ListViewRowEventArgs(0);
            rowRenderDelegate(row0ArgsUnfocused);

            Assert.NotNull(row0ArgsUnfocused.RowAttribute);
            Assert.Equal(Color.BrightYellow, row0ArgsUnfocused.RowAttribute.Value.Foreground);
        }
        finally
        {
            Application.Shutdown();
        }
    }

    [Fact]
    public void MainWindow_SearchResultsPane_SelectedRowRendersYellow()
    {
        Application.Init(new FakeDriver());
        try
        {
            var dummyFiles = new List<string> { "album.cue" };
            var service = new MetadataService(null, null, null);
            var window = new MainWindow(dummyFiles, service)
            {
                Frame = new Rect(0, 0, 80, 24)
            };
            window.LayoutSubviews();

            // Populate search results list
            window.ResultsListView.SetSource(new List<string> { "Result 1", "Result 2" });
            window.ResultsListView.SelectedItem = 0;

            var rowRenderDelegate = (Action<ListViewRowEventArgs>?)typeof(ListView)
                .GetField("RowRender", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?
                .GetValue(window.ResultsListView);
            Assert.NotNull(rowRenderDelegate);

            // When ResultsListView is focused
            window.ResultsListView.SetFocus();
            Assert.True(window.ResultsListView.HasFocus);

            var row0Args = new ListViewRowEventArgs(0);
            rowRenderDelegate(row0Args);
            Assert.NotNull(row0Args.RowAttribute);
            Assert.Equal(Color.BrightYellow, row0Args.RowAttribute.Value.Foreground);

            var row1Args = new ListViewRowEventArgs(1);
            rowRenderDelegate(row1Args);
            Assert.Null(row1Args.RowAttribute);

            // When ResultsListView loses focus (e.g. user moved focus to FoundDetailsTextView)
            window.FoundDetailsTextView.SetFocus();
            Assert.False(window.ResultsListView.HasFocus);

            var row0ArgsUnfocused = new ListViewRowEventArgs(0);
            rowRenderDelegate(row0ArgsUnfocused);
            Assert.NotNull(row0ArgsUnfocused.RowAttribute);
            Assert.Equal(Color.BrightYellow, row0ArgsUnfocused.RowAttribute.Value.Foreground);
        }
        finally
        {
            Application.Shutdown();
        }
    }

    [Fact]
    public void MainWindow_ApplyFoundCueData_SavesEditedDataToFile()
    {
        Application.Init(new FakeDriver());
        string tempFile = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid():N}.cue");
        try
        {
            File.WriteAllLines(tempFile, new[]
            {
                "PERFORMER \"Original Artist\"",
                "TITLE \"Original Album\"",
                "FILE \"audio.wav\" WAVE",
                "  TRACK 01 AUDIO",
                "    INDEX 01 00:00:00"
            });

            var dummyFiles = new List<string> { tempFile };
            var service = new MetadataService(null, null, null);
            var window = new MainWindow(dummyFiles, service);

            window.FoundDetailsTextView.Text =
                "Artist: Edited Artist\n" +
                "Album: Edited Album\n" +
                "Genre: Heavy Metal\n" +
                "Date: 1984\n" +
                "Label: EMI\n" +
                "Cat No: CDP 7 46045 2\n" +
                "Country: UK\n" +
                "Barcode: 5099974604528\n" +
                "Rel Date: 1984-09-03\n" +
                "Discs: 1 of 2  Tracks: 1\n" +
                "Comment: Edited Comment";

            window.ApplyFoundCueData();

            var savedData = CueFileParser.Parse(tempFile);
            Assert.Equal("Edited Artist", savedData.Artist);
            Assert.Equal("Edited Album", savedData.Album);
            Assert.Equal("Heavy Metal", savedData.Genre);
            Assert.Equal("1984", savedData.Date);
            Assert.Equal("EMI", savedData.Label);
            Assert.Equal("CDP 7 46045 2", savedData.CatalogNumber);
            Assert.Equal("UK", savedData.Country);
            Assert.Equal("5099974604528", savedData.Barcode);
            Assert.Equal("1984-09-03", savedData.ReleaseDate);
            Assert.Equal(1, savedData.DiscNumber);
            Assert.Equal(2, savedData.Discs);
            Assert.Equal("Edited Comment", savedData.Comment);
        }
        finally
        {
            Application.Shutdown();
            if (File.Exists(tempFile)) File.Delete(tempFile);
            if (File.Exists(tempFile + ".bak")) File.Delete(tempFile + ".bak");
        }
    }

    [Fact]
    public void MainWindow_ResetFoundCueDataPreview_RevertsEdits()
    {
        Application.Init(new FakeDriver());
        try
        {
            var dummyFiles = new List<string> { "file1.cue" };
            var service = new MetadataService(null, null, null);
            var window = new MainWindow(dummyFiles, service);

            // Seed search results via reflection or property
            var searchResultsField = typeof(MainWindow).GetField("_searchResults", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var results = new List<CueData>
            {
                new() { Artist = "Result Artist", Album = "Result Album", Date = "1995" }
            };
            searchResultsField!.SetValue(window, results);

            window.ResultsListView.SetSource(new List<string> { "Result 1" });
            window.ResultsListView.SelectedItem = 0;
            window.ResetFoundCueDataPreview();

            Assert.Contains("Result Artist", window.FoundDetailsTextView.Text.ToString());

            // User edits the text
            window.FoundDetailsTextView.Text = "Artist: User Custom Artist\nAlbum: Result Album\nDate: 1995";
            Assert.Contains("User Custom Artist", window.FoundDetailsTextView.Text.ToString());

            // Revert via Esc / ResetFoundCueDataPreview
            window.ResetFoundCueDataPreview();
            Assert.Contains("Result Artist", window.FoundDetailsTextView.Text.ToString());
            Assert.DoesNotContain("User Custom Artist", window.FoundDetailsTextView.Text.ToString());
        }
        finally
        {
            Application.Shutdown();
        }
    }

    [Fact]
    public void MainWindow_RootKeyEvent_DoesNotInterceptShortcuts_WhenFoundDetailsHasFocus()
    {
        Application.Init(new FakeDriver());
        try
        {
            var dummyFiles = new List<string> { "file1.cue" };
            var service = new MetadataService(null, null, null);
            var window = new MainWindow(dummyFiles, service);

            window.FoundDetailsTextView.SetFocus();
            Assert.True(window.FoundDetailsTextView.HasFocus);

            // Simulate pressing 's' key
            var keyEventS = new KeyEvent(Key.S, new KeyModifiers());
            bool handled = Application.RootKeyEvent?.Invoke(keyEventS) ?? false;

            // When FoundDetailsTextView has focus, 's' must NOT be handled as Deep Search shortcut
            Assert.False(handled);

            // Simulate pressing 'd' key
            var keyEventD = new KeyEvent(Key.D, new KeyModifiers());
            handled = Application.RootKeyEvent?.Invoke(keyEventD) ?? false;

            // When FoundDetailsTextView has focus, 'd' must NOT be handled as Diff shortcut
            Assert.False(handled);
        }
        finally
        {
            Application.Shutdown();
        }
    }
}
