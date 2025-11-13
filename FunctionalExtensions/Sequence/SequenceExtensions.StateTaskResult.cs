using System;
using System.Threading.Tasks;

namespace FunctionalExtensions;

public static partial class SequenceExtensions
{
    // StateTaskResult helpers.
    extension<TState, TValue>(StateTaskResult<TState, TValue> stateMonad)
    {
        public Task<Result<(TValue Value, TState State)>> RunAsync(TState initialState)
            => stateMonad.Invoke(initialState).Invoke();

        public StateTaskResult<TState, TResult> Map<TResult>(Func<TValue, TResult> selector)
            => new(state => stateMonad.Invoke(state).Map(tuple => (selector(tuple.Value), tuple.State)));

        public StateTaskResult<TState, TResult> Bind<TResult>(Func<TValue, StateTaskResult<TState, TResult>> binder)
            => new(state =>
                stateMonad.Invoke(state).Bind(tuple => binder(tuple.Value).Invoke(tuple.State)));

        public StateTaskResult<TState, TResult> Select<TResult>(Func<TValue, TResult> selector)
            => stateMonad.Map(selector);

        public StateTaskResult<TState, TResult> SelectMany<TResult>(Func<TValue, StateTaskResult<TState, TResult>> binder)
            => stateMonad.Bind(binder);

        public StateTaskResult<TState, TResult> SelectMany<TIntermediate, TResult>(
            Func<TValue, StateTaskResult<TState, TIntermediate>> binder,
            Func<TValue, TIntermediate, TResult> projector)
            => new(state =>
                stateMonad.Invoke(state).Bind(tuple =>
                    binder(tuple.Value).Invoke(tuple.State).Map(inner =>
                        (projector(tuple.Value, inner.Value), inner.State))));

        public StateTaskResult<TState, TResult> Apply<TResult>(StateTaskResult<TState, Func<TValue, TResult>> applicative)
            => new(state =>
                applicative.Invoke(state).Bind(funcTuple =>
                    stateMonad.Invoke(funcTuple.State).Map(valueTuple =>
                        (funcTuple.Value(valueTuple.Value), valueTuple.State))));

        public TaskResult<TValue> Evaluate(TState initialState)
            => stateMonad.Invoke(initialState).Map(tuple => tuple.Value);

        public TaskResult<TState> Execute(TState initialState)
            => stateMonad.Invoke(initialState).Map(tuple => tuple.State);

        public TaskResult<(TValue Value, TState State)> ToTaskResult(TState initialState)
            => stateMonad.Invoke(initialState);
    }

    extension<TState, TArg, TResult>(StateTaskResult<TState, Func<TArg, TResult>> applicative)
    {
        public static StateTaskResult<TState, TResult> operator *(StateTaskResult<TState, Func<TArg, TResult>> function, StateTaskResult<TState, TArg> value)
            => new(state =>
                function.Invoke(state).Bind(funcTuple =>
                    value.Invoke(funcTuple.State).Map(valueTuple =>
                        (funcTuple.Value(valueTuple.Value), valueTuple.State))));
    }
}
