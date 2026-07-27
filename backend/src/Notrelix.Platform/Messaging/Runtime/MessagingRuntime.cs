using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Notrelix.Platform.Messaging.Contracts;
using Notrelix.Platform.Messaging.Runtime.Governance;

namespace Notrelix.Platform.Messaging.Runtime;

public sealed class MessagingRuntime : IMessagingRuntime
{
    private static readonly ActivitySource ActivitySource = RuntimeActivitySource.Instance;

    private readonly IEventDescriptorProvider _descriptorProvider;
    private readonly EnvelopeBuilder _envelopeBuilder;
    private readonly ICanonicalizer _canonicalizer;
    private readonly IEventSerializer _serializer;
    private readonly SchemaValidationRule _schemaValidation;
    private readonly ICompatibilityEvaluator _compatibilityEvaluator;
    private readonly GovernanceEngine _governanceEngine;
    private readonly ILogger<MessagingRuntime>? _logger;

    public MessagingRuntime(
        IEventDescriptorProvider descriptorProvider,
        EnvelopeBuilder envelopeBuilder,
        ICanonicalizer canonicalizer,
        IEventSerializer serializer,
        SchemaValidationRule schemaValidation,
        ICompatibilityEvaluator compatibilityEvaluator,
        GovernanceEngine governanceEngine,
        ILogger<MessagingRuntime>? logger = null)
    {
        _descriptorProvider = descriptorProvider;
        _envelopeBuilder = envelopeBuilder;
        _canonicalizer = canonicalizer;
        _serializer = serializer;
        _schemaValidation = schemaValidation;
        _compatibilityEvaluator = compatibilityEvaluator;
        _governanceEngine = governanceEngine;
        _logger = logger;
    }

    public async Task<MessagingResult> PublishAsync(
        EventPublication publication,
        CancellationToken cancellationToken = default)
    {
        using var activity = ActivitySource.StartActivity("MessagingRuntime.Publish");
        activity?.SetTag("event.type", publication.Event.GetType().Name);

        EventDescriptor descriptor;
        using (var _ = ActivitySource.StartActivity("resolve_descriptor"))
        {
            descriptor = _descriptorProvider.Get(publication.Event.GetType());
            activity?.SetTag("event.name", descriptor.Name);
            activity?.SetTag("event.version", descriptor.Version);
            RuntimeMetrics.RecordStage(RuntimeStage.ResolveDescriptor);
        }

        EventEnvelope envelope;
        using (var _ = ActivitySource.StartActivity("build_envelope"))
        {
            envelope = _envelopeBuilder.Build(publication);
            RuntimeMetrics.RecordStage(RuntimeStage.BuildEnvelope);
        }

        byte[] rawData;
        using (var _ = ActivitySource.StartActivity("serialize"))
        {
            rawData = _serializer.Serialize(publication.Event, publication.Event.GetType()).ToArray();
            RuntimeMetrics.RecordStage(RuntimeStage.Serialize);
        }

        SchemaValidationResult schemaResult;
        using (var _ = ActivitySource.StartActivity("schema_validation"))
        {
            schemaResult = _schemaValidation.Validate(rawData, descriptor.Name, descriptor.Version);
            RuntimeMetrics.RecordStage(RuntimeStage.SchemaValidation);

            if (!schemaResult.IsValid)
            {
                RuntimeMetrics.IncrementSchemaValidationFailed();
                activity?.SetStatus(ActivityStatusCode.Error, schemaResult.Message);
                return new MessagingResult
                {
                    Success = false,
                    Errors = [schemaResult.Message ?? "Schema validation failed"],
                };
            }
        }

        CompatibilityResult compatibilityResult;
        using (var _ = ActivitySource.StartActivity("compatibility_check"))
        {
            compatibilityResult = _compatibilityEvaluator.Evaluate(descriptor, descriptor.Version);
            RuntimeMetrics.RecordStage(RuntimeStage.CompatibilityCheck);

            if (!compatibilityResult.Compatible)
            {
                activity?.SetStatus(ActivityStatusCode.Error, compatibilityResult.Message);
                return new MessagingResult
                {
                    Success = false,
                    Errors = [compatibilityResult.Message ?? "Compatibility check failed"],
                };
            }
        }

        IReadOnlyList<GovernanceResult> governanceResults;
        using (var _ = ActivitySource.StartActivity("governance"))
        {
            governanceResults = await _governanceEngine.EvaluateAsync(envelope, cancellationToken);
            RuntimeMetrics.RecordStage(RuntimeStage.Governance);

            var blocked = governanceResults.FirstOrDefault(r => r.Decision == GovernanceDecision.Block);
            if (blocked is not null)
            {
                RuntimeMetrics.IncrementGovernanceBlocked();
                activity?.SetStatus(ActivityStatusCode.Error, blocked.Reason);
                return new MessagingResult
                {
                    Success = false,
                    Errors = [$"Governance blocked by {blocked.PolicyName}: {blocked.Reason}"],
                };
            }

            if (governanceResults.Any(r => r.Decision == GovernanceDecision.Warn))
                RuntimeMetrics.IncrementGovernanceWarned();
        }

        activity?.SetStatus(ActivityStatusCode.Ok);
        RuntimeMetrics.IncrementPublished();

        return MessagingResult.Ok(envelope.Id);
    }
}
