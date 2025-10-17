using System.Text.Json.Serialization;

namespace SongsterGame.Api.Application.DTOs.HubResults;

/// <summary>
/// Success result for PlaceCard hub method.
/// </summary>
public record PlaceCardSuccessHubResult : HubResult
{
    [JsonPropertyName("isValid")]
    public required bool IsValid { get; init; }

    [JsonPropertyName("gameFinished")]
    public bool GameFinished { get; init; }

    public override bool Success => true;
}