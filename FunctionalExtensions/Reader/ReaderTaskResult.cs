namespace FunctionalExtensions;

/// <summary>
/// Reader transformer for <see cref="TaskResult{T}"/> enabling environment-aware async computations.
/// </summary>
public readonly record struct ReaderTaskResult<TEnv, TValue>(Func<TEnv, TaskResult<TValue>> Run)
{
    public TaskResult<TValue> Invoke(TEnv environment) => Run(environment);
}
