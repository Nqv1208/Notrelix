namespace Notrelix.Architecture.Tests;

/// <summary>
/// Architecture tests auditing IgnoreQueryFilters() usage in production code.
/// Every IgnoreQueryFilters() call must be classified — no unmapped usage allowed.
/// </summary>
public class IgnoreQueryFiltersArchitectureTests
{
    private static string GetSolutionRoot()
    {
        var current = AppContext.BaseDirectory;
        while (current != null && !File.Exists(Path.Combine(current, "backend.slnx")))
        {
            current = Path.GetDirectoryName(current);
        }
        if (current == null)
            throw new DirectoryNotFoundException("Could not find backend.slnx root.");
        return current;
    }

    private static string[] GetProductionCsFiles()
    {
        var root = GetSolutionRoot();
        var srcPath = Path.Combine(root, "src");
        return Directory.GetFiles(srcPath, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                     && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
            .ToArray();
    }

    private static List<(string File, int Line, string Method)> FindIgnoreQueryFiltersCalls(string[] files)
    {
        var results = new List<(string File, int Line, string Method)>();
        var pattern = new Regex(@"\.IgnoreQueryFilters\s*\(");

        foreach (var file in files)
        {
            var lines = File.ReadAllLines(file);
            for (var i = 0; i < lines.Length; i++)
            {
                if (pattern.IsMatch(lines[i]))
                {
                    var methodContext = ExtractMethodContext(lines, i);
                    results.Add((Path.GetRelativePath(GetSolutionRoot(), file), i + 1, methodContext));
                }
            }
        }

        return results;
    }

    private static string ExtractMethodContext(string[] lines, int lineIndex)
    {
        for (var i = lineIndex; i >= 0; i--)
        {
            var trimmed = lines[i].Trim();
            if (trimmed.StartsWith("public ") || trimmed.StartsWith("private ") ||
                trimmed.StartsWith("protected ") || trimmed.StartsWith("internal "))
            {
                var match = Regex.Match(trimmed, @"(?:async\s+)?(?:Task|void|ValueTask)\s+(\w+)");
                if (match.Success)
                    return match.Groups[1].Value;
            }
        }
        return "(unknown)";
    }

    private static readonly Dictionary<string, AllowlistEntry> IgnoreQueryFiltersAllowlist = new()
    {
        ["RestoreWorkspace.cs"] = new("RestoreWorkspace.cs", AllowlistClassification.Intentional,
            "Restore operation must see soft-deleted workspaces to restore them",
            "Keep as Intentional — restore requires IgnoreQueryFilters"),
        ["RestoreSpace.cs"] = new("RestoreSpace.cs", AllowlistClassification.Intentional,
            "Restore operation must see soft-deleted spaces to restore them",
            "Keep as Intentional — restore requires IgnoreQueryFilters"),
        ["RestoreTeam.cs"] = new("RestoreTeam.cs", AllowlistClassification.Intentional,
            "Restore operation must see soft-deleted teams to restore them",
            "Keep as Intentional — restore requires IgnoreQueryFilters"),
        ["RestoreBoard.cs"] = new("RestoreBoard.cs", AllowlistClassification.Intentional,
            "Restore operation must see soft-deleted boards to restore them; tenant predicate re-applied explicitly (WM-V3-DEC-013)",
            "Keep as Intentional — restore requires IgnoreQueryFilters"),
        ["RevokeResourcePermissionCommand.cs"] = new("RevokeResourcePermissionCommand.cs", AllowlistClassification.Intentional,
            "A repeated revoke must find the already-removed entry to answer success; workspace, resource kind and resource id are re-applied explicitly, so an id of another resource stays NotFound",
            "Keep as Intentional — an idempotent revoke must see the soft-deleted permission"),
        ["ListBoardItemTrash.cs"] = new("ListBoardItemTrash.cs", AllowlistClassification.Intentional,
            "The trash lists soft-deleted board items of one board; tenant predicate re-applied explicitly (WM-V3-DEC-013)",
            "Keep as Intentional — the trash must see soft-deleted rows"),
        ["RestoreBoardItem.cs"] = new("RestoreBoardItem.cs", AllowlistClassification.Intentional,
            "Restore operation must see soft-deleted board items to restore them; tenant predicate re-applied explicitly (WM-V3-DEC-013)",
            "Keep as Intentional — restore requires IgnoreQueryFilters"),
        ["RestoreBoardView.cs"] = new("RestoreBoardView.cs", AllowlistClassification.Intentional,
            "Restore operation must see soft-deleted board views to restore them; tenant predicate re-applied explicitly (WM-V3-DEC-013)",
            "Keep as Intentional — restore requires IgnoreQueryFilters"),
        ["RestoreForm.cs"] = new("RestoreForm.cs", AllowlistClassification.Intentional,
            "Restore operation must see soft-deleted forms to restore them; tenant predicate re-applied explicitly (WM-V3-DEC-013)",
            "Keep as Intentional — restore requires IgnoreQueryFilters"),
        ["RestoreApprovalRequest.cs"] = new("RestoreApprovalRequest.cs", AllowlistClassification.Intentional,
            "Restore operation must see soft-deleted approval requests to restore them; tenant predicate re-applied explicitly (WM-V3-DEC-013)",
            "Keep as Intentional — restore requires IgnoreQueryFilters"),
        ["RestoreSavedFilter.cs"] = new("RestoreSavedFilter.cs", AllowlistClassification.Intentional,
            "Restore operation must see soft-deleted saved filters to restore them; tenant predicate re-applied explicitly (WM-V3-DEC-013)",
            "Keep as Intentional — restore requires IgnoreQueryFilters"),
        ["RestorePageVersion.cs"] = new("RestorePageVersion.cs", AllowlistClassification.Intentional,
            "Restoring a page version must see the page's soft-deleted blocks to bring back one deleted since the entry; the page, account and workspace predicates are re-applied explicitly",
            "Keep as Intentional — restore requires IgnoreQueryFilters"),
        ["PageSubtreeStore.cs"] = new("PageSubtreeStore.cs", AllowlistClassification.Intentional,
            "The page ancestry and depth walks must pass through deleted pages: a deleted page still sits in the chain between a page and the ancestors above it, so cycle and nesting checks that stopped at it would be blind; account and workspace predicates are re-applied explicitly",
            "Keep as Intentional — tree checks need the deleted rows of the chain"),
        ["ApplicationDbContextInitialiser.cs"] = new("ApplicationDbContextInitialiser.cs", AllowlistClassification.SystemCommand,
            "Database seed/reset must clear all tables regardless of query filters",
            "Keep as SystemCommand — infrastructure maintenance operation"),
        ["WorkspaceAccessResolver.cs"] = new("WorkspaceAccessResolver.cs", AllowlistClassification.InfrastructureBootstrap,
            "Bootstrap resolver bypasses EF query filter to resolve tenant before RLS context is set",
            "Keep as InfrastructureBootstrap — resolver runs before RLS session is established"),
        ["TenantBootstrapStore.cs"] = new("TenantBootstrapStore.cs", AllowlistClassification.InfrastructureBootstrap,
            "Tenant bootstrap store bypasses EF query filter to resolve workspace before RLS context is set",
            "Keep as InfrastructureBootstrap — store runs before RLS session is established"),

        ["ResourceLocator.cs"] = new("ResourceLocator.cs", AllowlistClassification.InfrastructureBootstrap,
            "Resource locator bypasses EF query filter to resolve resource tenant context before RLS is set",
            "Keep as InfrastructureBootstrap — resolver runs before RLS session is established"),

        ["InvitationTokenScope.cs"] = new("InvitationTokenScope.cs", AllowlistClassification.InfrastructureBootstrap,
            "Invitation-token requests carry only the one-time secret: the invitation is located by the hash of the secret across tenants, then the request adopts that invitation's own Account and Workspace scope",
            "Keep as InfrastructureBootstrap — token locator runs before a tenant scope exists (same pattern as CalendarWebhookBindingResolver)"),
        ["InviteeInvitations.cs"] = new("InviteeInvitations.cs", AllowlistClassification.Intentional,
            "The invitee's own invitations are read by Global requests (the invited workspace is not known until the invitation is read), so the tenant query filter would hide every row; the invitee database policy (RLS 025) limits the read to pending invitations addressed to the person's confirmed email and the stored address is repeated in the query",
            "Keep as Intentional — invitee reads cannot carry a tenant scope; same pattern as InvitationTokenScope"),
        ["CalendarWebhookBindingResolver.cs"] = new("CalendarWebhookBindingResolver.cs", AllowlistClassification.InfrastructureBootstrap,
            "Webhook binding resolver bypasses EF query filter to resolve the owning calendar binding across tenant boundaries before the derived tenant can be adopted",
            "Keep as InfrastructureBootstrap — resolver runs before RLS session is established (same pattern as ResourceLocator)"),

        ["PostgresPageAuthorizationFacts.cs"] = new("PostgresPageAuthorizationFacts.cs", AllowlistClassification.InfrastructureBootstrap,
            "Documents-owned page authorization facts must see deleted/archived pages so the engine can resolve NotFound instead of exposing them as live resources",
            "Keep as InfrastructureBootstrap — facts resolution runs before RLS session is established"),
        ["BoardChildAuthorizationFactsAdapter.cs"] = new("BoardChildAuthorizationFactsAdapter.cs", AllowlistClassification.Intentional,
            "Work Management-owned Board-child relation for the access facts (GOV-033) must resolve soft-deleted children so restore flows are decided on their Board, exactly as the resource locator locates them; it reads only the parent Board id by primary key within the request's tenant session, and the Board's own lifecycle is decided by the access facts",
            "Keep as Intentional — restore flows resolve deleted children (same pattern as ResourceLocator)"),
        ["EmailTemplateMaterialization.cs"] = new("EmailTemplateMaterialization.cs", AllowlistClassification.Intentional,
            "Email template materialization bypasses EF query filter to resolve templates across tenant boundaries",
            "Keep as Intentional — cross-tenant template resolution"),
        ["EmailVerificationTokenIssuer.cs"] = new("EmailVerificationTokenIssuer.cs", AllowlistClassification.Intentional,
            "Token issuer bypasses EF query filter to revoke prior tokens across tenant boundaries",
            "Keep as Intentional — cross-tenant token revocation"),
        ["ActiveVerificationTokenLocker.cs"] = new("ActiveVerificationTokenLocker.cs", AllowlistClassification.Intentional,
            "Token locker bypasses EF query filter to lock active verification tokens across tenant boundaries",
            "Keep as Intentional — cross-tenant token locking"),
        ["WorkspaceOwnerUpdateLocker.cs"] = new("WorkspaceOwnerUpdateLocker.cs", AllowlistClassification.Intentional,
            "Workspace locker bypasses EF query filter and status filter is encoded in raw SQL to take SELECT ... FOR UPDATE on the active workspace row; the row lock is held for the owning request transaction to serialize owner-affecting membership operations",
            "Keep as Intentional — PostgreSQL row-lock serialization for last-owner invariants"),
        ["PostgresAttachmentReaperStore.cs"] = new("PostgresAttachmentReaperStore.cs", AllowlistClassification.Intentional,
            "The attachment object reaper must see soft-deleted attachments (the purge target) and runs under the explicit system/worker context with the RLS worker scope; it reads and updates only collab.attachments and never serves a request",
            "Keep as Intentional — retention purge of soft-deleted attachment objects needs the soft-delete filter bypassed"),
        ["GetMyWorkspaces.cs"] = new("GetMyWorkspaces.cs", AllowlistClassification.Intentional,
            "A global-scope read of the principal's own memberships: the EF tenant filter of the workspace-scoped sets keys on the request Workspace, which a global request does not have, so it would hide every row. The subject is fixed to the authenticated principal and the soft-delete conditions are stated explicitly",
            "Keep as Intentional — account-level list across the caller's own Workspaces"),
        ["GetUserWorkspaces.cs"] = new("GetUserWorkspaces.cs", AllowlistClassification.Intentional,
            "A global-scope read of the principal's own memberships (no request Workspace, so the EF tenant filter would hide every row); the subject is the authenticated principal and the soft-delete condition is explicit",
            "Keep as Intentional — account-level list across the caller's own Workspaces"),
        ["IdentityBootstrapReadAdapter.cs"] = new("IdentityBootstrapReadAdapter.cs", AllowlistClassification.Intentional,
            "The sign-in bootstrap is a global request (no request Workspace); the EF tenant filter would hide every membership. The subject is the authenticated user and the soft-delete condition is explicit",
            "Keep as Intentional — bootstrap lists the caller's own Workspaces"),
        ["AvatarAudienceReadAdapter.cs"] = new("AvatarAudienceReadAdapter.cs", AllowlistClassification.Intentional,
            "A global-scope audience check for a profile photo: both sides are fixed to the authenticated viewer and the requested owner, only active memberships count, and the EF tenant filter (request Workspace) would otherwise hide every row",
            "Keep as Intentional — shared-workspace fact across Workspaces for a global request"),
        ["PostgresAvatarReaperStore.cs"] = new("PostgresAvatarReaperStore.cs", AllowlistClassification.Intentional,
            "The avatar object reaper must count a soft-deleted user as still referencing its photo (a restored user keeps the photo) before it removes unreferenced objects; it runs under the explicit system/worker context with the RLS worker scope, reads only identity.users.avatar_storage_key and never serves a request",
            "Keep as Intentional — orphan sweep must see soft-deleted user rows"),
    };

    [Fact]
    public void IgnoreQueryFiltersUsage_ShouldBeClassified()
    {
        var files = GetProductionCsFiles();
        var calls = FindIgnoreQueryFiltersCalls(files);

        var violations = new List<string>();

        foreach (var (file, line, method) in calls)
        {
            var fileName = Path.GetFileName(file);
            if (!IgnoreQueryFiltersAllowlist.ContainsKey(fileName))
            {
                violations.Add($"{file}:{line} — method {method} — unclassified IgnoreQueryFilters() call");
            }
        }

        violations.Should().BeEmpty(
            $"All IgnoreQueryFilters() calls in production code must be classified in IgnoreQueryFiltersAllowlist. " +
            $"Add new entries with AllowlistClassification and reason. " +
            $"Violations: {string.Join("; ", violations)}");
    }

    [Fact]
    public void IgnoreQueryFiltersAllowlist_ShouldHaveNoDuplicateEntries()
    {
        var duplicates = IgnoreQueryFiltersAllowlist.Keys
            .GroupBy(k => k)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        duplicates.Should().BeEmpty(
            $"IgnoreQueryFiltersAllowlist must not contain duplicate entries. Duplicates: {string.Join(", ", duplicates)}");
    }

    [Fact]
    public void IgnoreQueryFiltersAllowlist_ShouldHaveNoFalsePositiveClassifications()
    {
        var falsePositives = IgnoreQueryFiltersAllowlist.Values
            .Where(e => e.Classification == AllowlistClassification.FalsePositive)
            .Select(e => e.RequestTypeName)
            .ToList();

        falsePositives.Should().BeEmpty(
            $"IgnoreQueryFiltersAllowlist must not contain FalsePositive entries. " +
            $"If an IgnoreQueryFilters() call is truly a false positive, remove it from production code instead. " +
            $"FalsePositives: {string.Join(", ", falsePositives)}");
    }
}
