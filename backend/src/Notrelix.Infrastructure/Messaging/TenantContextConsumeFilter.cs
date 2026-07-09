namespace Notrelix.Infrastructure.Messaging;

public sealed class TenantContextConsumeFilter<T> : IFilter<ConsumeContext<T>>
    where T : class
{
    private readonly ICurrentTenantContext _tenant;
    private readonly ILogger<TenantContextConsumeFilter<T>> _logger;

    public TenantContextConsumeFilter(
        ICurrentTenantContext tenant,
        ILogger<TenantContextConsumeFilter<T>> logger)
    {
        _tenant = tenant;
        _logger = logger;
    }

    public async Task Send(ConsumeContext<T> context, IPipe<ConsumeContext<T>> next)
    {
        var integrationEvent = context.Message as IIntegrationEvent;

        if (integrationEvent is null)
        {
            _logger.LogDebug("Message {Type} is not an integration event, skipping tenant context", typeof(T).Name);
            await next.Send(context);
            return;
        }

        try
        {
            if (integrationEvent.AccountId.HasValue)
            {
                if (integrationEvent.AccountId.Value == Guid.Empty)
                {
                    throw new InvalidOperationException(
                        $"Integration event {integrationEvent.MessageName} ({integrationEvent.EventId}) " +
                        "has an empty AccountId. Events must carry a valid account identifier.");
                }

                if (!integrationEvent.WorkspaceId.HasValue)
                {
                    _tenant.SetAccount(integrationEvent.AccountId.Value, integrationEvent.ActorUserId);
                }
                else
                {
                    _tenant.SetWorkspace(
                        integrationEvent.AccountId.Value,
                        integrationEvent.WorkspaceId.Value,
                        integrationEvent.ActorUserId);
                }
            }
            else
            {
                _tenant.SetSystem();
            }

            await next.Send(context);
        }
        finally
        {
            _tenant.Clear();
        }
    }

    public void Probe(ProbeContext context)
    {
        context.CreateFilterScope("tenantContextConsumeFilter");
    }
}
