# Rules

- Whenever instructed to build the project, ALWAYS run the publish command immediately afterward to package it as a single executable: `dotnet publish -c Release -r win-x64 -p:PublishSingleFile=true --self-contained true`.
- The 'rg' (ripgrep) CLI tool is installed and available in the terminal. If 'grep_search' fails (e.g. due to binary file detection or long lines), use 'run_command' with 'rg' and appropriate flags like '-a'.
