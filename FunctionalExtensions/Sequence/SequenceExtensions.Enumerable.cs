using System;
using System.Collections.Generic;
using System.Linq;

namespace FunctionalExtensions;

public static partial class SequenceExtensions
{
    // Instance-style extensions: all members in this block can be invoked as instance members on IEnumerable<TSource>.
    extension<TSource>(IEnumerable<TSource> source)
    {
        /// <summary>
        /// Property-like syntax that checks whether the sequence is empty. Because the compiler creates the forwarding
        /// logic, the property appears as though it were declared on IEnumerable{TSource}.
        /// </summary>
        public bool IsEmpty => !source.Any();

        /// <summary>
        /// Simple filtering extension method implemented without relying on the existing LINQ Where extension.
        /// </summary>
        public IEnumerable<TSource> Filter(Func<TSource, bool> predicate)
        {
            foreach (var item in source)
            {
                if (predicate(item))
                {
                    yield return item;
                }
            }
        }

        /// <summary>
        /// Returns the first element as an <see cref="Option{TSource}"/>, avoiding exceptions when the sequence is empty.
        /// </summary>
        public Option<TSource> FirstOption()
        {
            foreach (var item in source)
            {
                return Option<TSource>.Some(item);
            }

            return Option<TSource>.None;
        }

        /// <summary>
        /// Returns the first element matching <paramref name="predicate"/> as an <see cref="Option{TSource}"/>.
        /// </summary>
        public Option<TSource> FirstOption(Func<TSource, bool> predicate)
        {
            foreach (var item in source)
            {
                if (predicate(item))
                {
                    return Option<TSource>.Some(item);
                }
            }

            return Option<TSource>.None;
        }
    }

    // Static-style extensions: these become visible as if they were static members on IEnumerable<TSource>.
    extension<TSource>(IEnumerable<TSource>)
    {
        /// <summary>
        /// Static property exposed on IEnumerable{TSource} that returns an empty sequence.
        /// </summary>
        public static IEnumerable<TSource> Identity => Enumerable.Empty<TSource>();

        /// <summary>
        /// Static helper that combines two sequences without mutating either input.
        /// </summary>
        public static IEnumerable<TSource> Combine(IEnumerable<TSource> first, IEnumerable<TSource> second)
        {
            foreach (var item in first)
            {
                yield return item;
            }

            foreach (var item in second)
            {
                yield return item;
            }
        }

        /// <summary>
        /// Static operator that enables piping syntax such as <c>sequenceA | sequenceB</c>.
        /// </summary>
        public static IEnumerable<TSource> operator |(IEnumerable<TSource> left, IEnumerable<TSource> right)
            => Combine(left, right);

        /// <summary>
        /// Produces the set union of two sequences using the default equality comparer.
        /// </summary>
        public static IEnumerable<TSource> operator +(IEnumerable<TSource> left, IEnumerable<TSource> right)
            => left.Union(right);

        /// <summary>
        /// Appends a single value to the end of the sequence.
        /// </summary>
        public static IEnumerable<TSource> operator +(IEnumerable<TSource> source, TSource value)
            => source.Append(value);

        /// <summary>
        /// Prepends a single value to the beginning of the sequence.
        /// </summary>
        public static IEnumerable<TSource> operator +(TSource value, IEnumerable<TSource> source)
            => Enumerable.Repeat(value, 1).Concat(source);

        /// <summary>
        /// Produces the set difference between two sequences using the default equality comparer.
        /// </summary>
        public static IEnumerable<TSource> operator -(IEnumerable<TSource> left, IEnumerable<TSource> right)
            => left.Except(right);

        /// <summary>
        /// Produces the set intersection between two sequences using the default equality comparer.
        /// </summary>
        public static IEnumerable<TSource> operator &(IEnumerable<TSource> left, IEnumerable<TSource> right)
            => left.Intersect(right);

        /// <summary>
        /// Produces the symmetric difference between two sequences using the default equality comparer.
        /// </summary>
        public static IEnumerable<TSource> operator ^(IEnumerable<TSource> left, IEnumerable<TSource> right)
            => left.Except(right).Union(right.Except(left));

        /// <summary>
        /// Reverses the sequence without mutating the original source.
        /// </summary>
        public static IEnumerable<TSource> operator ~(IEnumerable<TSource> source)
            => source.Reverse();

        /// <summary>
        /// Repeats the sequence the specified number of times.
        /// </summary>
        public static IEnumerable<TSource> operator *(IEnumerable<TSource> source, int repetitions)
        {
            if (repetitions < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(repetitions));
            }

            return RepeatIterator(source, repetitions);
        }

        private static IEnumerable<TSource> RepeatIterator(IEnumerable<TSource> source, int repetitions)
        {
            var materialized = source as IReadOnlyCollection<TSource> ?? source.ToList();
            for (var i = 0; i < repetitions; i++)
            {
                foreach (var item in materialized)
                {
                    yield return item;
                }
            }
        }

        /// <summary>
        /// Materializes the sequence into a <see cref="List{T}"/> using unary <c>!</c> for brevity.
        /// Example: <c>var list = !mySequence;</c>
        /// </summary>
        public static List<TSource> operator !(IEnumerable<TSource> source)
            => source.ToList();

        /// <summary>
        /// Joins the sequence using the provided separator via bitwise-or with a string.
        /// Example: <c>var csv = numbers | ", ";</c>
        /// </summary>
        public static string operator |(IEnumerable<TSource> source, string separator)
            => string.Join(separator, source);

        /// <summary>
        /// Splits the sequence into chunks of <paramref name="size"/> via division syntax.
        /// Example: <c>var chunks = numbers / 3;</c>
        /// </summary>
        public static IEnumerable<IReadOnlyList<TSource>> operator /(IEnumerable<TSource> source, int size)
        {
            if (size <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(size));
            }

            return source.Chunk(size).Select(static chunk => (IReadOnlyList<TSource>)chunk.ToArray());
        }

        /// <summary>
        /// Returns the trailing <paramref name="count"/> elements using the modulus operator.
        /// Example: <c>var tail = numbers % 2;</c>
        /// </summary>
        public static IEnumerable<TSource> operator %(IEnumerable<TSource> source, int count)
        {
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            return source.TakeLast(count);
        }

        /// <summary>
        /// Takes the first <paramref name="count"/> elements using left shift syntax.
        /// Example: <c>var firstThree = numbers << 3;</c>
        /// </summary>
        public static IEnumerable<TSource> operator <<(IEnumerable<TSource> source, int count)
        {
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            return source.Take(count);
        }

        /// <summary>
        /// Skips the first <paramref name="count"/> elements using right shift syntax.
        /// Example: <c>var tail = numbers >> 2;</c>
        /// </summary>
        public static IEnumerable<TSource> operator >>(IEnumerable<TSource> source, int count)
        {
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            return source.Skip(count);
        }
    }
}
