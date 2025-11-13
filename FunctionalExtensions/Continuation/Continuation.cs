namespace FunctionalExtensions;

/// <summary>
/// Construction helpers for <see cref="Cont{TOutput, TValue}"/>.
/// </summary>
public static class Continuation
{
    public static Cont<TOutput, TValue> Return<TOutput, TValue>(TValue value)
        => new(continuation => continuation(value));

    public static Cont<TOutput, TValue> From<TOutput, TValue>(Func<Func<TValue, TOutput>, TOutput> runner)
        => new(runner);

    public static Cont<TOutput, TValue> CallCC<TOutput, TValue>(
        Func<Func<TValue, Cont<TOutput, TValue>>, Cont<TOutput, TValue>> function)
        => new(continuation =>
        {
            Cont<TOutput, TValue> Escape(TValue value) => new(_ => continuation(value));
            return function(Escape).Run(continuation);
        });
}
