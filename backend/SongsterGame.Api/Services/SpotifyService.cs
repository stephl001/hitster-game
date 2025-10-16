using SongsterGame.Api.Models;

namespace SongsterGame.Api.Services;

public class SpotifyService(IConfiguration configuration, HttpClient httpClient) : ISpotifyService
{
    private readonly HttpClient _httpClient = httpClient;

    public Task<List<MusicCard>> FetchPlaylistTracksAsync(string playlistId)
    {
        // TODO: Implement Spotify API integration
        // 1. Get access token
        // 2. Fetch playlist tracks
        // 3. Extract preview URLs, metadata
        // 4. Convert to MusicCard list
        throw new NotImplementedException("Spotify integration pending");
    }

    public Task<string> GetAuthorizationUrlAsync(string redirectUri)
    {
        // TODO: Build Spotify OAuth URL
        var clientId = configuration["Spotify:ClientId"];
        var scopes = "streaming user-read-email user-read-private";
        var authUrl = $"https://accounts.spotify.com/authorize?" +
                     $"client_id={clientId}&" +
                     $"response_type=code&" +
                     $"redirect_uri={Uri.EscapeDataString(redirectUri)}&" +
                     $"scope={Uri.EscapeDataString(scopes)}";

        return Task.FromResult(authUrl);
    }

    public Task<string> ExchangeCodeForTokenAsync(string code, string redirectUri)
    {
        // TODO: Exchange authorization code for access token
        throw new NotImplementedException("Spotify OAuth pending");
    }
}
