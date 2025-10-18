namespace SongsterGame.Api.Models;

public class MusicCard
{
    public string SpotifyId { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Artist { get; init; } = string.Empty;
    public required int Year { get; init; }
    public string PreviewUrl { get; init; } = string.Empty;
}
