using Microsoft.Extensions.Logging;
using Notrelix.Platform.Messaging.Contracts;
using Notrelix.Platform.Messaging.Runtime;
using Notrelix.Platform.Messaging.Runtime.Governance;

namespace Notrelix.Platform.Messaging.Contracts.Evolution;

public sealed class DeprecationPolicy : IGovernancePolicy
{
    public string Name => "Deprecation";

    private readonly IEventDescriptorProvider _descriptorProvider;
    private readonly ILogger<DeprecationPolicy>? _logger;

    public DeprecationPolicy(
        IEventDescriptorProvider descriptorProvider,
        ILogger<DeprecationPolicy>? logger = null)
    {
        _descriptorProvider = descriptorProvider;
        _logger = logger;
    }

    public Task<GovernanceResult> EvaluateAsync(
        EventEnvelope envelope,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var descriptor = _descriptorProvider.Get(envelope.EventName, envelope.EventVersion);

            if (!descriptor.Deprecated)
                return Task.FromResult(GovernanceResult.Allow(Name));

            if (descriptor.DeprecationDate is not null && descriptor.DeprecationDate > DateOnly.FromDateTime(DateTime.UtcNow))
            {
                _logger?.LogWarning("Event {EventName} v{Version} is deprecated, grace period until {DeprecationDate}",
                    envelope.EventName, envelope.EventVersion, descriptor.DeprecationDate);

                return Task.FromResult(GovernanceResult.Warn(Name,
                    $"Event {envelope.EventName} v{envelope.EventVersion} is deprecated. " +
                    $"Replacement: {descriptor.ReplacementEventName ?? "none"}. " +
                    $"Grace period until {descriptor.DeprecationDate}."));
            }

            _logger?.LogWarning("Event {EventName} v{Version} is deprecated and past grace period — blocking",
                envelope.EventName, envelope.EventVersion);

            return Task.FromResult(GovernanceResult.Block(Name,
                $"Event {envelope.EventName} v{envelope.EventVersion} is deprecated and past grace period. " +
                $"Use {descriptor.ReplacementEventName ?? "the replacement event"} instead."));
        }
        catch (UnknownEventDescriptorException)
        {
            return Task.FromResult(GovernanceResult.Allow(Name));
        }
    }
}
