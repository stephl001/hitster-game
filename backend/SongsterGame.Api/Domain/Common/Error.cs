namespace SongsterGame.Api.Domain.Common;

/// <summary>
/// Represents an error with a code and message.
/// </summary>
public sealed record Error(string Code, string Message)
{
    /// <summary>
    /// Represents no error (success state).
    /// </summary>
    public static readonly Error None = new(string.Empty, string.Empty);

    /// <summary>
    /// Creates a validation error.
    /// </summary>
    public static Error Validation(string message) => new("Validation", message);

    /// <summary>
    /// Creates a not found error.
    /// </summary>
    public static Error NotFound(string message) => new("NotFound", message);

    /// <summary>
    /// Creates a conflict error (e.g., duplicate entry).
    /// </summary>
    public static Error Conflict(string message) => new("Conflict", message);

    /// <summary>
    /// Creates a business rule violation error.
    /// </summary>
    public static Error BusinessRule(string message) => new("BusinessRule", message);

    /// <summary>
    /// Creates an unauthorized error.
    /// </summary>
    public static Error Unauthorized(string message) => new("Unauthorized", message);
}