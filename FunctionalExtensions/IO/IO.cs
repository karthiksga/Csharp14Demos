namespace FunctionalExtensions;

/// <summary>
/// Lazy IO monad that defers computation until explicitly run.
/// </summary>
public readonly record struct IO<T>(Func<T> Effect)
{
    public T Invoke() => Effect();

    public static implicit operator IO<T>(Func<T> effect)
        => new(effect);
}
