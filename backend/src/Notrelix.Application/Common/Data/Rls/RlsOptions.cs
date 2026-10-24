namespace Notrelix.Application.Common.Data.Rls;

public sealed class RlsOptions
{
    public bool Enabled { get; set; }
    public bool ApplyPoliciesOnStartup { get; set; }
    public bool SetSessionContext { get; set; }

    /// <summary>
    /// How the serving process reacts when its database connection runs as a superuser, a
    /// BYPASSRLS role or an owner of the tenant tables (row-level security would not apply to it).
    /// <see cref="RlsRoleEnforcementMode.Off"/> by default: environments opt in explicitly.
    /// </summary>
    public RlsRoleEnforcementMode RoleEnforcement { get; set; } = RlsRoleEnforcementMode.Off;
}

public enum RlsRoleEnforcementMode
{
    /// <summary>The runtime role is not inspected.</summary>
    Off = 0,

    /// <summary>A privileged runtime role is logged as a warning and startup continues.</summary>
    Warn = 1,

    /// <summary>A privileged runtime role fails startup.</summary>
    Enforce = 2,
}
