namespace FunctionalExtensions;

/// <summary>
/// Construction helpers for <see cref="TaskIO{T}"/>.
/// </summary>
public static class TaskIO
{
    public static TaskIO<T> Return<T>(T value)
        => new(System.Threading.Tasks.Task.FromResult(value));

    public static TaskIO<T> From<T>(Func<Task<T>> producer)
        => new(producer());

    public static TaskIO<Unit> From(Func<Task> producer)
        => new(producer().ContinueWith(_ => Unit.Value));
}
