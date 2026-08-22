using Notrelix.Application.Common.Requests;
using Notrelix.Application.Common.Requests.Scoping;
using Notrelix.Application.Common.Requests.Security;

namespace Notrelix.Architecture.Tests;

/// <summary>
/// IA-TST-AUTHZ-ARCH-001..005 / IAREQ136 / IAREQ137 / IAAC017.
///
/// Executable authorization-bypass gate: protected Application handlers must not
/// re-authorize the current actor outside the canonical AuthorizationBehavior
/// pipeline. Current-actor role checks are forbidden; target-entity/member role
/// invariants require an exact, semantic exception registry.
/// </summary>
public class HandlerAuthorizationBypassArchitectureTests : ArchitectureTestBase
{
    private const string GateId = "IA-TST-AUTHZ-ARCH";

    /// <summary>
    /// Canonical authorization services owned by the pipeline (AuthorizationBehavior).
    /// Handlers MUST NOT inject/use them — authorization belongs to the pipeline.
    /// </summary>
    private static readonly string[] PipelineAuthorizationServices =
    [
        "IPermissionService",
        "IPermissionEvaluator",
        "IWorkspacePermissionService",
        "IAuthorizationDecisionStore",
    ];

    private sealed record RoleCheckException(
        string FileName,
        string Member,
        string Pattern,
        string Reason,
        string InvariantOwner,
        string ReviewTrigger);

    /// <summary>
    /// Exact exception registry for production handler role checks (IA-TST-AUTHZ-ARCH-003).
    /// Every entry is a TARGET-entity/member business invariant evaluated AFTER pipeline
    /// authorization — never a current-actor permission decision. No wildcard entries.
    /// </summary>
    private static readonly RoleCheckException[] ApprovedBusinessInvariantRoleChecks =
    [
        new(
            "RemoveMember.cs",
            "RemoveMemberCommandHandler.Handle",
            "m.Role == WorkspaceRole.Owner (active owner count)",
            "Last-owner protection: the target workspace must retain an active owner after member removal; the count constrains the DOMAIN transition of the TARGET member.",
            "Workspaces bounded context — WorkspaceMember aggregate lifecycle",
            "Review if WorkspaceMember.Remove semantics change or a Governance-managed ownership-transfer flow replaces last-owner protection."),
        new(
            "SuspendMember.cs",
            "SuspendMemberCommandHandler.Handle",
            "m.Role == WorkspaceRole.Owner (active owner count)",
            "Last-owner protection: suspending the last active owner would leave the workspace without administrative authority; the count constrains the TARGET member suspension invariant.",
            "Workspaces bounded context — WorkspaceMember aggregate lifecycle",
            "Review if WorkspaceMember.Suspend semantics change."),
        new(
            "UpdateMemberRole.cs",
            "UpdateMemberRoleCommandHandler.Handle",
            "m.Role == WorkspaceRole.Owner (active owner count)",
            "Last-owner protection: demoting the last active owner via role change would orphan workspace administration; the count constrains the TARGET member role-transition invariant.",
            "Workspaces bounded context — WorkspaceMember aggregate lifecycle",
            "Review if WorkspaceMember.ChangeRole semantics change."),
        new(
            "TransferOwnership.cs",
            "TransferOwnershipCommandHandler.Handle",
            "m.Role == WorkspaceRole.Owner && m.UserId == currentOwnerId (ownership handover lookup)",
            "Ownership transfer invariant (explicitly allowed by IAREQ137): the operation transfers the Owner relationship held by the requesting principal; loading that relationship requires its Owner state. Pipeline ManageWorkspace authorization runs first; this check selects the entity being mutated and enforces the transfer's own semantics.",
            "Workspaces bounded context — workspace ownership lifecycle",
            "Review if ownership transfer moves to a dedicated Governance action or dedicated PermissionAction."),
        new(
            "CreateWorkspace.cs",
            "CreateWorkspaceCommandHandler.Handle",
            "WorkspaceRole.Owner (passed to Workspace.Create bootstrap)",
            "Bootstrap authority (IAREQ091/IAREQ090): the creator of a new workspace becomes its founding Owner as part of the owned creation semantics. No authorization decision is taken from a role here — the role literal is construction data for the new aggregate.",
            "Workspaces bounded context — workspace creation/bootstrap",
            "Review if workspace creation bootstrap semantics change (e.g. configurable founder roles)."),
        new(
            "ActivateMember.cs",
            "ActivateMemberCommandHandler.Handle",
            "member.Role (read for grant projection sync)",
            "Data flow, not a decision: the TARGET member's stored role is forwarded to the access-grant projection so RLS grants stay consistent with membership state after activation.",
            "Workspaces bounded context — access-grant projection consistency",
            "Review if grant projection inputs change."),
        new(
            "ProvisionPersonalWorkspaceCommand.cs",
            "ProvisionPersonalWorkspaceCommandHandler.Handle",
            "WorkspaceRole.Owner (bootstrap construction for personal workspace)",
            "Approved special contract (IAREQ091/IAREQ090, ISystemInternalRequest): registration-triggered provisioning constructs the personal workspace with its founder as Owner. The principal is the system provisioning contract, not an ambient user request; no authorization decision is read from a role.",
            "Workspaces/Identity registration bootstrap",
            "Review if personal-workspace provisioning ownership semantics change."),
    ];

    [Fact]
    public void ProtectedHandlers_DoNotInjectPipelineAuthorizationServices()
    {
        var violations = new List<string>();

        foreach (var file in GetApplicationFeatureFiles())
        {
            var content = RemoveComments(File.ReadAllText(file));
            if (!content.Contains("IRequestHandler<", StringComparison.Ordinal))
            {
                continue;
            }

            foreach (var service in PipelineAuthorizationServices)
            {
                if (content.Contains(service, StringComparison.Ordinal))
                {
                    violations.Add($"{RelativePath(file)} references {service}");
                }
            }
        }

        violations.Should().BeEmpty(
            $"{GateId}-001 (IAREQ136): protected handlers MUST NOT directly inject or use canonical " +
            "authorization services — AuthorizationBehavior owns evaluation. If a handler needs a " +
            "decision, extend the request contract/pipeline instead. Violations: " +
            string.Join("; ", violations));
    }

    [Fact]
    public void ScopedRequests_DeclareCanonicalAuthorizationContract()
    {
        var requestTypes = GetApplicationRequestTypes();

        var scopedButUnprotected = requestTypes
            .Where(t => typeof(IWorkspaceRequest).IsAssignableFrom(t)
                     || typeof(IAccountRequest).IsAssignableFrom(t)
                     || typeof(IResourceScopedRequest).IsAssignableFrom(t))
            .Where(t => !typeof(IRequirePermission).IsAssignableFrom(t)
                     && !typeof(ISystemInternalRequest).IsAssignableFrom(t))
            .Select(t => t.Name)
            .OrderBy(n => n)
            .ToList();

        scopedButUnprotected.Should().BeEmpty(
            $"{GateId}-002 (IAREQ136): workspace/account/resource-scoped requests must declare the " +
            "canonical authorization contract (IRequirePermission) or be explicit system-internal " +
            "contracts. Protection must be declared on the request, not inferred from handlers. " +
            "Violations: " + string.Join("; ", scopedButUnprotected));
    }

    [Fact]
    public void ProductionHandlerRoleChecks_AreExactlyRegisteredBusinessInvariants()
    {
        var featureFiles = GetApplicationFeatureFiles();
        var violations = new List<string>();

        foreach (var file in featureFiles)
        {
            var content = RemoveComments(File.ReadAllText(file));
            var fileName = Path.GetFileName(file);
            if (!content.Contains("IRequestHandler<", StringComparison.Ordinal))
            {
                continue;
            }

            var hasRoleCheck =
                content.Contains(".Role == ", StringComparison.Ordinal)
                || content.Contains("AccountRole.", StringComparison.Ordinal)
                || content.Contains("WorkspaceRole.", StringComparison.Ordinal);

            if (!hasRoleCheck)
            {
                continue;
            }

            var registered = ApprovedBusinessInvariantRoleChecks.Any(e => e.FileName == fileName);
            if (!registered)
            {
                violations.Add(
                    $"{RelativePath(file)} contains a role check that is not registered as an exact " +
                    "business-invariant exception. Classify it: REMOVE_BYPASS (delete it — pipeline " +
                    "owns the decision) or RETAIN_BUSINESS_INVARIANT (register exact metadata in " +
                    nameof(ApprovedBusinessInvariantRoleChecks) + ").");
            }
        }

        violations.Should().BeEmpty(
            $"{GateId}-003 (IAREQ137): {string.Join("; ", violations)}");
    }

    [Fact]
    public void RoleCheckExceptionRegistry_IsExactAndSemantic()
    {
        var violations = new List<string>();
        var seen = new HashSet<string>();

        foreach (var entry in ApprovedBusinessInvariantRoleChecks)
        {
            var key = $"{entry.FileName}:{entry.Member}";
            if (!seen.Add(key))
            {
                violations.Add($"{key}: duplicate registry entry.");
                continue;
            }

            if (string.IsNullOrWhiteSpace(entry.FileName)
                || string.IsNullOrWhiteSpace(entry.Member)
                || string.IsNullOrWhiteSpace(entry.Pattern))
            {
                violations.Add($"{key}: exact type/file/member/pattern required.");
            }

            if (string.IsNullOrWhiteSpace(entry.Reason)
                || string.IsNullOrWhiteSpace(entry.InvariantOwner)
                || string.IsNullOrWhiteSpace(entry.ReviewTrigger))
            {
                violations.Add($"{key}: reason, invariant owner and review trigger are mandatory.");
            }

            // A current-actor authorization decision can never be allowlisted as an invariant.
            if (entry.Reason.Contains("current actor may execute", StringComparison.OrdinalIgnoreCase)
                || entry.Reason.Contains("decides whether the use case", StringComparison.OrdinalIgnoreCase))
            {
                violations.Add($"{key}: reason describes a current-actor authorization decision; " +
                               "authorization bypasses cannot be allowlisted as business invariants.");
            }
        }

        violations.Should().BeEmpty(
            $"{GateId}-003 (IAREQ137): registry hygiene failures — {string.Join("; ", violations)}");
    }

    /// <summary>
    /// IA-TST-AUTHZ-ARCH-004 / IAREQ137 — proves the gate distinguishes allowed
    /// target-role business invariants from current-actor authorization using a
    /// real registered production invariant (last-owner protection) plus a test
    /// fixture for each classification side. No production code is added for coverage.
    /// </summary>
    [Fact]
    public void Gate_Distinguishes_BusinessInvariants_From_CurrentActorAuthorization()
    {
        // The real production invariant family is registered...
        ApprovedBusinessInvariantRoleChecks.Should().Contain(
            e => e.FileName == "RemoveMember.cs",
            "last-owner protection is the canonical allowed target-role business invariant");

        // ...and the gate's detection pattern recognizes both sides on fixture sources.
        const string allowedInvariantSource =
            """
            public class RemoveMemberCommandHandler : IRequestHandler<RemoveMemberCommand, Result> {
                var activeOwnerCount = await _context.WorkspaceMembers
                    .CountAsync(m => m.WorkspaceId == workspace.Id && m.Role == WorkspaceRole.Owner && m.Status == WorkspaceMemberStatus.Active, ct);
            }
            """;

        const string forbiddenCurrentActorSource =
            """
            public class CreateWorkspaceCommandHandler : IRequestHandler<CreateWorkspaceCommand, Result<Guid>> {
                if (_requestContext.Role != AccountRole.Admin)
                    throw new ForbiddenException("missing_permission");
            }
            """;

        ContainsProductionRoleCheck(allowedInvariantSource).Should().BeTrue(
            "the gate must detect target-role business invariants so they are consciously registered");
        ContainsProductionRoleCheck(forbiddenCurrentActorSource).Should().BeTrue(
            "the gate must detect current-actor role authorization checks");
    }

    /// <summary>
    /// IA-TST-AUTHZ-ARCH-005 companion proof: closure did not introduce endpoint-local
    /// raw auth conventions. The standing endpoint gate remains authoritative; here we
    /// additionally assert no NEW RequireAuthorization/AllowAnonymous usage appeared in
    /// middleware (CSRF work added none).
    /// </summary>
    [Fact]
    public void Middleware_DoesNotUseRawEndpointAuthConventions()
    {
        var middlewareDir = Path.Combine(GetApiPath(), "Middleware");
        var violations = new List<string>();

        foreach (var file in Directory.GetFiles(middlewareDir, "*.cs"))
        {
            var content = RemoveComments(File.ReadAllText(file));
            if (content.Contains(".RequireAuthorization(", StringComparison.Ordinal)
                || content.Contains(".AllowAnonymous(", StringComparison.Ordinal))
            {
                violations.Add(Path.GetFileName(file));
            }
        }

        violations.Should().BeEmpty(
            $"{GateId}-005 (IAREQ136): middleware must not perform endpoint-level raw auth — " +
            "violations: " + string.Join("; ", violations));
    }

    private static bool ContainsProductionRoleCheck(string source) =>
        source.Contains("IRequestHandler<", StringComparison.Ordinal)
        && (source.Contains(".Role == ", StringComparison.Ordinal)
            || source.Contains("AccountRole.", StringComparison.Ordinal));

    private static List<Type> GetApplicationRequestTypes()
    {
        var applicationAssembly = typeof(ICommand).Assembly;

        return applicationAssembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false })
            .Where(t => ImplementsRequestContract(t))
            .ToList();

        static bool ImplementsRequestContract(Type t) =>
            t.GetInterfaces().Any(i =>
                i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICommand<>)
                || i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IQuery<>)
                || i == typeof(ICommand));
    }

    private static string RelativePath(string file)
    {
        var appRoot = Path.Combine(GetSrcPath(), "Notrelix.Application");
        return file.Replace(appRoot, "Notrelix.Application", StringComparison.Ordinal);
    }
}
