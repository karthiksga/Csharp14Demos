namespace FunctionalExtensions;

/// <summary>
/// Minimal Option (Maybe) type that supports functional-style composition via user-defined operators.
/// </summary>
public readonly record struct Option<T>(bool HasValue, T? Value)
{
    public static Option<T> Some(T value)
        => new(true, value);

    public static Option<T> None
        => new(false, default);
}
