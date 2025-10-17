using MediatR;
using SongsterGame.Api.Application.DTOs.Responses;
using SongsterGame.Api.Domain.Common;

namespace SongsterGame.Api.Application.Features.StartGame;

/// <summary>
/// Command to start a game. Only the host can start the game.
/// </summary>
public sealed record StartGameCommand(
    string GameCode,
    string ConnectionId
) : IRequest<Result<StartGameResponse>>;
