using MetaBrainz.MusicBrainz;
using MetaBrainz.MusicBrainz.Interfaces.Entities;
using MetaBrainz.MusicBrainz.Interfaces.Searches;
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
                            release = await _mbClient.LookupReleaseAsync(release.Id, Include.ArtistCredits | Include.DiscIds | Include.Labels | Include.Genres | Include.UrlRelationships | Include.Recordings);
                        }
                        catch { }

                        var dto = MapToDto(release, sourceData.Artist, sourceData);
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
                                release = await _mbClient.LookupReleaseAsync(release.Id, Include.ArtistCredits | Include.DiscIds | Include.Labels | Include.Genres | Include.UrlRelationships | Include.Recordings);
                            }
                            catch { }

                            // If this was found via fallback search on discid, verify that the release ACTUALLY contains this discid,
                            // OR that the artist matches sourceData.Artist AND track count matches (if available).
                            bool hasMatchingDiscId = release.Media != null && release.Media.Any(m => m.Discs != null && m.Discs.Any(d => string.Equals(d.Id, sourceData.MusicBrainzDiscId, StringComparison.OrdinalIgnoreCase)));

                            string? releaseArtist = release.ArtistCredit?.FirstOrDefault()?.Name;
                            bool artistMatches = !string.IsNullOrWhiteSpace(sourceData.Artist) && StringExtensions.MatchesArtist(releaseArtist, sourceData.Artist);

                            bool tracksMatch = !sourceData.Tracks.HasValue || sourceData.Tracks.Value <= 0 || (release.Media != null && release.Media.Any(m => m.TrackCount == sourceData.Tracks.Value));

                            if (!hasMatchingDiscId && (!artistMatches || !tracksMatch))
                            {
                                Log($"Discarding DiscID fallback match '{release.Title}' by '{releaseArtist}': DiscID, artist, or track count did not match.");
                                continue;
                            }

                            var dto = MapToDto(release, sourceData.Artist, sourceData);
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
                var filteredList = list.Where(r => StringExtensions.MatchesArtist(r.Artist, sourceData.Artist)).ToList();
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
                    var release = await _mbClient.LookupReleaseAsync(Guid.Parse(releaseId), Include.ArtistCredits | Include.DiscIds | Include.Labels | Include.Genres | Include.UrlRelationships | Include.Recordings);
                    var dto = MapToDto(release, sourceData.Artist, sourceData);
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
                var filteredList = list.Where(r => StringExtensions.MatchesArtist(r.Artist, sourceData.Artist)).ToList();
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

        var resultsList = new List<ISearchResult<IRelease>>();

        if (!string.IsNullOrWhiteSpace(sourceData.Barcode) && DiscogsProvider.IsValidBarcodeCandidate(sourceData.Barcode))
        {
            var query = $"barcode:\"{sourceData.Barcode}\"";
            Log($"MusicBrainz query (Barcode): {query}");
            var searchResults = await _mbClient.FindReleasesAsync(query, 5);
            if (searchResults?.Results != null)
                resultsList.AddRange(searchResults.Results);
        }

        if (resultsList.Count == 0 && !string.IsNullOrWhiteSpace(sourceData.CatalogNumber))
        {
            string escapedCatNo = Regex.Replace(sourceData.CatalogNumber, @"([+\-&&||!(){}\[\]^""~*?:\\/])", @"\$1");
            string catQuery = !string.IsNullOrWhiteSpace(sourceData.Artist)
                ? $"artist:\"{sourceData.Artist}\" AND catno:\"{escapedCatNo}\""
                : $"catno:\"{escapedCatNo}\"";

            Log($"MusicBrainz query (CatNo): {catQuery}");
            try
            {
                var searchResults = await _mbClient.FindReleasesAsync(catQuery, 5);
                if (searchResults?.Results != null && searchResults.Results.Count > 0)
                {
                    resultsList.AddRange(searchResults.Results);
                }
            }
            catch (Exception ex)
            {
                Log($"MusicBrainz CatNo query error: {ex.Message}");
            }
        }

        if (resultsList.Count == 0 && !string.IsNullOrWhiteSpace(sourceData.Artist) && !string.IsNullOrWhiteSpace(sourceData.Album))
        {
            string query = $"artist:\"{sourceData.Artist}\" AND release:\"{sourceData.Album}\"";
            Log($"MusicBrainz query: {query}");
            var searchResults = await _mbClient.FindReleasesAsync(query, 5);
            if (searchResults?.Results != null)
                resultsList.AddRange(searchResults.Results);

            if (resultsList.Count == 0)
            {
                var cleanAlbum = StringExtensions.StripTitleAnnotations(sourceData.Album);
                if (!string.IsNullOrWhiteSpace(cleanAlbum) && !string.Equals(cleanAlbum, sourceData.Album, StringComparison.OrdinalIgnoreCase))
                {
                    string fallbackQuery = $"artist:\"{sourceData.Artist}\" AND release:\"{cleanAlbum}\"";
                    Log($"MusicBrainz initial query returned 0 results. Retrying with stripped album title: {fallbackQuery}");
                    var fallbackResults = await _mbClient.FindReleasesAsync(fallbackQuery, 5);
                    if (fallbackResults?.Results != null)
                        resultsList.AddRange(fallbackResults.Results);
                }
            }
        }

        if (resultsList.Count == 0)
            return list;

        if (resultsList.Count > 1 && !string.IsNullOrWhiteSpace(sourceData.Artist))
        {
            var filtered = resultsList.Where(r =>
                r.Item.ArtistCredit != null &&
                r.Item.ArtistCredit.Any(ac => StringExtensions.MatchesArtist(ac.Name, sourceData.Artist))
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
                release = await _mbClient.LookupReleaseAsync(release.Id, Include.ArtistCredits | Include.DiscIds | Include.Labels | Include.Genres | Include.UrlRelationships | Include.Recordings);
            }
            catch { /* Ignore lookup failure */ }

            var dto = MapToDto(release, sourceData.Artist, sourceData);
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

        if (list.Count > 1)
        {
            var targetCatNo = sourceData.CatalogNumber?.Replace(" ", "").ToLowerInvariant();
            list = list.OrderByDescending(d =>
            {
                int score = 0;
                if (!string.IsNullOrEmpty(targetCatNo) && !string.IsNullOrWhiteSpace(d.CatalogNumber))
                {
                    var cleanCat = d.CatalogNumber.Replace(" ", "").ToLowerInvariant();
                    if (cleanCat.Contains(targetCatNo) || targetCatNo.Contains(cleanCat))
                        score += 20;
                }
                if (sourceData.Tracks.HasValue && sourceData.Tracks.Value > 0 && d.Tracks == sourceData.Tracks.Value)
                {
                    score += 10;
                }
                if (StringExtensions.MatchesSourceCountry(d.Country, sourceData))
                {
                    score += 15;
                }
                return score;
            }).ToList();
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

    private static MusicBrainzReleaseDto MapToDto(IRelease release, string? fallbackArtist, CueData? sourceData = null)
    {
        var dto = new MusicBrainzReleaseDto
        {
            ReleaseId = release.Id.ToString(),
            Artist = release.ArtistCredit?.FirstOrDefault()?.Name ?? fallbackArtist,
            Album = release.Title,
            Barcode = release.Barcode,
            Date = CueData.SanitizeYear(release.Date?.ToString()),
            ReleaseDate = release.Date?.ToString(),
            Country = release.Country,
            Discs = release.Media?.Count,
            Tracks = release.Media?.Sum(m => m.TrackCount)
        };

        if (release.Media != null && release.Media.Count > 0)
        {
            if (release.Media.Count == 1)
            {
                dto.DiscNumber = 1;
            }
            else if (sourceData != null && sourceData.Tracks.HasValue && sourceData.Tracks.Value > 0)
            {
                var matchedMedia = release.Media.Where(m => m.TrackCount == sourceData.Tracks.Value).ToList();
                if (matchedMedia.Count == 1)
                {
                    dto.DiscNumber = matchedMedia[0].Position;
                }
            }
        }

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
