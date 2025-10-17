namespace SongsterGame.Api.Domain.Common;

/// <summary>
/// Represents the result of an operation without a return value.
/// </summary>
public class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public Error Error { get; }

    protected Result(bool isSuccess, Error error)
    {
        if (isSuccess && error != Error.None)
        {
            throw new InvalidOperationException("Successful result cannot have an error.");
        }

        if (!isSuccess && error == Error.None)
        {
            throw new InvalidOperationException("Failed result must have an error.");
        }

        IsSuccess = isSuccess;
        Error = error;
    }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static Result Success() => new(true, Error.None);

    /// <summary>
    /// Creates a failed result with an error.
    /// </summary>
    public static Result Failure(Error error) => new(false, error);

    /// <summary>
    /// Creates a successful result with a value.
    /// </summary>
    public static Result<T> Success<T>(T value) => new(value, true, Error.None);

    /// <summary>
    /// Creates a failed result with an error.
    /// </summary>
    public static Result<T> Failure<T>(Error error) => new(default, false, error);
}

/// <summary>
/// Represents the result of an operation with a return value.
/// </summary>
public class Result<T> : Result
{
    private readonly T? _value;

    /// <summary>
    /// Gets the value if the result is successful, otherwise throws an exception.
    /// </summary>
    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Cannot access value of a failed result.");

    internal Result(T? value, bool isSuccess, Error error) : base(isSuccess, error)
    {
        _value = value;
    }

    /// <summary>
    /// Implicitly converts a value to a successful result.
    /// </summary>
    public static implicit operator Result<T>(T value) => Success(value);
}