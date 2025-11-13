using System;

namespace FunctionalExtensions;

public static partial class SequenceExtensions
{
    // Result monad helpers.
    extension<T>(Result<T> result)
    {
        public bool IsOk => result.IsSuccess;
        public bool IsError => !result.IsSuccess;
        public string? Error => result.Error;

        public T ValueOr(T fallback)
            => result.IsSuccess ? result.Value! : fallback;

        public T ValueOrElse(Func<string?, T> fallback)
            => result.IsSuccess ? result.Value! : fallback(result.Error);

        public Result<TResult> Map<TResult>(Func<T, TResult> selector)
            => result.IsSuccess
                ? Result<TResult>.Ok(selector(result.Value!))
                : Result<TResult>.Fail(result.Error ?? "Unknown error");

        public Result<TResult> Bind<TResult>(Func<T, Result<TResult>> binder)
            => result.IsSuccess
                ? binder(result.Value!)
                : Result<TResult>.Fail(result.Error ?? "Unknown error");

        public Result<T> Tap(Action<T> inspector)
        {
            if (result.IsSuccess)
            {
                inspector(result.Value!);
            }

            return result;
        }

        public Result<T> Recover(Func<string?, T> recover)
            => result.IsSuccess ? result : Result<T>.Ok(recover(result.Error));

        public Result<T> RecoverWith(Func<string?, Result<T>> recover)
            => result.IsSuccess ? result : recover(result.Error);

        public Result<T> OrElse(Func<Result<T>> fallback)
            => result.IsSuccess ? result : fallback();

        public TResult Match<TResult>(Func<T, TResult> onOk, Func<string?, TResult> onError)
            => result.IsSuccess ? onOk(result.Value!) : onError(result.Error);

        public Result<TResult> Select<TResult>(Func<T, TResult> selector)
            => result.Map(selector);

        public Result<TResult> SelectMany<TResult>(Func<T, Result<TResult>> binder)
            => result.Bind(binder);

        public Result<TResult> SelectMany<TIntermediate, TResult>(
            Func<T, Result<TIntermediate>> binder,
            Func<T, TIntermediate, TResult> projector)
        {
            if (!result.IsSuccess)
            {
                return Result<TResult>.Fail(result.Error ?? "Unknown error");
            }

            var intermediate = binder(result.Value!);
            return intermediate.IsSuccess
                ? Result<TResult>.Ok(projector(result.Value!, intermediate.Value!))
                : Result<TResult>.Fail(intermediate.Error ?? "Unknown error");
        }

        public Result<TResult> Apply<TResult>(Result<Func<T, TResult>> applicative)
            => result.IsSuccess && applicative.IsSuccess
                ? Result<TResult>.Ok(applicative.Value!(result.Value!))
                : Result<TResult>.Fail(applicative.Error ?? result.Error ?? "Unknown error");

        public static Result<T> operator |(Result<T> first, Result<T> second)
            => first.IsSuccess ? first : second;

        /// <summary>
        /// Converts the result into an option, erasing the error information.
        /// </summary>
        public Option<T> ToOption()
            => result.IsSuccess ? Option<T>.Some(result.Value!) : Option<T>.None;

        /// <summary>
        /// Converts the result into a lazy IO action that throws when the result is an error.
        /// </summary>
        public IO<T> ToIO()
            => IO.From(() =>
            {
                if (result.IsSuccess)
                {
                    return result.Value!;
                }

                throw new InvalidOperationException(result.Error ?? "Unknown error");
            });

        /// <summary>
        /// Converts the result into a <see cref="Try{T}"/>, turning the error into an exception.
        /// </summary>
        public Try<T> ToTry()
            => result.IsSuccess
                ? Try<T>.Success(result.Value!)
                : Try<T>.Failure(new InvalidOperationException(result.Error ?? "Unknown error"));

        /// <summary>
        /// Wraps the result in a completed <see cref="TaskResult{T}"/>.
        /// </summary>
        public TaskResult<T> ToTaskResult()
            => TaskResults.FromResult(result);
    }

    extension<TArg, TResult>(Result<Func<TArg, TResult>> applicative)
    {
        public static Result<TResult> operator *(Result<Func<TArg, TResult>> function, Result<TArg> value)
            => function.IsSuccess && value.IsSuccess
                ? Result<TResult>.Ok(function.Value!(value.Value!))
                : Result<TResult>.Fail(function.Error ?? value.Error ?? "Unknown error");
    }

    extension<TArg1, TArg2, TResult>(Result<Func<TArg1, Func<TArg2, TResult>>> applicative)
    {
        public static Result<Func<TArg2, TResult>> operator *(Result<Func<TArg1, Func<TArg2, TResult>>> function, Result<TArg1> value)
            => function.IsSuccess && value.IsSuccess
                ? Result<Func<TArg2, TResult>>.Ok(function.Value!(value.Value!))
                : Result<Func<TArg2, TResult>>.Fail(function.Error ?? value.Error ?? "Unknown error");
    }
}
