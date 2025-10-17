using SongsterGame.Api.Application.Common;
using SongsterGame.Api.Application.DTOs.Common;
using SongsterGame.Api.Application.DTOs.Responses;
using SongsterGame.Api.Domain.Common;
using SongsterGame.Api.Domain.ValueObjects;
using SongsterGame.Api.Models;
using SongsterGame.Api.Services;

namespace SongsterGame.Api.Application.Features.PlaceCard;

/// <summary>
/// Handler for PlaceCardCommand.
/// </summary>
public class PlaceCardHandler(IGameService gameService)
    : SyncRequestHandler<PlaceCardCommand, Result<PlaceCardResponse>>
{
    protected override Result<PlaceCardResponse> HandleCommand(PlaceCardCommand request)
    {
        // 1. Parse and validate value objects
        var gameCodeResult = GameCode.Create(request.GameCode);
        if (gameCodeResult.IsFailure)
        {
            return Result.Failure<PlaceCardResponse>(gameCodeResult.Error);
        }

        var connectionIdResult = ConnectionId.Create(request.ConnectionId);
        if (connectionIdResult.IsFailure)
        {
            return Result.Failure<PlaceCardResponse>(connectionIdResult.Error);
        }

        var positionResult = Position.Create(request.Position);
        if (positionResult.IsFailure)
        {
            return Result.Failure<PlaceCardResponse>(positionResult.Error);
        }

        // 2. Get game from service
        var game = gameService.GetGame(gameCodeResult.Value);
        if (game is null)
        {
            return Result.Failure<PlaceCardResponse>(
                Error.NotFound($"Game with code '{request.GameCode}' not found.")
            );
        }

        // 3. Validate game state
        if (game.State != GameState.Playing)
        {
            return Result.Failure<PlaceCardResponse>(
                Error.BusinessRule("Game is not in playing state.")
            );
        }

        if (game.CurrentCard is null)
        {
            return Result.Failure<PlaceCardResponse>(
                Error.BusinessRule("No card is currently drawn.")
            );
        }

        // 4. Verify player exists and it's their turn
        var player = game.Players.FirstOrDefault(p => p.ConnectionId == connectionIdResult.Value);
        if (player is null)
        {
            return Result.Failure<PlaceCardResponse>(
                Error.NotFound("Player not found in game.")
            );
        }

        if (game.CurrentPlayer?.ConnectionId != connectionIdResult.Value)
        {
            return Result.Failure<PlaceCardResponse>(
                Error.BusinessRule("It is not your turn.")
            );
        }

        // 5. Validate position is within valid range
        if (positionResult.Value > player.Timeline.Count)
        {
            return Result.Failure<PlaceCardResponse>(
                Error.BusinessRule($"Position {request.Position} is out of range. Timeline length is {player.Timeline.Count}.")
            );
        }

        // 6. Use existing GameService to place card
        var isValid = gameService.PlaceCard(gameCodeResult.Value, connectionIdResult.Value, positionResult.Value);

        // 7. Check if game is finished
        var winner = gameService.GetWinner(gameCodeResult.Value);
        var gameFinished = winner is not null;

        // 8. Get updated game state
        var updatedGame = gameService.GetGame(gameCodeResult.Value);
        if (updatedGame is null)
        {
            return Result.Failure<PlaceCardResponse>(
                Error.BusinessRule("Game disappeared after card placement.")
            );
        }

        // 9. Map player timeline to DTOs
        var timelineDtos = player.Timeline
            .Select(c => new MusicCardDto(c.Title, c.Artist, c.Year, c.PreviewUrl))
            .ToList();

        var response = new PlaceCardResponse(
            IsValid: isValid,
            GameFinished: gameFinished,
            PlayerNickname: player.Nickname,
            PlayerTimeline: timelineDtos,
            CurrentPlayerNickname: updatedGame.CurrentPlayer?.Nickname,
            WinnerNickname: winner?.Nickname
        );

        return Result.Success(response);
    }
}
