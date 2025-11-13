using System;
using System.Collections.Generic;

namespace FunctionalExtensions;

public static partial class SequenceExtensions
{
    // Writer monad helpers.
    extension<TValue, TLog>(Writer<TValue, TLog> writer)
    {
        public Writer<TResult, TLog> Map<TResult>(Func<TValue, TResult> selector)
            => new(selector(writer.Value), writer.Logs);

        public Writer<TResult, TLog> Bind<TResult>(Func<TValue, Writer<TResult, TLog>> binder)
        {
            var next = binder(writer.Value);
            return new(next.Value, CombineLogs(writer.Logs, next.Logs));
        }

        public Writer<TValue, TLog> AppendLog(TLog log)
            => new(writer.Value, CombineLogs(writer.Logs, new[] { log }));

        public Writer<TValue, TLog> AppendLogs(params TLog[] logs)
            => new(writer.Value, CombineLogs(writer.Logs, logs));

        public Writer<TValue, TLog> Tap(Action<TValue> inspector)
        {
            inspector(writer.Value);
            return writer;
        }

        public Writer<TValue, TLog> TapLogs(Action<IReadOnlyList<TLog>> inspector)
        {
            inspector(writer.Logs);
            return writer;
        }

        public Writer<TResult, TLog> Select<TResult>(Func<TValue, TResult> selector)
            => writer.Map(selector);

        public Writer<TResult, TLog> SelectMany<TResult>(Func<TValue, Writer<TResult, TLog>> binder)
            => writer.Bind(binder);

        public Writer<TResult, TLog> SelectMany<TIntermediate, TResult>(
            Func<TValue, Writer<TIntermediate, TLog>> binder,
            Func<TValue, TIntermediate, TResult> projector)
        {
            var next = binder(writer.Value);
            var projected = projector(writer.Value, next.Value);
            return new(projected, CombineLogs(writer.Logs, next.Logs));
        }

        public Writer<TResult, TLog> Apply<TResult>(Writer<Func<TValue, TResult>, TLog> applicative)
        {
            var value = applicative.Value(writer.Value);
            return new(value, CombineLogs(applicative.Logs, writer.Logs));
        }

        public IO<TValue> ToIO(Action<TLog>? sink = null)
            => IO.From(() =>
            {
                if (sink is not null)
                {
                    foreach (var log in writer.Logs)
                    {
                        sink(log);
                    }
                }

                return writer.Value;
            });
    }

    extension<TValue, TResult, TLog>(Writer<Func<TValue, TResult>, TLog> applicative)
    {
        public static Writer<TResult, TLog> operator *(Writer<Func<TValue, TResult>, TLog> function, Writer<TValue, TLog> value)
            => new(function.Value(value.Value), CombineLogs(function.Logs, value.Logs));
    }

    extension<TValue>(Writer<TValue, string> writer)
    {
        public string PrettyPrint()
            => $"{writer.Value} | logs: [{string.Join(", ", writer.Logs)}]";
    }

    private static IReadOnlyList<TLog> CombineLogs<TLog>(IReadOnlyList<TLog> first, IReadOnlyList<TLog> second)
    {
        if (first.Count == 0 && second.Count == 0)
        {
            return Array.Empty<TLog>();
        }

        if (first.Count == 0)
        {
            return second;
        }

        if (second.Count == 0)
        {
            return first;
        }

        var combined = new TLog[first.Count + second.Count];
        for (var i = 0; i < first.Count; i++)
        {
            combined[i] = first[i];
        }

        for (var i = 0; i < second.Count; i++)
        {
            combined[first.Count + i] = second[i];
        }

        return Array.AsReadOnly(combined);
    }
}
