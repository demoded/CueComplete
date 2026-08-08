using CueComplete.Core.Metadata;
using System.Diagnostics;

namespace CueComplete.Core;

public class MetadataService
{
    public event Action<string>? OnLog;

    private readonly MusicBrainzProvider _musicBrainzProvider;
    private readonly DiscogsProvider _discogsProvider;

    public MetadataService(string? discogsKey, string? discogsSecret, string? discogsToken)
    {
        _discogsProvider = new DiscogsProvider(discogsKey, discogsSecret, discogsToken, logCallback: Log);
        _musicBrainzProvider = new MusicBrainzProvider(logCallback: Log);
    }

    public static bool IsLoggingEnabled { get; set; } = false;

    private void Log(string message)
    {
        if (IsLoggingEnabled)
        {
            try { System.IO.File.AppendAllText("app.log", $"[{DateTime.Now:O}] {message}\n"); } catch { }
        }
        OnLog?.Invoke(message);
    }

    public async Task<List<CueData>> SearchReleasesAsync(CueData sourceData, bool deepSearch = false)
    {
        Log($"Starting search for: Artist='{sourceData.Artist}', Album='{sourceData.Album}', Barcode='{sourceData.Barcode}', CatNo='{sourceData.CatalogNumber}', DeepSearch={deepSearch}");
        var results = new List<CueData>();

        bool hasCatalogNumber = !string.IsNullOrWhiteSpace(sourceData.CatalogNumber);
        bool hasBarcode = !string.IsNullOrWhiteSpace(sourceData.Barcode);

        if (!deepSearch && !hasCatalogNumber && !hasBarcode)
        {
            Log("No CatalogNumber or Barcode found. Falling back to deep search.");
            deepSearch = true;
        }

        if (!deepSearch)
        {
            if (hasCatalogNumber && _discogsProvider.HasCredentials)
            {
                try
                {
                    Log($"Fast search for Catalog Number: {sourceData.CatalogNumber}");
                    await _discogsProvider.PerformDiscogsTextSearchAsync(sourceData.CatalogNumber!, sourceData, results);
                }
                catch (Exception ex)
                {
                    Log($"Fast search Discogs Error: {ex.Message}");
                }
            }
            else if (hasBarcode && _discogsProvider.HasCredentials)
            {
                try
                {
                    Log($"Fast search for Barcode: {sourceData.Barcode}");
                    await _discogsProvider.PerformDiscogsTextSearchAsync(sourceData.Barcode!, sourceData, results);
                }
                catch (Exception ex)
                {
                    Log($"Fast search Discogs Error: {ex.Message}");
                }
            }
            if (results.Count > 0)
            {
                return results;
            }

            Log("Fast search returned no results. Falling back to deep search.");
            deepSearch = true;
        }

        var mbResults = new List<CueData>();

        // 0. Prioritized Discogs Catalog Number Search
        if (hasCatalogNumber && _discogsProvider.HasCredentials)
        {
            try
            {
                Log($"Prioritized Discogs search for Catalog Number: {sourceData.CatalogNumber}");
                await _discogsProvider.PerformDiscogsTextSearchAsync(sourceData.CatalogNumber!, sourceData, results);
            }
            catch (Exception ex)
            {
                Log($"Priority Discogs Error: {ex.Message}");
            }
        }

        // 1. MusicBrainz Search
        try
        {
            Log("Querying MusicBrainz...");
            mbResults = await _musicBrainzProvider.SearchAsync(sourceData, async (data, discogsTarget) =>
            {
                if (discogsTarget.StartsWith("catno:"))
                {
                    var catNo = discogsTarget.Substring(6);
                    await _discogsProvider.EnrichWithDiscogsCatNoAsync(data, catNo);
                }
                else
                {
                    await _discogsProvider.EnrichWithDiscogsReleaseIdAsync(data, discogsTarget);
                }
            });
            Log($"MusicBrainz returned {mbResults.Count} results.");
            results.AddRange(mbResults);
        }
        catch (Exception ex)
        {
            Log($"MusicBrainz Error: {ex.ToString()}");
            Debug.WriteLine($"MusicBrainz Error: {ex.Message}");
        }

        // 2. Discogs Search
        if (_discogsProvider.HasCredentials)
        {
            try
            {
                Log("Querying Discogs...");
                var discogsResults = await _discogsProvider.SearchAsync(sourceData, mbResults);
                Log($"Discogs returned {discogsResults.Count} results.");
                results.AddRange(discogsResults);
            }
            catch (Exception ex)
            {
                Log($"Discogs Error: {ex.ToString()}");
                Debug.WriteLine($"Discogs Error: {ex.Message}");
            }
        }
        else
        {
            Log("Skipping Discogs search (no credentials found).");
        }

        return results;
    }
}
