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

            Assert.Equal("CUE Files [1\\3]", window.LeftPaneTitle);

            window.FileListView.SelectedItem = 1;
            Assert.Equal("CUE Files [2\\3]", window.LeftPaneTitle);

            window.FileListView.SelectedItem = 2;
            Assert.Equal("CUE Files [3\\3]", window.LeftPaneTitle);
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

            Assert.Equal("CUE Files [0\\0]", window.LeftPaneTitle);
        }
        finally
        {
            Application.Shutdown();
        }
    }
}
