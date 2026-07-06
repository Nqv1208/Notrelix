namespace Notrelix.Application.Common.Messaging;

public enum IntegrationEventTenantScope
{
    /// <summary>No tenant scope — event is system-wide, handled under system context.</summary>
    None = 0,
    /// <summary>Account-scoped event — associated with an account but not a specific workspace.</summary>
    Account = 1,
    /// <summary>Workspace-scoped event — associated with a specific workspace within an account.</summary>
    Workspace = 2,
}
