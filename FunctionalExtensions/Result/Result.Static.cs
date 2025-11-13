namespace FunctionalExtensions;

/// <summary>
/// Factory helpers for <see cref="Result{T}"/>.
/// </summary>
public static class Result
{
    public static Result<T> Ok<T>(T value)
        => Result<T>.Ok(value);

    public static Result<T> Fail<T>(string error)
        => Result<T>.Fail(error);

    public static Result<T> Try<T>(Func<T> producer)
    {
        try
        {
            return Result<T>.Ok(producer());
        }
        catch (Exception ex)
        {
            return Result<T>.Fail(ex.Message);
        }
    }
}
