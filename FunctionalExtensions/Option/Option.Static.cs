namespace FunctionalExtensions;

/// <summary>
/// Helper factory for constructing <see cref="Option{T}"/> instances with concise syntax.
/// </summary>
public static class Option
{
    public static Option<T> Some<T>(T value)
        => Option<T>.Some(value);

    public static Option<T> None<T>()
        => Option<T>.None;

    public static Option<T> FromNullable<T>(T? value)
        where T : class
        => value is null ? Option<T>.None : Option<T>.Some(value);

    public static Option<T> FromNullable<T>(T? value)
        where T : struct
        => value.HasValue ? Option<T>.Some(value.Value) : Option<T>.None;
}
