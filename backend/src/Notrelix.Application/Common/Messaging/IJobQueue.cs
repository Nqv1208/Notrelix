namespace Notrelix.Application.Common.Messaging;

/// <summary>
/// Interface cho background job queue
/// </summary>
public interface IJobQueue
{
    Task EnqueueAsync<TJob>(TJob job, CancellationToken cancellationToken = default) where TJob : class;
    Task EnqueueAsync<TJob>(TJob job, TimeSpan delay, CancellationToken cancellationToken = default) where TJob : class;
}
