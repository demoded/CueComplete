using Terminal.Gui;
using CueComplete.Core;
using System.IO;

namespace CueComplete.UI;

public class MainWindow : Window
{
    private readonly MetadataService _metadataService;
    private readonly List<string> _cueFiles;
    
    private FrameView _cueFilesPane;
    private FrameView _searchResultsPane;
    private FrameView _sourceCueDetailsPane;
    private FrameView _foundCueDataPane;
    private ListView _fileListView;
    private ListView _resultsListView;
    private Label _sourcePathLabel;
    private TextView _sourceDetailsTextView;
    private TextView _foundDetailsTextView;

    public string LeftPaneTitle => _cueFilesPane?.Title?.ToString() ?? string.Empty;
    public string CueFilesPaneTitle => LeftPaneTitle;
    public string SearchResultsPaneTitle => _searchResultsPane?.Title?.ToString() ?? string.Empty;
    public string SourceCueDetailsPaneTitle => _sourceCueDetailsPane?.Title?.ToString() ?? string.Empty;
    public string FoundCueDataPaneTitle => _foundCueDataPane?.Title?.ToString() ?? string.Empty;
    public ListView FileListView => _fileListView;
    public ListView ResultsListView => _resultsListView;
    public string SourcePath => _sourcePathLabel?.Text?.ToString() ?? string.Empty;
    public string SourceDetailsText => _sourceDetailsTextView?.Text?.ToString() ?? string.Empty;
    public string FoundDetailsText => _foundDetailsTextView?.Text?.ToString() ?? string.Empty;
    
    private CueData? _currentCueData;
    private List<CueData> _searchResults = new();
    
    public MainWindow(List<string> cueFiles, MetadataService metadataService)
    {
        Title = "CueComplete";
        _metadataService = metadataService;
        _cueFiles = cueFiles;

        _cueFilesPane = new FrameView("Cue Files")
        {
            X = 0,
            Y = 0,
            Width = Dim.Percent(20),
            Height = 9
        };

        var displayFiles = _cueFiles.Select(f => {
            var dir = Path.GetDirectoryName(f);
            return string.IsNullOrEmpty(dir) ? Path.GetFileName(f) : Path.GetFileName(dir);
        }).ToList();

        _fileListView = new ListView(displayFiles)
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            AllowsMarking = false
        };
        _fileListView.OpenSelectedItem += FileListView_OpenSelectedItem;
        _fileListView.SelectedItemChanged += (e) => {
            UpdateLeftPaneTitle();
            if (_fileListView.HasFocus) UpdateFilePreview();
        };
        _fileListView.Enter += (e) => {
            UpdateLeftPaneTitle();
            UpdateFilePreview();
        };
        _cueFilesPane.Add(_fileListView);

        UpdateLeftPaneTitle();

        _searchResultsPane = new FrameView("Search Results")
        {
            X = Pos.Right(_cueFilesPane),
            Y = 0,
            Width = Dim.Fill(),
            Height = 9
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
        _searchResultsPane.Add(_resultsListView);

        _sourcePathLabel = new Label(string.Empty)
        {
            X = 0,
            Y = Pos.Bottom(_cueFilesPane),
            Width = Dim.Fill(),
            Height = 1
        };

        _sourceCueDetailsPane = new FrameView("Source cue details")
        {
            X = 0,
            Y = Pos.Bottom(_sourcePathLabel),
            Width = Dim.Percent(50),
            Height = Dim.Fill()
        };

        _sourceDetailsTextView = new TextView()
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            ReadOnly = true,
            WordWrap = true
        };
        _sourceCueDetailsPane.Add(_sourceDetailsTextView);

        _foundCueDataPane = new FrameView("Found cue data")
        {
            X = Pos.Right(_sourceCueDetailsPane),
            Y = Pos.Bottom(_sourcePathLabel),
            Width = Dim.Fill(),
            Height = Dim.Fill()
        };

        _foundDetailsTextView = new TextView()
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            ReadOnly = true,
            WordWrap = true
        };
        _foundCueDataPane.Add(_foundDetailsTextView);

        var statusBar = new StatusBar(new StatusItem[] {
            new StatusItem(Key.CtrlMask | Key.Q, "~^Q~ Quit", () => Application.RequestStop()),
            new StatusItem(Key.Null, "~Enter~ Apply", () => {
                if (_resultsListView.HasFocus && _resultsListView.SelectedItem >= 0)
                {
                    ResultsListView_OpenSelectedItem(new ListViewItemEventArgs(_resultsListView.SelectedItem, null));
                }
                else if (_fileListView.HasFocus && _fileListView.SelectedItem >= 0 && _fileListView.SelectedItem < _cueFiles.Count)
                {
                    LoadCueFile(_cueFiles[_fileListView.SelectedItem]);
                }
            }),
            new StatusItem(Key.Null, "~S~ Deep Search", () => {
                if (_fileListView.SelectedItem >= 0 && _fileListView.SelectedItem < _cueFiles.Count)
                {
                    LoadCueFile(_cueFiles[_fileListView.SelectedItem], deepSearch: true);
                }
            }),
            new StatusItem(Key.CtrlMask | Key.L, "~^L~ Toggle Log", () => ToggleLogging())
        });

        Application.RootKeyEvent += (e) => 
        {
            if (e.Key == (Key)'s' || e.Key == (Key)'S' || e.Key == Key.S)
            {
                if (_fileListView.SelectedItem >= 0 && _fileListView.SelectedItem < _cueFiles.Count)
                {
                    LoadCueFile(_cueFiles[_fileListView.SelectedItem], deepSearch: true);
                    return true; // Handled
                }
            }
            if (e.Key == (Key.CtrlMask | Key.L))
            {
                ToggleLogging();
                return true;
            }
            return false;
        };

        Add(_cueFilesPane, _searchResultsPane, _sourcePathLabel, _sourceCueDetailsPane, _foundCueDataPane, statusBar);

        if (_cueFiles.Count > 0)
        {
            UpdateFilePreview();
        }
    }

    private void ToggleLogging()
    {
        MetadataService.IsLoggingEnabled = !MetadataService.IsLoggingEnabled;
        var status = MetadataService.IsLoggingEnabled ? "enabled" : "disabled";
        MessageBox.Query("Logging", $"Application logging is now {status}.", "OK");
    }

    private void FileListView_OpenSelectedItem(ListViewItemEventArgs obj)
    {
        if (obj.Item >= 0 && obj.Item < _cueFiles.Count)
        {
            LoadCueFile(_cueFiles[obj.Item]);
        }
    }

    private static string FormatCueData(CueData? data)
    {
        if (data == null) return string.Empty;

        string discsStr = data.DiscNumber.HasValue && data.Discs.HasValue && data.Discs > 1
            ? $"{data.DiscNumber} of {data.Discs}"
            : $"{data.Discs}";

        return $"Artist: {data.Artist}\n" +
            $"Album: {data.Album}\n" +
            $"Genre: {data.Genre}\n" +
            $"Date: {data.Date}\n" +
            $"Label: {data.Label}\n" +
            $"Cat No: {data.CatalogNumber}\n" +
            $"Country: {data.Country}\n" +
            $"Barcode: {data.Barcode}\n" +
            $"Rel Date: {data.ReleaseDate}\n" +
            $"Discs: {discsStr}  Tracks: {data.Tracks}" +
            (!string.IsNullOrWhiteSpace(data.Comment) ? $"\nComment: {data.Comment}" : "");
    }

    private void UpdateLeftPaneTitle()
    {
        int total = _cueFiles.Count;
        int digits = Math.Max(1, total.ToString().Length);
        int current = 0;
        if (total > 0)
        {
            if (_fileListView != null && _fileListView.SelectedItem >= 0)
            {
                current = Math.Min(_fileListView.SelectedItem + 1, total);
            }
            else
            {
                current = 1;
            }
        }
        string currentStr = current.ToString($"D{digits}");
        string totalStr = total.ToString($"D{digits}");
        _cueFilesPane.Title = $"Cue Files [{currentStr}/{totalStr}]";
        _cueFilesPane.SetNeedsDisplay();
    }

    private void UpdateFilePreview()
    {
        UpdateLeftPaneTitle();
        if (_fileListView.SelectedItem >= 0 && _fileListView.SelectedItem < _cueFiles.Count)
        {
            var filePath = _cueFiles[_fileListView.SelectedItem];
            _sourcePathLabel.Text = filePath;
            try {
                var previewData = CueFileParser.Parse(filePath);
                _sourceDetailsTextView.Text = FormatCueData(previewData);
                _foundDetailsTextView.Text = string.Empty;
                
                _searchResults.Clear();
                _resultsListView.SetSource(_searchResults);
            } catch {
                _sourceDetailsTextView.Text = string.Empty;
                _foundDetailsTextView.Text = string.Empty;
            }
        }
        else
        {
            _sourcePathLabel.Text = string.Empty;
            _sourceDetailsTextView.Text = string.Empty;
            _foundDetailsTextView.Text = string.Empty;
        }
    }

    private void UpdateResultPreview()
    {
        if (_searchResults != null && _resultsListView.SelectedItem >= 0 && _resultsListView.SelectedItem < _searchResults.Count)
        {
            var data = _searchResults[_resultsListView.SelectedItem];
            _foundDetailsTextView.Text = FormatCueData(data);
        }
        else
        {
            _foundDetailsTextView.Text = string.Empty;
        }
    }

    private void LoadCueFile(string filePath, bool deepSearch = false)
    {
        _fileListView.SetFocus();
        UpdateLeftPaneTitle();
        _sourcePathLabel.Text = filePath;

        try
        {
            _currentCueData = CueFileParser.Parse(filePath);
        }
        catch (Exception ex)
        {
            _metadataService.Log($"Failed to parse CUE file '{filePath}': {ex.Message}");
            MessageBox.ErrorQuery("Parse Error", $"Failed to parse CUE file:\n{ex.Message}", "OK");
            return;
        }
        
        _sourceDetailsTextView.Text = FormatCueData(_currentCueData);
        _foundDetailsTextView.Text = string.Empty;
            
        _searchResults.Clear();
        _resultsListView.SetSource(_searchResults);
        
        var dialog = new Dialog("Searching", 50, 10);
        
        var progressLabel = new Label("Initializing...")
        {
            X = Pos.Center(),
            Y = Pos.Center()
        };
        dialog.Add(progressLabel);

        Action<string> onLogHandler = (msg) => 
        {
            try
            {
                Application.MainLoop?.Invoke(() => 
                {
                    try
                    {
                        progressLabel.Text = msg.Length > 45 ? msg.Substring(0, 42) + "..." : msg;
                        progressLabel.SetNeedsDisplay();
                    }
                    catch
                    {
                        // Ignore UI update errors if dialog is closing
                    }
                });
            }
            catch
            {
                // MainLoop might be shutting down
            }
        };

        _metadataService.OnLog += onLogHandler;
        
        Task.Run(async () => 
        {
            List<CueData> results = new();
            try
            {
                results = await _metadataService.SearchReleasesAsync(_currentCueData, deepSearch);
            }
            catch (Exception ex)
            {
                _metadataService.Log($"Search failed with unexpected error: {ex}");
            }
            finally
            {
                try
                {
                    Application.MainLoop?.Invoke(() => 
                    {
                        try
                        {
                            _metadataService.OnLog -= onLogHandler;
                            _searchResults = results;
                            var displayList = _searchResults.Select(r => 
                            {
                                string extra = "";
                                if (r.Discs.HasValue || r.Tracks.HasValue)
                                {
                                    var d = r.DiscNumber.HasValue && r.Discs.HasValue ? $"[CD {r.DiscNumber} of {r.Discs}]" : r.Discs.HasValue ? $"{r.Discs}xCD" : "";
                                    var t = r.Tracks.HasValue ? $"{r.Tracks} Tracks" : "";
                                    extra = " - " + string.Join(", ", new[] { d, t }.Where(s => !string.IsNullOrEmpty(s)));
                                }
                                return $"{r.Source} {r.Artist} - {r.Album} [{r.Date}] [{r.CatalogNumber}] [{r.Barcode}]{extra}";
                            }).ToList();
                            
                            _resultsListView.SetSource(displayList);

                            if (_searchResults.Count > 0)
                            {
                                _resultsListView.SelectedItem = 0;
                                UpdateResultPreview();
                                _resultsListView.SetFocus();
                            }
                            else
                            {
                                _foundDetailsTextView.Text = "(No results found)";
                            }
                        }
                        catch (Exception ex)
                        {
                            _metadataService.Log($"Error updating search results UI: {ex}");
                        }
                        finally
                        {
                            Application.RequestStop(dialog);
                        }
                    });
                }
                catch (Exception ex)
                {
                    _metadataService.Log($"Error dispatching dialog stop to MainLoop: {ex}");
                }
            }
        });

        Application.Run(dialog, ex =>
        {
            _metadataService.Log($"Search dialog error: {ex}");
            return true;
        });
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
            _currentCueData.Comment = selectedResult.Comment ?? _currentCueData.Comment;
            _currentCueData.DiscNumber = selectedResult.DiscNumber ?? _currentCueData.DiscNumber;
            _currentCueData.Discs = selectedResult.Discs ?? _currentCueData.Discs;

            if (_currentCueData.Discs.HasValue)
            {
                if (_currentCueData.Discs.Value == 1)
                {
                    _currentCueData.DiscNumber = 1;
                }
                else if (_currentCueData.DiscNumber.HasValue && _currentCueData.DiscNumber.Value > _currentCueData.Discs.Value)
                {
                    _currentCueData.DiscNumber = 1;
                }
            }

            try
            {
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
                    _sourceDetailsTextView.Text = FormatCueData(_currentCueData);
                    _foundDetailsTextView.Text = string.Empty;
                    MessageBox.Query("Done", "All files processed.", "OK");
                }
            }
            catch (Exception ex)
            {
                _metadataService.Log($"Failed to save CUE file: {ex}");
                MessageBox.ErrorQuery("Save Error", $"Failed to save CUE file:\n{ex.Message}", "OK");
            }
        }
    }
}
