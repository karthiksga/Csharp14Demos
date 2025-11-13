using System;
using System.Collections.Generic;
using System.Linq;

namespace FunctionalExtensions;

/// <summary>
/// Represents a reusable transformation that maps an <see cref="IEnumerable{T}"/> of <typeparamref name="TSource"/> into
/// an <see cref="IEnumerable{T}"/> of <typeparamref name="TResult"/>.
/// </summary>
public readonly record struct SequencePipe<TSource, TResult>(Func<IEnumerable<TSource>, IEnumerable<TResult>> Apply)
{
    public IEnumerable<TResult> Invoke(IEnumerable<TSource> source) => Apply(source);

    public SequencePipe<TSource, TNext> Then<TNext>(SequencePipe<TResult, TNext> next)
    {
        var apply = Apply;
        var nextApply = next.Apply;
        return new(source => nextApply(apply(source)));
    }

    public SequenceTerminal<TSource, TResultFinal> Then<TResultFinal>(SequenceTerminal<TResult, TResultFinal> terminal)
    {
        var apply = Apply;
        var terminalApply = terminal.Apply;
        return new(source => terminalApply(apply(source)));
    }

    public static IEnumerable<TResult> operator |(IEnumerable<TSource> source, SequencePipe<TSource, TResult> pipe)
        => pipe.Invoke(source);

    public static SequencePipe<TSource, TResult> operator +(SequencePipe<TSource, TResult> first, SequencePipe<TSource, TResult> second)
    {
        var firstApply = first.Apply;
        var secondApply = second.Apply;
        return new(source =>
        {
            var materializedSource = source is IReadOnlyList<TSource> readOnly ? readOnly : source.ToList();
            return firstApply(materializedSource).Concat(secondApply(materializedSource));
        });
    }

    public static SequencePipe<TSource, TResult> operator -(SequencePipe<TSource, TResult> first, SequencePipe<TSource, TResult> second)
    {
        var firstApply = first.Apply;
        var secondApply = second.Apply;
        return new(source =>
        {
            var materializedSource = source is IReadOnlyList<TSource> readOnly ? readOnly : source.ToList();
            return firstApply(materializedSource).Except(secondApply(materializedSource));
        });
    }

    public static SequencePipe<TSource, TResult> operator &(SequencePipe<TSource, TResult> first, SequencePipe<TSource, TResult> second)
    {
        var firstApply = first.Apply;
        var secondApply = second.Apply;
        return new(source =>
        {
            var materializedSource = source is IReadOnlyList<TSource> readOnly ? readOnly : source.ToList();
            return firstApply(materializedSource).Intersect(secondApply(materializedSource));
        });
    }

    public static SequencePipe<TSource, TResult> operator ^(SequencePipe<TSource, TResult> first, SequencePipe<TSource, TResult> second)
    {
        var firstApply = first.Apply;
        var secondApply = second.Apply;
        return new(source =>
        {
            var materializedSource = source is IReadOnlyList<TSource> readOnly ? readOnly : source.ToList();
            var left = firstApply(materializedSource).ToList();
            var right = secondApply(materializedSource).ToList();
            return left.Except(right).Concat(right.Except(left));
        });
    }

    public static SequencePipe<TSource, TResult> operator ~(SequencePipe<TSource, TResult> pipe)
    {
        var apply = pipe.Apply;
        return new(source =>
        {
            var materializedSource = source is IReadOnlyList<TSource> readOnly ? readOnly : source.ToList();
            return apply(materializedSource).Reverse();
        });
    }

    public static SequencePipe<TSource, TResult> operator *(SequencePipe<TSource, TResult> pipe, int repetitions)
    {
        if (repetitions < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(repetitions));
        }

        var apply = pipe.Apply;
        return new(source =>
        {
            var materializedSource = source is IReadOnlyList<TSource> readOnly ? readOnly : source.ToList();
            return Enumerable.Range(0, repetitions).SelectMany(_ => apply(materializedSource));
        });
    }
}
