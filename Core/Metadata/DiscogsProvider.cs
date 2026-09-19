using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Web;
using CueComplete.Core.Metadata.Models;

namespace CueComplete.Core.Metadata;

public class DiscogsProvider : IMetadataProvider
{
    public string Name => "Discogs";

    private readonly HttpClient _httpClient;
    private readonly string? _discogsKey;
    private readonly string? _discogsSecret;
    private readonly string? _discogsToken;
    private readonly Action<string>? _logCallback;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public DiscogsProvider(string? discogsKey, string? discogsSecret, string? discogsToken, HttpClient? httpClient = null, Action<string>? logCallback = null)
    {
        _discogsKey = discogsKey;
        _discogsSecret = discogsSecret;
        _discogsToken = discogsToken;
        _logCallback = logCallback;

        if (httpClient != null)
        {
            _httpClient = httpClient;
        }
        else
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("CueComplete", "1.0"));
        }
    }

    public bool HasCredentials => !string.IsNullOrWhiteSpace(_discogsToken) || (!string.IsNullOrWhiteSpace(_discogsKey) && !string.IsNullOrWhiteSpace(_discogsSecret));

    private void Log(string message)
    {
        _logCallback?.Invoke(message);
    }

    private void AddAuthHeader(HttpRequestMessage request)
    {
        if (!string.IsNullOrWhiteSpace(_discogsToken))
        {
            request.Headers.Add("Authorization", $"Discogs token={_discogsToken}");
        }
        else if (!string.IsNullOrWhiteSpace(_discogsKey) && !string.IsNullOrWhiteSpace(_discogsSecret))
        {
            request.Headers.Add("Authorization", $"Discogs key={_discogsKey}, secret={_discogsSecret}");
        }
    }

    public async Task<List<CueData>> SearchAsync(CueData sourceData)
    {
        return await SearchAsync(sourceData, new List<CueData>());
    }

    public async Task<List<CueData>> SearchAsync(CueData sourceData, List<CueData> mbResults)
    {
        var list = new List<CueData>();
        if (!HasCredentials)
        {
            Log("Skipping Discogs search (no credentials found).");
            return list;
        }

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
            await FetchAndApplyDiscogsDataAsync(data, $"https://api.discogs.com/releases/{id}", isDirectReleaseUrl: true, sourceData: sourceData);
            if (!string.IsNullOrEmpty(data.Album))
            {
                list.Add(data);
            }
        }

        // 2. Fetch by Barcodes
        foreach (var barcode in barcodes)
        {
            await PerformDiscogsBarcodeSearchAsync(barcode, sourceData, list);
        }

        // 3. Fallback to Text Search if nothing was found
        if (discogsIds.Count == 0 && barcodes.Count == 0 && list.Count == 0 && string.IsNullOrWhiteSpace(sourceData.CatalogNumber))
        {
            string query = $"{sourceData.Artist} {sourceData.Album}";
            await PerformDiscogsTextSearchAsync(query, sourceData, list);
        }

        if (list.Count > 0 && !string.IsNullOrWhiteSpace(sourceData.Artist))
        {
            var filtered = list.Where(d => StringExtensions.MatchesArtist(d.Artist, sourceData.Artist)).ToList();

            if (filtered.Count < list.Count)
            {
                Log($"Filtered Discogs search results by artist '{sourceData.Artist}': count changed from {list.Count} to {filtered.Count}");
                list = filtered;
            }
        }

        return list;
    }

    public Task PerformDiscogsTextSearchAsync(string query, CueData sourceData, List<CueData> list)
        => PerformDiscogsSearchAsync("q", query, sourceData, list);

    public Task PerformDiscogsCatNoSearchAsync(string catNo, CueData sourceData, List<CueData> list)
        => PerformDiscogsSearchAsync("catno", catNo, sourceData, list);

    public Task PerformDiscogsBarcodeSearchAsync(string barcode, CueData sourceData, List<CueData> list)
        => PerformDiscogsSearchAsync("barcode", barcode, sourceData, list);

    public async Task PerformDiscogsSearchAsync(string paramName, string query, CueData sourceData, List<CueData> list)
    {
        if (string.IsNullOrWhiteSpace(query)) return;

        var url = $"https://api.discogs.com/database/search?{paramName}={HttpUtility.UrlEncode(query)}&type=release";
        Log($"Discogs query URL: {url}");

        var request = new HttpRequestMessage(HttpMethod.Get, url);
        AddAuthHeader(request);

        var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            Log($"Discogs HTTP request failed with status: {response.StatusCode}");
            return;
        }

        var json = await response.Content.ReadAsStringAsync();
        Log($"Discogs raw JSON response: {json}");

        var searchResponse = JsonSerializer.Deserialize<DiscogsSearchResponse>(json, JsonOptions);
        if (searchResponse?.Results == null || searchResponse.Results.Count == 0) return;

        var tasks = new List<Task>();

        foreach (var item in searchResponse.Results.Take(5))
        {
            var data = new CueData
            {
                Source = "[DC]",
                Artist = sourceData.Artist,
                Album = item.Title,
            };

            if (item.Barcode != null)
            {
                foreach (var barcodeStr in item.Barcode)
                {
                    if (IsValidBarcodeCandidate(barcodeStr))
                    {
                        data.Barcode = barcodeStr;
                        break;
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(item.CatalogNumber))
                data.CatalogNumber = item.CatalogNumber;

            if (!string.IsNullOrWhiteSpace(item.Country))
                data.Country = item.Country;

            if (item.Year != null)
                data.Date = item.Year.ToString();

            if (item.Label != null && item.Label.Count > 0)
                data.Label = CleanDiscogsString(item.Label[0]);

            string? searchGenreValue = item.Style?.FirstOrDefault();
            if (string.IsNullOrWhiteSpace(searchGenreValue))
                searchGenreValue = item.Genre?.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(searchGenreValue))
                data.Genre = searchGenreValue;

            if (item.FormatQuantity.HasValue)
                data.Discs = item.FormatQuantity.Value;

            if (data.Album != null && data.Album.Contains(" - "))
            {
                var parts = data.Album.Split(new[] { " - " }, 2, StringSplitOptions.None);
                if (parts.Length == 2)
                {
                    data.Artist = CleanDiscogsString(parts[0].Trim());
                    data.Album = CleanDiscogsString(parts[1].Trim());
                }
            }

            if (!string.IsNullOrWhiteSpace(item.ResourceUrl))
            {
                tasks.Add(FetchAndApplyDiscogsDataAsync(data, item.ResourceUrl, isDirectReleaseUrl: true, sourceData: sourceData));
            }

            list.Add(data);
        }

        await Task.WhenAll(tasks);

        if (!string.IsNullOrWhiteSpace(sourceData.Artist))
        {
            var filtered = list.Where(d => StringExtensions.MatchesArtist(d.Artist, sourceData.Artist)).ToList();

            if (filtered.Count < list.Count)
            {
                Log($"Filtered Discogs text search results by artist '{sourceData.Artist}': count changed from {list.Count} to {filtered.Count}");
                list.Clear();
                list.AddRange(filtered);
            }
        }
    }

    public async Task EnrichWithDiscogsReleaseIdAsync(CueData data, string discogsId)
    {
        Log($"Enriching with Discogs Release ID: {discogsId}");
        var url = $"https://api.discogs.com/releases/{discogsId}";
        await FetchAndApplyDiscogsDataAsync(data, url, isDirectReleaseUrl: true);
    }

    public async Task EnrichWithDiscogsCatNoAsync(CueData data, string catNo)
    {
        Log($"Enriching with Discogs CatNo: {catNo}");
        var url = $"https://api.discogs.com/database/search?catno={HttpUtility.UrlEncode(catNo)}&type=release";
        await FetchAndApplyDiscogsDataAsync(data, url, isDirectReleaseUrl: false);
    }

    public async Task FetchAndApplyDiscogsDataAsync(CueData data, string url, bool isDirectReleaseUrl, CueData? sourceData = null)
    {
        if (!HasCredentials || string.IsNullOrWhiteSpace(url)) return;

        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            AddAuthHeader(request);

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode) return;

            var json = await response.Content.ReadAsStringAsync();
            Log($"Discogs API raw response for {url}: {json}");

            DiscogsReleaseResponse? releaseObj = null;

            if (isDirectReleaseUrl)
            {
                releaseObj = JsonSerializer.Deserialize<DiscogsReleaseResponse>(json, JsonOptions);
            }
            else
            {
                var searchResponse = JsonSerializer.Deserialize<DiscogsSearchResponse>(json, JsonOptions);
                if (searchResponse?.Results != null && searchResponse.Results.Count > 0)
                {
                    var firstResult = searchResponse.Results[0];
                    if (!string.IsNullOrWhiteSpace(firstResult.ResourceUrl))
                    {
                        await FetchAndApplyDiscogsDataAsync(data, firstResult.ResourceUrl, isDirectReleaseUrl: true, sourceData: sourceData);
                        return;
                    }
                }
            }

            if (releaseObj == null) return;

            if (isDirectReleaseUrl && !string.IsNullOrWhiteSpace(releaseObj.Released))
            {
                data.ReleaseDate = releaseObj.Released ?? data.ReleaseDate;
                if (string.IsNullOrWhiteSpace(data.Date))
                {
                    data.Date = releaseObj.Released;
                }
            }

            // Master URL Year resolution
            if (!string.IsNullOrWhiteSpace(releaseObj.MasterUrl))
            {
                string mUrl = releaseObj.MasterUrl;
                try
                {
                    var mReq = new HttpRequestMessage(HttpMethod.Get, mUrl);
                    AddAuthHeader(mReq);

                    var mRes = await _httpClient.SendAsync(mReq);
                    if (mRes.IsSuccessStatusCode)
                    {
                        var mJson = await mRes.Content.ReadAsStringAsync();
                        Log($"Discogs master API raw response for {mUrl}: {mJson}");
                        var masterObj = JsonSerializer.Deserialize<DiscogsMasterResponse>(mJson, JsonOptions);
                        if (masterObj?.Year != null)
                        {
                            data.Date = masterObj.Year.ToString();
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
                if (releaseObj.Artists != null && releaseObj.Artists.Count > 0)
                {
                    var artistObj = releaseObj.Artists[0];
                    string? name = CleanDiscogsString(artistObj.Name);
                    string? anv = artistObj.Anv;

                    string finalArtist = string.IsNullOrWhiteSpace(anv) ? (name ?? "") : $"{anv} = {name}";
                    if (!string.IsNullOrWhiteSpace(finalArtist))
                        data.Artist = finalArtist;
                }

                if (string.IsNullOrEmpty(data.Album) && !string.IsNullOrWhiteSpace(releaseObj.Title))
                    data.Album = releaseObj.Title;

                if (string.IsNullOrEmpty(data.Label) && releaseObj.Labels != null && releaseObj.Labels.Count > 0)
                {
                    data.Label = CleanDiscogsString(releaseObj.Labels[0].Name);
                    if (!string.IsNullOrWhiteSpace(releaseObj.Labels[0].CatalogNumber))
                        data.CatalogNumber = releaseObj.Labels[0].CatalogNumber;
                }

                if (string.IsNullOrEmpty(data.Country) && !string.IsNullOrWhiteSpace(releaseObj.Country))
                    data.Country = releaseObj.Country;

                if (releaseObj.Identifiers != null)
                {
                    var barcodeIdent = releaseObj.Identifiers.FirstOrDefault(ident =>
                        string.Equals(ident.Type, "Barcode", StringComparison.OrdinalIgnoreCase) &&
                        !string.IsNullOrWhiteSpace(ident.Value) &&
                        IsValidBarcodeCandidate(ident.Value));

                    if (barcodeIdent != null)
                    {
                        data.Barcode = barcodeIdent.Value;
                    }
                }
            }

            string? genreValue = releaseObj.Style?.FirstOrDefault();
            if (string.IsNullOrWhiteSpace(genreValue))
                genreValue = releaseObj.Genre?.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(genreValue))
                data.Genre = data.Genre ?? genreValue;

            if (isDirectReleaseUrl && releaseObj.Tracklist != null)
            {
                int totalTrackCount = 0;
                var discTracks = new Dictionary<string, int>();
                var discTitles = new Dictionary<string, string>();
                string currentHeading = "";

                foreach (var trackItem in releaseObj.Tracklist)
                {
                    var tType = trackItem.Type;
                    if (tType == "heading")
                    {
                        currentHeading = trackItem.Title ?? "";
                    }
                    else if (tType == "track")
                    {
                        var pos = trackItem.Position ?? "";
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
                            var match = Regex.Match(pos, @"^[a-zA-Z]+");
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

            if (isDirectReleaseUrl && releaseObj.Formats != null)
            {
                int discs = 0;
                foreach (var format in releaseObj.Formats)
                {
                    if (int.TryParse(format.Qty, out int q))
                        discs += q;
                }
                if (discs > 0 && (!data.Discs.HasValue || data.Discs.Value <= 1)) data.Discs = discs;
            }

            if (data.Discs.HasValue)
            {
                if (data.Discs.Value == 1)
                {
                    data.DiscNumber = 1;
                }
                else if (data.DiscNumber.HasValue && data.DiscNumber.Value > data.Discs.Value)
                {
                    data.DiscNumber = 1;
                }
            }
        }
        catch (Exception ex)
        {
            Log($"Discogs enrichment failed: {ex.Message}");
        }
    }

    public static string? CleanDiscogsString(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return input;
        return Regex.Replace(input, @" \(\d+\)$", "").Trim();
    }

    public static bool IsValidBarcodeCandidate(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return false;

        if (input.Contains("Matrix", StringComparison.OrdinalIgnoreCase) ||
            input.Contains("Runout", StringComparison.OrdinalIgnoreCase) ||
            input.Contains("SID", StringComparison.OrdinalIgnoreCase) ||
            input.Contains("IFPI", StringComparison.OrdinalIgnoreCase) ||
            input.Contains("JASRAC", StringComparison.OrdinalIgnoreCase) ||
            input.Contains("Mastering", StringComparison.OrdinalIgnoreCase) ||
            input.Contains("Mould", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!Regex.IsMatch(input, @"^[\d\s\-]+$"))
        {
            return false;
        }

        int digitCount = input.Count(char.IsDigit);
        return digitCount >= 6;
    }
}

