namespace Notrelix.Application.Common.Messaging;

public interface IOutboxWakeSignal
{
    void TrySignal();
    Task WaitAsync(TimeSpan timeout, CancellationToken cancellationToken);
}
