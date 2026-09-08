using System.Reflection;

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
            ApplyDeclaredTenantScope(integrationEvent);
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

    private void ApplyDeclaredTenantScope(IIntegrationEvent integrationEvent)
    {
        var scope = integrationEvent.GetType()
            .GetCustomAttribute<IntegrationEventTenantScopeAttribute>()
            ?.Scope;

        switch (scope)
        {
            case IntegrationEventTenantScope.Workspace:
                RequireAccountId(integrationEvent, scope.Value);
                RequireWorkspaceId(integrationEvent, scope.Value);
                _tenant.SetWorkspace(
                    integrationEvent.AccountId!.Value,
                    integrationEvent.WorkspaceId!.Value,
                    integrationEvent.ActorUserId);
                return;

            case IntegrationEventTenantScope.Account:
                RequireAccountId(integrationEvent, scope.Value);
                _tenant.SetAccount(integrationEvent.AccountId!.Value, integrationEvent.ActorUserId);
                return;

            case IntegrationEventTenantScope.None:
                _tenant.SetSystem();
                return;

            default:
                throw new IntegrationEventTenantEnvelopeException(
                    $"Integration event {integrationEvent.MessageName} ({integrationEvent.EventId}) " +
                    "is missing an explicit IntegrationEventTenantScope classification.");
        }
    }

    private static void RequireAccountId(IIntegrationEvent integrationEvent, IntegrationEventTenantScope scope)
    {
        if (integrationEvent.AccountId is null || integrationEvent.AccountId == Guid.Empty)
        {
            throw new IntegrationEventTenantEnvelopeException(
                $"Integration event {integrationEvent.MessageName} ({integrationEvent.EventId}) " +
                $"is classified {scope} but does not carry a non-empty AccountId.");
        }
    }

    private static void RequireWorkspaceId(IIntegrationEvent integrationEvent, IntegrationEventTenantScope scope)
    {
        if (integrationEvent.WorkspaceId is null || integrationEvent.WorkspaceId == Guid.Empty)
        {
            throw new IntegrationEventTenantEnvelopeException(
                $"Integration event {integrationEvent.MessageName} ({integrationEvent.EventId}) " +
                $"is classified {scope} but does not carry a non-empty WorkspaceId.");
        }
    }
}
