using MediatR;
using SongsterGame.Api.Application.DTOs.Responses;
using SongsterGame.Api.Domain.Common;

namespace SongsterGame.Api.Application.Features.JoinGame;

/// <summary>
/// Command to join an existing game.
/// </summary>
public sealed record JoinGameCommand(
    string GameCode,
    string ConnectionId,
    string Nickname
) : IRequest<Result<JoinGameResponse>>;