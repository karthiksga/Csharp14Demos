using System;

namespace FunctionalExtensions;

public static partial class SequenceExtensions
{
    // IO monad helpers.
    extension<T>(IO<T> io)
    {
        public T Run() => io.Invoke();

        public IO<TResult> Map<TResult>(Func<T, TResult> selector)
            => new(() => selector(io.Invoke()));

        public IO<TResult> Bind<TResult>(Func<T, IO<TResult>> binder)
            => new(() =>
            {
                var value = io.Invoke();
                return binder(value).Invoke();
            });

        public IO<T> Tap(Action<T> inspector)
            => new(() =>
            {
                var value = io.Invoke();
                inspector(value);
                return value;
            });

        public IO<TResult> Apply<TResult>(IO<Func<T, TResult>> applicative)
            => new(() =>
            {
                var func = applicative.Invoke();
                var value = io.Invoke();
                return func(value);
            });

        public IO<TResult> Select<TResult>(Func<T, TResult> selector)
            => io.Map(selector);

        public IO<TResult> SelectMany<TResult>(Func<T, IO<TResult>> binder)
            => io.Bind(binder);

        public IO<TResult> SelectMany<TIntermediate, TResult>(
            Func<T, IO<TIntermediate>> binder,
            Func<T, TIntermediate, TResult> projector)
            => new(() =>
            {
                var value = io.Invoke();
                var intermediate = binder(value).Invoke();
                return projector(value, intermediate);
            });

        public IO<TResult> Then<TResult>(IO<TResult> next)
            => new(() =>
            {
                _ = io.Invoke();
                return next.Invoke();
            });

        public Result<T> ToResult()
        {
            try
            {
                return Result<T>.Ok(io.Invoke());
            }
            catch (Exception ex)
            {
                return Result<T>.Fail(ex.Message);
            }
        }

        public Option<T> ToOption()
        {
            try
            {
                var value = io.Invoke();
                return value is null ? Option<T>.None : Option<T>.Some(value);
            }
            catch
            {
                return Option<T>.None;
            }
        }

        public Try<T> ToTry()
            => Try.Run(io.Invoke);

        public TaskResult<T> ToTaskResult(Func<Exception, string>? errorFactory = null)
            => TaskResults.From(async () =>
            {
                try
                {
                    var value = io.Invoke();
                    return Result<T>.Ok(value);
                }
                catch (Exception ex)
                {
                    return Result<T>.Fail(errorFactory?.Invoke(ex) ?? ex.Message);
                }
            });
    }

    extension<TArg, TResult>(IO<Func<TArg, TResult>> applicative)
    {
        public static IO<TResult> operator *(IO<Func<TArg, TResult>> function, IO<TArg> value)
            => new(() =>
            {
                var func = function.Invoke();
                var arg = value.Invoke();
                return func(arg);
            });
    }

    extension<TArg1, TArg2, TResult>(IO<Func<TArg1, Func<TArg2, TResult>>> applicative)
    {
        public static IO<Func<TArg2, TResult>> operator *(IO<Func<TArg1, Func<TArg2, TResult>>> function, IO<TArg1> value)
            => new(() =>
            {
                var func = function.Invoke();
                var arg = value.Invoke();
                return func(arg);
            });
    }

    extension<T>(Func<T> effect)
    {
        public IO<T> ToIO()
            => IO.From(effect);

        public Result<T> ToResult()
            => Result.Try(effect);

        public Try<T> ToTry()
            => Try.Run(effect);
    }

    extension(Action action)
    {
        public IO<Unit> ToIO()
            => IO.From(action);

        public Result<Unit> ToResult()
            => Result.Try(() =>
            {
                action();
                return Unit.Value;
            });

        public Try<Unit> ToTry()
            => Try.Run(action);
    }
}
