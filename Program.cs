using SpotifyAPI.Web;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;
using Image = SixLabors.ImageSharp.Image;

var builder = WebApplication.CreateBuilder(args);

var clientId = builder.Configuration["Spotify:ClientId"];
var clientSecret = builder.Configuration["Spotify:ClientSecret"];
var refreshToken = builder.Configuration["Spotify:RefreshToken"];

builder.Services.AddSingleton<SpotifyClient>(sp =>
{
    var config = SpotifyClientConfig.CreateDefault();

    var token = new OAuthClient(config)
        .RequestToken(new AuthorizationCodeRefreshRequest(clientId, clientSecret, refreshToken))
        .Result;

    return new SpotifyClient(config.WithToken(token.AccessToken));
});

var app = builder.Build();

app.MapGet("/cover.jpg", async (SpotifyClient spotify) =>
{
    var playback = await spotify.Player.GetCurrentlyPlaying(
        new PlayerCurrentlyPlayingRequest(PlayerCurrentlyPlayingRequest.AdditionalTypes.Track)
    );

    if (playback?.Item is not FullTrack track)
        return Results.NotFound();

    var imageUrl = track.Album.Images[0].Url;

    var http = new HttpClient();
    var imgBytes = await http.GetByteArrayAsync(imageUrl);

    using var image = Image.Load(imgBytes);

    image.Mutate(x => x.Resize(new ResizeOptions
    {
        Size = new Size(240, 240),
        Mode = ResizeMode.Crop
    }));

    using var ms = new MemoryStream();
    image.Save(ms, new JpegEncoder { Quality = 90 });

    return Results.File(ms.ToArray(), "image/jpeg");
});

app.Run();