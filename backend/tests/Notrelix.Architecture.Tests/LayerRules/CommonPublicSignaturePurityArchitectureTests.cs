using System.Reflection;

namespace Notrelix.Architecture.Tests.LayerRules;

/// <summary>
/// TAC-GATE-022 — Common public-signature semantic purity (TAC-FRZ-017).
///
/// Application/Common may own technical mechanisms, but its public signature
/// graph must not expose bounded-context business vocabulary. The historical
/// grant-projection seam leaked AccountRole/WorkspaceRole through Common; that
/// seam is now owner-relocated to Accounts/Workspaces and must not return.
/// </summary>
public class CommonPublicSignaturePurityArchitectureTests
{
    private static readonly IReadOnlySet<string> ApprovedCommonSignatureDebt =
        new HashSet<string>(StringComparer.Ordinal)
        {
            "Notrelix.Application.Common.Requests.Security.IRequirePermission -> Notrelix.Domain.Governance.Permissions.PermissionAction (BC business type)",
            "Notrelix.Application.Common.Security.Auth.AuthSessionIssuer -> Notrelix.Application.Features.Identity.Abstractions.IIdentityDbContext (BC-owned Application contract)",
            "Notrelix.Application.Common.Security.Auth.AuthSessionIssuer -> Notrelix.Domain.Identity.Users.User (BC business type)",
            "Notrelix.Application.Common.Security.Auth.IAuthSessionIssuer -> Notrelix.Domain.Identity.Users.User (BC business type)",
            "Notrelix.Application.Common.Security.Auth.IJwtService -> Notrelix.Domain.Identity.Users.User (BC business type)",
        };

    [Fact]
    public void CommonPublicSignatures_MustNotExpose_BcBusinessTypes()
    {
        var violations = CollectViolations();

        var unexpected = violations
            .Where(v => !ApprovedCommonSignatureDebt.Contains(v))
            .ToList();

        unexpected.Should().BeEmpty(
            "TAC-FRZ-017: Application/Common public signatures must not expose BC business types. " +
            "Move the semantic contract to the owning context or use a stable technical descriptor. " +
            "Violations:\n" + string.Join("\n", unexpected));
    }

    [Fact]
    public void ApprovedCommonSignatureDebt_MustMatch_CurrentSignatures()
    {
        var violations = CollectViolations().ToHashSet(StringComparer.Ordinal);

        foreach (var removed in ApprovedCommonSignatureDebt.Except(violations))
            removed.Should().BeNull(
                "TAC-FRZ-017: Common signature debt baseline must shrink in the same change that removes it");
    }

    [Fact]
    public void AccessGrantProjectionService_MustNotLeak_RolesThroughCommon()
    {
        var commonGrantContract = Type.GetType(
            "Notrelix.Application.Common.Tenancy.IAccessGrantProjectionService, Notrelix.Application",
            throwOnError: false);

        commonGrantContract.Should().BeNull(
            "TAC-FRZ-017: grant projection contracts belong to Accounts/Workspaces, not Common");
    }

    [Fact]
    public void GrantProjectionContracts_AreOwnedByTheirContexts()
    {
        typeof(Notrelix.Application.Features.Accounts.Members.Services.IAccountGrantProjectionService)
            .Should().NotBeNull();
        typeof(Notrelix.Application.Features.Workspaces.Members.Services.IWorkspaceGrantProjectionService)
            .Should().NotBeNull();
    }

    [Fact]
    public void Gate_Detects_AccountRole_In_CommonSignature()
    {
        Classify(typeof(Notrelix.Domain.Accounts.Members.AccountRole))
            .Should().Be("BC business type");
    }

    [Fact]
    public void Gate_Detects_WorkspaceRole_In_CommonSignature()
    {
        Classify(typeof(Notrelix.Domain.Workspaces.Members.WorkspaceRole))
            .Should().Be("BC business type");
    }

    [Fact]
    public void Gate_Allows_TechnicalAndSharedKernelTypes()
    {
        Classify(typeof(Guid)).Should().BeNull();
        Classify(typeof(Notrelix.Domain.SharedKernel.ResourceRef)).Should().BeNull();
        Classify(typeof(Notrelix.Application.Common.Time.IDateTimeProvider)).Should().BeNull();
    }

    private static List<string> CollectViolations()
    {
        var violations = new List<string>();

        foreach (var type in Assembly.Load("Notrelix.Application").GetTypes()
                     .Where(t => t.IsPublic && IsCommonType(t)))
        {
            foreach (var referenced in CollectPublicSignatureDependencies(type))
            {
                var reason = Classify(referenced);
                if (reason is not null)
                    violations.Add($"{type.FullName} -> {referenced.FullName} ({reason})");
            }
        }

        return violations.Distinct(StringComparer.Ordinal).OrderBy(v => v, StringComparer.Ordinal).ToList();
    }

    private static bool IsCommonType(Type type)
        => type.Namespace?.StartsWith("Notrelix.Application.Common.", StringComparison.Ordinal) == true;

    private static IEnumerable<Type> CollectPublicSignatureDependencies(Type type)
    {
        const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

        var collected = new List<Type>();

        void Collect(Type? candidate)
        {
            if (candidate is null)
                return;

            collected.Add(candidate);

            if (candidate.IsGenericType && !candidate.IsGenericTypeDefinition)
            {
                foreach (var argument in candidate.GetGenericArguments())
                    Collect(argument);
            }
        }

        Collect(type.BaseType);

        foreach (var iface in type.GetInterfaces())
            Collect(iface);

        foreach (var ctor in type.GetConstructors(flags))
            foreach (var parameter in ctor.GetParameters())
                Collect(parameter.ParameterType);

        foreach (var field in type.GetFields(flags))
            Collect(field.FieldType);

        foreach (var property in type.GetProperties(flags))
            Collect(property.PropertyType);

        foreach (var method in type.GetMethods(flags).Where(m => !m.IsSpecialName))
        {
            Collect(method.ReturnType);
            foreach (var parameter in method.GetParameters())
                Collect(parameter.ParameterType);
        }

        return collected
            .Where(t => t != type)
            .Distinct()
            .ToList();
    }

    private static string? Classify(Type referenced)
    {
        var ns = referenced.Namespace;
        if (ns is null)
            return null;

        if (ns == "System" || ns.StartsWith("System.", StringComparison.Ordinal))
            return null;

        if (ns.StartsWith("Notrelix.Application.Common.", StringComparison.Ordinal))
            return null;

        if (ns == "Notrelix.Domain.Common" || ns.StartsWith("Notrelix.Domain.Common.", StringComparison.Ordinal) ||
            ns == "Notrelix.Domain.SharedKernel" || ns.StartsWith("Notrelix.Domain.SharedKernel.", StringComparison.Ordinal))
            return null;

        if (ns.StartsWith("Notrelix.Domain.", StringComparison.Ordinal))
            return "BC business type";

        if (ns.StartsWith("Notrelix.Application.Features.", StringComparison.Ordinal))
            return "BC-owned Application contract";

        return null;
    }
}
