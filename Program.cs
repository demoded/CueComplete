using Terminal.Gui;
using CueComplete.Core;
using CueComplete.UI;
using DotNetEnv;

namespace CueComplete;

class Program
{
    static void Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        // Load .env from user profile
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var envPath = Path.Combine(userProfile, ".env");
        if (File.Exists(envPath))
        {
            Env.Load(envPath);
        }

        var consumerKey = Environment.GetEnvironmentVariable("DISCOGS_CONSUMER_KEY");
        var consumerSecret = Environment.GetEnvironmentVariable("DISCOGS_CONSUMER_SECRET");
        var personalAccessToken = Environment.GetEnvironmentVariable("DISCOGS_PERSONAL_ACCESS_TOKEN");

        var metadataService = new MetadataService(consumerKey, consumerSecret, personalAccessToken);

        try
        {
            Application.UseSystemConsole = true;
            Application.Init();

            string? targetDirectory = args.Length > 0 ? args[0] : null;

            if (string.IsNullOrEmpty(targetDirectory))
            {
                var openDialog = new OpenDialog("Select Directory", "Choose a directory containing .cue files")
                {
                    CanChooseFiles = false,
                    CanChooseDirectories = true,
                    AllowsMultipleSelection = false
                };

                Application.Run(openDialog);

                if (openDialog.Canceled)
                {
                    Application.Shutdown();
                    return;
                }

                targetDirectory = openDialog.FilePath.ToString();
            }

            if (!Directory.Exists(targetDirectory))
            {
                MessageBox.Query("Error", $"Directory does not exist:\n{targetDirectory}", "OK");
                Application.Shutdown();
                return;
            }

            var cueFiles = Directory.GetFiles(targetDirectory, "*.cue", SearchOption.AllDirectories).ToList();

            if (cueFiles.Count == 0)
            {
                MessageBox.Query("Info", "No .cue files found in the selected directory.", "OK");
                Application.Shutdown();
                return;
            }

            var mainWindow = new MainWindow(cueFiles, metadataService);
            Application.Run(mainWindow);
            Application.Shutdown();
        }
        catch (Exception ex)
        {
            File.WriteAllText("crash.log", ex.ToString());
            Console.WriteLine("A fatal error occurred. Please check crash.log for details.");
        }
    }
}
