using System;
using System.Threading.Tasks;

namespace FunctionalExtensions;

public static partial class SequenceExtensions
{
    // ReaderTaskResult helpers.
    extension<TEnv, TValue>(ReaderTaskResult<TEnv, TValue> reader)
    {
        public Task<Result<TValue>> RunAsync(TEnv environment)
            => reader.Invoke(environment).Invoke();

        public ReaderTaskResult<TEnv, TResult> Map<TResult>(Func<TValue, TResult> selector)
            => new(env => reader.Invoke(env).Map(selector));

        public ReaderTaskResult<TEnv, TResult> Bind<TResult>(Func<TValue, ReaderTaskResult<TEnv, TResult>> binder)
            => new(env => reader.Invoke(env).Bind(value => binder(value).Invoke(env)));

        public ReaderTaskResult<TEnv, TResult> Select<TResult>(Func<TValue, TResult> selector)
            => reader.Map(selector);

        public ReaderTaskResult<TEnv, TResult> SelectMany<TResult>(Func<TValue, ReaderTaskResult<TEnv, TResult>> binder)
            => reader.Bind(binder);

        public ReaderTaskResult<TEnv, TResult> SelectMany<TIntermediate, TResult>(
            Func<TValue, ReaderTaskResult<TEnv, TIntermediate>> binder,
            Func<TValue, TIntermediate, TResult> projector)
            => new(env =>
                reader.Invoke(env).Bind(value =>
                    binder(value).Invoke(env).Map(intermediate => projector(value, intermediate))));

        public ReaderTaskResult<TEnv, TValue> Local(Func<TEnv, TEnv> transformer)
            => new(env => reader.Invoke(transformer(env)));

        public ReaderTaskResult<TEnv, TResult> Apply<TResult>(ReaderTaskResult<TEnv, Func<TValue, TResult>> applicative)
            => new(env =>
                applicative.Invoke(env).Bind(func =>
                    reader.Invoke(env).Map(value => func(value))));

        public TaskResult<TValue> ToTaskResult(TEnv environment)
            => reader.Invoke(environment);
    }

    extension<TEnv, TArg, TResult>(ReaderTaskResult<TEnv, Func<TArg, TResult>> applicative)
    {
        public static ReaderTaskResult<TEnv, TResult> operator *(ReaderTaskResult<TEnv, Func<TArg, TResult>> function, ReaderTaskResult<TEnv, TArg> value)
            => new(env =>
                function.Invoke(env).Bind(func =>
                    value.Invoke(env).Map(arg => func(arg))));
    }
}
