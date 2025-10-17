using SongsterGame.Api.Application.DTOs.Common;

namespace SongsterGame.Api.Application.DTOs.Responses;

/// <summary>
/// Response returned after placing a card in the timeline.
/// </summary>
public sealed record PlaceCardResponse(
    bool IsValid,
    bool GameFinished,
    string PlayerNickname,
    IReadOnlyList<MusicCardDto> PlayerTimeline,
    string? CurrentPlayerNickname,
    string? WinnerNickname = null
);
