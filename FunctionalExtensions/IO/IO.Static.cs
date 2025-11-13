namespace FunctionalExtensions;

/// <summary>
/// Construction helpers for <see cref="IO{T}"/>.
/// </summary>
public static class IO
{
    public static IO<T> From<T>(Func<T> effect)
        => new(effect);

    public static IO<T> Return<T>(T value)
        => new(() => value);

    public static IO<Unit> From(Action action)
        => new(() =>
        {
            action();
            return Unit.Value;
        });
}
