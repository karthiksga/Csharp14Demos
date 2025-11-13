namespace FunctionalExtensions;

/// <summary>
/// Construction helpers for <see cref="Reader{TEnv, TValue}"/>.
/// </summary>
public static class Reader
{
    public static Reader<TEnv, TValue> Return<TEnv, TValue>(TValue value)
        => new(_ => value);

    public static Reader<TEnv, TEnv> Ask<TEnv>()
        => new(env => env);

    public static Reader<TEnv, TValue> From<TEnv, TValue>(Func<TEnv, TValue> projection)
        => new(projection);
}
