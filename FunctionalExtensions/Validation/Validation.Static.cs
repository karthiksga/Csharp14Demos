namespace FunctionalExtensions;

/// <summary>
/// Factory helpers for <see cref="Validation{TValue}"/>.
/// </summary>
public static class Validation
{
    public static Validation<TValue> Success<TValue>(TValue value)
        => Validation<TValue>.Success(value);

    public static Validation<TValue> Failure<TValue>(params string[] errors)
        => Validation<TValue>.Failure(errors);

    public static Validation<TValue> From<TValue>(TValue value, params string[] errors)
        => errors.Length == 0 ? Success(value) : Failure<TValue>(errors);
}
