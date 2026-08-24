namespace Notrelix.Application.Common.Realtime;

public interface IRealtimeChangeMapper<in TRequest, in TResponse>
{
    RealtimeResourceChangedV1 Map(TRequest request, TResponse response, long streamVersion);
}
