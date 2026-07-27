using Notrelix.Platform.Messaging.Contracts;

namespace Notrelix.Platform.Messaging.Runtime;

public sealed class FullCompatibilityEvaluator : ICompatibilityEvaluator
{
    public CompatibilityResult Evaluate(EventDescriptor producer, int consumerVersion)
    {
        return CompatibilityResult.Ok(CompatibilityLevel.Full);
    }
}
