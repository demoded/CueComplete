# CueComplete

![CueComplete Screenshot](assets/screenshot.png)

CueComplete is a Terminal User Interface (TUI) application built with .NET 10. It scans a specified directory for `.cue` files and helps you complete or enrich their metadata.

## Prerequisites

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download)

## Setup

The application uses external APIs (like Discogs) to fetch metadata. You need to provide your API credentials via a `.env` file located in your user profile directory (e.g., `C:\Users\%USERNAME%\.env` on Windows or `~/.env` on macOS/Linux).

Add the following variables to your `.env` file:

```env
DISCOGS_CONSUMER_KEY=your_consumer_key
DISCOGS_CONSUMER_SECRET=your_consumer_secret
DISCOGS_PERSONAL_ACCESS_TOKEN=your_personal_access_token
```

## Usage

You can run the application directly using the .NET CLI. By default, it will scan the current directory. You can also pass a specific directory path as an argument.

```pwsh
# Run in the current directory
dotnet run

# Run for a specific directory
dotnet run -- "C:\Path\To\Your\Music\Folder"
```

## Features

- **Terminal GUI**: Easy-to-use keyboard-driven interface using `Terminal.Gui`.
- **Recursive Scanning**: Automatically finds all `.cue` files in the given directory and its subdirectories.
- **Metadata Enrichment**: Connects to external services to fetch accurate track and album information.

## License

This project is licensed under the MIT License.
