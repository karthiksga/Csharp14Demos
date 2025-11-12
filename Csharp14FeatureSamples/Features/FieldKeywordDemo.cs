using System;

namespace Csharp14FeatureSamples.Features;

/// <summary>
/// Showcases the <c>field</c> contextual keyword that lets you reference the compiler generated backing field
/// directly from a property accessor without declaring your own private field.
/// </summary>
public sealed class FieldKeywordDemo : IFeatureDemo
{
    public string Title => "field-backed properties";

    public void Run()
    {
        var options = new ApplicationOptions
        {
            Name = "C# 14 Sample App",
            // The setter throws if we pass null, and the keyword redirects the assignment
            // to the compiler generated storage without us having to write plumbing code.
            Description = "Shows how the 'field' keyword reduces boilerplate."
        };

        Console.WriteLine($"Options.Name => {options.Name}");
        Console.WriteLine($"Options.Description => {options.Description}");

        try
        {
            options.Description = null!;
        }
        catch (ArgumentNullException ex)
        {
            Console.WriteLine($"Attempting to assign null triggers: {ex.ParamName}");
        }
    }

    private sealed class ApplicationOptions
    {
        // The getter stays auto-implemented, but the setter uses the contextual keyword.
        public string Name { get; init; } = string.Empty;

        public string Description
        {
            get;
            set => field = value ?? throw new ArgumentNullException(nameof(value));
        } = "Description not set.";
    }
}
