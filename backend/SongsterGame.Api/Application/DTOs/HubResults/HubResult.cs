using System.Text.Json.Serialization;

namespace SongsterGame.Api.Application.DTOs.HubResults;

/// <summary>
/// Base class for all SignalR hub method results.
/// Provides consistent response structure with success indicator.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(FailureHubResult), typeDiscriminator: "failure")]
[JsonDerivedType(typeof(CreateGameSuccessHubResult), typeDiscriminator: "createGameSuccess")]
[JsonDerivedType(typeof(JoinGameSuccessHubResult), typeDiscriminator: "joinGameSuccess")]
[JsonDerivedType(typeof(StartGameSuccessHubResult), typeDiscriminator: "startGameSuccess")]
[JsonDerivedType(typeof(PlaceCardSuccessHubResult), typeDiscriminator: "placeCardSuccess")]
public abstract record HubResult
{
    [JsonPropertyName("success")]
    public virtual bool Success { get; init; }
}

/// <summary>
/// Represents a failed hub operation with an error message.
/// </summary>
public record FailureHubResult : HubResult
{
    [JsonPropertyName("message")]
    public required string Message { get; init; }

    public override bool Success => false;
}