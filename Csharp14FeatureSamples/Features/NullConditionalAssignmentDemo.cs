using System;
using System.Collections.Generic;

namespace Csharp14FeatureSamples.Features;

/// <summary>
/// Demonstrates that the null-conditional operators <c>?.</c> and <c>?[]</c> can now appear on the left-hand side of
/// assignments and compound assignments.
/// </summary>
public sealed class NullConditionalAssignmentDemo : IFeatureDemo
{
    public string Title => "Null-conditional assignment";

    public void Run()
    {
        var active = new Customer("Ada Lovelace");
        Customer? missing = null;

        // The right side executes only for non-null receivers; no explicit null-check is needed.
        active?.CurrentOrder = new Order("C# 14 Launch Pack", 199m);
        missing?.CurrentOrder = new Order("This never runs", 0m);

        // Compound assignments short-circuit in the same way.
        active?.LoyaltyPoints += 50;
        missing?.LoyaltyPoints += 10;

        var order = active?.CurrentOrder;
        var orderDescription = order?.Description ?? "<no order>";
        var orderPrice = order?.Price ?? 0m;

        Console.WriteLine($"Active customer order => {orderDescription} (${orderPrice})");
        Console.WriteLine($"Active customer points => {active?.LoyaltyPoints ?? 0}");
        Console.WriteLine($"Missing customer order remains null => {missing?.CurrentOrder is null}");

        // Null-conditional assignment works with indexers as well.
        var activePreferences = active?.Preferences;
        #pragma warning disable CS8602 // Analyzer may not yet understand null-conditional assignment on indexers.
        activePreferences?["theme"] = "dark";

        var missingPreferences = missing?.Preferences;
        missingPreferences?["theme"] = "dark";
        #pragma warning restore CS8602

        var theme = activePreferences?["theme"] ?? "<unset>";
        Console.WriteLine($"Active preferences[\"theme\"] => {theme}");
    }

    private sealed class Customer
    {
        public Customer(string name)
        {
            Name = name;
        }

        public string Name { get; }

        public Order? CurrentOrder { get; set; }

        public int LoyaltyPoints { get; set; }

        public CustomerPreferences Preferences { get; } = new();
    }

    private sealed class Order
    {
        public Order(string description, decimal price)
        {
            Description = description;
            Price = price;
        }

        public string Description { get; }

        public decimal Price { get; }
    }

    private sealed class CustomerPreferences
    {
        private readonly Dictionary<string, string> _settings = new();

        public string? this[string key]
        {
            get => _settings.TryGetValue(key, out var value) ? value : null;
            set => _settings[key] = value ?? string.Empty;
        }
    }
}
