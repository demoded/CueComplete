using System.Text.Json.Serialization;

namespace CueComplete.Core.Metadata.Models;

public class DiscogsMasterResponse
{
    [JsonPropertyName("year")]
    public object? Year { get; set; }
}
