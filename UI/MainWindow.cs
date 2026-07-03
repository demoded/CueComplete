using Terminal.Gui;
using CueComplete.Core;
using System.IO;

namespace CueComplete.UI;

public class MainWindow : Window
{
    private readonly MetadataService _metadataService;
    private readonly List<string> _cueFiles;
    
    private ListView _fileListView;
    private TextView _detailsTextView;
    private ListView _resultsListView;
    
    private CueData? _currentCueData;
    private List<CueData> _searchResults = new();
    
    public MainWindow(List<string> cueFiles, MetadataService metadataService)
    {
        Title = "CueComplete";
        _metadataService = metadataService;
        _cueFiles = cueFiles;

        var leftPane = new FrameView("CUE Files")
        {
            X = 0,
            Y = 0,
            Width = Dim.Percent(30),
            Height = Dim.Fill()
        };

        _fileListView = new ListView(_cueFiles)
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            AllowsMarking = false
        };
        _fileListView.OpenSelectedItem += FileListView_OpenSelectedItem;
        _fileListView.SelectedItemChanged += (e) => { if (_fileListView.HasFocus) UpdateFilePreview(); };
        _fileListView.Enter += (e) => UpdateFilePreview();
        leftPane.Add(_fileListView);

        var rightPaneTop = new FrameView("Current Details")
        {
            X = Pos.Right(leftPane),
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Percent(40) + 3
        };
        
        _detailsTextView = new TextView()
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            ReadOnly = true,
            WordWrap = true
        };
        rightPaneTop.Add(_detailsTextView);

        var rightPaneBottom = new FrameView("Search Results (Press Enter to Apply)")
        {
            X = Pos.Right(leftPane),
            Y = Pos.Bottom(rightPaneTop),
            Width = Dim.Fill(),
            Height = Dim.Fill()
        };
        
        _resultsListView = new ListView()
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill()
        };
        _resultsListView.OpenSelectedItem += ResultsListView_OpenSelectedItem;
        _resultsListView.SelectedItemChanged += (e) => { if (_resultsListView.HasFocus) UpdateResultPreview(); };
        _resultsListView.Enter += (e) => UpdateResultPreview();
        rightPaneBottom.Add(_resultsListView);

        var statusBar = new StatusBar(new StatusItem[] {
            new StatusItem(Key.CtrlMask | Key.Q, "~^Q~ Quit", () => Application.RequestStop())
        });

        Add(leftPane, rightPaneTop, rightPaneBottom, statusBar);

        if (_cueFiles.Count > 0)
        {
            LoadCueFile(_cueFiles[0]);
        }
    }

    private void FileListView_OpenSelectedItem(ListViewItemEventArgs obj)
    {
        if (obj.Value is string filePath)
        {
            LoadCueFile(filePath);
        }
    }

    private void UpdateDetailsView(string title, CueData data)
    {
        _detailsTextView.Text = $"{title}\n\n" +
            $"Artist: {data.Artist}\n" +
            $"Album: {data.Album}\n" +
            $"Genre: {data.Genre}\n" +
            $"Date: {data.Date}\n" +
            $"Label: {data.Label}\n" +
            $"Cat No: {data.CatalogNumber}\n" +
            $"Country: {data.Country}\n" +
            $"Barcode: {data.Barcode}\n" +
            $"Rel Date: {data.ReleaseDate}";
    }

    private void UpdateFilePreview()
    {
        if (_fileListView.SelectedItem >= 0 && _fileListView.SelectedItem < _cueFiles.Count)
        {
            var filePath = _cueFiles[_fileListView.SelectedItem];
            try {
                var previewData = CueFileParser.Parse(filePath);
                UpdateDetailsView($"File: {Path.GetFileName(filePath)}", previewData);
            } catch {}
        }
    }

    private void UpdateResultPreview()
    {
        if (_searchResults != null && _resultsListView.SelectedItem >= 0 && _resultsListView.SelectedItem < _searchResults.Count)
        {
            var data = _searchResults[_resultsListView.SelectedItem];
            UpdateDetailsView("Search Result Preview:", data);
        }
    }

    private void LoadCueFile(string filePath)
    {
        _currentCueData = CueFileParser.Parse(filePath);
        
        UpdateDetailsView($"File: {Path.GetFileName(filePath)}", _currentCueData);
            
            
        _searchResults.Clear();
        _resultsListView.SetSource(_searchResults);
        
        var dialog = new Dialog("Searching", 50, 10);
        
        Task.Run(async () => 
        {
            var results = await _metadataService.SearchReleasesAsync(_currentCueData);
            
            Application.MainLoop.Invoke(() => 
            {
                _searchResults = results;
                var displayList = _searchResults.Select(r => 
                    $"{r.Artist} - {r.Album} [{r.Date}] [{r.CatalogNumber}] [{r.Barcode}]"
                ).ToList();
                
                _resultsListView.SetSource(displayList);
                Application.RequestStop(dialog);
            });
        });

        Application.Run(dialog);
    }

    private void ResultsListView_OpenSelectedItem(ListViewItemEventArgs obj)
    {
        if (_searchResults.Count > obj.Item && _currentCueData != null)
        {
            var selectedResult = _searchResults[obj.Item];
            
            // Merge Data (Use selected, fallback to original if selected doesn't have it)
            _currentCueData.Artist = selectedResult.Artist ?? _currentCueData.Artist;
            _currentCueData.Album = selectedResult.Album ?? _currentCueData.Album;
            _currentCueData.Genre = selectedResult.Genre ?? _currentCueData.Genre;
            _currentCueData.Date = selectedResult.Date ?? _currentCueData.Date;
            _currentCueData.Label = selectedResult.Label ?? _currentCueData.Label;
            _currentCueData.CatalogNumber = selectedResult.CatalogNumber ?? _currentCueData.CatalogNumber;
            _currentCueData.Country = selectedResult.Country ?? _currentCueData.Country;
            _currentCueData.Barcode = selectedResult.Barcode ?? _currentCueData.Barcode;
            _currentCueData.ReleaseDate = selectedResult.ReleaseDate ?? _currentCueData.ReleaseDate;

            var filePath = _cueFiles[_fileListView.SelectedItem];
            CueFileWriter.Save(filePath, _currentCueData);

            int nextIndex = _fileListView.SelectedItem + 1;
            if (nextIndex < _cueFiles.Count)
            {
                _fileListView.SelectedItem = nextIndex;
                LoadCueFile(_cueFiles[nextIndex]);
            }
            else
            {
                MessageBox.Query("Done", "All files processed.", "OK");
            }
        }
    }
}
