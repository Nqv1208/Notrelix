namespace Notrelix.Infrastructure.Data.Rls;

/// <summary>Reads <c>Rls:RoleEnforcement</c> from configuration before the options pipeline exists.</summary>
internal static class RlsRoleEnforcement
{
    /// <summary>True unless the host explicitly opted out (<c>Warn</c> or <c>Off</c>); unparseable values count as enforced.</summary>
    public static bool IsEnforced(IConfiguration configuration)
    {
        var raw = configuration["Rls:RoleEnforcement"];
        if (string.IsNullOrWhiteSpace(raw))
        {
            return true;
        }

        return !Enum.TryParse<RlsRoleEnforcementMode>(raw, ignoreCase: true, out var mode)
               || mode == RlsRoleEnforcementMode.Enforce;
    }
}
