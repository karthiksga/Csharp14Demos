using System;

namespace FunctionalExtensions;

public static partial class SequenceExtensions
{
    // Haskell-inspired Option helpers.
    extension<T>(Option<T> option)
    {
        /// <summary>
        /// Indicates that the option carries a value.
        /// </summary>
        public bool IsSome => option.HasValue;

        /// <summary>
        /// Indicates that the option is empty.
        /// </summary>
        public bool IsNone => !option.HasValue;

        /// <summary>
        /// Returns the contained value or <paramref name="fallback"/> when empty.
        /// </summary>
        public T ValueOr(T fallback)
            => option.HasValue ? option.Value! : fallback;

        /// <summary>
        /// Returns the contained value or computes one from <paramref name="fallback"/> when empty.
        /// </summary>
        public T ValueOrElse(Func<T> fallback)
            => option.HasValue ? option.Value! : fallback();

        /// <summary>
        /// Functional map operation that lifts <paramref name="selector"/> into the option context.
        /// </summary>
        public Option<TResult> Map<TResult>(Func<T, TResult> selector)
            => option.HasValue ? Option<TResult>.Some(selector(option.Value!)) : Option<TResult>.None;

        /// <summary>
        /// Functional bind (flat-map) operation that chains optional computations.
        /// </summary>
        public Option<TResult> Bind<TResult>(Func<T, Option<TResult>> binder)
            => option.HasValue ? binder(option.Value!) : Option<TResult>.None;

        /// <summary>
        /// Applies an optional function to the current optional value (<c>&lt;*&gt;</c> in Haskell).
        /// </summary>
        public Option<TResult> Apply<TResult>(Option<Func<T, TResult>> applicative)
            => applicative.HasValue && option.HasValue
                ? Option<TResult>.Some(applicative.Value!(option.Value!))
                : Option<TResult>.None;

        /// <summary>
        /// Filters the option by <paramref name="predicate"/>, producing None when the predicate fails.
        /// </summary>
        public Option<T> Where(Func<T, bool> predicate)
            => option.HasValue && predicate(option.Value!) ? option : Option<T>.None;

        /// <summary>
        /// Returns the current option when populated, otherwise evaluates <paramref name="fallback"/>.
        /// </summary>
        public Option<T> OrElse(Func<Option<T>> fallback)
            => option.HasValue ? option : fallback();

        /// <summary>
        /// Custom LINQ support method that enables query expressions on Option.
        /// </summary>
        public Option<TResult> Select<TResult>(Func<T, TResult> selector)
            => option.Map(selector);

        /// <summary>
        /// Custom LINQ support method that enables query expressions on Option.
        /// </summary>
        public Option<TResult> SelectMany<TResult>(Func<T, Option<TResult>> binder)
            => option.Bind(binder);

        /// <summary>
        /// Custom LINQ support method that enables query expressions with projections.
        /// </summary>
        public Option<TResult> SelectMany<TIntermediate, TResult>(
            Func<T, Option<TIntermediate>> binder,
            Func<T, TIntermediate, TResult> projector)
        {
            if (!option.HasValue)
            {
                return Option<TResult>.None;
            }

            var intermediate = binder(option.Value!);
            return intermediate.HasValue
                ? Option<TResult>.Some(projector(option.Value!, intermediate.Value!))
                : Option<TResult>.None;
        }

        /// <summary>
        /// Alternative choice operator (<c>&lt;|&gt;</c> in Haskell).
        /// </summary>
        public static Option<T> operator |(Option<T> first, Option<T> second)
            => first.HasValue ? first : second;

        /// <summary>
        /// Equality-friendly match helper that pipes an option into pattern-style handlers.
        /// </summary>
        public TResult Match<TResult>(Func<T, TResult> whenSome, Func<TResult> whenNone)
            => option.HasValue ? whenSome(option.Value!) : whenNone();

        /// <summary>
        /// Converts the option into a <see cref="Result{T}"/>, using <paramref name="error"/> when empty.
        /// </summary>
        public Result<T> ToResult(string error)
            => option.HasValue ? Result<T>.Ok(option.Value!) : Result<T>.Fail(error);

        /// <summary>
        /// Converts the option into a lazy IO action that throws when the option is empty.
        /// </summary>
        public IO<T> ToIO(Func<string>? errorFactory = null)
            => IO.From(() =>
            {
                if (option.HasValue)
                {
                    return option.Value!;
                }

                throw new InvalidOperationException(errorFactory?.Invoke() ?? "Option had no value.");
            });

        /// <summary>
        /// Converts the option into a <see cref="Try{T}"/>, using <paramref name="errorFactory"/> to build the exception when empty.
        /// </summary>
        public Try<T> ToTry(Func<string>? errorFactory = null)
            => option.HasValue
                ? Try<T>.Success(option.Value!)
                : Try<T>.Failure(new InvalidOperationException(errorFactory?.Invoke() ?? "Option had no value."));

        /// <summary>
        /// Converts the option into a <see cref="TaskResult{T}"/>, using <paramref name="error"/> when empty.
        /// </summary>
        public TaskResult<T> ToTaskResult(string error)
            => option.HasValue ? TaskResults.Return(option.Value!) : TaskResults.Fail<T>(error);

        /// <summary>
        /// Converts the option into a <see cref="TaskResult{T}"/>, using <paramref name="errorFactory"/> when empty.
        /// </summary>
        public TaskResult<T> ToTaskResult(Func<string> errorFactory)
            => option.HasValue ? TaskResults.Return(option.Value!) : TaskResults.Fail<T>(errorFactory());
    }

    // Applicative operators riding on Option<Func<...>> instances.
    extension<TArg, TResult>(Option<Func<TArg, TResult>> applicative)
    {
        /// <summary>
        /// Applies an optional function to an optional argument, mirroring Haskell's <c>&lt;*&gt;</c>.
        /// </summary>
        public static Option<TResult> operator *(Option<Func<TArg, TResult>> function, Option<TArg> value)
            => function.HasValue && value.HasValue
                ? Option<TResult>.Some(function.Value!(value.Value!))
                : Option<TResult>.None;
    }

    extension<TArg1, TArg2, TResult>(Option<Func<TArg1, Func<TArg2, TResult>>> applicative)
    {
        /// <summary>
        /// Supports multi-argument applicative style by partially applying the optional function.
        /// </summary>
        public static Option<Func<TArg2, TResult>> operator *(Option<Func<TArg1, Func<TArg2, TResult>>> function, Option<TArg1> value)
            => function.HasValue && value.HasValue
                ? Option<Func<TArg2, TResult>>.Some(function.Value!(value.Value!))
                : Option<Func<TArg2, TResult>>.None;
    }
}
