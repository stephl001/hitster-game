using System.Text.Json.Serialization;

namespace SongsterGame.Api.Application.DTOs.HubResults;

/// <summary>
/// Success result for StartGame hub method.
/// </summary>
public record StartGameSuccessHubResult : HubResult
{
    public override bool Success => true;
}