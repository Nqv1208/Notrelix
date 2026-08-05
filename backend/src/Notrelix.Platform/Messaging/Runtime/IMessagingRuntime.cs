namespace Notrelix.Platform.Messaging.Runtime;

public interface IMessagingRuntime
{
    Task<MessagingResult> PublishAsync(
        EventPublication publication,
        CancellationToken cancellationToken = default);
}

public sealed record MessagingResult
{
    public bool Success { get; init; }
    public Guid EnvelopeId { get; init; }
    public IReadOnlyList<string>? Errors { get; init; }
    public IReadOnlyList<RuntimeStage> CompletedStages { get; init; } = [];

    public static MessagingResult Ok(Guid envelopeId) =>
        new() { Success = true, EnvelopeId = envelopeId };
}

public enum RuntimeStage
{
    ResolveDescriptor,
    BuildEnvelope,
    Canonicalize,
    Serialize,
    SchemaValidation,
    CompatibilityCheck,
    Governance,
    TransportSend,
}
