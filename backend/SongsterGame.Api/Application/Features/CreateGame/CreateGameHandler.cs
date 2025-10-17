using SongsterGame.Api.Application.Common;
using SongsterGame.Api.Application.DTOs.Responses;
using SongsterGame.Api.Domain.Common;
using SongsterGame.Api.Domain.ValueObjects;
using SongsterGame.Api.Services;

namespace SongsterGame.Api.Application.Features.CreateGame;

/// <summary>
/// Handler for CreateGameCommand.
/// </summary>
public class CreateGameHandler(IGameService gameService)
    : SyncRequestHandler<CreateGameCommand, Result<CreateGameResponse>>
{
    protected override Result<CreateGameResponse> HandleCommand(CreateGameCommand request)
    {
        // 1. Parse and validate value objects
        var connectionIdResult = ConnectionId.Create(request.ConnectionId);
        if (connectionIdResult.IsFailure)
        {
            return Result.Failure<CreateGameResponse>(connectionIdResult.Error);
        }

        var nicknameResult = Nickname.Create(request.Nickname);
        if (nicknameResult.IsFailure)
        {
            return Result.Failure<CreateGameResponse>(nicknameResult.Error);
        }

        // 2. Use existing GameService (will be migrated to repository pattern later)
        var game = gameService.CreateGame(
            connectionIdResult.Value,
            nicknameResult.Value
        );

        if (game is null)
        {
            return Result.Failure<CreateGameResponse>(
                Error.Conflict("A game already exists. Only one game at a time is supported.")
            );
        }

        // 3. Map to DTO
        var response = new CreateGameResponse(
            game.GameCode,
            game.Players.Select(p => new PlayerDto(p.Nickname, p.IsHost)).ToList()
        );

        return Result.Success(response);
    }
}