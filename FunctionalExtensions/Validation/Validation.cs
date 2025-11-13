namespace FunctionalExtensions;

/// <summary>
/// Validation applicative accumulates errors while computing a value.
/// </summary>
public readonly record struct Validation<TValue>(bool IsValid, TValue? Value, IReadOnlyList<string> Errors)
{
    public static Validation<TValue> Success(TValue value)
        => new(true, value, Array.Empty<string>());

    public static Validation<TValue> Failure(params string[] errors)
        => new(false, default, Array.AsReadOnly(errors));

    public override string ToString()
        => IsValid ? $"Valid({Value})" : $"Invalid([{string.Join(", ", Errors)}])";
}
