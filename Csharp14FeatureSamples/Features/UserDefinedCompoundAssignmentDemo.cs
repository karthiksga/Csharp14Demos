using System;

namespace Csharp14FeatureSamples.Features;

/// <summary>
/// Shows how C# 14 lets types define their own compound assignment operators (for example <c>+=</c>)
/// so the operation can mutate the target in-place instead of relying on a separate binary operator.
/// </summary>
public sealed class UserDefinedCompoundAssignmentDemo : IFeatureDemo
{
    public string Title => "User-defined compound assignment";

    public void Run()
    {
        var budget = new ResourceBudget(capacity: 100);
        Console.WriteLine($"Initial remaining budget => {budget.Remaining}");

        budget += 25;
        budget += 30;

        Console.WriteLine($"After in-place += calls => Remaining: {budget.Remaining}, Consumed: {budget.Consumed}");

        try
        {
            budget += 60;
        }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine($"Exceeded budget triggers: {ex.Message}");
        }
    }
}

/// <summary>
/// Represents a pool of resources where allocations should reduce the remaining budget without re-allocating an object.
/// </summary>
public sealed class ResourceBudget
{
    public ResourceBudget(int capacity)
    {
        Remaining = capacity;
    }

    public int Remaining { get; private set; }

    public int Consumed { get; private set; }

    // The new syntax uses an instance 'void operator +=' to mutate state of the existing object.
    public void operator +=(int allocation)
    {
        if (allocation <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(allocation), "Allocation must be positive.");
        }

        if (allocation > Remaining)
        {
            throw new InvalidOperationException("Not enough budget available for the allocation.");
        }

        Remaining -= allocation;
        Consumed += allocation;
    }
}
