using Microsoft.AspNetCore.SignalR;
using SongsterGame.Api.Models;
using SongsterGame.Api.Services;

namespace SongsterGame.Api.Hubs;

public class GameHub : Hub
{
    private readonly IGameService _gameService;
    private readonly ILogger<GameHub> _logger;

    public GameHub(IGameService gameService, ILogger<GameHub> logger)
    {
        _gameService = gameService;
        _logger = logger;
    }

    public async Task<object> CreateGame(string nickname)
    {
        try
        {
            var game = _gameService.CreateGame(Context.ConnectionId, nickname);
            if (game == null)
            {
                return new { success = false, message = "A game already exists" };
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, game.GameCode);

            _logger.LogInformation("Game created: {GameCode} by {Nickname}", game.GameCode, nickname);

            return new
            {
                success = true,
                gameCode = game.GameCode,
                players = game.Players.Select(p => new { p.Nickname, p.IsHost })
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating game");
            return new { success = false, message = "Failed to create game" };
        }
    }

    public async Task<object> JoinGame(string gameCode, string nickname)
    {
        try
        {
            var success = _gameService.JoinGame(gameCode, Context.ConnectionId, nickname);
            if (!success)
            {
                return new { success = false, message = "Unable to join game" };
            }

            var game = _gameService.GetGame(gameCode);
            if (game == null)
            {
                return new { success = false, message = "Game not found" };
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, gameCode);

            // Notify all players in the game
            await Clients.Group(gameCode).SendAsync("PlayerJoined", new
            {
                nickname,
                players = game.Players.Select(p => new { p.Nickname, p.IsHost })
            });

            _logger.LogInformation("Player {Nickname} joined game {GameCode}", nickname, gameCode);

            return new
            {
                success = true,
                players = game.Players.Select(p => new { p.Nickname, p.IsHost })
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error joining game");
            return new { success = false, message = "Failed to join game" };
        }
    }

    public async Task<object> StartGame(string gameCode)
    {
        try
        {
            var game = _gameService.GetGame(gameCode);
            if (game == null)
            {
                return new { success = false, message = "Game not found" };
            }

            // Verify caller is host
            var host = game.Players.FirstOrDefault(p => p.IsHost);
            if (host?.ConnectionId != Context.ConnectionId)
            {
                return new { success = false, message = "Only host can start the game" };
            }

            var success = _gameService.StartGame(gameCode);
            if (!success)
            {
                return new { success = false, message = "Unable to start game" };
            }

            // Notify all players
            await Clients.Group(gameCode).SendAsync("GameStarted", new
            {
                currentTurn = game.CurrentPlayer?.Nickname,
                card = game.CurrentCard
            });

            _logger.LogInformation("Game {GameCode} started", gameCode);

            return new { success = true };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting game");
            return new { success = false, message = "Failed to start game" };
        }
    }

    public async Task<object> PlaceCard(string gameCode, int position)
    {
        try
        {
            var game = _gameService.GetGame(gameCode);
            if (game == null)
            {
                return new { success = false, message = "Game not found" };
            }

            var player = game.Players.FirstOrDefault(p => p.ConnectionId == Context.ConnectionId);
            if (player == null)
            {
                return new { success = false, message = "Player not found" };
            }

            var isValid = _gameService.PlaceCard(gameCode, Context.ConnectionId, position);

            // Check if game is finished
            var winner = _gameService.GetWinner(gameCode);
            if (winner != null)
            {
                await Clients.Group(gameCode).SendAsync("GameWon", new
                {
                    winner = winner.Nickname,
                    timeline = winner.Timeline
                });

                _logger.LogInformation("Game {GameCode} won by {Winner}", gameCode, winner.Nickname);

                return new { success = true, isValid, gameFinished = true };
            }

            // Notify about card placement
            await Clients.Group(gameCode).SendAsync("CardPlaced", new
            {
                player = player.Nickname,
                isValid,
                position,
                timeline = player.Timeline,
                currentTurn = game.CurrentPlayer?.Nickname,
                nextCard = game.CurrentCard
            });

            return new { success = true, isValid };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error placing card");
            return new { success = false, message = "Failed to place card" };
        }
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        try
        {
            var game = _gameService.GetCurrentGame();
            if (game != null)
            {
                var player = game.Players.FirstOrDefault(p => p.ConnectionId == Context.ConnectionId);
                if (player != null)
                {
                    _gameService.RemovePlayer(Context.ConnectionId);

                    await Clients.Group(game.GameCode).SendAsync("PlayerLeft", new
                    {
                        nickname = player.Nickname,
                        gameEnded = game.Players.Count == 0 || player.IsHost
                    });

                    _logger.LogInformation("Player {Nickname} left game {GameCode}", player.Nickname, game.GameCode);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling disconnect");
        }

        await base.OnDisconnectedAsync(exception);
    }
}
