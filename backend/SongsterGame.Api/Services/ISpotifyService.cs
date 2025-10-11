using SongsterGame.Api.Models;

namespace SongsterGame.Api.Services;

public interface ISpotifyService
{
    Task<List<MusicCard>> FetchPlaylistTracksAsync(string playlistId);
    Task<string> GetAuthorizationUrlAsync(string redirectUri);
    Task<string> ExchangeCodeForTokenAsync(string code, string redirectUri);
}
