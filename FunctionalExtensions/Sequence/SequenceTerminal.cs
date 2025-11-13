using System;
using System.Collections.Generic;
using System.Linq;

namespace FunctionalExtensions;

/// <summary>
/// Represents a terminal operation that folds an <see cref="IEnumerable{T}"/> of <typeparamref name="TSource"/> into
/// a single <typeparamref name="TResult"/>.
/// </summary>
public readonly record struct SequenceTerminal<TSource, TResult>(Func<IEnumerable<TSource>, TResult> Apply)
{
    public TResult Invoke(IEnumerable<TSource> source) => Apply(source);

    public static TResult operator |(IEnumerable<TSource> source, SequenceTerminal<TSource, TResult> terminal)
        => terminal.Invoke(source);

    public SequenceTerminal<TSource, TResultNext> Select<TResultNext>(Func<TResult, TResultNext> projector)
    {
        var apply = Apply;
        return new(source => projector(apply(source)));
    }

    public static SequenceTerminal<TSource, (TResult First, TResult Second)> operator &(
        SequenceTerminal<TSource, TResult> first,
        SequenceTerminal<TSource, TResult> second)
    {
        var firstApply = first.Apply;
        var secondApply = second.Apply;
        return new(source =>
        {
            var materialized = source.ToList();
            return (firstApply(materialized), secondApply(materialized));
        });
    }
}
