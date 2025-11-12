using System;
using System.Collections.Generic;

namespace Csharp14FeatureSamples.Features;

/// <summary>
/// Demonstrates that <c>nameof</c> accepts open generic types in C# 14, which helps with diagnostics
/// and source generation scenarios where the generic arity is meaningful.
/// </summary>
public sealed class NameOfUnboundGenericDemo : IFeatureDemo
{
    public string Title => "nameof with unbound generics";

    public void Run()
    {
        Console.WriteLine($"nameof(List<>) => {nameof(List<>)}");
        Console.WriteLine($"nameof(Dictionary<,>) => {nameof(Dictionary<,>)}");

        // The feature is handy when constructing error messages or attribute arguments that need to
        // describe shape instead of a concrete closed generic.
        var expectedShape = nameof(IComparer<>);
        Console.WriteLine($"We can describe required comparator shape with nameof: {expectedShape}");
    }
}
