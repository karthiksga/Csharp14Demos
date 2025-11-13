namespace FunctionalExtensions;

/// <summary>
/// <summary>
/// Reader monad carries an environment through chained computations.
/// </summary>
public readonly record struct Reader<TEnv, TValue>(Func<TEnv, TValue> Run)
{
    public TValue Invoke(TEnv environment) => Run(environment);

    public override string ToString() => $"Reader({typeof(TEnv).Name} -> {typeof(TValue).Name})";
}
