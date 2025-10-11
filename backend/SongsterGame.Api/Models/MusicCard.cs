namespace SongsterGame.Api.Models;

public class MusicCard
{
    public string SpotifyId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Artist { get; set; } = string.Empty;
    public int Year { get; set; }
    public string PreviewUrl { get; set; } = string.Empty;
    public string? AlbumArtUrl { get; set; }
}
