using Orleans.Runtime;
using OrleansURLShortener;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.UseOrleans(siloBuilder => { 
    siloBuilder.UseLocalhostClustering();
    siloBuilder.AddMemoryGrainStorage("urls");
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();


app.MapGet("/", static () =>
{
    return "Welcome to the URL shortener, powered by Orleans!";
});

app.MapGet("/shorten", 
    static async (IGrainFactory grains, HttpRequest request, string url) =>
    {
        var host = $"{request.Scheme}://{request.Host.Value}";

        if (string.IsNullOrWhiteSpace(url) && Uri.IsWellFormedUriString(url, UriKind.Absolute) is false)
        {
            return Results.BadRequest($"""
                The URL query string is required and needs to be well formed.
                Consider, ${host}/shorten?url=https://www.microsoft.com.
                """);
        }
        var shortenedRouteSegment = Guid.NewGuid().GetHashCode().ToString("X");

        var shortenerGrain =
            grains.GetGrain<IUrlShortenerGrain>(shortenedRouteSegment);

        await shortenerGrain.SetUrl(url);

        var resultBuilder = new UriBuilder(host)
        {
            Path = $"/go/{shortenedRouteSegment}"
        };

        return Results.Ok(resultBuilder.Uri);
    });

app.MapGet("/go/{shortenedRouteSegment:required}",
    static async (IGrainFactory grains, string shortenedRouteSegment) =>
    {
        // Retrieve the grain using the shortened ID and url to the original URL
        var shortenerGrain =
            grains.GetGrain<IUrlShortenerGrain>(shortenedRouteSegment);

        var url = await shortenerGrain.GetUrl();

        // Handles missing schemes, defaults to "http://".
        var redirectBuilder = new UriBuilder(url);

        return Results.Redirect(redirectBuilder.Uri.ToString());
    });

app.Run();