using MediatR;
using Microsoft.AspNetCore.SignalR;
using SongsterGame.Api.Application.Features.CreateGame;
using SongsterGame.Api.Services;

namespace SongsterGame.Api.Hubs;

public class GameHub(
    IGameService gameService,
    IMediator mediator,
    ILogger<GameHub> logger) : Hub
{
    public async Task<object> CreateGame(string nickname)
    {
        try
        {
            // Use new Clean Architecture approach with MediatR
            var command = new CreateGameCommand(Context.ConnectionId, nickname);
            var result = await mediator.Send(command);

            if (result.IsFailure)
            {
                return new { success = false, message = result.Error.Message };
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, result.Value.GameCode);

            logger.LogInformation("Game created: {GameCode} by {Nickname}", result.Value.GameCode, nickname);

            return new
            {
                success = true,
                gameCode = result.Value.GameCode,
                players = result.Value.Players.Select(p => new { p.Nickname, p.IsHost })
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating game");
            return new { success = false, message = "Failed to create game" };
        }
    }

    public async Task<object> JoinGame(string gameCode, string nickname)
    {
        try
        {
            var success = gameService.JoinGame(gameCode, Context.ConnectionId, nickname);
            if (!success)
            {
                return new { success = false, message = "Unable to join game" };
            }

            var game = gameService.GetGame(gameCode);
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

            logger.LogInformation("Player {Nickname} joined game {GameCode}", nickname, gameCode);

            return new
            {
                success = true,
                players = game.Players.Select(p => new { p.Nickname, p.IsHost })
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error joining game");
            return new { success = false, message = "Failed to join game" };
        }
    }

    public async Task<object> StartGame(string gameCode)
    {
        try
        {
            var game = gameService.GetGame(gameCode);
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

            var success = gameService.StartGame(gameCode);
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

            logger.LogInformation("Game {GameCode} started", gameCode);

            return new { success = true };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error starting game");
            return new { success = false, message = "Failed to start game" };
        }
    }

    public async Task<object> PlaceCard(string gameCode, int position)
    {
        try
        {
            var game = gameService.GetGame(gameCode);
            if (game == null)
            {
                return new { success = false, message = "Game not found" };
            }

            var player = game.Players.FirstOrDefault(p => p.ConnectionId == Context.ConnectionId);
            if (player == null)
            {
                return new { success = false, message = "Player not found" };
            }

            var isValid = gameService.PlaceCard(gameCode, Context.ConnectionId, position);

            // Check if game is finished
            var winner = gameService.GetWinner(gameCode);
            if (winner != null)
            {
                await Clients.Group(gameCode).SendAsync("GameWon", new
                {
                    winner = winner.Nickname,
                    timeline = winner.Timeline
                });

                logger.LogInformation("Game {GameCode} won by {Winner}", gameCode, winner.Nickname);

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
            logger.LogError(ex, "Error placing card");
            return new { success = false, message = "Failed to place card" };
        }
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        try
        {
            var game = gameService.GetCurrentGame();
            if (game != null)
            {
                var player = game.Players.FirstOrDefault(p => p.ConnectionId == Context.ConnectionId);
                if (player != null)
                {
                    gameService.RemovePlayer(Context.ConnectionId);

                    await Clients.Group(game.GameCode).SendAsync("PlayerLeft", new
                    {
                        nickname = player.Nickname,
                        gameEnded = game.Players.Count == 0 || player.IsHost
                    });

                    logger.LogInformation("Player {Nickname} left game {GameCode}", player.Nickname, game.GameCode);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error handling disconnect");
        }

        await base.OnDisconnectedAsync(exception);
    }
}
