namespace FunctionalExtensions;

/// <summary>
/// Task-result fusion that marries async workflows with success/error signalling.
/// </summary>
public readonly record struct TaskResult<T>(Task<Result<T>> Task)
{
    public Task<Result<T>> Invoke() => Task;

    public static implicit operator TaskResult<T>(Task<Result<T>> task)
        => new(task);
}
