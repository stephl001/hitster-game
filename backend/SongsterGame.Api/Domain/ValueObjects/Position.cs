using SongsterGame.Api.Domain.Common;

namespace SongsterGame.Api.Domain.ValueObjects;

/// <summary>
/// Represents a position in a player's timeline where a card can be placed.
/// </summary>
public readonly record struct Position
{
    public int Value { get; }

    private Position(int value) => Value = value;

    /// <summary>
    /// Creates a Position with validation.
    /// Position must be non-negative. Upper bound validation is context-dependent (timeline length).
    /// </summary>
    public static Result<Position> Create(int value)
    {
        if (value < 0)
            return Result.Failure<Position>(Error.Validation("Position cannot be negative"));
        
        return Result.Success(new Position(value));
    }

    public static implicit operator int(Position position) => position.Value;

    public override string ToString() => Value.ToString();
}
