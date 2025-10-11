using SongsterGame.Api.Models;

namespace SongsterGame.Api.Services;

public class GameService : IGameService
{
    private Game? _currentGame; // MVP: Single game in memory
    private readonly ISpotifyService _spotifyService;
    private readonly Random _random = new();

    public GameService(ISpotifyService spotifyService)
    {
        _spotifyService = spotifyService;
    }

    public Game? CreateGame(string hostConnectionId, string hostNickname)
    {
        // MVP: Only one game at a time
        if (_currentGame != null)
        {
            return null; // Game already exists
        }

        var gameCode = GenerateGameCode();
        _currentGame = new Game
        {
            GameCode = gameCode,
            Players = new List<Player>
            {
                new Player
                {
                    ConnectionId = hostConnectionId,
                    Nickname = hostNickname,
                    IsHost = true
                }
            },
            State = GameState.Lobby
        };

        return _currentGame;
    }

    public Game? GetGame(string gameCode)
    {
        return _currentGame?.GameCode == gameCode ? _currentGame : null;
    }

    public Game? GetCurrentGame()
    {
        return _currentGame;
    }

    public bool JoinGame(string gameCode, string connectionId, string nickname)
    {
        var game = GetGame(gameCode);
        if (game == null || game.State != GameState.Lobby || game.Players.Count >= Game.MaxPlayers)
        {
            return false;
        }

        // Check if nickname already exists
        if (game.Players.Any(p => p.Nickname.Equals(nickname, StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        game.Players.Add(new Player
        {
            ConnectionId = connectionId,
            Nickname = nickname,
            IsHost = false
        });

        return true;
    }

    public bool StartGame(string gameCode)
    {
        var game = GetGame(gameCode);
        if (game == null || game.State != GameState.Lobby || game.Players.Count < 2)
        {
            return false;
        }

        // TODO: Fetch music from Spotify
        // For now, create a mock deck
        game.MusicDeck = CreateMockDeck();
        game.State = GameState.Playing;
        DrawCard(gameCode);

        return true;
    }

    public void DrawCard(string gameCode)
    {
        var game = GetGame(gameCode);
        if (game == null || game.MusicDeck.Count == 0)
        {
            return;
        }

        game.CurrentCard = game.MusicDeck[0];
        game.MusicDeck.RemoveAt(0);
    }

    public bool PlaceCard(string gameCode, string connectionId, int position)
    {
        var game = GetGame(gameCode);
        if (game == null || game.CurrentCard == null || game.State != GameState.Playing)
        {
            return false;
        }

        var player = game.Players.FirstOrDefault(p => p.ConnectionId == connectionId);
        if (player == null || game.CurrentPlayer?.ConnectionId != connectionId)
        {
            return false; // Not this player's turn
        }

        var isValid = ValidatePlacement(player.Timeline, game.CurrentCard, position);
        if (isValid)
        {
            player.Timeline.Insert(position, game.CurrentCard);

            // Check win condition
            if (player.Timeline.Count >= Game.WinningTimelineLength)
            {
                game.State = GameState.Finished;
                return true;
            }
        }

        // Clear current card and move to next turn
        game.CurrentCard = null;
        NextTurn(gameCode);

        return isValid;
    }

    public bool ValidatePlacement(List<MusicCard> timeline, MusicCard card, int position)
    {
        if (position < 0 || position > timeline.Count)
        {
            return false;
        }

        int? yearBefore = position > 0 ? timeline[position - 1].Year : null;
        int? yearAfter = position < timeline.Count ? timeline[position].Year : null;

        if (yearBefore.HasValue && card.Year < yearBefore.Value)
        {
            return false;
        }

        if (yearAfter.HasValue && card.Year > yearAfter.Value)
        {
            return false;
        }

        return true;
    }

    public void NextTurn(string gameCode)
    {
        var game = GetGame(gameCode);
        if (game == null)
        {
            return;
        }

        game.CurrentTurnIndex = (game.CurrentTurnIndex + 1) % game.Players.Count;
        DrawCard(gameCode);
    }

    public Player? GetWinner(string gameCode)
    {
        var game = GetGame(gameCode);
        return game?.Players.FirstOrDefault(p => p.Timeline.Count >= Game.WinningTimelineLength);
    }

    public void RemovePlayer(string connectionId)
    {
        if (_currentGame == null)
        {
            return;
        }

        var player = _currentGame.Players.FirstOrDefault(p => p.ConnectionId == connectionId);
        if (player != null)
        {
            _currentGame.Players.Remove(player);

            // If host leaves or no players left, end game
            if (player.IsHost || _currentGame.Players.Count == 0)
            {
                _currentGame = null;
            }
        }
    }

    public string GenerateGameCode()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        return new string(Enumerable.Range(0, 8)
            .Select(_ => chars[_random.Next(chars.Length)])
            .ToArray());
    }

    private List<MusicCard> CreateMockDeck()
    {
        // Mock deck for testing - replace with Spotify integration
        var mockCards = new List<MusicCard>
        {
            new() { SpotifyId = "1", Title = "Billie Jean", Artist = "Michael Jackson", Year = 1983, PreviewUrl = "" },
            new() { SpotifyId = "2", Title = "Bohemian Rhapsody", Artist = "Queen", Year = 1975, PreviewUrl = "" },
            new() { SpotifyId = "3", Title = "Smells Like Teen Spirit", Artist = "Nirvana", Year = 1991, PreviewUrl = "" },
            new() { SpotifyId = "4", Title = "Hey Jude", Artist = "The Beatles", Year = 1968, PreviewUrl = "" },
            new() { SpotifyId = "5", Title = "Rolling in the Deep", Artist = "Adele", Year = 2010, PreviewUrl = "" },
            new() { SpotifyId = "6", Title = "Thriller", Artist = "Michael Jackson", Year = 1982, PreviewUrl = "" },
            new() { SpotifyId = "7", Title = "Imagine", Artist = "John Lennon", Year = 1971, PreviewUrl = "" },
            new() { SpotifyId = "8", Title = "Sweet Child O' Mine", Artist = "Guns N' Roses", Year = 1987, PreviewUrl = "" },
            new() { SpotifyId = "9", Title = "Hotel California", Artist = "Eagles", Year = 1976, PreviewUrl = "" },
            new() { SpotifyId = "10", Title = "Lose Yourself", Artist = "Eminem", Year = 2002, PreviewUrl = "" },
        };

        return mockCards.OrderBy(_ => _random.Next()).ToList();
    }
}
