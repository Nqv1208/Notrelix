using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Notrelix.Application.Features.Workspaces.Spaces.Commands.AddSpaceMember;
using Notrelix.Application.Features.Workspaces.Spaces.Commands.ArchiveSpace;
using Notrelix.Application.Features.Workspaces.Spaces.Commands.CreateSpace;
using Notrelix.Application.Features.Workspaces.Spaces.Commands.DeleteSpace;
using Notrelix.Application.Features.Workspaces.Spaces.Commands.MoveSpace;
using Notrelix.Application.Features.Workspaces.Spaces.Commands.RemoveSpaceMember;
using Notrelix.Application.Features.Workspaces.Spaces.Queries.GetSpaceMembers;
using Notrelix.Application.Features.Workspaces.Spaces.Queries.GetSpaceTree;
using Notrelix.Domain.Workspaces;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Domain.Workspaces.Spaces;
using Notrelix.Domain.Workspaces.Teams;
using Notrelix.Infrastructure.Data.Workspaces;
using Notrelix.Integration.Tests.Containers;
using DomainBusinessRuleException = Notrelix.Domain.Common.Exceptions.BusinessRuleException;
using PreconditionFailed = Notrelix.Application.Common.Exceptions.PreconditionFailedException;
using AppNotFound = Notrelix.Application.Common.Exceptions.NotFoundException;

namespace Notrelix.Integration.Tests.Workspaces.OrgTree;

/// <summary>
/// Space org tree on PostgreSQL through the real pipeline (WSP-021..WSP-024,
/// ADR-009): the closure index stays exact across create and subtree moves, a
/// move into the own subtree and concurrent opposite moves cannot create a
/// cycle, stale versions are rejected without change, archive/delete never
/// cascade, membership keeps a last Admin, and the migration's existing-data
/// steps backfill legacy Spaces deterministically.
/// </summary>
[Collection("Database")]
[Trait("Category", "Integration")]
public sealed class SpaceHierarchyIntegrationTests : IAsyncLifetime
{
    private readonly PostgresTestContainer _db;
    private readonly OrgTreeTestHost _host;

    public SpaceHierarchyIntegrationTests(PostgresTestContainer db)
    {
        _db = db;
        _host = new OrgTreeTestHost(db);
    }

    public async Task InitializeAsync() => await new DatabaseReset(_db.OwnerConnectionString).ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    // ── Closure index ─────────────────────────────────────────────────────

    [Fact]
    public async Task CreatingANestedTree_RecordsEveryAncestorWithItsDepth()
    {
        var stack = await _host.SeedWorkspaceAsync();
        using var owner = _host.CreateProvider(stack.AccountId, stack.OwnerId);

        var it = await CreateAsync(owner, stack, "IT", type: "Department");
        var security = await CreateAsync(owner, stack, "Security", it);
        var soc = await CreateAsync(owner, stack, "SOC", security);

        (await PathsAsync(stack)).Should().BeEquivalentTo(new[]
        {
            (it, it, 0), (security, security, 0), (soc, soc, 0),
            (it, security, 1), (security, soc, 1), (it, soc, 2),
        });
        (await SpaceAsync(it)).Visibility.Should().Be(SpaceVisibility.Private, "a department Space is Private by default");
        (await SpaceAsync(it)).Members.Should().ContainSingle(m => m.SubjectId == stack.OwnerId && m.Role == SpaceRole.Admin);
    }

    [Fact]
    public async Task MovingASubtree_RepointsOnlyItsExternalAncestors()
    {
        var stack = await _host.SeedWorkspaceAsync();
        using var owner = _host.CreateProvider(stack.AccountId, stack.OwnerId);
        var it = await CreateAsync(owner, stack, "IT");
        var security = await CreateAsync(owner, stack, "Security", it);
        var soc = await CreateAsync(owner, stack, "SOC", security);
        var finance = await CreateAsync(owner, stack, "Finance");
        var audit = await CreateAsync(owner, stack, "Audit", finance);

        await OrgTreeTestHost.SendAsync(owner, new MoveSpaceCommand(stack.WorkspaceId, security, audit, null, null, (await SpaceAsync(security)).Version));

        (await SpaceAsync(security)).ParentSpaceId.Should().Be(audit);
        (await PathsAsync(stack)).Should().BeEquivalentTo(new[]
        {
            (it, it, 0), (security, security, 0), (soc, soc, 0), (finance, finance, 0), (audit, audit, 0),
            (finance, audit, 1),
            (audit, security, 1), (finance, security, 2),
            (security, soc, 1), (audit, soc, 2), (finance, soc, 3),
        });

        await OrgTreeTestHost.SendAsync(owner, new MoveSpaceCommand(stack.WorkspaceId, security, null, null, null, (await SpaceAsync(security)).Version));

        (await PathsAsync(stack)).Should().BeEquivalentTo(new[]
        {
            (it, it, 0), (security, security, 0), (soc, soc, 0), (finance, finance, 0), (audit, audit, 0),
            (finance, audit, 1), (security, soc, 1),
        }, "moving to the root drops every external ancestor");
    }

    [Fact]
    public async Task MovingASpaceIntoItsOwnSubtree_IsRejected_AndLeavesTreeAndVersionUnchanged()
    {
        var stack = await _host.SeedWorkspaceAsync();
        using var owner = _host.CreateProvider(stack.AccountId, stack.OwnerId);
        var it = await CreateAsync(owner, stack, "IT");
        var security = await CreateAsync(owner, stack, "Security", it);
        var soc = await CreateAsync(owner, stack, "SOC", security);
        var before = await PathsAsync(stack);
        var version = (await SpaceAsync(it)).Version;

        var act = () => OrgTreeTestHost.SendAsync(owner, new MoveSpaceCommand(stack.WorkspaceId, it, soc, null, null, version));

        (await act.Should().ThrowAsync<DomainBusinessRuleException>()).Which.RuleCode.Should().Be(WorkspaceRuleCodes.Workspaces_Space_MoveCreatesCycle);
        (await PathsAsync(stack)).Should().BeEquivalentTo(before);
        (await SpaceAsync(it)).Version.Should().Be(version);
        (await SpaceAsync(it)).ParentSpaceId.Should().BeNull();
    }

    [Fact]
    public async Task AStaleExpectedVersion_IsRejected_WithoutMoving()
    {
        var stack = await _host.SeedWorkspaceAsync();
        using var owner = _host.CreateProvider(stack.AccountId, stack.OwnerId);
        var it = await CreateAsync(owner, stack, "IT");
        var finance = await CreateAsync(owner, stack, "Finance");
        var stale = (await SpaceAsync(it)).Version;
        var member = await _host.AddMemberAsync(stack, WorkspaceRole.Member);
        await OrgTreeTestHost.SendAsync(owner, new AddSpaceMemberCommand(stack.WorkspaceId, it, "User", member, "Viewer", stale));
        (await SpaceAsync(it)).Version.Should().BeGreaterThan(stale, "a membership change is a versioned Space mutation");

        var act = () => OrgTreeTestHost.SendAsync(owner, new MoveSpaceCommand(stack.WorkspaceId, it, finance, null, null, stale));

        await act.Should().ThrowAsync<PreconditionFailed>();
        (await SpaceAsync(it)).ParentSpaceId.Should().BeNull();
    }

    [Fact]
    public async Task ConcurrentOppositeMoves_CannotFormACycle()
    {
        var stack = await _host.SeedWorkspaceAsync();
        using var setup = _host.CreateProvider(stack.AccountId, stack.OwnerId);
        var a = await CreateAsync(setup, stack, "A");
        var b = await CreateAsync(setup, stack, "B");
        var versionA = (await SpaceAsync(a)).Version;
        var versionB = (await SpaceAsync(b)).Version;

        using var first = _host.CreateProvider(stack.AccountId, stack.OwnerId);
        using var second = _host.CreateProvider(stack.AccountId, stack.OwnerId);
        var outcomes = await Task.WhenAll(
            TryAsync(() => OrgTreeTestHost.SendAsync(first, new MoveSpaceCommand(stack.WorkspaceId, a, b, null, null, versionA))),
            TryAsync(() => OrgTreeTestHost.SendAsync(second, new MoveSpaceCommand(stack.WorkspaceId, b, a, null, null, versionB))));

        outcomes.Count(ok => ok).Should().Be(1, "the hierarchy lock serializes the moves; the second sees the first and is a cycle");
        var paths = await PathsAsync(stack);
        paths.Should().NotContain(p => p.Ancestor == p.Descendant && p.Depth > 0);
        paths.Should().NotContain(p => p.Ancestor == a && p.Descendant == b && paths.Any(q => q.Ancestor == b && q.Descendant == a && q.Depth > 0));
    }

    [Fact]
    public async Task SiblingNames_AreUniqueCaseInsensitively_PerParent()
    {
        var stack = await _host.SeedWorkspaceAsync();
        using var owner = _host.CreateProvider(stack.AccountId, stack.OwnerId);
        var it = await CreateAsync(owner, stack, "IT");
        await CreateAsync(owner, stack, "Projects", it);
        await CreateAsync(owner, stack, "Projects");

        var duplicate = () => CreateAsync(owner, stack, "projects", it);

        (await duplicate.Should().ThrowAsync<DomainBusinessRuleException>()).Which.RuleCode.Should().Be(WorkspaceRuleCodes.Workspaces_Space_DuplicateSiblingName);
    }

    [Fact]
    public async Task GetSpaceTree_ReturnsParentsAndSiblingOrder()
    {
        var stack = await _host.SeedWorkspaceAsync();
        using var owner = _host.CreateProvider(stack.AccountId, stack.OwnerId);
        var it = await CreateAsync(owner, stack, "IT");
        var network = await CreateAsync(owner, stack, "Network", it);
        var security = await CreateAsync(owner, stack, "Security", it, previous: null, next: network);

        var tree = await OrgTreeTestHost.QueryAsync(owner, new GetSpaceTreeQuery(stack.WorkspaceId));

        tree.Where(n => n.ParentSpaceId == it).OrderBy(n => n.Position, StringComparer.Ordinal).Select(n => n.Id)
            .Should().Equal(security, network);
        tree.Single(n => n.Id == it).ParentSpaceId.Should().BeNull();
    }

    // ── Archive / delete never cascade (WSP-022) ──────────────────────────

    [Fact]
    public async Task ArchiveAndDelete_AreBlockedByActiveChildrenAndPlacedBoards_WithoutCascade()
    {
        var stack = await _host.SeedWorkspaceAsync();
        using var owner = _host.CreateProvider(stack.AccountId, stack.OwnerId);
        var it = await CreateAsync(owner, stack, "IT");
        var security = await CreateAsync(owner, stack, "Security", it);
        var finance = await CreateAsync(owner, stack, "Finance");
        await _host.SeedBoardAsync(stack, "Budget", spaceId: finance);

        var archiveParent = () => OrgTreeTestHost.SendAsync(owner, new ArchiveSpaceCommand(stack.WorkspaceId, it));
        (await archiveParent.Should().ThrowAsync<DomainBusinessRuleException>()).Which.RuleCode.Should().Be(WorkspaceRuleCodes.Workspaces_Space_CannotArchiveWithActiveContent);

        var archiveWithBoard = () => OrgTreeTestHost.SendAsync(owner, new ArchiveSpaceCommand(stack.WorkspaceId, finance));
        (await archiveWithBoard.Should().ThrowAsync<DomainBusinessRuleException>()).Which.RuleCode.Should().Be(WorkspaceRuleCodes.Workspaces_Space_CannotArchiveWithActiveContent);

        await OrgTreeTestHost.SendAsync(owner, new ArchiveSpaceCommand(stack.WorkspaceId, security));
        await OrgTreeTestHost.SendAsync(owner, new ArchiveSpaceCommand(stack.WorkspaceId, it));
        (await SpaceAsync(it)).Status.Should().Be(SpaceStatus.Archived, "only archived children remain");

        var deleteWithArchivedChild = () => OrgTreeTestHost.SendAsync(owner, new DeleteSpaceCommand(stack.WorkspaceId, it));
        (await deleteWithArchivedChild.Should().ThrowAsync<DomainBusinessRuleException>()).Which.RuleCode.Should().Be(WorkspaceRuleCodes.Workspaces_Space_CannotDeleteWithContent);
        (await _host.ReadAsync(c => c.Spaces.IgnoreQueryFilters().Where(s => s.Id == security).Select(s => s.DeletedAt).SingleAsync()))
            .Should().BeNull("nothing is deleted by cascade");
    }

    // ── Membership (WSP-024) ──────────────────────────────────────────────

    [Fact]
    public async Task Membership_AcceptsSameWorkspaceUsersAndTeams_AndKeepsALastAdmin()
    {
        var stack = await _host.SeedWorkspaceAsync();
        using var owner = _host.CreateProvider(stack.AccountId, stack.OwnerId);
        var accountant = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Accountant");
        var outsider = await _host.AddOutsiderAsync(stack);
        var team = Team.Create(stack.AccountId, stack.WorkspaceId, "Finance team", stack.OwnerId, OrgTreeTestHost.BaseTime);
        await _host.WithSystemContextAsync(c => { c.Teams.Add(team); return Task.CompletedTask; });
        var accounting = await CreateAsync(owner, stack, "Accounting", type: "Department");

        await OrgTreeTestHost.SendAsync(owner, new AddSpaceMemberCommand(stack.WorkspaceId, accounting, "User", accountant, "Member", await VersionAsync(accounting)));
        await OrgTreeTestHost.SendAsync(owner, new AddSpaceMemberCommand(stack.WorkspaceId, accounting, "Team", team.Id, "Viewer", await VersionAsync(accounting)));

        var version = await VersionAsync(accounting);
        var addOutsider = () => OrgTreeTestHost.SendAsync(owner, new AddSpaceMemberCommand(stack.WorkspaceId, accounting, "User", outsider, "Member", version));
        await addOutsider.Should().ThrowAsync<AppNotFound>("a non-member of the Workspace is not a Space subject (WSP-014)");

        var removeLastAdmin = () => OrgTreeTestHost.SendAsync(owner, new RemoveSpaceMemberCommand(stack.WorkspaceId, accounting, "User", stack.OwnerId, version));
        (await removeLastAdmin.Should().ThrowAsync<DomainBusinessRuleException>()).Which.RuleCode.Should().Be(WorkspaceRuleCodes.Workspaces_Space_CannotRemoveLastAdmin);
        (await VersionAsync(accounting)).Should().Be(version, "a rejected change leaves the Space unchanged");

        var members = await OrgTreeTestHost.QueryAsync(owner, new GetSpaceMembersQuery(stack.WorkspaceId, accounting));
        members.Select(m => (m.SubjectType, m.SubjectId, m.Role)).Should().BeEquivalentTo(new[]
        {
            ("User", stack.OwnerId, "Admin"),
            ("User", accountant, "Member"),
            ("Team", team.Id, "Viewer"),
        });
        members.Single(m => m.SubjectType == "Team").DisplayName.Should().Be("Finance team");
    }

    // ── Migration existing-data steps ─────────────────────────────────────

    [Fact]
    public async Task MigrationBackfill_GivesLegacySpacesOrderedKeys_ClosureSelfRows_AndCreatorAdmins()
    {
        var accountId = Guid.NewGuid();
        var wsA = Guid.NewGuid();
        var wsB = Guid.NewGuid();
        var creator = Guid.NewGuid();
        var (beta, alpha, gamma, other, noCreator) = (Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        await using var connection = new NpgsqlConnection(_db.OwnerConnectionString);
        await connection.OpenAsync();
        await using var tx = await connection.BeginTransactionAsync();

        // Simulate the pre-migration shape: legacy rows hold the column default
        // and no closure/member rows; the unique index does not exist yet.
        await ExecAsync(connection, tx, "DROP INDEX workspace.ux_workspace_spaces_parent_position_active");
        foreach (var (id, ws, name, by) in new[]
                 {
                     (beta, wsA, "beta", (Guid?)creator), (alpha, wsA, "Alpha", creator), (gamma, wsA, "Gamma", creator),
                     (other, wsB, "Other", creator), (noCreator, wsB, "Legacy", (Guid?)null),
                 })
        {
            await ExecAsync(connection, tx, $"""
                INSERT INTO workspace.spaces (id, account_id, workspace_id, name, visibility, status, space_type, position, created_at, created_by, version)
                VALUES ('{id}', '{accountId}', '{ws}', '{name}', 'Private', 'Active', 'Folder', '', now(), {(by is null ? "NULL" : $"'{by}'")}, 1)
                """);
        }

        await ExecAsync(connection, tx, SpaceHierarchyMigrationSql.BackfillPositions);
        await ExecAsync(connection, tx, SpaceHierarchyMigrationSql.BackfillClosure);
        await ExecAsync(connection, tx, SpaceHierarchyMigrationSql.BackfillCreatorAdmins);
        // Idempotent: a second run changes nothing.
        await ExecAsync(connection, tx, SpaceHierarchyMigrationSql.BackfillPositions);
        await ExecAsync(connection, tx, SpaceHierarchyMigrationSql.BackfillClosure);
        await ExecAsync(connection, tx, SpaceHierarchyMigrationSql.BackfillCreatorAdmins);

        var positions = await QueryAsync(connection, tx, $"SELECT id::text || '=' || position FROM workspace.spaces WHERE workspace_id IN ('{wsA}', '{wsB}') ORDER BY workspace_id, position COLLATE \"C\"");
        positions.Should().Equal(
            wsA.CompareTo(wsB) < 0
                ? [$"{alpha}=a0", $"{beta}=a1", $"{gamma}=a2", $"{noCreator}=a0", $"{other}=a1"]
                : [$"{noCreator}=a0", $"{other}=a1", $"{alpha}=a0", $"{beta}=a1", $"{gamma}=a2"],
            "siblings get distinct keys in case-insensitive name order");
        (await QueryAsync(connection, tx, $"SELECT count(*)::text FROM workspace.space_paths WHERE workspace_id IN ('{wsA}', '{wsB}') AND ancestor_id = descendant_id AND depth = 0"))
            .Should().Equal("5");
        (await QueryAsync(connection, tx, $"SELECT space_id::text || ':' || role FROM workspace.space_members WHERE workspace_id IN ('{wsA}', '{wsB}') ORDER BY 1"))
            .Should().BeEquivalentTo([$"{alpha}:Admin", $"{beta}:Admin", $"{gamma}:Admin", $"{other}:Admin"], "a Space without a recorded creator gets no guessed Admin");

        await tx.RollbackAsync();
    }

    [Fact]
    public async Task MigrationPreflight_FailsClosed_OnABoardPlacedInAnotherWorkspacesSpace()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var foreign = await _host.SeedWorkspaceAsync();
        var foreignSpace = Space.Create(foreign.AccountId, foreign.WorkspaceId, "Foreign", SpaceVisibility.Private, foreign.OwnerId, OrgTreeTestHost.BaseTime);
        await _host.WithSystemContextAsync(c => { c.Spaces.Add(foreignSpace); return Task.CompletedTask; });
        var board = await _host.SeedBoardAsync(stack, "Misplaced");

        await using var connection = new NpgsqlConnection(_db.OwnerConnectionString);
        await connection.OpenAsync();
        await using var tx = await connection.BeginTransactionAsync();
        await ExecAsync(connection, tx, $"UPDATE work.boards SET space_id = '{foreignSpace.Id}' WHERE id = '{board}'");

        var preflight = () => ExecAsync(connection, tx, SpaceHierarchyMigrationSql.Preflight);

        (await preflight.Should().ThrowAsync<PostgresException>()).Which.MessageText.Should().StartWith(SpaceHierarchyMigrationSql.ErrorPrefix);
        await tx.RollbackAsync();
    }

    [Fact]
    public async Task PlacementColumns_HaveNoCrossContextForeignKey_ButKeepTheirIndex()
    {
        // Space containment is not a cross-context object graph (docs/product/workspaces.md §79):
        // Boards and Pages reference a Space by scalar id only; existence is validated in Application.
        await using var connection = new NpgsqlConnection(_db.OwnerConnectionString);
        await connection.OpenAsync();
        await using var tx = await connection.BeginTransactionAsync();

        (await QueryAsync(connection, tx, """
            SELECT n.nspname || '.' || c.relname
            FROM pg_constraint k
            JOIN pg_class c ON c.oid = k.conrelid
            JOIN pg_namespace n ON n.oid = c.relnamespace
            JOIN pg_class t ON t.oid = k.confrelid
            JOIN pg_namespace tn ON tn.oid = t.relnamespace
            WHERE k.contype = 'f' AND tn.nspname = 'workspace' AND t.relname = 'spaces'
              AND n.nspname IN ('work', 'docs')
            """))
            .Should().BeEmpty("work.boards and docs.pages hold no foreign key to workspace.spaces");

        (await QueryAsync(connection, tx, """
            SELECT schemaname || '.' || tablename || ':' || indexname
            FROM pg_indexes
            WHERE indexname IN ('ix_boards_space_id', 'ix_pages_space_id')
            ORDER BY 1
            """))
            .Should().Equal("docs.pages:ix_pages_space_id", "work.boards:ix_boards_space_id");

        (await QueryAsync(connection, tx, """
            SELECT table_schema || '.' || table_name FROM information_schema.columns
            WHERE column_name = 'space_id' AND ((table_schema = 'work' AND table_name = 'boards')
                                             OR (table_schema = 'docs' AND table_name = 'pages'))
            ORDER BY 1
            """))
            .Should().Equal("docs.pages", "work.boards");

        await tx.RollbackAsync();
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private static async Task<Guid> CreateAsync(
        ServiceProvider provider, OrgStack stack, string name, Guid? parent = null, string? type = null, Guid? previous = null, Guid? next = null) =>
        await OrgTreeTestHost.QueryAsync(provider, new CreateSpaceCommand(stack.WorkspaceId, name, null, null, type, parent, previous, next));

    private Task<Space> SpaceAsync(Guid id) =>
        _host.ReadAsync(c => c.Spaces.Include(s => s.Members).SingleAsync(s => s.Id == id));

    private async Task<long> VersionAsync(Guid id) => (await SpaceAsync(id)).Version;

    private Task<List<(Guid Ancestor, Guid Descendant, int Depth)>> PathsAsync(OrgStack stack) =>
        _host.ReadAsync(async c => (await c.SpacePaths
                .Where(p => p.WorkspaceId == stack.WorkspaceId)
                .Select(p => new { p.AncestorId, p.DescendantId, p.Depth })
                .ToListAsync())
            .Select(p => (p.AncestorId, p.DescendantId, p.Depth))
            .ToList());

    private static async Task<bool> TryAsync(Func<Task> action)
    {
        try
        {
            await action();
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static async Task ExecAsync(NpgsqlConnection connection, NpgsqlTransaction tx, string sql)
    {
        await using var command = new NpgsqlCommand(sql, connection, tx);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task<List<string>> QueryAsync(NpgsqlConnection connection, NpgsqlTransaction tx, string sql)
    {
        await using var command = new NpgsqlCommand(sql, connection, tx);
        await using var reader = await command.ExecuteReaderAsync();
        var rows = new List<string>();
        while (await reader.ReadAsync())
            rows.Add(reader.GetString(0));
        return rows;
    }
}
