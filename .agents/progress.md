# Progress Log

## Task: Split `Core/MetadataService.cs` into Logical Submodules

### [2026-08-09 07:28] Initial Review & Planning
- Analyzed `Core/MetadataService.cs` (~730 lines) and identified separate responsibilities (MusicBrainz API calls, FreeDB reverse lookup, Discogs API calls & parsing, search orchestration, logging).
- Drafted initial modularization plan in [`.agents/plan 20260809T0900.md`](file:///D:/git/CueComplete/.agents/plan%2020260809T0900.md).

### [2026-08-09 07:33] Architecture & Requirements Refinement
- Updated plan with requirements for strongly-typed DTO models (`DiscogsSearchResponse`, `DiscogsReleaseResponse`, `DiscogsMasterResponse`, `MusicBrainzReleaseDto`, `FreeDbLookupResult`) to replace manual `JsonDocument` parsing and regex string extractions.
- Standardized provider submodules under `Core/Metadata/` namespace (`IMetadataProvider.cs`, `MusicBrainzProvider.cs`, `DiscogsProvider.cs`, `MetadataService.cs`).

### [2026-08-09 07:36] Rules & Guidelines Update
- Updated [`.agents/AGENTS.md`](file:///D:/git/CueComplete/.agents/AGENTS.md) to add the mandatory rule for logging all changes step by step into progress log.

### [2026-08-09 07:37] Core Abstractions & Interfaces
- Created [`Core/Metadata/IMetadataProvider.cs`](file:///D:/git/CueComplete/Core/Metadata/IMetadataProvider.cs) defining the common `IMetadataProvider` interface contract.

### [2026-08-09 07:38] Strongly-Typed Response DTO Models
- Created response DTO models in [`Core/Metadata/Models/`](file:///D:/git/CueComplete/Core/Metadata/Models/):
  - [`DiscogsSearchResponse.cs`](file:///D:/git/CueComplete/Core/Metadata/Models/DiscogsSearchResponse.cs)
  - [`DiscogsReleaseResponse.cs`](file:///D:/git/CueComplete/Core/Metadata/Models/DiscogsReleaseResponse.cs)
  - [`DiscogsMasterResponse.cs`](file:///D:/git/CueComplete/Core/Metadata/Models/DiscogsMasterResponse.cs)
  - [`MusicBrainzReleaseDto.cs`](file:///D:/git/CueComplete/Core/Metadata/Models/MusicBrainzReleaseDto.cs)
  - [`FreeDbLookupResult.cs`](file:///D:/git/CueComplete/Core/Metadata/Models/FreeDbLookupResult.cs)

### [2026-08-09 07:38] Extracted `MusicBrainzProvider`
- Created [`Core/Metadata/MusicBrainzProvider.cs`](file:///D:/git/CueComplete/Core/Metadata/MusicBrainzProvider.cs) implementing `IMetadataProvider`.
- Encapsulated MusicBrainz client queries, FreeDB lookup, and DTO mapping into `MusicBrainzProvider`.

### [2026-08-09 07:38] Extracted `DiscogsProvider`
- Created [`Core/Metadata/DiscogsProvider.cs`](file:///D:/git/CueComplete/Core/Metadata/DiscogsProvider.cs) implementing `IMetadataProvider`.
- Replaced manual `JsonDocument` parsing with `System.Text.Json.JsonSerializer.Deserialize<T>()` using DTO models.
- Encapsulated Discogs REST API authentication, release lookup, text search, master year resolution, tracklist multi-disc analysis, and string cleaning.

### [2026-08-09 07:38] Refactored `MetadataService`
- Refactored [`Core/MetadataService.cs`](file:///D:/git/CueComplete/Core/MetadataService.cs) to delegate provider queries to `MusicBrainzProvider` and `DiscogsProvider`.
- Preserved top-level orchestration, fast search, logging, and public constructor signature.
- Reduced `MetadataService.cs` size from ~730 lines to ~140 lines.

### [2026-08-09 08:01] Build & Verification
- Ran `dotnet build` successfully with 0 errors and zero CS8604 nullability warnings in metadata files.
- Executed `dotnet publish -c Release -r win-x64 -p:PublishSingleFile=true --self-contained true` successfully to build single executable binary.

### [2026-08-09 10:31] Updated AGENTS.md Guidelines
- Updated [`.agents/AGENTS.md`](file:///D:/git/CueComplete/.agents/AGENTS.md) to enforce storing plan files (`.agents/plan YYYYMMDDTHHMM.md`) and progress log (`.agents/progress.md`) in `.agents/`.
- Converted all progress log entries to datetime-based headers.

### [2026-08-09 12:14] Fix Barcode Extraction in DiscogsProvider
- Fixed [`Core/Metadata/DiscogsProvider.cs`](file:///D:/git/CueComplete/Core/Metadata/DiscogsProvider.cs) to ensure barcodes are extracted specifically from release identifiers with `Type == "Barcode"`.
- Added `IsValidBarcodeCandidate` validation helper to filter out matrix, runout, SID, and rights society (JASRAC) strings.
- Updated `FetchAndApplyDiscogsDataAsync` to always apply valid `Type == "Barcode"` identifiers from direct release objects.

### [2026-08-09 12:24] Integration & Unit Tests for Barcode Extraction
- Created xUnit test project [`CueComplete.Tests/CueComplete.Tests.csproj`](file:///D:/git/CueComplete/CueComplete.Tests/CueComplete.Tests.csproj) referenced in [`CueComplete.slnx`](file:///D:/git/CueComplete/CueComplete.slnx).
- Added [`CueComplete.Tests/BarcodeExtractionTests.cs`](file:///D:/git/CueComplete/CueComplete.Tests/BarcodeExtractionTests.cs) covering:
  - `IsValidBarcodeCandidate` theory tests for valid EAN/UPC barcodes vs matrix, runout, SID, and JASRAC strings.
  - `FetchAndApplyDiscogsDataAsync` integration test verifying that `Identifier.type == "Barcode"` overrides matrix/runout entries.
  - `PerformDiscogsTextSearchAsync` integration test verifying filtering of matrix/runout strings in search result candidate arrays.
- All 14 tests passing cleanly via `dotnet test`.

### [2026-08-09 14:23] Minor Release v1.1.0
- Updated version in [`CueComplete.csproj`](file:///D:/git/CueComplete/CueComplete.csproj) to `1.1.0`.
- Created and pushed git tag `v1.1.0` to trigger automated GitHub Actions release workflow.

### [2026-08-09 14:33] Fix GitHub Actions Release Workflow Build Failure
- Added `<IsPublishable>false</IsPublishable>` and `<PublishSingleFile>false</PublishSingleFile>` to [`CueComplete.Tests/CueComplete.Tests.csproj`](file:///D:/git/CueComplete/CueComplete.Tests/CueComplete.Tests.csproj) to prevent test project single-file publish errors (`NETSDK1098`).
- Updated [`.github/workflows/release.yml`](file:///D:/git/CueComplete/.github/workflows/release.yml) to explicitly target `CueComplete.csproj` during `dotnet publish`.
- Re-tagged `v1.1.0` and pushed tag to GitHub.

### [2026-08-09 21:10] Format Discs and Tracks in One Line
- Updated [`UI/MainWindow.cs`](file:///D:/git/CueComplete/UI/MainWindow.cs) in `UpdateDetailsView` to render `Discs` and `Tracks` on a single line (`Discs: {data.Discs}  Tracks: {data.Tracks}`).

### [2026-08-09 21:26] Support MusicBrainz Disc ID Calculation and Search Lookup
- Added [`Core/DiscIdCalculator.cs`](file:///D:/git/CueComplete/Core/DiscIdCalculator.cs) to calculate 28-character Base64 SHA-1 MusicBrainz Disc ID and 8-character hex FreeDB ID from CUE sheet track offsets.
- Added `MusicBrainzDiscId` property to [`Core/Models.cs`](file:///D:/git/CueComplete/Core/Models.cs).
- Updated [`Core/CueFileParser.cs`](file:///D:/git/CueComplete/Core/CueFileParser.cs) to parse `INDEX 01` track offsets and `REM MUSICBRAINZ_DISCID` header tags.
- Updated [`Core/Metadata/MusicBrainzProvider.cs`](file:///D:/git/CueComplete/Core/Metadata/MusicBrainzProvider.cs) to perform direct MusicBrainz DiscID lookup via `LookupDiscIdAsync` alongside FreeDB lookups.
- Added unit test `Test_ParseJoanJettCue_CalculatesMusicBrainzDiscIdAndFreeDbId` in [`CueComplete.Tests/DiscIdCalculatorTests.cs`](file:///D:/git/CueComplete/CueComplete.Tests/DiscIdCalculatorTests.cs) asserting exact expected MusicBrainz Disc ID (`0vHsFsZvLZHNcsI1CReFp0jgFsc-`) and FreeDB ID (`F50B8A10`) for `Joan Jett - Bad Reputation (VICP-5173).flac (faulty barcode).cue` (17/17 tests passing).

### [2026-08-09 21:42] Fallback Search Query for MusicBrainz DiscID Index
- Added fallback Lucene query (`discid:"<discid>" OR cdtoc:"<discid>"`) via `_mbClient.FindReleasesAsync` in [`Core/Metadata/MusicBrainzProvider.cs`](file:///D:/git/CueComplete/Core/Metadata/MusicBrainzProvider.cs) when direct `/discid/{discid}` API lookup yields no direct matches.

### [2026-08-09 21:51] Fix False Positive Releases in DiscID Search
- Added Lucene special character escaping (`-`, `/`, etc.) to DiscID search query strings in [`Core/Metadata/MusicBrainzProvider.cs`](file:///D:/git/CueComplete/Core/Metadata/MusicBrainzProvider.cs).
- Applied artist verification filtering to candidate results from DiscID searches, preventing false positive matches (such as "Stefan Wolf" audio dramas).

### [2026-08-09 21:55] Add Artist Verification Filtering to Discogs Results
- Added candidate artist verification filtering to `PerformDiscogsTextSearchAsync` and `SearchAsync` in [`Core/Metadata/DiscogsProvider.cs`](file:///D:/git/CueComplete/Core/Metadata/DiscogsProvider.cs) to eliminate false positive releases (such as "Daevid Allen" when searching for "Joan Jett").

### [2026-08-09 21:58] Remove FreeDB ID and MB DiscID from Current Details View
- Updated [`UI/MainWindow.cs`](file:///D:/git/CueComplete/UI/MainWindow.cs) in `UpdateDetailsView` to remove `FreeDB ID` and `MB DiscID` text fields from the UI pane.

### [2026-08-09 22:02] Minor Release v1.2.0
- Updated version in [`CueComplete.csproj`](file:///D:/git/CueComplete/CueComplete.csproj) to `1.2.0`.
- Merged `feature/ui-single-line-discs-tracks` into `master`.
- Created and pushed git tag `v1.2.0` to trigger automated GitHub Actions release workflow.

### [2026-09-04 21:12] Add Validation to Ensure REM DATE Shows Only 4-Digit Year Value
- Identified issue where MusicBrainz returns release date in `YYYY-MM` or `YYYY-MM-DD` format (e.g. `1988-09`), which caused `REM DATE "1988-09"` to be written to CUE files.
- Added `CueData.SanitizeYear(string? date)` in [`Core/Models.cs`](file:///D:/git/CueComplete/Core/Models.cs) using regex `\b(1[89]\d{2}|20\d{2})\b` (fallback `\b\d{4}\b`) to extract 4-digit years.
- Made `CueData.Date` property sanitize input values on `set`, ensuring any date assigned to `Date` is strictly a 4-digit year.
- Updated `MusicBrainzProvider.MapToDto` in [`Core/Metadata/MusicBrainzProvider.cs`](file:///D:/git/CueComplete/Core/Metadata/MusicBrainzProvider.cs) to explicitly sanitize `Date` while preserving full release date in `ReleaseDate`.
- Updated `DiscogsSearchResult` in [`Core/Metadata/Models/DiscogsSearchResponse.cs`](file:///D:/git/CueComplete/Core/Metadata/Models/DiscogsSearchResponse.cs) and [`Core/Metadata/DiscogsProvider.cs`](file:///D:/git/CueComplete/Core/Metadata/DiscogsProvider.cs) to map `year` and fallback to `releaseObj.Released` for `data.Date`.
- Updated [`Core/CueFileWriter.cs`](file:///D:/git/CueComplete/Core/CueFileWriter.cs) to ensure `REM DATE` only outputs a sanitized 4-digit year (falling back to year extracted from `ReleaseDate` if `Date` is empty).
- Fixed `REM DATE` in [`TestStubs/1988 - Atomic Winter [US Metal Records, CD-US 14, Replica]/Destiny - Atomic Winter.cue`](file:///D:/git/CueComplete/TestStubs/1988%20-%20Atomic%20Winter%20%5BUS%20Metal%20Records,%20CD-US%2014,%20Replica%5D/Destiny%20-%20Atomic%20Winter.cue) to `REM DATE "1988"`.
- Added comprehensive unit tests in [`CueComplete.Tests/DateValidationTests.cs`](file:///D:/git/CueComplete/CueComplete.Tests/DateValidationTests.cs) covering year sanitization, property setters, parser, and writer outputs.
- Updated [`CueComplete.Tests/DiscIdCalculatorTests.cs`](file:///D:/git/CueComplete/CueComplete.Tests/DiscIdCalculatorTests.cs) to gracefully skip the Joan Jett file test when missing from local workspace.
- Executed `dotnet test` (all 37 tests passing), `dotnet build -c Release`, and `dotnet publish -c Release -r win-x64 -p:PublishSingleFile=true --self-contained true`.

### [2026-09-04 21:37] Fix Discogs Catalog Number Search and Thread-Safe Logging
- Investigated why fast search did not match `Destiny - Atomic Winter` via Discogs when searching catalog number `CD-US 14`.
- Found that `PerformDiscogsTextSearchAsync` was performing a generic query `?q=CD-US+14` which matched 585,000+ items (returning generic releases with "14" in titles), rather than targeted catalog number search `?catno=CD-US+14` (which returns Destiny - Atomic Winter as release 2851256 and 6877213).
- Added `PerformDiscogsCatNoSearchAsync` (`catno=`) and `PerformDiscogsBarcodeSearchAsync` (`barcode=`) in [`Core/Metadata/DiscogsProvider.cs`](file:///D:/git/CueComplete/Core/Metadata/DiscogsProvider.cs).
- Updated [`Core/MetadataService.cs`](file:///D:/git/CueComplete/Core/MetadataService.cs) to route catalog number searches to `catno=` and barcode searches to `barcode=`.
- Added thread-safe locking (`_logLock`) to `MetadataService.Log` to avoid file write lock collisions during concurrent multi-threaded async enrichment.
- Added catch block to `Task.Run` in [`UI/MainWindow.cs`](file:///D:/git/CueComplete/UI/MainWindow.cs) to ensure any unhandled search errors are explicitly logged.
- Added unit tests in [`CueComplete.Tests/BarcodeExtractionTests.cs`](file:///D:/git/CueComplete/CueComplete.Tests/BarcodeExtractionTests.cs) verifying `catno=` and `barcode=` parameters in outgoing HTTP requests.
- All 39 tests passing via `dotnet test`; built and published release binary via `dotnet publish -c Release -r win-x64 -p:PublishSingleFile=true --self-contained true`.

### [2026-09-04 21:53] Patch Release v1.2.1
- Updated version in [`CueComplete.csproj`](file:///D:/git/CueComplete/CueComplete.csproj) to `1.2.1`.
- Merged `fix/rem-date-validation` into `master`.
- Created and pushed git tag `v1.2.1` to trigger automated GitHub Actions release workflow.

### [2026-09-05 09:27] Add File Counter to CUE Files Pane
- Promoted `_leftPane` from a local variable in [`UI/MainWindow.cs`](file:///D:/git/CueComplete/UI/MainWindow.cs) to a class field.
- Added `UpdateLeftPaneTitle()` method in [`UI/MainWindow.cs`](file:///D:/git/CueComplete/UI/MainWindow.cs) to format the frame title as `CUE Files [{current}\{total}]`.
- Hooked `UpdateLeftPaneTitle()` into `MainWindow` initialization, `_fileListView.SelectedItemChanged`, `_fileListView.Enter`, `UpdateFilePreview()`, `LoadCueFile()`, and `ResultsListView_OpenSelectedItem()`.
- Exposed `LeftPaneTitle` and `FileListView` properties on `MainWindow` for inspection and testing.
- Added unit tests in [`CueComplete.Tests/MainWindowTests.cs`](file:///D:/git/CueComplete/CueComplete.Tests/MainWindowTests.cs) verifying counter calculation across list items and empty list state (41/41 tests passing).
- Verified build and single-file executable packaging via `dotnet publish -c Release -r win-x64 -p:PublishSingleFile=true --self-contained true`.

### [2026-09-05 09:34] Branch & Pull Request Creation
- Created branch `feat/cue-files-counter`.
- Committed changes with message `feat(ui): add [current\total] file counter to CUE Files pane`.
- Pushed branch to `origin/feat/cue-files-counter`.
- Created pull request [#5](https://github.com/demoded/CueComplete/pull/5) targeting `master`.

### [2026-09-05 10:50] Fix False Positive DISCNUMBER from Catalog Numbers and Enforce Deserialized Values
- Investigated `REM DISCNUMBER "33"` in `TestStubs/1994 - Blut [1994, Massacre, MASS CD 033, DE]/Atrocity - Blut.cue`.
- Identified that `CueFileParser.cs` used a regex `\b(?:CD|Disc)\s*(\d+)` against the folder name, which erroneously matched `MASS CD 033` (the catalog number) as disc number 33.
- Updated `CueFileParser.cs` to strip release metadata brackets (`\[.*\]`) containing commas before matching disc/CD patterns, and added checks against known catalog numbers.
- Fixed tracklist position prefix logic in [`Core/Metadata/DiscogsProvider.cs`](file:///D:/git/CueComplete/Core/Metadata/DiscogsProvider.cs) so purely numeric track positions (`1`, `2`, ..., `15`) are grouped into a single disc rather than 15 separate prefixes.
- Enforced in `DiscogsProvider.cs` that single-disc releases (`Discs == 1`) always set `DiscNumber = 1`.
- Added `DiscNumber` to [`Core/Metadata/Models/MusicBrainzReleaseDto.cs`](file:///D:/git/CueComplete/Core/Metadata/Models/MusicBrainzReleaseDto.cs) and populated it from deserialized `release.Media` positions in [`Core/Metadata/MusicBrainzProvider.cs`](file:///D:/git/CueComplete/Core/Metadata/MusicBrainzProvider.cs).
- Added sanity validation in [`UI/MainWindow.cs`](file:///D:/git/CueComplete/UI/MainWindow.cs) and [`Core/CueFileWriter.cs`](file:///D:/git/CueComplete/Core/CueFileWriter.cs) ensuring that `DiscNumber` is set to `1` whenever `Discs == 1` or if `DiscNumber > Discs`.
- Corrected `REM DISCNUMBER "33"` to `REM DISCNUMBER "1"` in `TestStubs/1994 - Blut [1994, Massacre, MASS CD 033, DE]/Atrocity - Blut.cue`.
- Added unit tests in [`CueComplete.Tests/DiscNumberTests.cs`](file:///D:/git/CueComplete/CueComplete.Tests/DiscNumberTests.cs) (all 42 tests passing).
- Verified build and single-file publish via `dotnet publish -c Release -r win-x64 -p:PublishSingleFile=true --self-contained true`.

### [2026-09-05 10:57] Branch & Pull Request Creation
- Created branch `fix/disc-number-validation`.
- Committed changes with message `fix(metadata): prevent catalog numbers matching as disc number and enforce deserialized disc numbers`.
- Pushed branch to `origin/fix/disc-number-validation`.
- Created pull request [#6](https://github.com/demoded/CueComplete/pull/6) targeting `master`.

=======

### [2026-09-19 17:03] Fix Application Hang Caused by NetMainLoop Concurrency Race Condition
- Investigated hang in unresponsive process PID 16700 and memory dump [`CueComplete.dmp`](file:///D:/git/CueComplete/CueComplete.dmp).
- Identified that `Terminal.Gui.NetMainLoop.NetInputHandler()` crashed with an unhandled `System.InvalidOperationException: Queue empty.` caused by concurrent unsynchronized access to `Queue<InputResult?>` between the background input thread and the UI thread's `MainIteration()`.
- The crash occurred because [`Program.cs`](file:///D:/git/CueComplete/Program.cs) configured `Application.UseSystemConsole = true;`, forcing the use of `NetDriver` instead of the Windows-native `WindowsDriver`.
- Removed `Application.UseSystemConsole = true;` from [`Program.cs`](file:///D:/git/CueComplete/Program.cs), allowing `Terminal.Gui.Application.Init()` to default to `WindowsDriver` and `WindowsMainLoop` on Windows.
- Added `*.dmp` pattern to [`.gitignore`](file:///D:/git/CueComplete/.gitignore) to exclude process memory dumps from version control.
- Verified all 44 unit tests pass via `dotnet test`.
- Verified single-file release package compilation via `dotnet publish -c Release -r win-x64 -p:PublishSingleFile=true --self-contained true`.

### [2026-09-19 17:06] Add Comprehensive Exception Handling Across UI and Application Lifecycle
- Hooked `AppDomain.CurrentDomain.UnhandledException` and `TaskScheduler.UnobservedTaskException` in [`Program.cs`](file:///D:/git/CueComplete/Program.cs) to log unobserved/unhandled exceptions to `crash.log`.
- Implemented `HandleUiException` in [`Program.cs`](file:///D:/git/CueComplete/Program.cs) and passed it to `Application.Run(openDialog, ...)` and `Application.Run(mainWindow, ...)` to gracefully handle and log UI loop exceptions without abrupt crashes.
- Wrapped `CueFileParser.Parse` in [`UI/MainWindow.cs`](file:///D:/git/CueComplete/UI/MainWindow.cs) in a try-catch block with logging and `MessageBox.ErrorQuery` if parsing fails.
- Protected `Application.MainLoop.Invoke` calls in `onLogHandler` and the search completion callback in [`UI/MainWindow.cs`](file:///D:/git/CueComplete/UI/MainWindow.cs) with exception guards.
- Ensured `Application.RequestStop(dialog)` is executed inside a `finally` block within `MainLoop.Invoke` so search dialogs cannot remain stuck on screen.
- Passed an `errorHandler` to `Application.Run(dialog, ...)` to log and cleanly exit the search dialog loop if an exception occurs.
- Wrapped `CueFileWriter.Save` in `ResultsListView_OpenSelectedItem` with a try-catch block to display user-friendly error messages if saving fails.
- Verified all 44 unit tests pass via `dotnet test`.
- Packaged single-file release executable via `dotnet publish -c Release -r win-x64 -p:PublishSingleFile=true --self-contained true`.

### [2026-09-19 18:10] Branch & Pull Request Creation
- Created branch `fix/prevent-hang-and-add-exception-handling`.
- Committed changes with message `fix(ui): prevent application hang by using WindowsDriver and add lifecycle exception handling`.
- Pushed branch to `origin/fix/prevent-hang-and-add-exception-handling`.
- Created pull request [#7](https://github.com/demoded/CueComplete/pull/7) targeting `master`.

### [2026-09-19 18:14] Patch Release v1.2.2
- Updated version in [`CueComplete.csproj`](file:///D:/git/CueComplete/CueComplete.csproj) to `1.2.2`.
- Merged pull request [#7](https://github.com/demoded/CueComplete/pull/7) (`fix/prevent-hang-and-add-exception-handling`) into `master`.
- Created and pushed git tag `v1.2.2` to trigger automated GitHub Actions release workflow.

### [2026-09-19 22:05] Implement New UI Layout
- Reviewed [`.agents/new layout 20260919.md`](file:///D:/git/CueComplete/.agents/new%20layout%2020260919.md) and aligned on specifications via plan [`.agents/plan 20260919T2203.md`](file:///D:/git/CueComplete/.agents/plan%2020260919T2203.md).
- Updated [`UI/MainWindow.cs`](file:///D:/git/CueComplete/UI/MainWindow.cs) to match the new multi-pane layout:
  - Top row: Split into `Cue Files` (width: 20%, height: 9 lines) and `Search Results` (width: 80%, height: 9 lines).
  - Formatted `Cue Files` title with dynamic zero-padded counter and forward slash (e.g. `Cue Files [01/99]`, `Cue Files [1/3]`, `Cue Files [0/0]`).
  - Middle row: Added borderless single-line `_sourcePathLabel` (`Height = 1`) displaying the full source CUE file path.
  - Bottom row: Added side-by-side comparison panes `Source cue details` (50% width) and `Found cue data` (50% width) for comparing source metadata and selected search result metadata.
  - Updated StatusBar to include `~Enter~ Apply` alongside `~^Q~ Quit`, `~S~ Deep Search`, and `~^L~ Toggle Log`.
- Updated unit tests in [`CueComplete.Tests/MainWindowTests.cs`](file:///D:/git/CueComplete/CueComplete.Tests/MainWindowTests.cs) covering counter formatting, zero-padding for 2+ digits, source path display, and pane structure (47/47 passing).
- Verified `dotnet test`, `dotnet build -c Release`, and packaged single executable binary via `dotnet publish -c Release -r win-x64 -p:PublishSingleFile=true --self-contained true`.

### [2026-09-19 22:30] Branch & Pull Request Creation
- Created branch `feat/new-ui-layout`.
- Committed changes with message `feat(ui): update application layout with split panes and metadata comparison`.
- Pushed branch to `origin/feat/new-ui-layout`.
- Created pull request [#8](https://github.com/demoded/CueComplete/pull/8) targeting `master`.

### [2026-09-20 07:25] Lock Bottom Pane to 20 Lines and Make Top Section Flexible
- Drafted plan in [`.agents/plan 20260920T0725.md`](file:///D:/git/CueComplete/.agents/plan%2020260920T0725.md).
- Updated [`UI/MainWindow.cs`](file:///D:/git/CueComplete/UI/MainWindow.cs):
  - Changed bottom panes (`Source cue details` and `Found cue data`) height to fixed 20 lines (`Height = 20`).
  - Changed top panes (`Cue Files` and `Search Results`) height to `Dim.Fill(21)` so that the top section is flexible and fills all remaining vertical space.
  - Preserved middle `_sourcePathLabel` at 1 line height (`Height = 1`).
  - Exposed pane and label properties (`CueFilesPane`, `SearchResultsPane`, `SourceCueDetailsPane`, `FoundCueDataPane`, `SourcePathLabel`) on `MainWindow`.
- Added test `MainWindow_LayoutDimensions_BottomSectionLockedTo20LinesAndTopFlexible` in [`CueComplete.Tests/MainWindowTests.cs`](file:///D:/git/CueComplete/CueComplete.Tests/MainWindowTests.cs) (48/48 tests passing).
- Verified `dotnet test`, `dotnet build -c Release`, and published single executable binary via `dotnet publish -c Release -r win-x64 -p:PublishSingleFile=true --self-contained true`.

### [2026-09-20 08:10] Implement Diacritic- and Case-Insensitive Artist Matching with CompareOptions
- Added [`Core/StringExtensions.cs`](file:///D:/git/CueComplete/Core/StringExtensions.cs) providing `ContainsIgnoreCaseAndDiacritics` and `MatchesArtist` utilizing `CultureInfo.InvariantCulture.CompareInfo.IndexOf` with `CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace`.
- Replaced rigid `.ToLowerInvariant().Contains(...)` artist post-filtering in [`Core/Metadata/DiscogsProvider.cs`](file:///D:/git/CueComplete/Core/Metadata/DiscogsProvider.cs) (`SearchAsync` and `PerformDiscogsSearchAsync`) with `StringExtensions.MatchesArtist`.
- Replaced `.ToLower().Contains(...)` artist post-filtering in [`Core/Metadata/MusicBrainzProvider.cs`](file:///D:/git/CueComplete/Core/Metadata/MusicBrainzProvider.cs) (DiscID search, FreeDB search, and text search release credits) with `StringExtensions.MatchesArtist`.
- Added unit and integration tests in [`CueComplete.Tests/StringComparisonTests.cs`](file:///D:/git/CueComplete/CueComplete.Tests/StringComparisonTests.cs) covering Cyrillic `ё` vs `е` matching (e.g. `Черный Кофе` and `Чёрный Кофе`), Latin diacritics/accents (`Motörhead`, `Beyoncé`), and Discogs catalog search verification for `MR 23143 CD`.
- Verified all 68 unit tests pass via `dotnet test`.
- Packaged single-file release executable via `dotnet publish -c Release -r win-x64 -p:PublishSingleFile=true --self-contained true`.

### [2026-09-20 09:00] Branch & Pull Request Creation
- Created branch `fix/artist-matching-diacritics`.
- Committed changes with message `fix(metadata): match artists ignoring diacritics and cyrillic yo using compareoptions`.
- Pushed branch to `origin/fix/artist-matching-diacritics`.
- Created pull request [#9](https://github.com/demoded/CueComplete/pull/9) targeting `master`.

### [2026-09-20 09:16] Minor Release v1.3.0
- Merged pull request [#8](https://github.com/demoded/CueComplete/pull/8) (`feat/new-ui-layout`) into `master`.
- Merged pull request [#9](https://github.com/demoded/CueComplete/pull/9) (`fix/artist-matching-diacritics`) into `master`.
- Updated version in [`CueComplete.csproj`](file:///D:/git/CueComplete/CueComplete.csproj) to `1.3.0`.
- Verified all 72 unit tests pass via `dotnet test`.
- Packaged single-file release executable via `dotnet publish -c Release -r win-x64 -p:PublishSingleFile=true --self-contained true`.
### [2026-09-20 10:20] Fix MusicBrainz Artist Credit Resolution and Filter Dummy Barcodes
- Drafted plan in [`.agents/plan 20260920T1010.md`](file:///D:/git/CueComplete/.agents/plan%2020260920T1010.md).
- Added `Include.ArtistCredits | Include.DiscIds` to all `LookupReleaseAsync` calls in [`Core/Metadata/MusicBrainzProvider.cs`](file:///D:/git/CueComplete/Core/Metadata/MusicBrainzProvider.cs) to ensure release artist credits are populated instead of erroneously falling back to `sourceData.Artist`.
- Added validation for fallback DiscID search matches in [`Core/Metadata/MusicBrainzProvider.cs`](file:///D:/git/CueComplete/Core/Metadata/MusicBrainzProvider.cs) to verify that a release actually contains the DiscID in its media discs, or that both the artist and track count match the CUE data before accepting it.
- Updated `IsValidBarcodeCandidate` in [`Core/Metadata/DiscogsProvider.cs`](file:///D:/git/CueComplete/Core/Metadata/DiscogsProvider.cs) to reject all-zero dummy barcodes (e.g. `CATALOG 0000000000000`).
- Updated [`Core/CueFileParser.cs`](file:///D:/git/CueComplete/Core/CueFileParser.cs) and [`Core/MetadataService.cs`](file:///D:/git/CueComplete/Core/MetadataService.cs) to validate `CATALOG` / `Barcode` with `IsValidBarcodeCandidate`, preventing dummy barcodes from suppressing artist/album search queries.
- Updated text search results in [`Core/Metadata/MusicBrainzProvider.cs`](file:///D:/git/CueComplete/Core/Metadata/MusicBrainzProvider.cs) to prioritize releases matching the target catalog number and track count.
- Added unit tests in [`CueComplete.Tests/BarcodeExtractionTests.cs`](file:///D:/git/CueComplete/CueComplete.Tests/BarcodeExtractionTests.cs) and [`CueComplete.Tests/MetadataValidationTests.cs`](file:///D:/git/CueComplete/CueComplete.Tests/MetadataValidationTests.cs) (78/78 tests passing).
- Verified `dotnet test` and published single executable via `dotnet publish -c Release -r win-x64 -p:PublishSingleFile=true --self-contained true`.

### [2026-09-20 10:35] Support Annotated Titles, MusicBrainz CatNo Search, and Artist '&' Normalization
- Drafted plan in [`.agents/plan 20260920T1031.md`](file:///D:/git/CueComplete/.agents/plan%2020260920T1031.md).
- Added `StripTitleAnnotations(string? title)` to [`Core/StringExtensions.cs`](file:///D:/git/CueComplete/Core/StringExtensions.cs) to strip trailing annotations like `(Japan)`, `[Deluxe Edition]`, `(Remastered)`, while preserving non-annotated titles (e.g. `(Untitled)`).
- Updated `MatchesArtist` in [`Core/StringExtensions.cs`](file:///D:/git/CueComplete/Core/StringExtensions.cs) to normalize ` & ` and ` and ` (as well as `+`) so that artist names like `Flotsam & Jetsam` match `Flotsam and Jetsam`.
- Added `MatchesCountry` and `MatchesSourceCountry` in [`Core/StringExtensions.cs`](file:///D:/git/CueComplete/Core/StringExtensions.cs) mapping country aliases (e.g. `JP` <-> `Japan`, `US` <-> `United States`, `UK` <-> `GB`, `XE` <-> `Europe`, `DE` <-> `Germany`) and matching release countries against source country or album title annotations.
- Updated [`Core/Metadata/MusicBrainzProvider.cs`](file:///D:/git/CueComplete/Core/Metadata/MusicBrainzProvider.cs):
  - Added catalog number query support (`catno:"..."` / `artist:"..." AND catno:"..."`) when barcode is absent.
  - Added stripped title annotation fallback when exact `artist:"..." AND release:"..."` query returns 0 results.
  - Added country match scoring bonus (+15 points) when ranking MusicBrainz results.
- Updated [`Core/Metadata/DiscogsProvider.cs`](file:///D:/git/CueComplete/Core/Metadata/DiscogsProvider.cs):
  - Fixed fallback text search to execute whenever prior lookups (DiscogsId, barcode, catno) return 0 results, removing the erroneous restriction that skipped text search when a catalog number was specified.
  - Added stripped title fallback in Discogs text search if the initial search with full title yields no results.
- Updated [`Core/CueFileParser.cs`](file:///D:/git/CueComplete/Core/CueFileParser.cs):
  - Added `ExtractCountryFromFolderName` to extract country codes from folder bracket metadata (e.g. `[1990, JP, WMCP-78]`) when `Country` is not defined in the CUE file.
- Added 24 unit test cases in [`CueComplete.Tests/StringComparisonTests.cs`](file:///D:/git/CueComplete/CueComplete.Tests/StringComparisonTests.cs) (102/102 unit tests passing).
- Verified `dotnet test`, `dotnet build`, and packaged single executable binary via `dotnet publish -c Release -r win-x64 -p:PublishSingleFile=true --self-contained true`.

### [2026-09-20 11:20] Patch Release v1.3.1
- Merged pull request [#10](https://github.com/demoded/CueComplete/pull/10) (`fix/annotated-titles-and-catno-search`) into `master`.
- Updated version in [`CueComplete.csproj`](file:///D:/git/CueComplete/CueComplete.csproj) to `1.3.1`.
- Created semantic version tag `v1.3.1` and pushed to GitHub to trigger automated release workflow.

### [2026-09-20 11:32] Update Application Screenshot
- Updated [`assets/screenshot.png`](file:///D:/git/CueComplete/assets/screenshot.png) reflecting the updated application UI layout.

### [2026-09-20 11:36] Update Search Logic Documentation in README
- Updated `Search Logic` section in [`README.md`](file:///D:/git/CueComplete/README.md) to document identifier sanitization (barcodes, catno, country, title annotations), auto-fallback from fast to deep search, MusicBrainz multi-stage lookup (DiscID, FreeDB, barcode, catno, stripped title fallback, Discogs enrichment, country-weighted scoring), Discogs cross-referencing and text fallback, and diacritic/conjunction normalization.

### [2026-11-17 11:42] Upgrade Project to .NET 11
- Created upgrade plan in [`.agents/plan 20261117T1136.md`](file:///D:/git/CueComplete/.agents/plan%2020261117T1136.md).
- Updated [`CueComplete.csproj`](file:///D:/git/CueComplete/CueComplete.csproj) target framework from `net10.0` to `net11.0`.
- Removed redundant `System.Text.Encoding.CodePages` package reference from [`CueComplete.csproj`](file:///D:/git/CueComplete/CueComplete.csproj) to eliminate compiler pruning warning `NU1510`.
- Updated [`CueComplete.Tests/CueComplete.Tests.csproj`](file:///D:/git/CueComplete/CueComplete.Tests/CueComplete.Tests.csproj) target framework from `net10.0` to `net11.0`.
- Updated [`.github/workflows/release.yml`](file:///D:/git/CueComplete/.github/workflows/release.yml) .NET setup version from `10.0.x` to `11.0.x`.
- Updated documentation in [`README.md`](file:///D:/git/CueComplete/README.md) to reference .NET 11 and .NET 11.0 SDK prerequisites.
- Executed `dotnet test` - all 102 unit tests passed cleanly with 0 failures and 0 warnings.
- Executed `dotnet build -c Release` (0 errors, 0 warnings).
- Executed `dotnet publish -c Release -r win-x64 -p:PublishSingleFile=true --self-contained true` to verify single-file release packaging.

### [2026-11-17 11:58] Create Feature Branch and Pull Request
- Created branch `chore/upgrade-to-dotnet-11` and reset local `master` to align with `origin/master`.
- Pushed branch `chore/upgrade-to-dotnet-11` to `origin`.
- Opened Pull Request [#11](https://github.com/demoded/CueComplete/pull/11) to merge `chore/upgrade-to-dotnet-11` into `master`.

### [2026-11-17 12:07] Merge Pull Request and Publish Release
- Pulled latest `master` from `origin/master` after merging PR [#11](https://github.com/demoded/CueComplete/pull/11).
- Deleted merged local feature branch `chore/upgrade-to-dotnet-11`.
- Verified `dotnet test` (102/102 unit tests passed).
- Built project in Release configuration via `dotnet build -c Release` (0 errors, 0 warnings).
- Packaged single-file release executable via `dotnet publish -c Release -r win-x64 -p:PublishSingleFile=true --self-contained true` into `bin/Release/net11.0/win-x64/publish/CueComplete.exe`.

### [2026-11-17 12:46] Plan Header Diff View Feature
- Created feature plan in [`.agents/plan 20261117T1246.md`](file:///D:/git/CueComplete/.agents/plan%2020261117T1246.md) detailing the architecture and implementation for comparing `.cue` header metadata with `.cue.bak` backups while ignoring track details.

### [2026-11-17 13:05] Implement Header Diff View Feature
- Created [`Core/HeaderDiffService.cs`](file:///D:/git/CueComplete/Core/HeaderDiffService.cs) to compare current `.cue` header fields with `.cue.bak` backup files while explicitly ignoring track information.
- Created [`UI/DiffDialog.cs`](file:///D:/git/CueComplete/UI/DiffDialog.cs) modal dialog presenting formatted diff table with status badges (`[MODIFIED]`, `[ADDED]`, `[REMOVED]`) and keyboard dismissal.
- Updated [`UI/MainWindow.cs`](file:///D:/git/CueComplete/UI/MainWindow.cs) to add `~D~ Diff` in `StatusBar`, handle `D`/`d` keyboard shortcut in `RootKeyEvent`, and implement `ShowDiff()` with friendly missing backup notifications.
- Added comprehensive unit tests in [`CueComplete.Tests/HeaderDiffServiceTests.cs`](file:///D:/git/CueComplete/CueComplete.Tests/HeaderDiffServiceTests.cs) and [`CueComplete.Tests/MainWindowTests.cs`](file:///D:/git/CueComplete/CueComplete.Tests/MainWindowTests.cs) (114/114 tests passing).
- Documented Header Diff Viewer feature in [`README.md`](file:///D:/git/CueComplete/README.md).
- Verified `dotnet test` (all 114 tests passed) and `dotnet build -c Release` (0 errors, 0 warnings).
- Created branch `feat/cue-header-diff-view` and opened Pull Request [#13](https://github.com/demoded/CueComplete/pull/13).

### [2026-11-17 15:28] Merge Pull Request #13 to Master
- Pulled latest `master` from `origin/master` after merging PR [#13](https://github.com/demoded/CueComplete/pull/13).
- Deleted merged local feature branch `feat/cue-header-diff-view`.
- Verified `dotnet test` - all 114 unit tests passed.

### [2026-11-17 15:37] Update Documentation and Application Screenshot
- Created plan in [`.agents/plan 20261117T1536.md`](file:///D:/git/CueComplete/.agents/plan%2020261117T1536.md).
- Updated [`README.md`](file:///D:/git/CueComplete/README.md) with a comprehensive Keyboard Shortcuts table and enhanced descriptions for automatic backups and Header Diff Viewer.
- Updated application screenshot [`assets/screenshot.png`](file:///D:/git/CueComplete/assets/screenshot.png) capturing the updated layout and status bar actions.
- Created branch `docs/update-readme-and-screenshot` and opened Pull Request [#14](https://github.com/demoded/CueComplete/pull/14).

### [2026-11-17 16:11] Plan Remove Discogs Search 5-Item Limit
- Created plan in [`.agents/plan 20261117T1611.md`](file:///D:/git/CueComplete/.agents/plan%2020261117T1611.md) to remove the premature `.Take(5)` limitation in `DiscogsProvider.PerformDiscogsSearchAsync` and pre-filter search candidates by artist when available.

### [2026-11-17 16:14] Remove Discogs Search 5-Item Limit and Add Pre-filtering
- Updated [`Core/Metadata/DiscogsProvider.cs`](file:///D:/git/CueComplete/Core/Metadata/DiscogsProvider.cs) to remove the hardcoded `.Take(5)` truncation on `searchResponse.Results`.
- Implemented pre-filtering of candidate items by comparing parsed `"Artist - Title"` with `sourceData.Artist` using `StringExtensions.MatchesArtist`, ensuring detailed release API calls are dispatched only for matching candidates (conserving rate limits) while maintaining fallback to all results when no title matches or artist is unspecified.
- Added unit tests `DiscogsProvider_CatNoSearch_FindsMatchBeyondFirst5Items` and `DiscogsProvider_BarcodeSearch_ProcessesAllResultsBeyondFirst5Items_WhenArtistNotSpecified` in [`CueComplete.Tests/StringComparisonTests.cs`](file:///D:/git/CueComplete/CueComplete.Tests/StringComparisonTests.cs).
- Verified all 116 unit tests passed via `dotnet test`.
- Built Release configuration via `dotnet build -c Release` (0 warnings, 0 errors) and packaged win-x64 single-file executable via `dotnet publish -c Release -r win-x64 -p:PublishSingleFile=true --self-contained true`.
- Created branch `fix/discogs-search-result-limit` and opened Pull Request [#15](https://github.com/demoded/CueComplete/pull/15).

### [2026-11-17 16:38] Merge Pull Requests #14 and #15 to Master & Build
- Pulled latest `master` from `origin/master` with merged PR [#14](https://github.com/demoded/CueComplete/pull/14) and PR [#15](https://github.com/demoded/CueComplete/pull/15).
- Cleaned up local merged branches `fix/discogs-search-result-limit` and `docs/update-readme-and-screenshot`.
- Verified `dotnet test` (all 116 tests passed).
- Built project in Release configuration via `dotnet build -c Release` (0 warnings, 0 errors).
- Packaged single-file release executable via `dotnet publish -c Release -r win-x64 -p:PublishSingleFile=true --self-contained true` into `bin/Release/net11.0/win-x64/publish/CueComplete.exe`.

### [2026-11-17 16:49] Release v1.4.0
- Updated `<Version>` in [`CueComplete.csproj`](file:///D:/git/CueComplete/CueComplete.csproj) to `1.4.0`.
- Created git tag `v1.4.0` and pushed to GitHub to trigger automated release workflow.

### [2026-11-17 18:15] Plan Skip Non-Matching Search Fallback
- Created plan in [`.agents/plan 20261117T1815.md`](file:///D:/git/CueComplete/.agents/plan%2020261117T1815.md) to prevent firing release-detail HTTP requests when 0 items match the artist, and fall back to searching by artist and album.

### [2026-11-17 18:17] Skip Raw Results Fallback and Search by Artist and Album
- Updated [`Core/Metadata/DiscogsProvider.cs`](file:///D:/git/CueComplete/Core/Metadata/DiscogsProvider.cs) to prevent falling back to all raw results when 0 candidate items match `sourceData.Artist`, eliminating wasteful release detail requests that exhaust API rate limits.
- Updated `DiscogsProvider.SearchAsync` to fall back to text search whenever `list.Count == 0`, regardless of whether unused discogsIds/barcodes were present.
- Updated [`Core/MetadataService.cs`](file:///D:/git/CueComplete/Core/MetadataService.cs) so Fast Search falls back to Discogs artist and album search (including stripped title fallback) when CatNo/Barcode yields 0 matches, and avoided repeating the same failed CatNo query in Deep Search.
- Added constructor in `MetadataService` for testability with injected providers.
- Added unit tests `DiscogsProvider_CatNoSearch_DoesNotFireRequestsForNonMatchingResults_WhenZeroMatchArtist` and `MetadataService_FastSearch_FallsBackToArtistAndAlbum_WhenCatNoReturnsZeroMatches` in [`CueComplete.Tests/StringComparisonTests.cs`](file:///D:/git/CueComplete/CueComplete.Tests/StringComparisonTests.cs).
- Verified `dotnet test` (all 118 unit tests passed).
- Built Release configuration via `dotnet build -c Release` (0 warnings, 0 errors) and packaged win-x64 single-file executable via `dotnet publish -c Release -r win-x64 -p:PublishSingleFile=true --self-contained true`.
- Created branch `fix/skip-non-matching-catno-fallback` and opened Pull Request [#16](https://github.com/demoded/CueComplete/pull/16).

### [2026-11-17 19:04] Merge Pull Request #16 to Master & Build
- Pulled latest `master` from `origin/master` with merged PR [#16](https://github.com/demoded/CueComplete/pull/16).
- Cleaned up local merged branch `fix/skip-non-matching-catno-fallback`.
- Verified `dotnet test` (all 118 unit tests passed).
- Built project in Release configuration via `dotnet build -c Release` (0 warnings, 0 errors).
- Packaged single-file release executable via `dotnet publish -c Release -r win-x64 -p:PublishSingleFile=true --self-contained true` into `bin/Release/net11.0/win-x64/publish/CueComplete.exe`.

### [2026-11-17 19:21] Plan Match Artist Acronyms in Search & String Extensions
- Created plan in [`.agents/plan 20261117T1921.md`](file:///D:/git/CueComplete/.agents/plan%2020261117T1921.md) to implement bidirectional acronym/initialism matching in `StringExtensions.MatchesArtist` and add acronym text search fallback in `MetadataService` and `DiscogsProvider`.

### [2026-11-17 19:35] Implement Artist Acronym Matching and Search Fallback
- Updated [`Core/StringExtensions.cs`](file:///D:/git/CueComplete/Core/StringExtensions.cs):
  - Added `IsAcronymMatch` to support bidirectional acronym matching (handling 2-6 letter abbreviations, uppercase names, dotted abbreviations like `M.P.`, and word initials with or without common stopwords).
  - Enhanced `NormalizeArtist` to strip Discogs numeric suffixes (e.g. `(22)`).
  - Added `GetAcronym` helper to generate standard acronym from multi-word artist names.
- Updated [`Core/Metadata/DiscogsProvider.cs`](file:///D:/git/CueComplete/Core/Metadata/DiscogsProvider.cs) and [`Core/MetadataService.cs`](file:///D:/git/CueComplete/Core/MetadataService.cs):
  - Added acronym search query fallback (`"{acronym} {album}"`) when spelled-out artist and album search returns 0 results.
- Added comprehensive unit tests in [`CueComplete.Tests/StringComparisonTests.cs`](file:///D:/git/CueComplete/CueComplete.Tests/StringComparisonTests.cs):
  - `MatchesArtist` tests covering `MP` <-> `Metal Priest`, `MP (22)`, `M.P.`, `ELO`, `OMD`, `BOC`, `ELP`, Cyrillic `ЧК`, and negative cases (`Michael Jackson`, `Madonna`, `Metallica`, etc.).
  - `GetAcronym` evaluation tests.
  - `DiscogsProvider_CatNoSearch_MatchesArtistAcronym_MP_ForMetalPriest` integration test verifying `MP (22) - Get It Now` is matched for `Metal Priest` on CatNo `73053`.
  - `MetadataService_FastSearch_FallsBackToAcronym_WhenTextSearchYieldsZeroMatches` integration test verifying acronym fallback finds the target release.
- Verified all 145 unit tests passed via `dotnet test`.
- Built Release configuration via `dotnet build -c Release` (0 warnings, 0 errors) and packaged win-x64 single-file executable via `dotnet publish -c Release -r win-x64 -p:PublishSingleFile=true --self-contained true`.
- Created branch `fix/artist-acronym-matching` and opened Pull Request [#17](https://github.com/demoded/CueComplete/pull/17).

### [2026-11-17 19:52] Implement Album Matching in Discogs Search Filtering
- Updated [`Core/StringExtensions.cs`](file:///D:/git/CueComplete/Core/StringExtensions.cs):
  - Added `MatchesAlbum` supporting exact, stripped-annotation, and normalized bidirectional album title comparison.
- Updated [`Core/Metadata/DiscogsProvider.cs`](file:///D:/git/CueComplete/Core/Metadata/DiscogsProvider.cs):
  - Updated candidate pre-filtering and post-fetch filtering to evaluate `MatchesArtist || MatchesAlbum`, ensuring releases whose album matches the query are retained even if the artist name varies or is an alias.
- Added unit tests in [`CueComplete.Tests/StringComparisonTests.cs`](file:///D:/git/CueComplete/CueComplete.Tests/StringComparisonTests.cs):
  - `MatchesAlbum_EvaluatesCorrectly` testing exact, stripped, and normalized title comparisons.
  - `DiscogsProvider_CatNoSearch_MatchesAlbum_WhenArtistDiffers` verifying a candidate is matched when album matches despite differing artist.
- Verified all 156 unit tests passed via `dotnet test`.
- Built Release configuration via `dotnet build -c Release` (0 warnings, 0 errors) and packaged win-x64 single-file executable via `dotnet publish -c Release -r win-x64 -p:PublishSingleFile=true --self-contained true`.

### [2026-11-21 18:12] Plan Allow Edit "Found cue data" Page (Issue #18)
- Created plan in [`.agents/plan 20261121T1812.md`](file:///D:/git/CueComplete/.agents/plan%2020261121T1812.md) to implement editable metadata in the "Found cue data" pane, allowing users to fine-tune metadata prior to saving or manually input metadata when no search results are returned.

### [2026-11-21 18:31] Implement Editable "Found cue data" Pane (Issue #18)
- Updated [`Core/CueFileParser.cs`](file:///D:/git/CueComplete/Core/CueFileParser.cs):
  - Added `ParseFormattedText(string text, CueData? template = null)` to extract all 11 metadata fields from formatted key-value text lines (`Artist:`, `Album:`, `Genre:`, `Date:`, `Label:`, `Cat No:`/`Catalog Number:`, `Country:`, `Barcode:`, `Rel Date:`/`Release Date:`, `Discs:`/`Tracks:`, `Comment:`).
  - Added `ParseDiscsAndTracks` supporting combinations like `1 of 2 Tracks: 8`, `2 Tracks: 12`, `1`, etc., and standalone `Tracks:` keys.
  - Automatically sanitizes 4-digit year for `Date`.
  - Preserves template metadata (`DiscId`, `MusicBrainzDiscId`, `DiscogsId`, `Source`, `OriginalLines`, `OriginalEncoding`).
- Updated [`UI/MainWindow.cs`](file:///D:/git/CueComplete/UI/MainWindow.cs):
  - Made `_foundDetailsTextView` editable: configured `ReadOnly = false` and `AllowsTab = false` so Tab/Shift+Tab navigates focus between views rather than inserting tabs.
  - Added public property accessors `FoundDetailsTextView` and `SourceDetailsTextView`.
  - Added keypress handling in `_foundDetailsTextView`: `Enter` / `Ctrl+Enter` triggers `ApplyFoundCueData()` (preventing newline insertion), and `Esc` calls `ResetFoundCueDataPreview()` to revert to the unedited search result preview.
  - Added focus guard in `Application.RootKeyEvent` so shortcut keys like 's' (Deep Search) and 'd' (Diff) are not intercepted when the user is typing inside `_foundDetailsTextView`.
  - Added edit preservation across focus changes: tracks `_lastPreviewedResultIndex` in `UpdateResultPreview(bool force = false)` so switching focus back to `_resultsListView` does not wipe out active edits.
  - Implemented `ApplyFoundCueData()` to parse `_foundDetailsTextView.Text`, merge edits into `_currentCueData`, enforce disc count validation, save via `CueFileWriter.Save`, and advance to the next file.
  - Updated `ResultsListView_OpenSelectedItem` and status bar `~Enter~ Apply` to invoke `ApplyFoundCueData()`.
  - Added `OnDoneMessage` delegate to allow unit test execution with `FakeDriver` without blocking on modal `MessageBox.Query`.
  - Updated `FormatCueData` to always display the `Comment:` line so users have all fields accessible for editing.
  - Automatically populates `_foundDetailsTextView` with source file metadata when focused if no search results were found (`"(No results found)"`), enabling manual metadata entry and applying.
- Added comprehensive unit tests:
  - [`CueComplete.Tests/CueFileParserTests.cs`](file:///D:/git/CueComplete/CueComplete.Tests/CueFileParserTests.cs):
    - `ParseFormattedText_ValidFullMetadata_ParsesAllFieldsCorrectly`
    - `ParseFormattedText_DiscsAndTracksFormats_ParsesCorrectly`
    - `ParseFormattedText_AlternateKeys_ParsesCorrectly`
    - `ParseFormattedText_EmptyAndWhitespace_HandlesSafely`
    - `ParseFormattedText_PreservesTemplateMetadata`
    - `ParseFormattedText_RoundTrip_WithFormatCueData`
  - [`CueComplete.Tests/MainWindowTests.cs`](file:///D:/git/CueComplete/CueComplete.Tests/MainWindowTests.cs):
    - `MainWindow_FoundCueDataPane_IsEditableAndAllowsTabbing`
    - `MainWindow_ApplyFoundCueData_SavesEditedDataToFile`
    - `MainWindow_ResetFoundCueDataPreview_RevertsEdits`
    - `MainWindow_RootKeyEvent_DoesNotInterceptShortcuts_WhenFoundDetailsHasFocus`
- Updated documentation in [`README.md`](file:///D:/git/CueComplete/README.md) to document editable Found cue data and the `Esc` shortcut.
- Verified all 171 unit tests passed via `dotnet test`.
- Built Release configuration via `dotnet build -c Release` (0 warnings, 0 errors) and packaged win-x64 single-file executable via `dotnet publish -c Release -r win-x64 -p:PublishSingleFile=true --self-contained true`.
- Created branch `feat/allow-edit-found-cue-data` and opened Pull Request [#21](https://github.com/demoded/CueComplete/pull/21).

### [2026-11-22 06:26] Plan Fix Main Window Navigation and Found Cue Data Focus
- Created plan in [`.agents/plan 20261122T0626.md`](file:///D:/git/CueComplete/.agents/plan%2020261122T0626.md) to update main window navigation flow by removing focus from "Source cue details" and positioning cursor on the first line when focusing "Found cue data".

### [2026-11-22 06:51] Fix Main Window Navigation and Found Cue Data Focus
- Updated [`UI/MainWindow.cs`](file:///D:/git/CueComplete/UI/MainWindow.cs):
  - Set `CanFocus = false` on `_sourceDetailsTextView` and `_sourceCueDetailsPane` to remove cursor and focus stop from "Source cue details". Navigation flow is now directly: `Cue Files` > `Search Results` > `Found cue data`.
  - Added `PositionCursorOnFirstLine()` which positions the cursor at the first line (`Y = 0`) and sets `TopRow = 0` whenever "Found cue data" receives focus (`Enter` event) or preview text updates.
- Updated [`CueComplete.Tests/MainWindowTests.cs`](file:///D:/git/CueComplete/CueComplete.Tests/MainWindowTests.cs):
  - Added `MainWindow_Navigation_SourceDetailsCannotFocus_FoundDetailsReceivesFocusAndCursorOnFirstLine` verifying `SourceDetailsTextView.CanFocus` is false, `FoundDetailsTextView.CanFocus` is true, and cursor is positioned at `Y = 0`.
  - Added `MainWindow_TabNavigation_FlowsFromCueFilesToSearchResultsToFoundCueData` verifying Tab navigation flows from `FileListView` to `ResultsListView` to `FoundDetailsTextView` (skipping `SourceDetailsTextView`) and wraps back.
- Verified all 173 unit tests passed via `dotnet test`.
- Built Release configuration via `dotnet build -c Release` (0 warnings, 0 errors) and packaged win-x64 single-file executable via `dotnet publish -c Release -r win-x64 -p:PublishSingleFile=true --self-contained true`.

### [2026-11-22 07:36] Plan Cue Files Color Tweak (Issue #20)
- Created plan in [`.agents/plan 20261122T0736.md`](file:///D:/git/CueComplete/.agents/plan%2020261122T0736.md) to render the selected file in the `Cue Files` pane in yellow (`Color.BrightYellow`).

### [2026-11-22 07:40] Implement Cue Files Selected File Color Tweak (Issue #20)
- Updated [`UI/MainWindow.cs`](file:///D:/git/CueComplete/UI/MainWindow.cs):
  - Configured `_fileListView.ColorScheme` with custom Focus, HotNormal, and HotFocus attributes using `Color.BrightYellow`.
  - Added `_fileListView.RowRender` handler to render the selected row (`args.Row == _fileListView.SelectedItem`) with `Color.BrightYellow` foreground across both focused (`bg = Color.DarkGray`) and unfocused (`bg = Color.Blue`) states.
- Updated [`CueComplete.Tests/MainWindowTests.cs`](file:///D:/git/CueComplete/CueComplete.Tests/MainWindowTests.cs):
  - Added `MainWindow_CueFilesPane_SelectedFileRendersYellow` verifying that the selected file in `_fileListView` renders with `Color.BrightYellow` foreground when focused and unfocused, while unselected rows retain default coloring.
- Verified all 174 unit tests passed via `dotnet test`.
- Built Release configuration via `dotnet build -c Release` (0 warnings, 0 errors) and packaged win-x64 single-file executable via `dotnet publish -c Release -r win-x64 -p:PublishSingleFile=true --self-contained true`.
- Created branch `feat/cue-files-color-tweak` and opened Pull Request [#22](https://github.com/demoded/CueComplete/pull/22).









