namespace FunctionalExtensions;

/// <summary>
/// Lightweight unit type that plays nicely with IO style workflows.
/// </summary>
public readonly record struct Unit
{
    public static readonly Unit Value = new();

    public override string ToString() => "()";
}
