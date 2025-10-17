using System.Text.Json.Serialization;
using SongsterGame.Api.Application.DTOs.Common;

namespace SongsterGame.Api.Application.DTOs.Events;

/// <summary>
/// Event broadcasted to all players when a card is placed.
/// </summary>
public sealed record CardPlacedEvent(
    [property: JsonPropertyName("player")] string PlayerNickname,
    [property: JsonPropertyName("isValid")] bool IsValid,
    [property: JsonPropertyName("position")] int Position,
    [property: JsonPropertyName("timeline")] IReadOnlyList<MusicCardDto> Timeline,
    [property: JsonPropertyName("currentTurn")] string? CurrentTurn,
    [property: JsonPropertyName("nextCard")] object? NextCard
);
