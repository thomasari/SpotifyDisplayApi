using SpotifyAPI.Web;

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
    var img = await http.GetByteArrayAsync(imageUrl);

    return Results.File(img, "image/jpeg");
});

app.Run();