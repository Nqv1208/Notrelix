using Notrelix.Platform.Messaging.Contracts;

namespace Notrelix.Platform.Messaging.Runtime;

public enum CompatibilityLevel
{
    None,
    Backward,
    Forward,
    Full,
}

public sealed record CompatibilityResult
{
    public bool Compatible { get; init; }
    public CompatibilityLevel Level { get; init; }
    public string? Message { get; init; }

    public static CompatibilityResult Ok(CompatibilityLevel level) =>
        new() { Compatible = true, Level = level };

    public static CompatibilityResult Fail(string message) =>
        new() { Compatible = false, Level = CompatibilityLevel.None, Message = message };
}

public interface ICompatibilityEvaluator
{
    CompatibilityResult Evaluate(EventDescriptor producer, int consumerVersion);
}
