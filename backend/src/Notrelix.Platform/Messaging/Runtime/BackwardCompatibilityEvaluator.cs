using Notrelix.Platform.Messaging.Contracts;

namespace Notrelix.Platform.Messaging.Runtime;

public sealed class BackwardCompatibilityEvaluator : ICompatibilityEvaluator
{
    public CompatibilityResult Evaluate(EventDescriptor producer, int consumerVersion)
    {
        if (consumerVersion < producer.Version)
            return CompatibilityResult.Fail(
                $"Consumer v{consumerVersion} is older than producer v{producer.Version} " +
                "and backward compatibility is not supported");

        return CompatibilityResult.Ok(CompatibilityLevel.Backward);
    }
}
