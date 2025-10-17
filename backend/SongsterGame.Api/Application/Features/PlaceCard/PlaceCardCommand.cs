using MediatR;
using SongsterGame.Api.Application.DTOs.Responses;
using SongsterGame.Api.Domain.Common;

namespace SongsterGame.Api.Application.Features.PlaceCard;

/// <summary>
/// Command to place a card in a player's timeline.
/// </summary>
public sealed record PlaceCardCommand(
    string GameCode,
    string ConnectionId,
    int Position
) : IRequest<Result<PlaceCardResponse>>;
