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

    public MetadataService(DiscogsProvider discogsProvider, MusicBrainzProvider? musicBrainzProvider = null)
    {
        _discogsProvider = discogsProvider;
        _musicBrainzProvider = musicBrainzProvider ?? new MusicBrainzProvider(logCallback: Log);
    }

    public static bool IsLoggingEnabled { get; set; } = false;
    private static readonly object _logLock = new();

    public void Log(string message)
    {
        if (IsLoggingEnabled)
        {
            lock (_logLock)
            {
                try { System.IO.File.AppendAllText("app.log", $"[{DateTime.Now:O}] {message}\n"); } catch { }
            }
        }
        OnLog?.Invoke(message);
    }

    public async Task<List<CueData>> SearchReleasesAsync(CueData sourceData, bool deepSearch = false)
    {
        Log($"Starting search for: Artist='{sourceData.Artist}', Album='{sourceData.Album}', Barcode='{sourceData.Barcode}', CatNo='{sourceData.CatalogNumber}', DeepSearch={deepSearch}");
        var results = new List<CueData>();

        bool hasCatalogNumber = !string.IsNullOrWhiteSpace(sourceData.CatalogNumber);
        bool hasBarcode = !string.IsNullOrWhiteSpace(sourceData.Barcode) && DiscogsProvider.IsValidBarcodeCandidate(sourceData.Barcode);

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
                    await _discogsProvider.PerformDiscogsCatNoSearchAsync(sourceData.CatalogNumber!, sourceData, results);
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
                    await _discogsProvider.PerformDiscogsBarcodeSearchAsync(sourceData.Barcode!, sourceData, results);
                }
                catch (Exception ex)
                {
                    Log($"Fast search Discogs Error: {ex.Message}");
                }
            }
            if (results.Count == 0 && _discogsProvider.HasCredentials && !string.IsNullOrWhiteSpace(sourceData.Artist) && !string.IsNullOrWhiteSpace(sourceData.Album))
            {
                try
                {
                    Log($"Fast search: 0 matching CatNo/Barcode results found. Falling back to search by artist and album: '{sourceData.Artist} - {sourceData.Album}'");
                    string query = $"{sourceData.Artist} {sourceData.Album}".Trim();
                    await _discogsProvider.PerformDiscogsTextSearchAsync(query, sourceData, results);

                    if (results.Count == 0)
                    {
                        var cleanAlbum = StringExtensions.StripTitleAnnotations(sourceData.Album);
                        if (!string.IsNullOrWhiteSpace(cleanAlbum) && !string.Equals(cleanAlbum, sourceData.Album, StringComparison.OrdinalIgnoreCase))
                        {
                            string fallbackQuery = $"{sourceData.Artist} {cleanAlbum}".Trim();
                            Log($"Fast search with full album returned 0 results. Retrying with stripped album title: '{cleanAlbum}'");
                            await _discogsProvider.PerformDiscogsTextSearchAsync(fallbackQuery, sourceData, results);
                        }
                    }

                    if (results.Count == 0)
                    {
                        var acronym = StringExtensions.GetAcronym(sourceData.Artist);
                        if (!string.IsNullOrWhiteSpace(acronym))
                        {
                            string acronymQuery = $"{acronym} {sourceData.Album}".Trim();
                            Log($"Fast search returned 0 results. Retrying with artist acronym query: '{acronymQuery}'");
                            await _discogsProvider.PerformDiscogsTextSearchAsync(acronymQuery, sourceData, results);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log($"Fast search Discogs Text Search Error: {ex.Message}");
                }
            }

            if (results.Count > 0)
            {
                return ScoreAndOrderResults(results, sourceData);
            }

            Log("Fast search returned no results. Falling back to deep search.");
            deepSearch = true;
        }

        var mbResults = new List<CueData>();

        // 0. Prioritized Discogs Catalog Number Search
        if (hasCatalogNumber && _discogsProvider.HasCredentials && results.Count == 0)
        {
            try
            {
                Log($"Prioritized Discogs search for Catalog Number: {sourceData.CatalogNumber}");
                await _discogsProvider.PerformDiscogsCatNoSearchAsync(sourceData.CatalogNumber!, sourceData, results);
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

        return ScoreAndOrderResults(results, sourceData);
    }

    public static List<CueData> ScoreAndOrderResults(List<CueData> results, CueData sourceData)
    {
        if (results == null || results.Count == 0 || sourceData == null)
            return results ?? new List<CueData>();

        foreach (var r in results)
        {
            r.Score = ReleaseScorer.CalculateScore(r, sourceData);
        }

        return results
            .OrderByDescending(r => r.Score ?? 0)
            .ToList();
    }
}
