using Notrelix.Infrastructure.Data;

namespace Notrelix.Infrastructure.Messaging;

/// <summary>
/// First filter of every consume pipeline: a message is consumed by the background principal, so the
/// consume scope's <see cref="ApplicationDbContext"/> (the deduplication claim, the consumer's own writes
/// and the commands it dispatches, which all share that scoped context) connects as the worker login, not
/// as the application login that serves requests.
/// </summary>
/// <remarks>
/// It must be registered before any filter that resolves the context (the tenant and deduplication
/// filters): the connection is fixed when the context is first resolved.
/// </remarks>
public sealed class WorkerPrincipalConsumeFilter<T> : IFilter<ConsumeContext<T>>
    where T : class
{
    private readonly DatabasePrincipal _principal;

    public WorkerPrincipalConsumeFilter(DatabasePrincipal principal)
    {
        _principal = principal;
    }

    public Task Send(ConsumeContext<T> context, IPipe<ConsumeContext<T>> next)
    {
        _principal.UseWorker();
        return next.Send(context);
    }

    public void Probe(ProbeContext context)
    {
        context.CreateFilterScope("workerPrincipalConsumeFilter");
    }
}
