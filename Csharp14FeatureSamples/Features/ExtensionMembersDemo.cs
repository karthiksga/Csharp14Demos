using System;
using System.Collections.Generic;
using System.Linq;

namespace Csharp14FeatureSamples.Features;

/// <summary>
/// Demonstrates the new <c>extension</c> member blocks that C# 14 adds, including
/// extension properties, methods that operate as instance members, and static members
/// that appear to hang directly off the extended type.
/// </summary>
public sealed class ExtensionMembersDemo : IFeatureDemo
{
    public string Title => "Extension members";

    public void Run()
    {
        // Instance extension members are invoked as if they were declared on the type itself.
        var numbers = new List<int> { 1, 2, 3, 4 };
        Console.WriteLine($"numbers.IsEmpty => {numbers.IsEmpty}");

        var odds = numbers.Filter(n => n % 2 == 1).ToList();
        Console.WriteLine($"numbers.Filter(n => n % 2 == 1) => [{string.Join(", ", odds)}]");

        // Static extension members surface as if they were native static APIs on IEnumerable<T>.
        // The call below accesses the synthetic Identity property produced by our extension block.
        var explicitIdentity = IEnumerable<int>.Identity;
        Console.WriteLine($"IEnumerable<int>.Identity.Any() => {explicitIdentity.Any()}");

        // Static operator extensions allow natural composition syntax on the owning type.
        var combined = numbers | new[] { 5, 6 };
        Console.WriteLine($"numbers | new[] {{ 5, 6 }} => [{string.Join(", ", combined)}]");
    }
}

/// <summary>
/// Central location for the extension blocks that light up the syntax used in <see cref="ExtensionMembersDemo"/>.
/// </summary>
public static class SequenceExtensions
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
    }
}
