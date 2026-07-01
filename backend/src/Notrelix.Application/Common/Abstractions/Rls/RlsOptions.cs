namespace Notrelix.Application.Common.Abstractions.Rls;

public sealed class RlsOptions
{
    public bool Enabled { get; set; }
    public bool ApplyPoliciesOnStartup { get; set; }
    public bool SetSessionContext { get; set; }
}
