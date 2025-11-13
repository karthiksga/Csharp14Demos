namespace FunctionalExtensions;

/// <summary>
/// Writer monad accumulates logs alongside a value.
/// </summary>
public readonly record struct Writer<TValue, TLog>(TValue Value, IReadOnlyList<TLog> Logs)
{
    public void Deconstruct(out TValue value, out IReadOnlyList<TLog> logs)
    {
        value = Value;
        logs = Logs;
    }

    public override string ToString()
        => $"Writer(Value: {Value}, Logs: [{string.Join(", ", Logs)}])";
}
