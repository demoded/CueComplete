using MetaBrainz.MusicBrainz;
using MetaBrainz.MusicBrainz.Interfaces.Entities;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using CueComplete.Core.Metadata.Models;

namespace CueComplete.Core.Metadata;

public class MusicBrainzProvider : IMetadataProvider
{
    public string Name => "MusicBrainz";

    private readonly Query _mbClient;
    private readonly HttpClient _httpClient;
    private readonly Action<string>? _logCallback;

    public MusicBrainzProvider(HttpClient? httpClient = null, Action<string>? logCallback = null)
    {
        _logCallback = logCallback;
        _mbClient = new Query("CueComplete", "1.0", "mailto:user@example.com");

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

    private void Log(string message)
    {
        _logCallback?.Invoke(message);
    }

    public async Task<List<CueData>> SearchAsync(CueData sourceData)
    {
        return await SearchAsync(sourceData, null);
    }

    public async Task<List<CueData>> SearchAsync(CueData sourceData, Func<CueData, string, Task>? discogsEnricher)
    {
        var list = new List<CueData>();

        if (!string.IsNullOrWhiteSpace(sourceData.MusicBrainzDiscId))
        {
            Log($"Looking up MusicBrainz DiscID: {sourceData.MusicBrainzDiscId}");
            try
            {
                var discResult = await _mbClient.LookupDiscIdAsync(sourceData.MusicBrainzDiscId);
                if (discResult?.Releases != null)
                {
                    foreach (var rel in discResult.Releases.Take(10))
                    {
                        var release = rel;
                        try
                        {
                            release = await _mbClient.LookupReleaseAsync(release.Id, Include.Labels | Include.Genres | Include.UrlRelationships | Include.Recordings);
                        }
                        catch { }

                        var dto = MapToDto(release, sourceData.Artist);
                        var data = dto.ToCueData();

                        string? discogsReleaseId = ExtractDiscogsReleaseId(release);
                        if (!string.IsNullOrWhiteSpace(discogsReleaseId))
                        {
                            data.DiscogsId = discogsReleaseId;
                            Log($"Found Discogs link in MusicBrainz DiscID lookup: {discogsReleaseId}");
                            if (discogsEnricher != null)
                            {
                                await discogsEnricher(data, discogsReleaseId);
                            }
                        }
                        else if (!string.IsNullOrWhiteSpace(data.CatalogNumber) && discogsEnricher != null)
                        {
                            Log($"No Discogs link, trying to enrich via CatNo: {data.CatalogNumber}");
                            await discogsEnricher(data, $"catno:{data.CatalogNumber}");
                        }

                        Log($"MusicBrainz match (DiscID): ID={release.Id}, Title={release.Title}, Date={release.Date}, Barcode={release.Barcode}");
                        list.Add(data);
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"MusicBrainz DiscID lookup error: {ex.Message}");
            }

            // Fallback: Search MusicBrainz index by discid/cdtoc if direct lookup returns no results or fails
            if (list.Count == 0)
            {
                try
                {
                    string escapedDiscId = Regex.Replace(sourceData.MusicBrainzDiscId, @"([+\-&&||!(){}\[\]^""~*?:\\/])", @"\$1");
                    Log($"Querying MusicBrainz search index for DiscID: {sourceData.MusicBrainzDiscId} (escaped: {escapedDiscId})");
                    var discSearchResults = await _mbClient.FindReleasesAsync($"discid:{escapedDiscId} OR cdtoc:{escapedDiscId}", 5);
                    if (discSearchResults?.Results != null)
                    {
                        foreach (var res in discSearchResults.Results)
                        {
                            var release = res.Item;
                            try
                            {
                                release = await _mbClient.LookupReleaseAsync(release.Id, Include.Labels | Include.Genres | Include.UrlRelationships | Include.Recordings);
                            }
                            catch { }

                            var dto = MapToDto(release, sourceData.Artist);
                            var data = dto.ToCueData();

                            string? discogsReleaseId = ExtractDiscogsReleaseId(release);
                            if (!string.IsNullOrWhiteSpace(discogsReleaseId))
                            {
                                data.DiscogsId = discogsReleaseId;
                                Log($"Found Discogs link in MusicBrainz DiscID search: {discogsReleaseId}");
                                if (discogsEnricher != null) await discogsEnricher(data, discogsReleaseId);
                            }
                            else if (!string.IsNullOrWhiteSpace(data.CatalogNumber) && discogsEnricher != null)
                            {
                                Log($"No Discogs link, trying to enrich via CatNo: {data.CatalogNumber}");
                                await discogsEnricher(data, $"catno:{data.CatalogNumber}");
                            }

                            Log($"MusicBrainz match (DiscID Search): ID={release.Id}, Title={release.Title}, Date={release.Date}, Barcode={release.Barcode}");
                            list.Add(data);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log($"MusicBrainz DiscID search query error: {ex.Message}");
                }
            }

            if (list.Count > 0 && !string.IsNullOrWhiteSpace(sourceData.Artist))
            {
                var artistStr = sourceData.Artist.ToLower();
                var filteredList = list.Where(r => r.Artist != null && r.Artist.ToLower().Contains(artistStr)).ToList();
                if (filteredList.Count > 0)
                {
                    Log($"Filtered DiscID MusicBrainz results by artist '{sourceData.Artist}': count changed from {list.Count} to {filteredList.Count}");
                    list = filteredList;
                }
                else
                {
                    Log($"All DiscID MusicBrainz results filtered out by artist '{sourceData.Artist}'. Likely false positive matches.");
                    list.Clear();
                }
            }

            if (list.Count > 0) return list;
        }

        if (!string.IsNullOrWhiteSpace(sourceData.DiscId))
        {
            Log($"Looking up FreeDB ID: {sourceData.DiscId}");
            var freedbResult = await GetReleaseIdsFromFreeDbAsync(sourceData.DiscId);
            foreach (var releaseId in freedbResult.ReleaseIds.Take(10))
            {
                Log($"Found MusicBrainz Release ID via FreeDB: {releaseId}");
                try
                {
                    var release = await _mbClient.LookupReleaseAsync(Guid.Parse(releaseId), Include.Labels | Include.Genres | Include.UrlRelationships | Include.Recordings);
                    var dto = MapToDto(release, sourceData.Artist);
                    var data = dto.ToCueData();

                    string? discogsReleaseId = ExtractDiscogsReleaseId(release);
                    if (!string.IsNullOrWhiteSpace(discogsReleaseId))
                    {
                        data.DiscogsId = discogsReleaseId;
                        Log($"Found Discogs link in MusicBrainz: {discogsReleaseId}");
                        if (discogsEnricher != null)
                        {
                            await discogsEnricher(data, discogsReleaseId);
                        }
                    }
                    else if (!string.IsNullOrWhiteSpace(data.CatalogNumber) && discogsEnricher != null)
                    {
                        Log($"No Discogs link, trying to enrich via CatNo: {data.CatalogNumber}");
                        await discogsEnricher(data, $"catno:{data.CatalogNumber}");
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

            try
            {
                release = await _mbClient.LookupReleaseAsync(release.Id, Include.Labels | Include.Genres | Include.UrlRelationships | Include.Recordings);
            }
            catch { /* Ignore lookup failure */ }

            var dto = MapToDto(release, sourceData.Artist);
            var data = dto.ToCueData();

            string? discogsReleaseId = ExtractDiscogsReleaseId(release);
            if (!string.IsNullOrWhiteSpace(discogsReleaseId))
            {
                data.DiscogsId = discogsReleaseId;
                Log($"Found Discogs link in MusicBrainz: {discogsReleaseId}");
                if (discogsEnricher != null)
                {
                    await discogsEnricher(data, discogsReleaseId);
                }
            }
            else if (!string.IsNullOrWhiteSpace(data.CatalogNumber) && discogsEnricher != null)
            {
                Log($"No Discogs link, trying to enrich via CatNo: {data.CatalogNumber}");
                await discogsEnricher(data, $"catno:{data.CatalogNumber}");
            }

            Log($"MusicBrainz match: ID={release.Id}, Title={release.Title}, Date={release.Date}, Barcode={release.Barcode}");
            list.Add(data);
        }

        return list;
    }

    public async Task<FreeDbLookupResult> GetReleaseIdsFromFreeDbAsync(string freeDbId)
    {
        var result = new FreeDbLookupResult { DiscId = freeDbId };
        try
        {
            var url = $"https://musicbrainz.org/otherlookup/freedbid?other-lookup.freedbid={freeDbId}";
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode) return result;

            var html = await response.Content.ReadAsStringAsync();
            Log($"FreeDB API raw response for {url}: {html}");
            var matches = Regex.Matches(html, @"href=""/release/([a-f0-9\-]{36})""");
            foreach (Match match in matches)
            {
                result.ReleaseIds.Add(match.Groups[1].Value);
            }
            result.ReleaseIds = result.ReleaseIds.Distinct().ToList();
        }
        catch (Exception ex)
        {
            Log($"FreeDB lookup error: {ex.Message}");
        }
        return result;
    }

    private static MusicBrainzReleaseDto MapToDto(IRelease release, string? fallbackArtist)
    {
        var dto = new MusicBrainzReleaseDto
        {
            ReleaseId = release.Id.ToString(),
            Artist = release.ArtistCredit?.FirstOrDefault()?.Name ?? fallbackArtist,
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
            dto.Label = labelInfo.Label?.Name;
            dto.CatalogNumber = labelInfo.CatalogNumber;
        }

        if (release.Genres != null && release.Genres.Count > 0)
        {
            dto.Genre = release.Genres[0].Name;
        }

        return dto;
    }

    private static string? ExtractDiscogsReleaseId(IRelease release)
    {
        if (release.Relationships == null) return null;
        foreach (var rel in release.Relationships)
        {
            if (rel.Type == "discogs" && rel.TargetType == EntityType.Url && rel.Url?.Resource != null)
            {
                var match = Regex.Match(rel.Url.Resource.ToString(), @"discogs\.com/release/(\d+)");
                if (match.Success)
                {
                    return match.Groups[1].Value;
                }
            }
        }
        return null;
    }
}
