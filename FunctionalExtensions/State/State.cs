namespace FunctionalExtensions;

/// <summary>
/// State monad threads mutable state through pure computations.
/// </summary>
public readonly record struct State<TState, TValue>(Func<TState, (TValue Value, TState State)> Run)
{
    public (TValue Value, TState State) Invoke(TState state)
        => Run(state);
}
