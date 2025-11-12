using System;

namespace Csharp14FeatureSamples.Features;

/// <summary>
/// Illustrates that simple lambdas can include parameter modifiers such as <c>out</c> and <c>scoped</c>
/// without requiring explicit parameter types.
/// </summary>
public sealed class SimpleLambdaModifiersDemo : IFeatureDemo
{
    public string Title => "Lambda parameter modifiers";

    public void Run()
    {
        // Prior to C# 14 we had to spell out parameter types when using 'out', but the compiler can now
        // infer them from the delegate signature while still respecting the modifier.
        TryParse<int> parseInt = (text, out result) => int.TryParse(text, out result);

        if (parseInt("42", out var parsed))
        {
            Console.WriteLine($"Parsed value using inferred lambda parameters => {parsed}");
        }

        // The scoped modifier keeps the span from escaping, mirroring the rules when the parameter is declared normally.
        SpanAnalyzer hasDigit = (scoped text) =>
        {
            foreach (var ch in text)
            {
                if (char.IsDigit(ch))
                {
                    return true;
                }
            }

            return false;
        };

        const string withDigits = "Version 14";
        const string withoutDigits = "Preview";

        Console.WriteLine($"\"{withDigits}\" contains a digit => {hasDigit(withDigits)}");
        Console.WriteLine($"\"{withoutDigits}\" contains a digit => {hasDigit(withoutDigits)}");
    }

    private delegate bool TryParse<T>(string text, out T result);

    private delegate bool SpanAnalyzer(scoped ReadOnlySpan<char> text);
}
