namespace FunctionalExtensions;

/// <summary>
/// Writer transformer for <see cref="TaskResult{T}"/> enabling asynchronous log accumulation.
/// </summary>
public readonly record struct WriterTaskResult<TValue, TLog>(TaskResult<(TValue Value, IReadOnlyList<TLog> Logs)> Run)
{
    public TaskResult<(TValue Value, IReadOnlyList<TLog> Logs)> Invoke() => Run;
}
