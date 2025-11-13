namespace FunctionalExtensions;

/// <summary>
/// Result monad representing either a successful value or an error message.
/// </summary>
public readonly record struct Result<T>(bool IsSuccess, T? Value, string? Error)
{
    public static Result<T> Ok(T value)
        => new(true, value, null);

    public static Result<T> Fail(string error)
        => new(false, default, error);

    public override string ToString()
        => IsSuccess ? $"Ok({Value})" : $"Error({Error})";
}
