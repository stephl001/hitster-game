namespace SongsterGame.Api.Application.DTOs.Responses;

/// <summary>
/// Immutable DTO for create game response.
/// </summary>
public sealed record CreateGameResponse(
    string GameCode,
    IReadOnlyList<PlayerDto> Players
);