using System;

namespace FunctionalExtensions;

public static partial class SequenceExtensions
{
    // Generic value piping helpers to enable terse sink invocation such as value | WriteLine.
    extension<TValue>(TValue subject)
    {
        /// <summary>
        /// Writes the value to the console and returns it for potential fluent usage.
        /// </summary>
        public TValue WriteLine()
        {
            Console.WriteLine(subject);
            return subject;
        }

        /// <summary>
        /// Pipes the value into the provided <paramref name="sink"/> action before returning the original value.
        /// Enables patterns such as <c>var logged = $"Hello, world" | WriteLine;</c>.
        /// </summary>
        public static TValue operator |(TValue value, Action<TValue> sink)
        {
            sink(value);
            return value;
        }

        /// <summary>
        /// Pipes the value into the provided <paramref name="sink"/> action via <c>&gt;&gt;</c> before returning it.
        /// Enables patterns such as <c>$"Hello, world" &gt;&gt; WriteLine;</c>.
        /// </summary>
        public static TValue operator >>(TValue value, Action<TValue> sink)
            => value | sink;

        /// <summary>
        /// Converts the value into an <see cref="Option{TValue}"/>, treating null references as None.
        /// </summary>
        public Option<TValue> ToOption()
            => subject is null ? Option<TValue>.None : Option<TValue>.Some(subject);

        /// <summary>
        /// Converts the value into an <see cref="Option{TValue}"/> when <paramref name="predicate"/> passes.
        /// </summary>
        public Option<TValue> ToOption(Func<TValue, bool> predicate)
            => predicate(subject) ? Option<TValue>.Some(subject) : Option<TValue>.None;

        /// <summary>
        /// Lifts the value into a successful <see cref="Result{TValue}"/>.
        /// </summary>
        public Result<TValue> ToOk()
            => Result<TValue>.Ok(subject);

        /// <summary>
        /// Validates the value, producing either <c>Ok</c> or <c>Error</c>.
        /// </summary>
        public Result<TValue> Validate(Func<TValue, bool> predicate, Func<TValue, string> errorFactory)
            => predicate(subject) ? Result<TValue>.Ok(subject) : Result<TValue>.Fail(errorFactory(subject));

        /// <summary>
        /// Wraps the value in a pure <see cref="IO{T}"/>.
        /// </summary>
        public IO<TValue> ToIO()
            => IO.Return(subject);

        /// <summary>
        /// Lifts the value into a successful <see cref="TaskResult{T}"/>.
        /// </summary>
        public TaskResult<TValue> ToTaskResult()
            => TaskResults.Return(subject);
    }
}
