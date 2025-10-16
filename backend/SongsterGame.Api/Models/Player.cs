namespace SongsterGame.Api.Models;

public class Player
{
    public string ConnectionId { get; init; } = string.Empty;
    public string Nickname { get; init; } = string.Empty;
    public List<MusicCard> Timeline { get; set; } = [];
    public int SongsterTokens { get; set; } = 0;
    public bool IsHost { get; init; }
}
