using SongsterGame.Api.Domain.Common;

namespace SongsterGame.Api.Domain.ValueObjects;

/// <summary>
/// Represents a player nickname with validation rules.
/// </summary>
public readonly record struct Nickname
{
    private const int MinLength = 2;
    private const int MaxLength = 20;

    public string Value { get; }

    private Nickname(string value) => Value = value;

    /// <summary>
    /// Creates a Nickname from a string value with validation.
    /// </summary>
    public static Result<Nickname> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Failure<Nickname>(Error.Validation("Nickname cannot be empty"));
        }

        var trimmed = value.Trim();

        if (trimmed.Length < MinLength)
        {
            return Result.Failure<Nickname>(Error.Validation($"Nickname must be at least {MinLength} characters"));
        }

        if (trimmed.Length > MaxLength)
        {
            return Result.Failure<Nickname>(Error.Validation($"Nickname cannot exceed {MaxLength} characters"));
        }

        return Result.Success(new Nickname(trimmed));
    }

    public static implicit operator string(Nickname nickname) => nickname.Value;

    public override string ToString() => Value;
}