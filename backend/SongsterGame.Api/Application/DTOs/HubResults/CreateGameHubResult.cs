using System.Text.Json.Serialization;
using SongsterGame.Api.Application.DTOs.Responses;

namespace SongsterGame.Api.Application.DTOs.HubResults;

/// <summary>
/// Success result for CreateGame hub method.
/// </summary>
public record CreateGameSuccessHubResult : HubResult
{
    [JsonPropertyName("gameCode")]
    public required string GameCode { get; init; }

    [JsonPropertyName("players")]
    public required IReadOnlyList<PlayerDto> Players { get; init; }

    public override bool Success => true;
}