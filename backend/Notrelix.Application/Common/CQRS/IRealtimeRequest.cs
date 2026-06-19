namespace Notrelix.Application.Common.CQRS;

public interface IRealtimeRequest
{
    RealtimeTopic Topic { get; }
}
