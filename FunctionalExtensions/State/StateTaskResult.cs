namespace FunctionalExtensions;

/// <summary>
/// State transformer for <see cref="TaskResult{T}"/> enabling async state threading.
/// </summary>
public readonly record struct StateTaskResult<TState, TValue>(Func<TState, TaskResult<(TValue Value, TState State)>> Run)
{
    public TaskResult<(TValue Value, TState State)> Invoke(TState state)
        => Run(state);
}
