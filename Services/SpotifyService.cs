namespace SpotifyDisplayApi.Services;

using SpotifyAPI.Web;

public interface ISpotifyService
{
    Task<string?> GetCurrentImageUrl();
}

public class SpotifyService : ISpotifyService
{
    private readonly SpotifyClient _spotify;

    public SpotifyService(IConfiguration config)
    {
        var clientId = config["Spotify:ClientId"];
        var clientSecret = config["Spotify:ClientSecret"];
        var refreshToken = config["Spotify:RefreshToken"];

        var spotifyConfig = SpotifyClientConfig.CreateDefault();

        var token = new OAuthClient(spotifyConfig)
            .RequestToken(new AuthorizationCodeRefreshRequest(clientId, clientSecret, refreshToken))
            .Result;

        _spotify = new SpotifyClient(spotifyConfig.WithToken(token.AccessToken));
    }

    public async Task<string?> GetCurrentImageUrl()
    {
        var playback = await _spotify.Player.GetCurrentlyPlaying(
            new PlayerCurrentlyPlayingRequest()
        );

        return playback?.Item switch
        {
            FullEpisode e => e.Images[0].Url,
            FullTrack t => t.Album.Images[0].Url,
            _ => null
        };
    }
}