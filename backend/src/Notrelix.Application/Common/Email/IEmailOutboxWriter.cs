namespace Notrelix.Application.Common.Email;

public interface IEmailTemplatePayload
{
}

public sealed record ProtectedSecretEnvelope(string Value);

public sealed record WorkspaceInvitationEmailPayload(
    Guid InvitationId,
    int TokenGeneration,
    ProtectedSecretEnvelope ProtectedToken,
    DateTimeOffset ExpiresAt) : IEmailTemplatePayload;

public sealed record EmailVerificationEmailPayload(
    Guid VerificationTokenId,
    Guid UserId,
    ProtectedSecretEnvelope ProtectedToken,
    DateTimeOffset ExpiresAt) : IEmailTemplatePayload;

public sealed record QueueRenderedEmailRequest(
    string DeduplicationKey,
    string RecipientEmail,
    string? RecipientName,
    string Subject,
    string? BodyHtml,
    string? BodyText,
    Guid? WorkspaceId,
    Guid? RecipientUserId,
    string SourceContext,
    string TemplateKey,
    int TemplateVersion = 1,
    int Priority = 100,
    Guid? SourceEventId = null,
    Guid? SourceMessageId = null,
    DateTimeOffset? SensitivePayloadExpiresAt = null);

public sealed record QueueTemplatedEmailRequest<TPayload>(
    string DeduplicationKey,
    string RecipientEmail,
    string? RecipientName,
    Guid? WorkspaceId,
    Guid? RecipientUserId,
    string SourceContext,
    string TemplateKey,
    int TemplateVersion,
    TPayload Payload,
    DateTimeOffset SensitivePayloadExpiresAt,
    int Priority = 100,
    Guid? SourceEventId = null,
    Guid? SourceMessageId = null)
    where TPayload : IEmailTemplatePayload;

public sealed record EmailDeliveryRequest(
    string RecipientEmail,
    string? RecipientName,
    string Subject,
    string BodyHtml,
    string? BodyText,
    string IdempotencyKey);

public sealed record EmailDeliveryResult(
    string Provider,
    string? ProviderMessageId);

public interface IEmailOutboxWriter
{
    Task QueueRenderedEmailAsync(
        QueueRenderedEmailRequest request,
        CancellationToken cancellationToken);

    Task QueueTemplatedEmailAsync<TPayload>(
        QueueTemplatedEmailRequest<TPayload> request,
        CancellationToken cancellationToken)
        where TPayload : IEmailTemplatePayload;
}
