using Orleans.Runtime;

namespace OrleansURLShortener;

public class UrlShortenerGrain([PersistentState(stateName: "url",storageName: "urls")] IPersistentState<UrlDetails> state) : Grain, IUrlShortenerGrain
{
    public Task<string> GetUrl() => Task.FromResult(state.State.FullUrl);

    public async Task SetUrl(string fullUrl)
    {
        state.State = new()
        {
            ShortenedRouteSegment = this.GetPrimaryKeyString(),
            FullUrl = fullUrl
        };

        await state.WriteStateAsync();
    }
}
public interface IUrlShortenerGrain : IGrainWithStringKey
{
    Task SetUrl(string fullUrl);
    Task<string> GetUrl();
}

[GenerateSerializer, Alias(nameof(UrlDetails))]
public sealed record class UrlDetails
{
    [Id(0)]
    public string FullUrl { get; set; } = "";

    [Id(1)]
    public string ShortenedRouteSegment { get; set; } = "";
}
