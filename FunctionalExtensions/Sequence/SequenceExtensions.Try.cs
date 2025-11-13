using System;

namespace FunctionalExtensions;

public static partial class SequenceExtensions
{
    // Try monad helpers.
    extension<T>(Try<T> attempt)
    {
        public bool IsSuccess => attempt.IsSuccess;
        public bool IsFailure => !attempt.IsSuccess;
        public Exception? Exception => attempt.Exception;

        public T GetOrThrow()
            => attempt.GetOrThrow();

        public Try<TResult> Map<TResult>(Func<T, TResult> selector)
            => attempt.IsSuccess
                ? Try<TResult>.Success(selector(attempt.Value!))
                : Try<TResult>.Failure(attempt.Exception ?? new InvalidOperationException("Unknown error"));

        public Try<TResult> Bind<TResult>(Func<T, Try<TResult>> binder)
            => attempt.IsSuccess
                ? binder(attempt.Value!)
                : Try<TResult>.Failure(attempt.Exception ?? new InvalidOperationException("Unknown error"));

        public Try<T> Recover(Func<Exception?, T> recover)
            => attempt.IsSuccess ? attempt : Try<T>.Success(recover(attempt.Exception));

        public Try<T> RecoverWith(Func<Exception?, Try<T>> recover)
            => attempt.IsSuccess ? attempt : recover(attempt.Exception);

        public TResult Match<TResult>(Func<T, TResult> onSuccess, Func<Exception?, TResult> onFailure)
            => attempt.IsSuccess ? onSuccess(attempt.Value!) : onFailure(attempt.Exception);

        public Result<T> ToResult()
            => attempt.IsSuccess
                ? Result<T>.Ok(attempt.Value!)
                : Result<T>.Fail(attempt.Exception?.Message ?? "Unknown error");

        public Option<T> ToOption()
            => attempt.IsSuccess ? Option<T>.Some(attempt.Value!) : Option<T>.None;

        public IO<T> ToIO()
            => IO.From(attempt.GetOrThrow);

        public TaskResult<T> ToTaskResult()
            => TaskResults.FromResult(attempt.ToResult());

        public Try<TResult> Select<TResult>(Func<T, TResult> selector)
            => attempt.Map(selector);

        public Try<TResult> SelectMany<TResult>(Func<T, Try<TResult>> binder)
            => attempt.Bind(binder);

        public Try<TResult> SelectMany<TIntermediate, TResult>(
            Func<T, Try<TIntermediate>> binder,
            Func<T, TIntermediate, TResult> projector)
        {
            if (!attempt.IsSuccess)
            {
                return Try<TResult>.Failure(attempt.Exception ?? new InvalidOperationException("Unknown error"));
            }

            var intermediate = binder(attempt.Value!);
            return intermediate.IsSuccess
                ? Try<TResult>.Success(projector(attempt.Value!, intermediate.Value!))
                : Try<TResult>.Failure(intermediate.Exception ?? new InvalidOperationException("Unknown error"));
        }

        public Try<TResult> Apply<TResult>(Try<Func<T, TResult>> applicative)
            => attempt.IsSuccess && applicative.IsSuccess
                ? Try<TResult>.Success(applicative.Value!(attempt.Value!))
                : Try<TResult>.Failure(applicative.Exception ?? attempt.Exception ?? new InvalidOperationException("Unknown error"));

        public static Try<T> operator |(Try<T> first, Try<T> second)
            => first.IsSuccess ? first : second;
    }

    extension<TArg, TResult>(Try<Func<TArg, TResult>> applicative)
    {
        public static Try<TResult> operator *(Try<Func<TArg, TResult>> function, Try<TArg> value)
            => function.IsSuccess && value.IsSuccess
                ? Try<TResult>.Success(function.Value!(value.Value!))
                : Try<TResult>.Failure(function.Exception ?? value.Exception ?? new InvalidOperationException("Unknown error"));
    }

    extension<TArg1, TArg2, TResult>(Try<Func<TArg1, Func<TArg2, TResult>>> applicative)
    {
        public static Try<Func<TArg2, TResult>> operator *(Try<Func<TArg1, Func<TArg2, TResult>>> function, Try<TArg1> value)
            => function.IsSuccess && value.IsSuccess
                ? Try<Func<TArg2, TResult>>.Success(function.Value!(value.Value!))
                : Try<Func<TArg2, TResult>>.Failure(function.Exception ?? value.Exception ?? new InvalidOperationException("Unknown error"));
    }
}
