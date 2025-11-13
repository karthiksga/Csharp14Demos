namespace FunctionalExtensions;

/// <summary>
/// Try monad wraps computations that may throw, capturing the exception instead of throwing.
/// </summary>
public readonly record struct Try<T>(bool IsSuccess, T? Value, Exception? Exception)
{
    public static Try<T> Success(T value)
        => new(true, value, null);

    public static Try<T> Failure(Exception exception)
        => new(false, default, exception);

    public T GetOrThrow()
        => IsSuccess ? Value! : throw Exception ?? new InvalidOperationException("Try was not successful.");

    public override string ToString()
        => IsSuccess ? $"Success({Value})" : $"Failure({Exception?.Message ?? "<unknown>"})";
}
