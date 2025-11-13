using System;

namespace FunctionalExtensions;

public static partial class SequenceExtensions
{
    // State monad helpers.
    extension<TState, TValue>(State<TState, TValue> stateMonad)
    {
        public (TValue Value, TState State) RunState(TState initialState)
            => stateMonad.Invoke(initialState);

        public TValue Evaluate(TState initialState)
            => stateMonad.Invoke(initialState).Value;

        public TState Execute(TState initialState)
            => stateMonad.Invoke(initialState).State;

        public State<TState, TResult> Map<TResult>(Func<TValue, TResult> selector)
            => new(state =>
            {
                var (value, nextState) = stateMonad.Invoke(state);
                return (selector(value), nextState);
            });

        public State<TState, TResult> Bind<TResult>(Func<TValue, State<TState, TResult>> binder)
            => new(state =>
            {
                var (value, nextState) = stateMonad.Invoke(state);
                return binder(value).Invoke(nextState);
            });

        public State<TState, TResult> Select<TResult>(Func<TValue, TResult> selector)
            => stateMonad.Map(selector);

        public State<TState, TResult> SelectMany<TResult>(Func<TValue, State<TState, TResult>> binder)
            => stateMonad.Bind(binder);

        public State<TState, TResult> SelectMany<TIntermediate, TResult>(
            Func<TValue, State<TState, TIntermediate>> binder,
            Func<TValue, TIntermediate, TResult> projector)
            => new(state =>
            {
                var (value, firstState) = stateMonad.Invoke(state);
                var (intermediate, secondState) = binder(value).Invoke(firstState);
                return (projector(value, intermediate), secondState);
            });

        public State<TState, TResult> Apply<TResult>(State<TState, Func<TValue, TResult>> applicative)
            => new(state =>
            {
                var (func, firstState) = applicative.Invoke(state);
                var (value, secondState) = stateMonad.Invoke(firstState);
                return (func(value), secondState);
            });

        public IO<TValue> ToIO(TState initialState)
            => IO.From(() => stateMonad.Invoke(initialState).Value);

        public Result<TValue> ToResult(TState initialState)
        {
            try
            {
                return Result<TValue>.Ok(stateMonad.Invoke(initialState).Value);
            }
            catch (Exception ex)
            {
                return Result<TValue>.Fail(ex.Message);
            }
        }
    }

    extension<TState, TArg, TResult>(State<TState, Func<TArg, TResult>> applicative)
    {
        public static State<TState, TResult> operator *(State<TState, Func<TArg, TResult>> function, State<TState, TArg> value)
            => new(state =>
            {
                var (func, state1) = function.Invoke(state);
                var (arg, state2) = value.Invoke(state1);
                return (func(arg), state2);
            });
    }
}
