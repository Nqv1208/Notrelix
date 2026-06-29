namespace Notrelix.Application.Common.CQRS;

public interface IMessageTriggeredRequest
{
    Guid MessageId { get; }
    Guid? SourceEventId { get; }
    string SourceMessageName { get; }
    int SourceMessageVersion { get; }
    string ConsumerName { get; }
    Guid? WorkspaceId { get; }
}
