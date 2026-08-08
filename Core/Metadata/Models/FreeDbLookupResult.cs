namespace CueComplete.Core.Metadata.Models;

public class FreeDbLookupResult
{
    public string DiscId { get; set; } = string.Empty;
    public List<string> ReleaseIds { get; set; } = new();
}
