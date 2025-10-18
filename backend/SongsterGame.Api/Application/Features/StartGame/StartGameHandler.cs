using SongsterGame.Api.Application.Common;
using SongsterGame.Api.Application.DTOs.Responses;
using SongsterGame.Api.Domain.Common;
using SongsterGame.Api.Domain.ValueObjects;
using SongsterGame.Api.Models;
using SongsterGame.Api.Services;

namespace SongsterGame.Api.Application.Features.StartGame;

/// <summary>
/// Handler for StartGameCommand.
/// </summary>
public class StartGameHandler(IGameService gameService)
    : SyncRequestHandler<StartGameCommand, Result<StartGameResponse>>
{
    protected override Result<StartGameResponse> HandleCommand(StartGameCommand request)
    {
        // 1. Parse and validate value objects
        var gameCodeResult = GameCode.Create(request.GameCode);
        if (gameCodeResult.IsFailure)
            return Result.Failure<StartGameResponse>(gameCodeResult.Error);
        
        var connectionIdResult = ConnectionId.Create(request.ConnectionId);
        if (connectionIdResult.IsFailure)
            return Result.Failure<StartGameResponse>(connectionIdResult.Error);
        
        // 2. Get game from service
        var game = gameService.GetGame(gameCodeResult.Value);
        if (game is null)
            return Result.Failure<StartGameResponse>(Error.NotFound($"Game with code '{request.GameCode}' not found."));
        

        // 3. Validate game state
        if (game.State != GameState.Lobby)
            return Result.Failure<StartGameResponse>(Error.BusinessRule("Game has already been started."));
        
        // 4. Verify caller is host
        var host = game.Players.FirstOrDefault(p => p.IsHost);
        if (host is null)
            return Result.Failure<StartGameResponse>(Error.BusinessRule("No host found for this game."));
        
        if (host.ConnectionId != connectionIdResult.Value)
            return Result.Failure<StartGameResponse>(Error.Unauthorized("Only the host can start the game."));
        
        // 5. Validate minimum players
        if (game.Players.Count < 2)
            return Result.Failure<StartGameResponse>(Error.BusinessRule("At least 2 players are required to start the game."));
        
        // 6. Use existing GameService to start (will be migrated to repository pattern later)
        var success = gameService.StartGame(gameCodeResult.Value);
        if (!success)
            return Result.Failure<StartGameResponse>(Error.BusinessRule("Failed to start game due to an unexpected error."));
        
        // 7. Get updated game and map to DTO
        var updatedGame = gameService.GetGame(gameCodeResult.Value);
        if (updatedGame is null)
            return Result.Failure<StartGameResponse>(Error.BusinessRule("Game disappeared after start operation."));
        
        var response = new StartGameResponse(updatedGame.CurrentPlayer?.Nickname ?? "Unknown", updatedGame.CurrentCard?.Title);

        return Result.Success(response);
    }
}
