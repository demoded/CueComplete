# Rules

- Whenever instructed to build the project, ALWAYS run the publish command immediately afterward to package it as a single executable: `dotnet publish -c Release -r win-x64 -p:PublishSingleFile=true --self-contained true`.
