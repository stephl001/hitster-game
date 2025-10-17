using SongsterGame.Api.Application.DTOs.Responses;

namespace SongsterGame.Api.Application.DTOs.Events;

/// <summary>
/// Event broadcasted to all players when a new player joins the game.
/// </summary>
public sealed record PlayerJoinedEvent(
    string Nickname,
    IReadOnlyList<PlayerDto> Players
);