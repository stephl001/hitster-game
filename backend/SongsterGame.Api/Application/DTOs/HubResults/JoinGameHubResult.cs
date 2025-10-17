using System.Text.Json.Serialization;
using SongsterGame.Api.Application.DTOs.Responses;

namespace SongsterGame.Api.Application.DTOs.HubResults;

/// <summary>
/// Success result for JoinGame hub method.
/// </summary>
public record JoinGameSuccessHubResult : HubResult
{
    [JsonPropertyName("players")]
    public required IReadOnlyList<PlayerDto> Players { get; init; }

    public override bool Success => true;
}