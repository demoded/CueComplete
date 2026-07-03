using MetaBrainz.MusicBrainz;
using System.Net.Http.Headers;
using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Web;
using MetaBrainz.MusicBrainz.Interfaces.Entities;

namespace CueComplete.Core;

public class MetadataService
{
    private readonly Query _mbClient;
    private readonly HttpClient _httpClient;
    private readonly string? _discogsKey;
    private readonly string? _discogsSecret;
    private readonly string? _discogsToken;

    public MetadataService(string? discogsKey, string? discogsSecret, string? discogsToken)
    {
        _discogsKey = discogsKey;
        _discogsSecret = discogsSecret;
        _discogsToken = discogsToken;
        _mbClient = new Query("CueComplete", "1.0", "mailto:user@example.com");
        
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("CueComplete", "1.0"));
    }

    private void Log(string message)
    {
        try { System.IO.File.AppendAllText("app.log", $"[{DateTime.Now:O}] {message}\n"); } catch { }
    }

    public async Task<List<CueData>> SearchReleasesAsync(CueData sourceData)
    {
        Log($"Starting search for: Artist='{sourceData.Artist}', Album='{sourceData.Album}', Barcode='{sourceData.Barcode}'");
        var results = new List<CueData>();
        
        // 1. MusicBrainz Search
        try
        {
            Log("Querying MusicBrainz...");
            var mbResults = await SearchMusicBrainzAsync(sourceData);
            Log($"MusicBrainz returned {mbResults.Count} results.");
            results.AddRange(mbResults);
        }
        catch (Exception ex)
        {
            Log($"MusicBrainz Error: {ex.ToString()}");
            Debug.WriteLine($"MusicBrainz Error: {ex.Message}");
        }

        // 2. Discogs Search
        if (!string.IsNullOrWhiteSpace(_discogsToken) || 
           (!string.IsNullOrWhiteSpace(_discogsKey) && !string.IsNullOrWhiteSpace(_discogsSecret)))
        {
            try
            {
                Log("Querying Discogs...");
                var discogsResults = await SearchDiscogsAsync(sourceData);
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

    private async Task<string?> GetMusicBrainzReleaseIdFromFreeDbAsync(string freeDbId)
    {
        try
        {
            var url = $"https://musicbrainz.org/otherlookup/freedbid?other-lookup.freedbid={freeDbId}";
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode) return null;
            
            var html = await response.Content.ReadAsStringAsync();
            var match = Regex.Match(html, @"href=""/release/([a-f0-9\-]{36})""");
            if (match.Success)
            {
                return match.Groups[1].Value;
            }
        }
        catch (Exception ex)
        {
            Log($"FreeDB lookup error: {ex.Message}");
        }
        return null;
    }

    private async Task<List<CueData>> SearchMusicBrainzAsync(CueData sourceData)
    {
        var list = new List<CueData>();
        
        if (!string.IsNullOrWhiteSpace(sourceData.DiscId))
        {
            Log($"Looking up FreeDB ID: {sourceData.DiscId}");
            var releaseId = await GetMusicBrainzReleaseIdFromFreeDbAsync(sourceData.DiscId);
            if (!string.IsNullOrWhiteSpace(releaseId))
            {
                Log($"Found MusicBrainz Release ID via FreeDB: {releaseId}");
                try
                {
                    var release = await _mbClient.LookupReleaseAsync(Guid.Parse(releaseId), Include.Labels | Include.Genres | Include.UrlRelationships | Include.Recordings);
                    var data = new CueData
                    {
                        Source = "[MB]",
                        Artist = release.ArtistCredit?.FirstOrDefault()?.Name ?? sourceData.Artist,
                        Album = release.Title,
                        Barcode = release.Barcode,
                        Date = release.Date?.ToString(),
                        ReleaseDate = release.Date?.ToString(),
                        Country = release.Country,
                        Discs = release.Media?.Count,
                        Tracks = release.Media?.Sum(m => m.TrackCount)
                    };
                    
                    if (release.LabelInfo != null && release.LabelInfo.Count > 0)
                    {
                        var labelInfo = release.LabelInfo[0];
                        data.Label = labelInfo.Label?.Name;
                        data.CatalogNumber = labelInfo.CatalogNumber;
                    }

                    if (release.Genres != null && release.Genres.Count > 0)
                    {
                        data.Genre = release.Genres[0].Name;
                    }

                    string? discogsReleaseId = null;
                    if (release.Relationships != null)
                    {
                        foreach (var rel in release.Relationships)
                        {
                            if (rel.Type == "discogs" && rel.TargetType == EntityType.Url && rel.Url?.Resource != null)
                            {
                                var match = Regex.Match(rel.Url.Resource.ToString(), @"discogs\.com/release/(\d+)");
                                if (match.Success)
                                {
                                    discogsReleaseId = match.Groups[1].Value;
                                    break;
                                }
                            }
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(discogsReleaseId))
                    {
                        Log($"Found Discogs link in MusicBrainz: {discogsReleaseId}");
                        await EnrichWithDiscogsReleaseIdAsync(data, discogsReleaseId);
                    }
                    else if (!string.IsNullOrWhiteSpace(data.CatalogNumber))
                    {
                        Log($"No Discogs link, trying to enrich via CatNo: {data.CatalogNumber}");
                        await EnrichWithDiscogsCatNoAsync(data, data.CatalogNumber);
                    }

                    Log($"MusicBrainz match (FreeDB): ID={release.Id}, Title={release.Title}, Date={release.Date}, Barcode={release.Barcode}");
                    list.Add(data);
                    return list;
                }
                catch (Exception ex)
                {
                    Log($"Failed to lookup release by ID {releaseId}: {ex.Message}");
                }
            }
        }

        string query = "";
        if (!string.IsNullOrWhiteSpace(sourceData.Barcode))
            query = $"barcode:\"{sourceData.Barcode}\"";
        else if (!string.IsNullOrWhiteSpace(sourceData.Artist) && !string.IsNullOrWhiteSpace(sourceData.Album))
            query = $"artist:\"{sourceData.Artist}\" AND release:\"{sourceData.Album}\"";
        else
            return list;
            
        Log($"MusicBrainz query: {query}");
        var searchResults = await _mbClient.FindReleasesAsync(query, 5);
        
        var resultsList = searchResults.Results.ToList();
        if (resultsList.Count > 1 && !string.IsNullOrWhiteSpace(sourceData.Artist))
        {
            var artistStr = sourceData.Artist.ToLower();
            var filtered = resultsList.Where(r => 
                r.Item.ArtistCredit != null && 
                r.Item.ArtistCredit.Any(ac => ac.Name != null && ac.Name.ToLower().Contains(artistStr))
            ).ToList();
            
            if (filtered.Count > 0)
            {
                Log($"Filtered MusicBrainz results by artist '{sourceData.Artist}': count changed from {resultsList.Count} to {filtered.Count}");
                resultsList = filtered;
            }
        }
        
        foreach (var result in resultsList)
        {
            var release = result.Item;
            
            // Try to get detailed release for more info (like labels/genres and url relations)
            try {
                release = await _mbClient.LookupReleaseAsync(release.Id, Include.Labels | Include.Genres | Include.UrlRelationships | Include.Recordings);
            } catch { /* Ignore lookup failure */ }

            var data = new CueData
            {
                Source = "[MB]",
                Artist = release.ArtistCredit?.FirstOrDefault()?.Name ?? sourceData.Artist,
                Album = release.Title,
                Barcode = release.Barcode,
                Date = release.Date?.ToString(),
                ReleaseDate = release.Date?.ToString(),
                Country = release.Country,
                Discs = release.Media?.Count,
                Tracks = release.Media?.Sum(m => m.TrackCount)
            };
            
            if (release.LabelInfo != null && release.LabelInfo.Count > 0)
            {
                var labelInfo = release.LabelInfo[0];
                data.Label = labelInfo.Label?.Name;
                data.CatalogNumber = labelInfo.CatalogNumber;
            }

            if (release.Genres != null && release.Genres.Count > 0)
            {
                data.Genre = release.Genres[0].Name;
            }

            string? discogsReleaseId = null;
            if (release.Relationships != null)
            {
                foreach (var rel in release.Relationships)
                {
                    if (rel.Type == "discogs" && rel.TargetType == EntityType.Url && rel.Url?.Resource != null)
                    {
                        var match = Regex.Match(rel.Url.Resource.ToString(), @"discogs\.com/release/(\d+)");
                        if (match.Success)
                        {
                            discogsReleaseId = match.Groups[1].Value;
                            break;
                        }
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(discogsReleaseId))
            {
                Log($"Found Discogs link in MusicBrainz: {discogsReleaseId}");
                await EnrichWithDiscogsReleaseIdAsync(data, discogsReleaseId);
            }
            else if (!string.IsNullOrWhiteSpace(data.CatalogNumber))
            {
                Log($"No Discogs link, trying to enrich via CatNo: {data.CatalogNumber}");
                await EnrichWithDiscogsCatNoAsync(data, data.CatalogNumber);
            }

            Log($"MusicBrainz match: ID={release.Id}, Title={release.Title}, Date={release.Date}, Barcode={release.Barcode}");
            list.Add(data);
        }

        return list;
    }

    private async Task EnrichWithDiscogsReleaseIdAsync(CueData data, string discogsId)
    {
        Log($"Enriching with Discogs Release ID: {discogsId}");
        var url = $"https://api.discogs.com/releases/{discogsId}";
        await FetchAndApplyDiscogsDataAsync(data, url, true);
    }

    private async Task EnrichWithDiscogsCatNoAsync(CueData data, string catNo)
    {
        Log($"Enriching with Discogs CatNo: {catNo}");
        var url = $"https://api.discogs.com/database/search?catno={HttpUtility.UrlEncode(catNo)}&type=release";
        await FetchAndApplyDiscogsDataAsync(data, url, false);
    }

    private async Task FetchAndApplyDiscogsDataAsync(CueData data, string url, bool isDirectReleaseUrl)
    {
        if (string.IsNullOrWhiteSpace(_discogsToken) && 
           (string.IsNullOrWhiteSpace(_discogsKey) || string.IsNullOrWhiteSpace(_discogsSecret)))
        {
            return;
        }

        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            if (!string.IsNullOrWhiteSpace(_discogsToken))
                request.Headers.Add("Authorization", $"Discogs token={_discogsToken}");
            else
                request.Headers.Add("Authorization", $"Discogs key={_discogsKey}, secret={_discogsSecret}");

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode) return;

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            JsonElement item = doc.RootElement;
            if (!isDirectReleaseUrl)
            {
                var resultsArray = doc.RootElement.GetProperty("results");
                if (resultsArray.GetArrayLength() == 0) return;
                item = resultsArray[0];
            }

            if (isDirectReleaseUrl && item.TryGetProperty("released", out var released))
            {
                data.ReleaseDate = released.GetString() ?? data.ReleaseDate;
                data.Date = released.GetString() ?? data.Date;
            }
            else if (!isDirectReleaseUrl && item.TryGetProperty("year", out var year))
            {
                data.Date = year.GetString() ?? data.Date;
            }
            
            if (item.TryGetProperty("genre", out var genres) && genres.ValueKind == JsonValueKind.Array && genres.GetArrayLength() > 0)
                data.Genre = data.Genre ?? genres[0].GetString();

            if (isDirectReleaseUrl && item.TryGetProperty("tracklist", out var tracklist) && tracklist.ValueKind == JsonValueKind.Array)
            {
                data.Tracks = tracklist.GetArrayLength();
            }

            if (isDirectReleaseUrl && item.TryGetProperty("formats", out var formats) && formats.ValueKind == JsonValueKind.Array)
            {
                int discs = 0;
                foreach (var format in formats.EnumerateArray())
                {
                    if (format.TryGetProperty("qty", out var qtyStr) && int.TryParse(qtyStr.GetString(), out int q))
                        discs += q;
                }
                if (discs > 0) data.Discs = discs;
            }
        }
        catch (Exception ex)
        {
            Log($"Discogs enrichment failed: {ex.Message}");
        }
    }

    private async Task<List<CueData>> SearchDiscogsAsync(CueData sourceData)
    {
        var list = new List<CueData>();
        
        string query = $"{sourceData.Artist} {sourceData.Album}";
        if (!string.IsNullOrWhiteSpace(sourceData.Barcode))
            query = sourceData.Barcode;

        var url = $"https://api.discogs.com/database/search?q={HttpUtility.UrlEncode(query)}&type=release";
        Log($"Discogs query URL: {url}");
        
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        
        if (!string.IsNullOrWhiteSpace(_discogsToken))
        {
            request.Headers.Add("Authorization", $"Discogs token={_discogsToken}");
        }
        else
        {
            request.Headers.Add("Authorization", $"Discogs key={_discogsKey}, secret={_discogsSecret}");
        }

        var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            Log($"Discogs HTTP request failed with status: {response.StatusCode}");
            return list;
        }

        var json = await response.Content.ReadAsStringAsync();
        Log($"Discogs raw JSON response: {json}");
        using var doc = JsonDocument.Parse(json);
        
        var resultsArray = doc.RootElement.GetProperty("results");
        foreach (var item in resultsArray.EnumerateArray().Take(5))
        {
            var data = new CueData
            {
                Source = "[DC]",
                Artist = sourceData.Artist, 
                Album = item.GetProperty("title").GetString(), 
            };
            
            if (item.TryGetProperty("barcode", out var barcodes) && barcodes.ValueKind == JsonValueKind.Array && barcodes.GetArrayLength() > 0)
                data.Barcode = barcodes[0].GetString();
            
            if (item.TryGetProperty("year", out var year))
                data.Date = year.GetString();
                
            if (item.TryGetProperty("country", out var country))
                data.Country = country.GetString();
                
            if (item.TryGetProperty("label", out var labels) && labels.ValueKind == JsonValueKind.Array && labels.GetArrayLength() > 0)
                data.Label = CleanDiscogsString(labels[0].GetString());
                
            if (item.TryGetProperty("genre", out var genres) && genres.ValueKind == JsonValueKind.Array && genres.GetArrayLength() > 0)
                data.Genre = genres[0].GetString();
            
            if (item.TryGetProperty("format_quantity", out var qty) && qty.ValueKind == JsonValueKind.Number)
                data.Discs = qty.GetInt32();

            if (data.Album != null && data.Album.Contains(" - "))
            {
                var parts = data.Album.Split(new[] { " - " }, 2, StringSplitOptions.None);
                if (parts.Length == 2)
                {
                    data.Artist = CleanDiscogsString(parts[0].Trim());
                    data.Album = CleanDiscogsString(parts[1].Trim());
                }
            }

            list.Add(data);
        }

        return list;
    }

    private static string? CleanDiscogsString(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return input;
        return Regex.Replace(input, @" \(\d+\)$", "").Trim();
    }
}
