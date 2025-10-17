namespace SongsterGame.Api.Application.DTOs.Responses;

/// <summary>
/// Response returned when a player successfully joins a game.
/// </summary>
public sealed record JoinGameResponse(
    string GameCode,
    IReadOnlyList<PlayerDto> Players
);