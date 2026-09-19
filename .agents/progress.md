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
