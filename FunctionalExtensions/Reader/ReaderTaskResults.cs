namespace FunctionalExtensions;

/// <summary>
/// Factory helpers for <see cref="ReaderTaskResult{TEnv, TValue}"/>.
/// </summary>
public static class ReaderTaskResults
{
    public static ReaderTaskResult<TEnv, TValue> Return<TEnv, TValue>(TValue value)
        => new(_ => TaskResults.Return(value));

    public static ReaderTaskResult<TEnv, TEnv> Ask<TEnv>()
        => new(env => TaskResults.Return(env));

    public static ReaderTaskResult<TEnv, TValue> From<TEnv, TValue>(Func<TEnv, TaskResult<TValue>> runner)
        => new(runner);
}
