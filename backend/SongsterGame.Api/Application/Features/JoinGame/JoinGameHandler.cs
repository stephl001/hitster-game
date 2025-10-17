using MediatR;
using SongsterGame.Api.Application.DTOs.Responses;
using SongsterGame.Api.Domain.Common;
using SongsterGame.Api.Domain.ValueObjects;
using SongsterGame.Api.Models;
using SongsterGame.Api.Services;

namespace SongsterGame.Api.Application.Features.JoinGame;

/// <summary>
/// Handler for JoinGameCommand.
/// </summary>
public class JoinGameHandler : IRequestHandler<JoinGameCommand, Result<JoinGameResponse>>
{
    private readonly IGameService _gameService;

    public JoinGameHandler(IGameService gameService)
    {
        _gameService = gameService;
    }

    public async Task<Result<JoinGameResponse>> Handle(
        JoinGameCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Parse and validate value objects
        var gameCodeResult = GameCode.Create(request.GameCode);
        if (gameCodeResult.IsFailure)
        {
            return Result.Failure<JoinGameResponse>(gameCodeResult.Error);
        }

        var connectionIdResult = ConnectionId.Create(request.ConnectionId);
        if (connectionIdResult.IsFailure)
        {
            return Result.Failure<JoinGameResponse>(connectionIdResult.Error);
        }

        var nicknameResult = Nickname.Create(request.Nickname);
        if (nicknameResult.IsFailure)
        {
            return Result.Failure<JoinGameResponse>(nicknameResult.Error);
        }

        // 2. Get game from service
        var game = _gameService.GetGame(gameCodeResult.Value);
        if (game is null)
        {
            return Result.Failure<JoinGameResponse>(
                Error.NotFound($"Game with code '{request.GameCode}' not found.")
            );
        }

        // 3. Validate game state
        if (game.State != GameState.Lobby)
        {
            return Result.Failure<JoinGameResponse>(
                Error.BusinessRule("Cannot join a game that has already started.")
            );
        }

        // 4. Validate max players
        if (game.Players.Count >= Game.MaxPlayers)
        {
            return Result.Failure<JoinGameResponse>(
                Error.BusinessRule($"Game is full. Maximum {Game.MaxPlayers} players allowed.")
            );
        }

        // 5. Check if nickname already exists (case-insensitive)
        if (game.Players.Any(p => p.Nickname.Equals(request.Nickname, StringComparison.OrdinalIgnoreCase)))
        {
            return Result.Failure<JoinGameResponse>(
                Error.Conflict($"A player with nickname '{request.Nickname}' already exists in this game.")
            );
        }

        // 6. Use existing GameService to join (will be migrated to repository pattern later)
        var success = _gameService.JoinGame(
            gameCodeResult.Value,
            connectionIdResult.Value,
            nicknameResult.Value
        );

        if (!success)
        {
            return Result.Failure<JoinGameResponse>(
                Error.BusinessRule("Failed to join game due to an unexpected error.")
            );
        }

        // 7. Get updated game and map to DTO
        var updatedGame = _gameService.GetGame(gameCodeResult.Value);
        if (updatedGame is null)
        {
            return Result.Failure<JoinGameResponse>(
                Error.BusinessRule("Game disappeared after join operation.")
            );
        }

        var response = new JoinGameResponse(
            updatedGame.GameCode,
            updatedGame.Players.Select(p => new PlayerDto(p.Nickname, p.IsHost)).ToList()
        );

        return Result.Success(response);
    }
}