using MediatR;
using SongsterGame.Api.Application.DTOs.Responses;
using SongsterGame.Api.Domain.Common;

namespace SongsterGame.Api.Application.Features.CreateGame;

/// <summary>
/// Command to create a new game.
/// </summary>
public sealed record CreateGameCommand(
    string ConnectionId,
    string Nickname
) : IRequest<Result<CreateGameResponse>>;