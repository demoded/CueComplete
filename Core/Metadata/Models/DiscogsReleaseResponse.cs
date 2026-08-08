using System.Text.Json.Serialization;

namespace CueComplete.Core.Metadata.Models;

public class DiscogsReleaseResponse
{
    [JsonPropertyName("released")]
    public string? Released { get; set; }

    [JsonPropertyName("master_url")]
    public string? MasterUrl { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("country")]
    public string? Country { get; set; }

    [JsonPropertyName("artists")]
    public List<DiscogsArtist>? Artists { get; set; }

    [JsonPropertyName("labels")]
    public List<DiscogsLabel>? Labels { get; set; }

    [JsonPropertyName("identifiers")]
    public List<DiscogsIdentifier>? Identifiers { get; set; }

    [JsonPropertyName("genre")]
    public List<string>? Genre { get; set; }

    [JsonPropertyName("style")]
    public List<string>? Style { get; set; }

    [JsonPropertyName("tracklist")]
    public List<DiscogsTrack>? Tracklist { get; set; }

    [JsonPropertyName("formats")]
    public List<DiscogsFormat>? Formats { get; set; }
}

public class DiscogsArtist
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("anv")]
    public string? Anv { get; set; }
}

public class DiscogsLabel
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("catno")]
    public string? CatalogNumber { get; set; }
}

public class DiscogsIdentifier
{
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("value")]
    public string? Value { get; set; }
}

public class DiscogsTrack
{
    [JsonPropertyName("type_")]
    public string? Type { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("position")]
    public string? Position { get; set; }
}

public class DiscogsFormat
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("qty")]
    public string? Qty { get; set; }
}
