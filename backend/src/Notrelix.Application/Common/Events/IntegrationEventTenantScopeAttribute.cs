namespace Notrelix.Application.Common.Events;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class IntegrationEventTenantScopeAttribute : Attribute
{
    public IntegrationEventTenantScopeAttribute(IntegrationEventTenantScope scope)
    {
        Scope = scope;
    }

    public IntegrationEventTenantScope Scope { get; }
}
