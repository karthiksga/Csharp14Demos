using System;

namespace FunctionalExtensions;

public static partial class SequenceExtensions
{
    // Continuation monad helpers.
    extension<TOutput, TValue>(Cont<TOutput, TValue> continuationMonad)
    {
        public TOutput Run(Func<TValue, TOutput> continuation)
            => continuationMonad.Invoke(continuation);

        public Cont<TOutput, TResult> Map<TResult>(Func<TValue, TResult> selector)
            => new(k => continuationMonad.Invoke(value => k(selector(value))));

        public Cont<TOutput, TResult> Bind<TResult>(Func<TValue, Cont<TOutput, TResult>> binder)
            => new(k => continuationMonad.Invoke(value => binder(value).Run(k)));

        public Cont<TOutput, TResult> Select<TResult>(Func<TValue, TResult> selector)
            => continuationMonad.Map(selector);

        public Cont<TOutput, TResult> SelectMany<TResult>(Func<TValue, Cont<TOutput, TResult>> binder)
            => continuationMonad.Bind(binder);

        public Cont<TOutput, TResult> SelectMany<TIntermediate, TResult>(
            Func<TValue, Cont<TOutput, TIntermediate>> binder,
            Func<TValue, TIntermediate, TResult> projector)
            => new(k => continuationMonad.Invoke(value =>
                binder(value).Run(intermediate => k(projector(value, intermediate)))));

        public Cont<TOutput, TResult> Apply<TResult>(Cont<TOutput, Func<TValue, TResult>> applicative)
            => new(k => applicative.Run(func => continuationMonad.Run(value => k(func(value)))));

        public Cont<TOutput, TResult> Then<TResult>(Cont<TOutput, TResult> next)
            => new(k => continuationMonad.Run(_ => next.Run(k)));

        public IO<TOutput> ToIO(Func<TValue, TOutput> finalContinuation)
            => IO.From(() => continuationMonad.Run(finalContinuation));

        public Result<TOutput> ToResult(Func<TValue, TOutput> finalContinuation)
        {
            try
            {
                return Result<TOutput>.Ok(continuationMonad.Run(finalContinuation));
            }
            catch (Exception ex)
            {
                return Result<TOutput>.Fail(ex.Message);
            }
        }
    }

    extension<TOutput, TArg, TResult>(Cont<TOutput, Func<TArg, TResult>> applicative)
    {
        public static Cont<TOutput, TResult> operator *(Cont<TOutput, Func<TArg, TResult>> function, Cont<TOutput, TArg> value)
            => new(k => function.Run(func => value.Run(arg => k(func(arg)))));
    }
}
