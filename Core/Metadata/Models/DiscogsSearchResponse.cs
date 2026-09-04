using System.Text.Json.Serialization;

namespace CueComplete.Core.Metadata.Models;

public class DiscogsSearchResponse
{
    [JsonPropertyName("results")]
    public List<DiscogsSearchResult>? Results { get; set; }
}

public class DiscogsSearchResult
{
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("barcode")]
    public List<string>? Barcode { get; set; }

    [JsonPropertyName("catno")]
    public string? CatalogNumber { get; set; }

    [JsonPropertyName("country")]
    public string? Country { get; set; }

    [JsonPropertyName("year")]
    public object? Year { get; set; }

    [JsonPropertyName("label")]
    public List<string>? Label { get; set; }

    [JsonPropertyName("style")]
    public List<string>? Style { get; set; }

    [JsonPropertyName("genre")]
    public List<string>? Genre { get; set; }

    [JsonPropertyName("format_quantity")]
    public int? FormatQuantity { get; set; }

    [JsonPropertyName("resource_url")]
    public string? ResourceUrl { get; set; }
}
