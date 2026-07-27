using Notrelix.Platform.Messaging.Runtime;

namespace Notrelix.Platform.Messaging.Contracts.Evolution;

public sealed class VersionCompatibilityEvaluator : ICompatibilityEvaluator
{
    private readonly CompatibilityLevel _defaultLevel;

    public VersionCompatibilityEvaluator(CompatibilityLevel defaultLevel = CompatibilityLevel.Backward)
    {
        _defaultLevel = defaultLevel;
    }

    public CompatibilityResult Evaluate(EventDescriptor producer, int consumerVersion)
    {
        if (_defaultLevel == CompatibilityLevel.None)
            return CompatibilityResult.Ok(CompatibilityLevel.None);

        if (producer.Version == consumerVersion)
            return CompatibilityResult.Ok(CompatibilityLevel.Full);

        return _defaultLevel switch
        {
            CompatibilityLevel.Backward => EvaluateBackward(producer.Version, consumerVersion),
            CompatibilityLevel.Forward => EvaluateForward(producer.Version, consumerVersion),
            CompatibilityLevel.Full => CompatibilityResult.Ok(CompatibilityLevel.Full),
            _ => CompatibilityResult.Ok(CompatibilityLevel.None),
        };
    }

    private static CompatibilityResult EvaluateBackward(int producerVersion, int consumerVersion)
    {
        if (consumerVersion < producerVersion)
            return CompatibilityResult.Fail(
                $"Backward incompatible: consumer v{consumerVersion} cannot read producer v{producerVersion}");

        return CompatibilityResult.Ok(CompatibilityLevel.Backward);
    }

    private static CompatibilityResult EvaluateForward(int producerVersion, int consumerVersion)
    {
        if (consumerVersion > producerVersion)
            return CompatibilityResult.Fail(
                $"Forward incompatible: consumer v{consumerVersion} cannot read producer v{producerVersion}");

        return CompatibilityResult.Ok(CompatibilityLevel.Forward);
    }
}
