using SongsterGame.Api.Domain.Common;

namespace SongsterGame.Api.Domain.ValueObjects;

/// <summary>
/// Represents a SignalR connection identifier.
/// </summary>
public readonly record struct ConnectionId
{
    public string Value { get; }

    private ConnectionId(string value) => Value = value;

    /// <summary>
    /// Creates a ConnectionId from a string value with validation.
    /// </summary>
    public static Result<ConnectionId> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Failure<ConnectionId>(Error.Validation("Connection ID cannot be empty"));
        }

        return Result.Success(new ConnectionId(value.Trim()));
    }

    public static implicit operator string(ConnectionId connectionId) => connectionId.Value;

    public override string ToString() => Value;
}