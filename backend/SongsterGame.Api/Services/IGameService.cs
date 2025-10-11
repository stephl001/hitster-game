using SongsterGame.Api.Models;

namespace SongsterGame.Api.Services;

public interface IGameService
{
    Game? CreateGame(string hostConnectionId, string hostNickname);
    Game? GetGame(string gameCode);
    Game? GetCurrentGame();
    bool JoinGame(string gameCode, string connectionId, string nickname);
    bool StartGame(string gameCode);
    bool PlaceCard(string gameCode, string connectionId, int position);
    bool ValidatePlacement(List<MusicCard> timeline, MusicCard card, int position);
    void NextTurn(string gameCode);
    Player? GetWinner(string gameCode);
    void RemovePlayer(string connectionId);
    void DrawCard(string gameCode);
    string GenerateGameCode();
}
