using System.Reflection;
using FluentAssertions;
using Notrelix.Domain.Common;

namespace Notrelix.Architecture.Tests;

public class DomainTenantScopeTests
{
    private static readonly Assembly DomainAssembly = typeof(AggregateRoot).Assembly;

    /// <summary>
    /// Aggregates that are genuinely global (no tenant scope).
    /// Must NOT implement IAccountScoped or IWorkspaceScoped.
    /// </summary>
    private static readonly HashSet<string> GlobalAggregates = new()
    {
        // Billing global
        "Notrelix.Domain.Billing.Plans.Plan",
        "Notrelix.Domain.Billing.Plans.FeatureCode",
        "Notrelix.Domain.Billing.Plans.PlanPrice",
        "Notrelix.Domain.Billing.Plans.BillingPeriod",
        "Notrelix.Domain.Billing.BillingEvents.BillingEvent",

        // Identity global (user identity is account-level but not workspace-scoped)
        "Notrelix.Domain.Identity.Users.User",
        "Notrelix.Domain.Identity.Tokens.EmailVerificationToken",
        "Notrelix.Domain.Identity.Tokens.PasswordResetToken",
        "Notrelix.Domain.Identity.Sessions.UserSession",
        "Notrelix.Domain.Identity.Security.UserLoginAttempt",
        "Notrelix.Domain.Identity.Security.UserSecuritySettings",
        "Notrelix.Domain.Identity.Profiles.UserProfile",
        "Notrelix.Domain.Identity.Mfa.UserMfaMethod",

        // WorkManagement templates (global scope, not tied to a specific workspace)
        "Notrelix.Domain.WorkManagement.Templates.BoardTemplate",

        // Documents templates
        "Notrelix.Domain.Documents.Templates.PageTemplate",

        // Integrations
        "Notrelix.Domain.Integrations.Webhooks.Events.InboundWebhookEvent",

        // Automation
        "Notrelix.Domain.Automation.Templates.AutomationTemplate",
    };

    /// <summary>
    /// Aggregates that span multiple scopes (e.g., hybrid System/Workspace templates).
    /// These implement a scope interface but are classified differently.
    /// </summary>
    private static readonly HashSet<string> HybridAggregates = new()
    {
        "Notrelix.Domain.Governance.Templates.PermissionTemplate",
    };

    [Fact]
    public void TenantScopedEntities_ShouldImplementScopeInterface()
    {
        var violations = new List<string>();

        foreach (var type in DomainAssembly.GetTypes())
        {
            if (!IsDomainType(type)) continue;
            if (type.IsEnum || type.IsInterface || type.IsAbstract) continue;

            if (!typeof(AggregateRoot).IsAssignableFrom(type)) continue;

            // Skip global and hybrid aggregates
            if (GlobalAggregates.Contains(type.FullName!)) continue;
            if (HybridAggregates.Contains(type.FullName!)) continue;

            // Check if the aggregate has workspace-scoped properties
            var hasWorkspaceId = type.GetProperty("WorkspaceId") is not null;
            var hasAccountId = type.GetProperty("AccountId") is not null;

            if (hasWorkspaceId || hasAccountId)
            {
                var implementsScope = typeof(IWorkspaceScoped).IsAssignableFrom(type) ||
                                     typeof(IAccountScoped).IsAssignableFrom(type);

                if (!implementsScope)
                {
                    violations.Add($"{type.FullName} (has {(hasWorkspaceId ? "WorkspaceId" : "AccountId")} but doesn't implement scope interface)");
                }
            }
        }

        violations.Should().BeEmpty(
            "tenant-scoped aggregates must implement IWorkspaceScoped or IAccountScoped: " +
            string.Join("\n", violations));
    }

    [Fact]
    public void GlobalAggregates_ShouldNotImplementTenantScope()
    {
        var violations = new List<string>();

        foreach (var aggregateName in GlobalAggregates)
        {
            var type = DomainAssembly.GetType(aggregateName);
            if (type is null) continue;

            var implementsWorkspace = typeof(IWorkspaceScoped).IsAssignableFrom(type);
            var implementsAccount = typeof(IAccountScoped).IsAssignableFrom(type);

            if (implementsWorkspace || implementsAccount)
            {
                violations.Add($"{type.FullName} is Global but implements scope interface");
            }
        }

        violations.Should().BeEmpty(
            "global aggregates must not implement tenant scope interfaces: " +
            string.Join("\n", violations));
    }

    private static bool IsDomainType(Type type)
    {
        if (type.Namespace is null) return false;
        return type.Namespace.StartsWith("Notrelix.Domain.", StringComparison.Ordinal);
    }
}
