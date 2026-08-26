using Notrelix.Application.Common.Requests.Scoping;

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
) : ICommand<SendWelcomeEmailResult>, ISystemInternalRequest, IWriteRequest, IMessageTriggeredRequest, ISystemOperation, IGlobalRequest
{
    public string ConsumerName => ConsumerNames.WelcomeEmailSending;
    public Guid? WorkspaceId => null;

    public string OperationName => "SendWelcomeEmail";
    public SystemOperationReason Reason => new("Identity", "Welcome email for newly registered user");
    Guid ISystemOperation.CorrelationId => MessageId;
}