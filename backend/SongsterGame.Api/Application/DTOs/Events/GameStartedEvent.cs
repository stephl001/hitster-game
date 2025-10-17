using System.Text.Json.Serialization;

namespace SongsterGame.Api.Application.DTOs.Events;

/// <summary>
/// Event broadcasted to all players when the game starts.
/// </summary>
public sealed record GameStartedEvent(
    [property: JsonPropertyName("currentTurn")] string CurrentTurn,
    [property: JsonPropertyName("card")] object? Card
);
