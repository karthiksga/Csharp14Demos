namespace FunctionalExtensions;

/// <summary>
/// Lightweight async wrapper that mirrors Haskell's Task/IO monad.
/// </summary>
public readonly record struct TaskIO<T>(Task<T> Task)
{
    public Task<T> Invoke() => Task;

    public static implicit operator TaskIO<T>(Task<T> task)
        => new(task);
}
