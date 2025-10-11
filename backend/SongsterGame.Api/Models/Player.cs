namespace SongsterGame.Api.Models;

public class Player
{
    public string ConnectionId { get; set; } = string.Empty;
    public string Nickname { get; set; } = string.Empty;
    public List<MusicCard> Timeline { get; set; } = new();
    public int SongsterTokens { get; set; } = 0;
    public bool IsHost { get; set; } = false;
}
