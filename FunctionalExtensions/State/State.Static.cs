namespace FunctionalExtensions;

/// <summary>
/// Construction helpers for <see cref="State{TState, TValue}"/>.
/// </summary>
public static class State
{
    public static State<TState, TValue> Return<TState, TValue>(TValue value)
        => new(state => (value, state));

    public static State<TState, TState> Get<TState>()
        => new(state => (state, state));

    public static State<TState, Unit> Put<TState>(TState newState)
        => new(_ => (Unit.Value, newState));

    public static State<TState, Unit> Modify<TState>(Func<TState, TState> transformer)
        => new(state => (Unit.Value, transformer(state)));

    public static State<TState, TValue> From<TState, TValue>(Func<TState, (TValue Value, TState State)> runner)
        => new(runner);
}
