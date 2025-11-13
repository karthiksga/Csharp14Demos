using System;

namespace FunctionalExtensions;

public static partial class SequenceExtensions
{
    // Reader monad helpers.
    extension<TEnv, TValue>(Reader<TEnv, TValue> reader)
    {
        public TValue Run(TEnv environment)
            => reader.Invoke(environment);

        public Reader<TEnv, TResult> Map<TResult>(Func<TValue, TResult> selector)
            => new(env => selector(reader.Invoke(env)));

        public Reader<TEnv, TResult> Bind<TResult>(Func<TValue, Reader<TEnv, TResult>> binder)
            => new(env => binder(reader.Invoke(env)).Invoke(env));

        public Reader<TEnv, TValue> Local(Func<TEnv, TEnv> transformer)
            => new(env => reader.Invoke(transformer(env)));

        public Reader<TEnv, TResult> Select<TResult>(Func<TValue, TResult> selector)
            => reader.Map(selector);

        public Reader<TEnv, TResult> SelectMany<TResult>(Func<TValue, Reader<TEnv, TResult>> binder)
            => reader.Bind(binder);

        public Reader<TEnv, TResult> SelectMany<TIntermediate, TResult>(
            Func<TValue, Reader<TEnv, TIntermediate>> binder,
            Func<TValue, TIntermediate, TResult> projector)
            => new(env =>
            {
                var value = reader.Invoke(env);
                var intermediate = binder(value).Invoke(env);
                return projector(value, intermediate);
            });

        public Reader<TEnv, TResult> Apply<TResult>(Reader<TEnv, Func<TValue, TResult>> applicative)
            => new(env =>
            {
                var func = applicative.Invoke(env);
                var value = reader.Invoke(env);
                return func(value);
            });

        public Func<TEnv, TValue> ToFunc()
            => reader.Run;
    }

    extension<TEnv, TArg, TResult>(Reader<TEnv, Func<TArg, TResult>> applicative)
    {
        public static Reader<TEnv, TResult> operator *(Reader<TEnv, Func<TArg, TResult>> function, Reader<TEnv, TArg> value)
            => new(env =>
            {
                var func = function.Invoke(env);
                var arg = value.Invoke(env);
                return func(arg);
            });
    }
}
