namespace SongsterGame.Api.Models;

public class Player
{
    public required string ConnectionId { get; init; }
    public required string Nickname { get; init; }
    public List<MusicCard> Timeline { get; init; } = [];
    public int SongsterTokens { get; set; } = 0;
    public bool IsHost { get; init; }
}
