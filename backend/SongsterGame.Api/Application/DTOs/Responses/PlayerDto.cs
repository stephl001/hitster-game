namespace SongsterGame.Api.Application.DTOs.Responses;

/// <summary>
/// Immutable DTO representing a player in the game.
/// </summary>
public sealed record PlayerDto(
    string Nickname,
    bool IsHost
);