using Notrelix.Application.Common.Requests.Execution;

namespace Notrelix.Architecture.Tests.Authorization;

/// <summary>
/// A suspended Account is read-only for everyone in it. The suspension is found while the access facts load, so
/// every authenticated write inside an Account, Workspace or Resource must be one that loads them (it requires a
/// permission); a write that did not would stay writable while its Account is suspended (fail open).
/// </summary>
public sealed class AccountSuspensionWriteGateArchitectureTests
{
    private static readonly ApplicationScopeKind[] TenantScopes =
    [
        ApplicationScopeKind.Account,
        ApplicationScopeKind.Workspace,
        ApplicationScopeKind.Resource,
    ];

    [Fact]
    public void EveryAuthenticatedTenantScopedWrite_LoadsTheAccessFacts_SoSuspensionIsAlwaysChecked()
    {
        var registry = RequestDescriptorRegistry.Create(typeof(ICommand<>).Assembly);

        var tenantWrites = registry.Descriptors
            .Where(d => d.Principal == ApplicationPrincipalKind.Authenticated
                && d.DataAccess == ApplicationDataAccessKind.Write
                && TenantScopes.Contains(d.Scope))
            .ToList();

        // Non-zero evidence: the gate must have inspected the real write surface, not an empty set.
        tenantWrites.Count.Should().BeGreaterThan(100, "the tenant write surface is large; an empty scan proves nothing");

        var skipping = tenantWrites
            .Where(d => !d.Access.RequiresDatastoreFacts)
            .Select(d => d.RequestType.FullName)
            .ToList();

        skipping.Should().BeEmpty(
            "a tenant-scoped write that loads no access facts never reaches the AccountSuspended check: " +
            string.Join("; ", skipping));
    }

    [Fact]
    public void TheWritesThatLoadNoFacts_AreOnlyPersonalGlobalOrNonAuthenticated()
    {
        var registry = RequestDescriptorRegistry.Create(typeof(ICommand<>).Assembly);

        var open = registry.Descriptors
            .Where(d => d.DataAccess == ApplicationDataAccessKind.Write && !d.Access.RequiresDatastoreFacts)
            .ToList();

        open.Should().NotBeEmpty("personal identity writes exist and are classified Global");
        open.Should().OnlyContain(
            d => d.Principal != ApplicationPrincipalKind.Authenticated || d.Scope == ApplicationScopeKind.Global,
            "the only authenticated writes without facts are personal (Global scope) ones");
    }
}
