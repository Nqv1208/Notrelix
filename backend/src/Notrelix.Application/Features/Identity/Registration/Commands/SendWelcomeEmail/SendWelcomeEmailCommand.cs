namespace Notrelix.Application.Features.Identity.Registration.Commands.SendWelcomeEmail;

public sealed record SendWelcomeEmailCommand(
    Guid UserId,
    string Email,
    string DisplayName,
    Guid MessageId,
    Guid? SourceEventId,
    string SourceMessageName,
    int SourceMessageVersion,
    string? CorrelationId,
    string? CausationId,
    DateTimeOffset OccurredAt
) : ICommand<SendWelcomeEmailResult>, ISystemInternalRequest, ITransactionalRequest, IMessageTriggeredRequest
{
    public UseCaseSecurityKind SecurityKind => UseCaseSecurityKind.SystemInternal;
    public string ConsumerName => ConsumerNames.WelcomeEmailSending;
    public Guid? WorkspaceId => null;
}