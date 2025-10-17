using System.Text.Json.Serialization;
using SongsterGame.Api.Application.DTOs.Common;

namespace SongsterGame.Api.Application.DTOs.Events;

/// <summary>
/// Event broadcasted to all players when a player wins the game.
/// </summary>
public sealed record GameWonEvent(
    [property: JsonPropertyName("winner")] string WinnerNickname,
    [property: JsonPropertyName("timeline")] IReadOnlyList<MusicCardDto> Timeline
);
