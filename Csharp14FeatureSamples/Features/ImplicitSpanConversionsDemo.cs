using System;
using System.Collections.Generic;

namespace Csharp14FeatureSamples.Features;

/// <summary>
/// Highlights the new "first-class" treatment of <see cref="Span{T}"/> and <see cref="ReadOnlySpan{T}"/>
/// in C# 14, which enables additional implicit conversions and better overload resolution behavior.
/// </summary>
public sealed class ImplicitSpanConversionsDemo : IFeatureDemo
{
    public string Title => "Implicit span conversions";

    public void Run()
    {
        int[] numbers = [1, 2, 3, 4];

        // Arrays now convert implicitly to Span<T>, letting us mutate the underlying storage
        // without calling AsSpan explicitly.
        Span<int> writable = numbers;
        writable[0] = 42;

        // Span<T> automatically converts to ReadOnlySpan<T> when a read-only view is required.
        ReadOnlySpan<int> readOnlyView = writable;

        // Strings transparently project to ReadOnlySpan<char>, which keeps allocations down when
        // performing slice-intensive operations.
        ReadOnlySpan<char> text = "C# 14 improves span ergonomics";

        Console.WriteLine($"Modified array via Span<int>: [{string.Join(", ", numbers)}]");
        Console.WriteLine($"numbers.StartsWith(42) => {numbers.StartsWith(42)}");
        Console.WriteLine($"Length of ReadOnlySpan<int> view => {readOnlyView.Length}");
        Console.WriteLine($"First word extracted with Slice => \"{text[..3].ToString()}\"");
    }
}

/// <summary>
/// Helper extensions used by the span sample to keep the focus on the new conversions.
/// </summary>
public static class SpanDemoExtensions
{
    public static bool StartsWith<T>(this ReadOnlySpan<T> span, T value) where T : IEquatable<T>
        => !span.IsEmpty && EqualityComparer<T>.Default.Equals(span[0], value);
}
