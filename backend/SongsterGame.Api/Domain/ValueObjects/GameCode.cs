using SongsterGame.Api.Domain.Common;

namespace SongsterGame.Api.Domain.ValueObjects;

/// <summary>
/// Represents a unique game code (8 alphanumeric characters).
/// </summary>
public readonly record struct GameCode
{
    private const int Length = 8;
    private const string AllowedChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

    public string Value { get; }

    private GameCode(string value) => Value = value;

    /// <summary>
    /// Creates a GameCode from a string value with validation.
    /// </summary>
    public static Result<GameCode> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Failure<GameCode>(Error.Validation("Game code cannot be empty"));
        }

        var trimmed = value.Trim().ToUpperInvariant();

        if (trimmed.Length != Length)
        {
            return Result.Failure<GameCode>(Error.Validation($"Game code must be exactly {Length} characters"));
        }

        if (!trimmed.All(c => AllowedChars.Contains(c)))
        {
            return Result.Failure<GameCode>(Error.Validation("Game code contains invalid characters (only A-Z and 0-9 allowed)"));
        }

        return Result.Success(new GameCode(trimmed));
    }

    /// <summary>
    /// Generates a new random game code.
    /// </summary>
    public static GameCode Generate()
    {
        var random = Random.Shared;
        var code = new string(Enumerable.Range(0, Length)
            .Select(_ => AllowedChars[random.Next(AllowedChars.Length)])
            .ToArray());
        return new GameCode(code);
    }

    public static implicit operator string(GameCode code) => code.Value;

    public override string ToString() => Value;
}