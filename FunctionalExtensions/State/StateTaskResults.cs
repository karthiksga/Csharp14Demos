namespace FunctionalExtensions;

/// <summary>
/// Factory helpers for <see cref="StateTaskResult{TState, TValue}"/>.
/// </summary>
public static class StateTaskResults
{
    public static StateTaskResult<TState, TValue> Return<TState, TValue>(TValue value)
        => new(state => TaskResults.Return((value, state)));

    public static StateTaskResult<TState, TState> Get<TState>()
        => new(state => TaskResults.Return((state, state)));

    public static StateTaskResult<TState, Unit> Put<TState>(TState newState)
        => new(_ => TaskResults.Return((Unit.Value, newState)));

    public static StateTaskResult<TState, Unit> Modify<TState>(Func<TState, TState> transformer)
        => new(state => TaskResults.Return((Unit.Value, transformer(state))));

    public static StateTaskResult<TState, TValue> From<TState, TValue>(Func<TState, TaskResult<(TValue Value, TState State)>> runner)
        => new(runner);
}
