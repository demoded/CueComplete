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
    public event Action<string>? OnLog;

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
        bool hasDiscogsCreds = !string.IsNullOrWhiteSpace(_discogsToken) || (!string.IsNullOrWhiteSpace(_discogsKey) && !string.IsNullOrWhiteSpace(_discogsSecret));

        if (!deepSearch && !hasCatalogNumber && !hasBarcode)
        {
            Log("No CatalogNumber or Barcode found. Falling back to deep search.");
            deepSearch = true;
        }

        if (!deepSearch)
        {
            if (hasCatalogNumber && hasDiscogsCreds)
            {
                try
                {
                    Log($"Fast search for Catalog Number: {sourceData.CatalogNumber}");
                    await PerformDiscogsTextSearchAsync(sourceData.CatalogNumber, sourceData, results);
                }
                catch (Exception ex)
                {
                    Log($"Fast search Discogs Error: {ex.Message}");
                }
            }
            else if (hasBarcode && hasDiscogsCreds)
            {
                try
                {
                    Log($"Fast search for Barcode: {sourceData.Barcode}");
                    await PerformDiscogsTextSearchAsync(sourceData.Barcode, sourceData, results);
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
        if (hasCatalogNumber && hasDiscogsCreds)
        {
            try
            {
                Log($"Prioritized Discogs search for Catalog Number: {sourceData.CatalogNumber}");
                await PerformDiscogsTextSearchAsync(sourceData.CatalogNumber, sourceData, results);
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
            mbResults = await SearchMusicBrainzAsync(sourceData);
            Log($"MusicBrainz returned {mbResults.Count} results.");
            results.AddRange(mbResults);
        }
        catch (Exception ex)
        {
            Log($"MusicBrainz Error: {ex.ToString()}");
            Debug.WriteLine($"MusicBrainz Error: {ex.Message}");
        }

        // 2. Discogs Search
        if (hasDiscogsCreds)
        {
            try
            {
                Log("Querying Discogs...");
                var discogsResults = await SearchDiscogsAsync(sourceData, mbResults);
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

    private async Task<List<string>> GetMusicBrainzReleaseIdsFromFreeDbAsync(string freeDbId)
    {
        var ids = new List<string>();
        try
        {
            var url = $"https://musicbrainz.org/otherlookup/freedbid?other-lookup.freedbid={freeDbId}";
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode) return ids;
            
            var html = await response.Content.ReadAsStringAsync();
            Log($"FreeDB API raw response for {url}: {html}");
            var matches = Regex.Matches(html, @"href=""/release/([a-f0-9\-]{36})""");
            foreach (Match match in matches)
            {
                ids.Add(match.Groups[1].Value);
            }
        }
        catch (Exception ex)
        {
            Log($"FreeDB lookup error: {ex.Message}");
        }
        return ids.Distinct().ToList();
    }

    private async Task<List<CueData>> SearchMusicBrainzAsync(CueData sourceData)
    {
        var list = new List<CueData>();
        
        if (!string.IsNullOrWhiteSpace(sourceData.DiscId))
        {
            Log($"Looking up FreeDB ID: {sourceData.DiscId}");
            var releaseIds = await GetMusicBrainzReleaseIdsFromFreeDbAsync(sourceData.DiscId);
            foreach (var releaseId in releaseIds.Take(10))
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
                        data.DiscogsId = discogsReleaseId;
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
                }
                catch (Exception ex)
                {
                    Log($"Failed to lookup release by ID {releaseId}: {ex.Message}");
                }
            }
            
            if (list.Count > 0 && !string.IsNullOrWhiteSpace(sourceData.Artist))
            {
                var artistStr = sourceData.Artist.ToLower();
                var filteredList = list.Where(r => r.Artist != null && r.Artist.ToLower().Contains(artistStr)).ToList();
                if (filteredList.Count > 0)
                {
                    Log($"Filtered FreeDB MusicBrainz results by artist '{sourceData.Artist}': count changed from {list.Count} to {filteredList.Count}");
                    list = filteredList;
                }
                else
                {
                    Log($"All FreeDB MusicBrainz results filtered out by artist '{sourceData.Artist}'. Likely a FreeDB ID collision.");
                    list.Clear();
                }
            }

            if (list.Count > 0) return list;
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
                data.DiscogsId = discogsReleaseId;
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

    private async Task FetchAndApplyDiscogsDataAsync(CueData data, string url, bool isDirectReleaseUrl, CueData? sourceData = null)
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
            Log($"Discogs API raw response for {url}: {json}");
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
            }
            
            // Only use Discogs master 'year' value for the DATE field
            if (item.TryGetProperty("master_url", out var masterUrlProp) && !string.IsNullOrWhiteSpace(masterUrlProp.GetString()))
            {
                string mUrl = masterUrlProp.GetString()!;
                try 
                {
                    var mReq = new HttpRequestMessage(HttpMethod.Get, mUrl);
                    if (!string.IsNullOrWhiteSpace(_discogsToken))
                        mReq.Headers.Add("Authorization", $"Discogs token={_discogsToken}");
                    else
                        mReq.Headers.Add("Authorization", $"Discogs key={_discogsKey}, secret={_discogsSecret}");
                    
                    var mRes = await _httpClient.SendAsync(mReq);
                    if (mRes.IsSuccessStatusCode)
                    {
                        var mJson = await mRes.Content.ReadAsStringAsync();
                        Log($"Discogs master API raw response for {mUrl}: {mJson}");
                        using var mDoc = JsonDocument.Parse(mJson);
                        if (mDoc.RootElement.TryGetProperty("year", out var mYear))
                        {
                            data.Date = mYear.ToString();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log($"Failed to fetch master_url for year: {ex.Message}");
                }
            }
            
            if (isDirectReleaseUrl)
            {
                if (item.TryGetProperty("artists", out var artists) && artists.ValueKind == JsonValueKind.Array && artists.GetArrayLength() > 0)
                {
                    var artistObj = artists[0];
                    string? name = artistObj.TryGetProperty("name", out var nProp) ? nProp.GetString() : null;
                    string? anv = artistObj.TryGetProperty("anv", out var aProp) ? aProp.GetString() : null;
                    
                    name = CleanDiscogsString(name);
                    string finalArtist = string.IsNullOrWhiteSpace(anv) ? (name ?? "") : $"{anv} = {name}";
                    
                    if (!string.IsNullOrWhiteSpace(finalArtist))
                        data.Artist = finalArtist;
                }
                    
                if (string.IsNullOrEmpty(data.Album) && item.TryGetProperty("title", out var title))
                    data.Album = title.GetString();
                    
                if (string.IsNullOrEmpty(data.Label) && item.TryGetProperty("labels", out var labels) && labels.ValueKind == JsonValueKind.Array && labels.GetArrayLength() > 0)
                {
                    data.Label = CleanDiscogsString(labels[0].GetProperty("name").GetString());
                    if (labels[0].TryGetProperty("catno", out var catno))
                        data.CatalogNumber = catno.GetString();
                }
                
                if (string.IsNullOrEmpty(data.Country) && item.TryGetProperty("country", out var country))
                    data.Country = country.GetString();
                    
                if (string.IsNullOrEmpty(data.Barcode) && item.TryGetProperty("identifiers", out var idents) && idents.ValueKind == JsonValueKind.Array)
                {
                    foreach (var ident in idents.EnumerateArray())
                    {
                        if (ident.TryGetProperty("type", out var t) && t.GetString() == "Barcode")
                        {
                            data.Barcode = ident.GetProperty("value").GetString();
                            break;
                        }
                    }
                }
            }
            
            string? genreValue = null;
            if (item.TryGetProperty("style", out var styles) && styles.ValueKind == JsonValueKind.Array && styles.GetArrayLength() > 0)
                genreValue = styles[0].GetString();
            if (string.IsNullOrWhiteSpace(genreValue) && item.TryGetProperty("genre", out var genres) && genres.ValueKind == JsonValueKind.Array && genres.GetArrayLength() > 0)
                genreValue = genres[0].GetString();
            if (!string.IsNullOrWhiteSpace(genreValue))
                data.Genre = data.Genre ?? genreValue;

            if (isDirectReleaseUrl && item.TryGetProperty("tracklist", out var tracklist) && tracklist.ValueKind == JsonValueKind.Array)
            {
                int totalTrackCount = 0;
                var discTracks = new Dictionary<string, int>();
                var discTitles = new Dictionary<string, string>();
                string currentHeading = "";
                
                foreach (var trackItem in tracklist.EnumerateArray())
                {
                    if (trackItem.TryGetProperty("type_", out var typeProp))
                    {
                        var tType = typeProp.GetString();
                        if (tType == "heading")
                        {
                            currentHeading = trackItem.TryGetProperty("title", out var titleProp) ? titleProp.GetString() ?? "" : "";
                        }
                        else if (tType == "track")
                        {
                            if (trackItem.TryGetProperty("position", out var posProp))
                            {
                                var pos = posProp.GetString() ?? "";
                                var posLower = pos.ToLowerInvariant();
                                if (posLower.Contains("video") || posLower.Contains("data") || posLower.Contains("cd-rom") || posLower.Contains("cdrom") || posLower.Contains("multimedia") || posLower.Contains("enhanced") || posLower.Contains("dvd") || posLower.Contains("blu"))
                                {
                                    continue;
                                }
                                
                                string prefix = pos;
                                int dashIndex = pos.IndexOf('-');
                                if (dashIndex > 0)
                                {
                                    prefix = pos.Substring(0, dashIndex).Trim();
                                }
                                else 
                                {
                                    prefix = "Default";
                                    var match = System.Text.RegularExpressions.Regex.Match(pos, @"^([a-zA-Z]+|\d+)");
                                    if (match.Success) prefix = match.Value;
                                }
                                
                                if (!discTracks.ContainsKey(prefix))
                                {
                                    discTracks[prefix] = 0;
                                    discTitles[prefix] = currentHeading; 
                                }
                                discTracks[prefix]++;
                                totalTrackCount++;
                            }
                        }
                    }
                }
                
                data.Tracks = totalTrackCount;
                
                if (sourceData != null && sourceData.Tracks.HasValue && sourceData.Tracks.Value > 0)
                {
                    var matchedDiscs = discTracks.Where(kvp => kvp.Value == sourceData.Tracks.Value).ToList();
                    if (matchedDiscs.Count == 1)
                    {
                        var matchedPrefix = matchedDiscs[0].Key;
                        var orderedPrefixes = discTracks.Keys.ToList();
                        
                        data.DiscNumber = orderedPrefixes.IndexOf(matchedPrefix) + 1;
                        data.Discs = orderedPrefixes.Count;
                        
                        var title = discTitles.ContainsKey(matchedPrefix) ? discTitles[matchedPrefix] : null;
                        if (!string.IsNullOrWhiteSpace(title))
                        {
                            data.Comment = title;
                        }
                    }
                }
            }

            if (isDirectReleaseUrl && item.TryGetProperty("formats", out var formats) && formats.ValueKind == JsonValueKind.Array)
            {
                int discs = 0;
                foreach (var format in formats.EnumerateArray())
                {
                    if (format.TryGetProperty("qty", out var qtyStr) && int.TryParse(qtyStr.GetString(), out int q))
                        discs += q;
                }
                // Only overwrite Discs from formats if we haven't already determined it from multi-CD logic above, 
                // or if multi-CD logic yielded 1 disc but format says otherwise.
                if (discs > 0 && (!data.Discs.HasValue || data.Discs.Value <= 1)) data.Discs = discs;
            }
        }
        catch (Exception ex)
        {
            Log($"Discogs enrichment failed: {ex.Message}");
        }
    }

    private async Task<List<CueData>> SearchDiscogsAsync(CueData sourceData, List<CueData> mbResults)
    {
        var list = new List<CueData>();
        
        var discogsIds = mbResults.Where(r => !string.IsNullOrWhiteSpace(r.DiscogsId)).Select(r => r.DiscogsId!).Distinct().Take(10).ToList();
        var barcodes = mbResults.Where(r => !string.IsNullOrWhiteSpace(r.Barcode)).Select(r => r.Barcode!).Distinct().Take(10).ToList();
        
        if (!string.IsNullOrWhiteSpace(sourceData.DiscogsId) && !discogsIds.Contains(sourceData.DiscogsId))
            discogsIds.Insert(0, sourceData.DiscogsId);
            
        if (!string.IsNullOrWhiteSpace(sourceData.Barcode) && !barcodes.Contains(sourceData.Barcode))
            barcodes.Insert(0, sourceData.Barcode);
            
        // 1. Fetch exact releases by DiscogsId
        foreach (var id in discogsIds)
        {
            var data = new CueData { Source = "[DC]", DiscogsId = id };
            await FetchAndApplyDiscogsDataAsync(data, $"https://api.discogs.com/releases/{id}", true, sourceData);
            if (!string.IsNullOrEmpty(data.Album))
            {
                list.Add(data);
            }
        }
        
        // 2. Fetch by Barcodes
        foreach (var barcode in barcodes)
        {
            await PerformDiscogsTextSearchAsync(barcode, sourceData, list);
        }
        
        // 3. Fallback to Text Search if nothing was found
        if (discogsIds.Count == 0 && barcodes.Count == 0 && list.Count == 0 && string.IsNullOrWhiteSpace(sourceData.CatalogNumber))
        {
            string query = $"{sourceData.Artist} {sourceData.Album}";
            await PerformDiscogsTextSearchAsync(query, sourceData, list);
        }

        return list;
    }
    
    private async Task PerformDiscogsTextSearchAsync(string query, CueData sourceData, List<CueData> list)
    {
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
            return;
        }

        var json = await response.Content.ReadAsStringAsync();
        Log($"Discogs raw JSON response: {json}");
        using var doc = JsonDocument.Parse(json);
        
        var resultsArray = doc.RootElement.GetProperty("results");
        var tasks = new List<Task>();
        
        foreach (var item in resultsArray.EnumerateArray().Take(5))
        {
            var data = new CueData
            {
                Source = "[DC]",
                Artist = sourceData.Artist, 
                Album = item.GetProperty("title").GetString(), 
            };
            
            if (item.TryGetProperty("barcode", out var dbBarcodes) && dbBarcodes.ValueKind == JsonValueKind.Array)
            {
                foreach (var b in dbBarcodes.EnumerateArray())
                {
                    var barcodeStr = b.GetString();
                    if (!string.IsNullOrWhiteSpace(barcodeStr))
                    {
                        var digitCount = barcodeStr.Count(char.IsDigit);
                        if (digitCount >= 6 && !barcodeStr.Contains("JASRAC", StringComparison.OrdinalIgnoreCase))
                        {
                            data.Barcode = barcodeStr;
                            break;
                        }
                    }
                }
            }
            
            if (item.TryGetProperty("catno", out var catno))
                data.CatalogNumber = catno.GetString();
                
            if (item.TryGetProperty("country", out var country))
                data.Country = country.GetString();
                
            if (item.TryGetProperty("label", out var labels) && labels.ValueKind == JsonValueKind.Array && labels.GetArrayLength() > 0)
                data.Label = CleanDiscogsString(labels[0].GetString());
                
            string? searchGenreValue = null;
            if (item.TryGetProperty("style", out var searchStyles) && searchStyles.ValueKind == JsonValueKind.Array && searchStyles.GetArrayLength() > 0)
                searchGenreValue = searchStyles[0].GetString();
            if (string.IsNullOrWhiteSpace(searchGenreValue) && item.TryGetProperty("genre", out var searchGenres) && searchGenres.ValueKind == JsonValueKind.Array && searchGenres.GetArrayLength() > 0)
                searchGenreValue = searchGenres[0].GetString();
            if (!string.IsNullOrWhiteSpace(searchGenreValue))
                data.Genre = searchGenreValue;
            
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

            if (item.TryGetProperty("resource_url", out var resourceUrl))
            {
                tasks.Add(FetchAndApplyDiscogsDataAsync(data, resourceUrl.GetString(), true, sourceData));
            }

            list.Add(data);
        }
        
        await Task.WhenAll(tasks);
    }

    private static string? CleanDiscogsString(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return input;
        return Regex.Replace(input, @" \(\d+\)$", "").Trim();
    }
}
