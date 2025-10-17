using MediatR;
using Microsoft.AspNetCore.SignalR;
using SongsterGame.Api.Application.DTOs.Common;
using SongsterGame.Api.Application.DTOs.Events;
using SongsterGame.Api.Application.DTOs.HubResults;
using SongsterGame.Api.Application.Features.CreateGame;
using SongsterGame.Api.Application.Features.JoinGame;
using SongsterGame.Api.Application.Features.PlaceCard;
using SongsterGame.Api.Application.Features.StartGame;
using SongsterGame.Api.Services;

namespace SongsterGame.Api.Hubs;

public class GameHub(
    IGameService gameService,
    IMediator mediator,
    ILogger<GameHub> logger) : Hub
{
    public async Task<HubResult> CreateGame(string nickname)
    {
        try
        {
            // Use new Clean Architecture approach with MediatR
            var command = new CreateGameCommand(Context.ConnectionId, nickname);
            var result = await mediator.Send(command);

            if (result.IsFailure)
            {
                return new FailureHubResult { Message = result.Error.Message };
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, result.Value.GameCode);

            logger.LogInformation("Game created: {GameCode} by {Nickname}", result.Value.GameCode, nickname);

            return new CreateGameSuccessHubResult
            {
                GameCode = result.Value.GameCode,
                Players = result.Value.Players
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating game");
            return new FailureHubResult { Message = "Failed to create game" };
        }
    }

    public async Task<HubResult> JoinGame(string gameCode, string nickname)
    {
        try
        {
            // Use new Clean Architecture approach with MediatR
            var command = new JoinGameCommand(gameCode, Context.ConnectionId, nickname);
            var result = await mediator.Send(command);

            if (result.IsFailure)
            {
                return new FailureHubResult { Message = result.Error.Message };
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, gameCode);

            // Create event DTO for SignalR broadcast
            var playerJoinedEvent = new PlayerJoinedEvent(
                nickname,
                result.Value.Players
            );

            // Notify all players in the game
            await Clients.Group(gameCode).SendAsync("PlayerJoined", playerJoinedEvent);

            logger.LogInformation("Player {Nickname} joined game {GameCode}", nickname, gameCode);

            return new JoinGameSuccessHubResult
            {
                Players = result.Value.Players
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error joining game");
            return new FailureHubResult { Message = "Failed to join game" };
        }
    }

    public async Task<HubResult> StartGame(string gameCode)
    {
        try
        {
            // Use new Clean Architecture approach with MediatR
            var command = new StartGameCommand(gameCode, Context.ConnectionId);
            var result = await mediator.Send(command);

            if (result.IsFailure)
            {
                return new FailureHubResult { Message = result.Error.Message };
            }

            // Get updated game for current card information
            var game = gameService.GetGame(gameCode);

            // Create event DTO for SignalR broadcast
            var gameStartedEvent = new GameStartedEvent(
                result.Value.CurrentPlayerNickname,
                game?.CurrentCard
            );

            // Notify all players
            await Clients.Group(gameCode).SendAsync("GameStarted", gameStartedEvent);

            logger.LogInformation("Game {GameCode} started", gameCode);

            return new StartGameSuccessHubResult();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error starting game");
            return new FailureHubResult { Message = "Failed to start game" };
        }
    }

    public async Task<HubResult> PlaceCard(string gameCode, int position)
    {
        try
        {
            // Use new Clean Architecture approach with MediatR
            var command = new PlaceCardCommand(gameCode, Context.ConnectionId, position);
            var result = await mediator.Send(command);

            if (result.IsFailure)
            {
                return new FailureHubResult { Message = result.Error.Message };
            }

            // Get updated game for current card information
            var game = gameService.GetGame(gameCode);

            // Check if game is finished
            if (result.Value.GameFinished && result.Value.WinnerNickname is not null)
            {
                // Create event DTO for SignalR broadcast
                var gameWonEvent = new GameWonEvent(
                    result.Value.WinnerNickname,
                    result.Value.PlayerTimeline
                );

                await Clients.Group(gameCode).SendAsync("GameWon", gameWonEvent);

                logger.LogInformation("Game {GameCode} won by {Winner}", gameCode, result.Value.WinnerNickname);

                return new PlaceCardSuccessHubResult
                {
                    IsValid = result.Value.IsValid,
                    GameFinished = true
                };
            }

            // Create event DTO for card placement
            var cardPlacedEvent = new CardPlacedEvent(
                result.Value.PlayerNickname,
                result.Value.IsValid,
                position,
                result.Value.PlayerTimeline,
                result.Value.CurrentPlayerNickname,
                game?.CurrentCard
            );

            // Notify about card placement
            await Clients.Group(gameCode).SendAsync("CardPlaced", cardPlacedEvent);

            return new PlaceCardSuccessHubResult
            {
                IsValid = result.Value.IsValid,
                GameFinished = false
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error placing card");
            return new FailureHubResult { Message = "Failed to place card" };
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
