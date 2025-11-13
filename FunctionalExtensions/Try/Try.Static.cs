namespace FunctionalExtensions;

/// <summary>
/// Factory helpers for <see cref="Try{T}"/>.
/// </summary>
public static class Try
{
    public static Try<T> Run<T>(Func<T> producer)
    {
        try
        {
            return Try<T>.Success(producer());
        }
        catch (Exception ex)
        {
            return Try<T>.Failure(ex);
        }
    }

    public static Try<Unit> Run(Action action)
    {
        try
        {
            action();
            return Try<Unit>.Success(Unit.Value);
        }
        catch (Exception ex)
        {
            return Try<Unit>.Failure(ex);
        }
    }
}
