namespace Csharp14FeatureSamples.Features;

/// <summary>
/// Contract implemented by each feature sample so the console host can execute them uniformly.
/// </summary>
public interface IFeatureDemo
{
    /// <summary>
    /// Short, human readable name of the feature being demonstrated.
    /// </summary>
    string Title { get; }

    /// <summary>
    /// Runs the sample code and writes explanatory text to the console.
    /// </summary>
    void Run();
}
