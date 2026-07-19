# Rules

- Whenever instructed to build the project, ALWAYS run the publish command immediately afterward to package it as a single executable: `dotnet publish -c Release -r win-x64 -p:PublishSingleFile=true --self-contained true`.
- **Note on Publishing/Releases**: Official binary releases are automated via GitHub Actions. Do not publish on every commit. To trigger a new release, update the `<Version>` in `CueComplete.csproj`, create a semantic version tag (e.g., `v1.0.1`), and push the tag to GitHub.
- The 'rg' (ripgrep) CLI tool is installed and available in the terminal. If 'grep_search' fails (e.g. due to binary file detection or long lines), use 'run_command' with 'rg' and appropriate flags like '-a'.
- ALWAYS allow commands that start with `Get-Content` without asking for permission, as this has been explicitly granted by the user for this project.
- **Commit Messages**: ALWAYS use Conventional Commits formatting for git commit messages so the automated GitHub Action can generate categorized release notes. Use prefixes like `feat: ` (new features), `fix: ` (bug fixes), `docs: ` (documentation), or `chore: ` (maintenance/refactoring).
