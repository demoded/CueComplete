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
        var targetDirectory = args.Length > 0 ? args[0] : Directory.GetCurrentDirectory();
        
        if (!Directory.Exists(targetDirectory))
        {
            Console.WriteLine($"Directory does not exist: {targetDirectory}");
            return;
        }

        Console.WriteLine($"Scanning for .cue files in: {targetDirectory}");
        var cueFiles = Directory.GetFiles(targetDirectory, "*.cue", SearchOption.AllDirectories).ToList();

        if (cueFiles.Count == 0)
        {
            Console.WriteLine("No .cue files found. Exiting.");
            return;
        }

        Console.WriteLine($"Found {cueFiles.Count} .cue file(s).");
        
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
