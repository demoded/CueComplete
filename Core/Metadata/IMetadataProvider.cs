namespace CueComplete.Core.Metadata;

public interface IMetadataProvider
{
    string Name { get; }
    Task<List<CueData>> SearchAsync(CueData sourceData);
}
