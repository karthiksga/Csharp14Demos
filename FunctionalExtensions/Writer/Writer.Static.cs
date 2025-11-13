namespace FunctionalExtensions;

/// <summary>
/// Construction helpers for <see cref="Writer{TValue, TLog}"/>.
/// </summary>
public static class Writer
{
    public static Writer<TValue, TLog> Return<TValue, TLog>(TValue value)
        => new(value, Array.Empty<TLog>());

    public static Writer<Unit, TLog> Tell<TLog>(TLog log)
        => new(Unit.Value, new[] { log });

    public static Writer<TValue, TLog> From<TValue, TLog>(TValue value, params TLog[] logs)
        => new(value, logs);
}
