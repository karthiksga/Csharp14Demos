namespace FunctionalExtensions;

/// <summary>
/// Continuation monad models computations in continuation-passing style.
/// </summary>
public readonly record struct Cont<TOutput, TValue>(Func<Func<TValue, TOutput>, TOutput> Run)
{
    public TOutput Invoke(Func<TValue, TOutput> continuation)
        => Run(continuation);
}
