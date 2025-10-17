namespace SongsterGame.Api.Application.DTOs.Responses;

/// <summary>
/// Response returned when a game is successfully started.
/// </summary>
public sealed record StartGameResponse(
    string CurrentPlayerNickname,
    string? CurrentCardTitle
);
