namespace SongsterGame.Api.Models;

public class Game(TimeProvider timeProvider)
{
    public string GameCode { get; set; } = string.Empty;
    public List<Player> Players { get; set; } = [];
    public List<MusicCard> MusicDeck { get; set; } = [];
    public MusicCard? CurrentCard { get; set; }
    public int CurrentTurnIndex { get; set; }
    public GameState State { get; set; } = GameState.Lobby;
    public DateTime CreatedAt { get; set; } = timeProvider.GetUtcNow().UtcDateTime;
    public const int MaxPlayers = 4;
    public const int WinningTimelineLength = 10;

    public Player? CurrentPlayer => Players.Count > 0 && CurrentTurnIndex < Players.Count
        ? Players[CurrentTurnIndex]
        : null;
}
