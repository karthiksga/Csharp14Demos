using System;

namespace Csharp14FeatureSamples.Features;

/// <summary>
/// Highlights that C# 14 extends the set of members that can be declared as <c>partial</c>,
/// notably constructors and events.
/// </summary>
public sealed class PartialMembersDemo : IFeatureDemo
{
    public string Title => "Partial constructors and events";

    public void Run()
    {
        var broadcaster = new StatusBroadcaster("Build pipeline");

        // Because the event is partial, the backing storage can live in a different declaration,
        // and the signature reads like a field-style event in this definition.
        broadcaster.StatusChanged += (_, message) =>
        {
            Console.WriteLine($"Subscriber received: {message}");
        };

        broadcaster.Broadcast("Compilation started");
        broadcaster.Broadcast("Compilation finished");
    }
}

// First declaration supplies the contract: we define that the constructor and event exist, but the bodies live elsewhere.
partial class StatusBroadcaster
{
    public partial StatusBroadcaster(string channel);

    public partial event EventHandler<string>? StatusChanged;
}

// Second declaration provides the executable details.
partial class StatusBroadcaster
{
    private readonly string _channel;
    private EventHandler<string>? _handlers;

    public partial StatusBroadcaster(string channel)
    {
        _channel = channel;
        // The partial event add accessor lives below, so this call routes through it.
        StatusChanged += (_, message) =>
        {
            Console.WriteLine($"[{_channel}] {message}");
        };
    }

    public partial event EventHandler<string>? StatusChanged
    {
        add => _handlers += value;
        remove => _handlers -= value;
    }

    public void Broadcast(string message)
    {
        _handlers?.Invoke(this, message);
    }
}
