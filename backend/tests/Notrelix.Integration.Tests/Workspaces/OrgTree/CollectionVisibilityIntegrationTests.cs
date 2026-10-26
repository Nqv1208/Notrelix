using Microsoft.Extensions.DependencyInjection;
using Notrelix.Application.Features.Documents.Pages.Queries.GetPage;
using Notrelix.Application.Features.Documents.Pages.Queries.GetPageTree;
using Notrelix.Application.Features.Documents.Pages.Queries.GetWorkspacePages;
using Notrelix.Application.Features.Documents.Pages.Queries.SearchPages;
using Notrelix.Application.Features.WorkManagement.BoardItems.Queries.GetMyBoardItems;
using Notrelix.Application.Features.WorkManagement.Boards.Queries.GetBoard;
using Notrelix.Application.Features.WorkManagement.Boards.Queries.GetBoards;
using Notrelix.Application.Features.WorkManagement.Boards.Queries.GetBoardsBySlug;
using Notrelix.Application.Features.Workspaces.Spaces.Queries.GetSpace;
using Notrelix.Application.Features.Workspaces.Spaces.Queries.GetWorkspaceSpaces;
using Notrelix.Application.Features.Workspaces.WorkspaceHome.Queries.GetWorkspaceDashboard;
using Notrelix.Domain.Governance.Permissions;
using Notrelix.Domain.WorkManagement.Boards;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Domain.Workspaces.Spaces;
using Notrelix.Integration.Tests.Containers;
using AppForbidden = Notrelix.Application.Common.Exceptions.ForbiddenException;
using AppNotFound = Notrelix.Application.Common.Exceptions.NotFoundException;

namespace Notrelix.Integration.Tests.Workspaces.OrgTree;

/// <summary>
/// GOV-029 / BE-SEC-028: collection reads (Board lists by Workspace and slug,
/// My Work, Page list/tree/search, Space list, dashboard Space count) apply the
/// same decision a single read of each resource makes, in the datastore, on
/// PostgreSQL through the real pipeline. The parity tests pin every list to the
/// single-read engine decision across a principal × resource matrix.
/// </summary>
[Collection("Database")]
[Trait("Category", "Integration")]
public sealed class CollectionVisibilityIntegrationTests : IAsyncLifetime
{
    private readonly PostgresTestContainer _db;
    private readonly OrgTreeTestHost _host;

    public CollectionVisibilityIntegrationTests(PostgresTestContainer db)
    {
        _db = db;
        _host = new OrgTreeTestHost(db);
    }

    public async Task InitializeAsync() => await new DatabaseReset(_db.OwnerConnectionString).ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    // ── Boards ────────────────────────────────────────────────────────────

    [Fact]
    public async Task BoardLists_HideAPrivateBoard_FromAWorkspaceMemberWithoutAccess()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var member = await _host.AddMemberAsync(stack, WorkspaceRole.Member);
        var open = await _host.SeedBoardAsync(stack, "Open");
        var secret = await _host.SeedBoardAsync(stack, "Secret", BoardVisibility.Private);

        using var memberProvider = _host.CreateProvider(stack.AccountId, member);
        var byWorkspace = await OrgTreeTestHost.QueryAsync(memberProvider, new GetBoardsQuery(stack.WorkspaceId));
        var bySlug = await OrgTreeTestHost.QueryAsync(memberProvider, new GetBoardsBySlugQuery(stack.WorkspaceId, stack.Slug));

        byWorkspace.Select(b => b.Id).Should().BeEquivalentTo([open]);
        bySlug.Select(b => b.Id).Should().BeEquivalentTo([open]);
        var single = () => OrgTreeTestHost.SendAsync(memberProvider, new GetBoardQuery(secret));
        await single.Should().ThrowAsync<AppNotFound>("the single read hides the same board");

        using var ownerProvider = _host.CreateProvider(stack.AccountId, stack.OwnerId);
        (await OrgTreeTestHost.QueryAsync(ownerProvider, new GetBoardsQuery(stack.WorkspaceId)))
            .Select(b => b.Id).Should().BeEquivalentTo([open, secret], "the Workspace Owner reads every board");
    }

    [Fact]
    public async Task BoardLists_HideABoardInASpaceWithoutClosureRows_ThatTheSingleReadHides()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var member = await _host.AddMemberAsync(stack, WorkspaceRole.Member);
        var space = await _host.SeedSpaceAsync(stack, "Accounting", SpaceVisibility.Workspace, stack.OwnerId, type: SpaceType.Department);
        var board = await _host.SeedBoardAsync(stack, "Ledger", spaceId: space);
        await _host.WithSystemContextAsync(async seed =>
        {
            var paths = seed.SpacePaths.Where(p => p.DescendantId == space);
            seed.SpacePaths.RemoveRange(paths);
            await Task.CompletedTask;
        });

        using var memberProvider = _host.CreateProvider(stack.AccountId, member);
        var byWorkspace = await OrgTreeTestHost.QueryAsync(memberProvider, new GetBoardsQuery(stack.WorkspaceId));
        var single = () => OrgTreeTestHost.SendAsync(memberProvider, new GetBoardQuery(board));

        byWorkspace.Select(b => b.Id).Should().NotContain(board, "the list must not show what the single read hides");
        await single.Should().ThrowAsync<AppNotFound>();
    }

    [Fact]
    public async Task BoardLists_AreDeniedToANonMember()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var outsider = await _host.AddOutsiderAsync(stack);
        await _host.SeedBoardAsync(stack, "Open");

        using var provider = _host.CreateProvider(stack.AccountId, outsider);
        var list = () => OrgTreeTestHost.SendAsync(provider, new GetBoardsQuery(stack.WorkspaceId));

        await list.Should().ThrowAsync<AppForbidden>();
    }

    [Fact]
    public async Task BoardList_Parity_EqualsTheSingleReadDecision_ForEveryPrincipalAndBoard()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var admin = await _host.AddMemberAsync(stack, WorkspaceRole.Admin, "Admin");
        var member = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Member");
        var boardMember = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "BoardMember");
        var grantee = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Grantee");
        var denied = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Denied");
        var guest = await _host.AddMemberAsync(stack, WorkspaceRole.Guest, "Guest");

        var open = await _host.SeedBoardAsync(stack, "Open");
        var secret = await _host.SeedBoardAsync(stack, "Secret", BoardVisibility.Private);
        var shared = await _host.SeedBoardAsync(stack, "Shared", BoardVisibility.Private);
        var publicLink = await _host.SeedBoardAsync(stack, "PublicLink", BoardVisibility.PublicLink);
        var archived = await _host.SeedBoardAsync(stack, "Archived", transition: b => b.Archive(stack.OwnerId, OrgTreeTestHost.BaseTime));

        await _host.SeedBoardMemberAsync(secret, boardMember, BoardRole.Member);
        await _host.SeedResourcePermissionAsync(stack, "work-management.board", shared, PermissionSubjectType.User, grantee, PermissionLevel.Viewer);
        await _host.SeedResourcePermissionAsync(stack, "work-management.board", open, PermissionSubjectType.User, guest, PermissionLevel.Viewer);
        await _host.SeedRuleAsync(stack, PermissionSubjectType.User, denied, PermissionAction.ViewBoard, PermissionEffect.Deny, "work-management.board", open);
        await _host.SeedRuleAsync(stack, PermissionSubjectType.User, denied, PermissionAction.ViewBoard, PermissionEffect.Allow, "work-management.board", secret);

        var boards = new[] { open, secret, shared, publicLink, archived };
        var expected = new Dictionary<Guid, Guid[]>
        {
            [stack.OwnerId] = [open, secret, shared, publicLink],
            [admin] = [open],
            [member] = [open],
            [boardMember] = [open, secret],
            [grantee] = [open, shared],
            [denied] = [secret],
            [guest] = [open],
        };

        foreach (var (principal, visible) in expected)
        {
            using var provider = _host.CreateProvider(stack.AccountId, principal);
            var listed = (await OrgTreeTestHost.QueryAsync(provider, new GetBoardsQuery(stack.WorkspaceId))).Select(b => b.Id).ToList();
            var single = new List<Guid>();
            foreach (var board in boards)
            {
                if (await CanReadAsync(provider, new GetBoardQuery(board)))
                    single.Add(board);
            }

            listed.Should().BeEquivalentTo(single, $"list membership equals the single-read decision for principal {principal}");
            listed.Should().BeEquivalentTo(visible, $"principal {principal} sees exactly the expected boards");
        }
    }

    [Fact]
    public async Task MyWork_HidesAssignedItems_OnABoardTheAssigneeCannotRead()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var member = await _host.AddMemberAsync(stack, WorkspaceRole.Member);
        var open = await _host.SeedBoardAsync(stack, "Open");
        var secret = await _host.SeedBoardAsync(stack, "Secret", BoardVisibility.Private);
        var visibleItem = await _host.SeedAssignedItemAsync(stack, open, member, "Visible task");
        await _host.SeedAssignedItemAsync(stack, secret, member, "Hidden task");

        using var provider = _host.CreateProvider(stack.AccountId, member);
        var page = await OrgTreeTestHost.QueryAsync(provider, new GetMyBoardItemsQuery(stack.WorkspaceId));

        page.Items.Select(i => i.Id).Should().BeEquivalentTo([visibleItem]);
    }

    // ── Pages ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task PageList_TreeAndSearch_Parity_EqualsTheSingleReadDecision()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var member = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Member");
        var grantee = await _host.AddMemberAsync(stack, WorkspaceRole.Guest, "Grantee");
        var denied = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Denied");

        var root = await _host.SeedPageAsync(stack, "Handbook root");
        var child = await _host.SeedPageAsync(stack, "Handbook child", root);
        var archived = await _host.SeedPageAsync(stack, "Handbook archived", transition: p => p.Archive(stack.OwnerId, OrgTreeTestHost.BaseTime));
        await _host.SeedResourcePermissionAsync(stack, "documents.page", child, PermissionSubjectType.User, grantee, PermissionLevel.Viewer);
        await _host.SeedRuleAsync(stack, PermissionSubjectType.User, denied, PermissionAction.ViewPage, PermissionEffect.Deny, "documents.page", root);

        var pages = new[] { root, child, archived };
        var expected = new Dictionary<Guid, Guid[]>
        {
            [stack.OwnerId] = [root, child],
            [member] = [root, child],
            [grantee] = [child],
            [denied] = [child],
        };

        foreach (var (principal, visible) in expected)
        {
            using var provider = _host.CreateProvider(stack.AccountId, principal);
            var single = new List<Guid>();
            foreach (var pageId in pages)
            {
                if (await CanReadAsync(provider, new GetPageQuery(pageId)))
                    single.Add(pageId);
            }

            var listed = (await OrgTreeTestHost.QueryAsync(provider, new GetWorkspacePagesQuery(stack.WorkspaceId))).Select(p => p.Id);
            var tree = await OrgTreeTestHost.QueryAsync(provider, new GetPageTreeQuery(stack.WorkspaceId));
            var found = (await OrgTreeTestHost.QueryAsync(provider, new SearchPagesQuery(stack.WorkspaceId, "handbook"))).Select(p => p.Id);

            single.Should().BeEquivalentTo(visible, $"principal {principal} single reads");
            listed.Should().BeEquivalentTo(single, "the page list applies the single-read decision");
            tree.Select(t => t.Id).Should().BeEquivalentTo(single, "the page tree applies the single-read decision");
            found.Should().BeEquivalentTo(single, "search never reveals a page the caller cannot read");
            foreach (var node in tree)
            {
                if (node.ParentId is { } parentId)
                    single.Should().Contain(parentId, "a hidden parent id is never disclosed through the tree");
            }
        }
    }

    // ── Spaces ────────────────────────────────────────────────────────────

    [Fact]
    public async Task SpaceList_SingleRead_AndDashboardCount_HideAPrivateSpace_FromOtherMembers()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var admin = await _host.AddMemberAsync(stack, WorkspaceRole.Admin, "Admin");
        var creator = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Creator");
        var member = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Member");
        var guest = await _host.AddMemberAsync(stack, WorkspaceRole.Guest, "Guest");

        var open = await SeedSpaceAsync(stack, "Company", SpaceVisibility.Workspace, stack.OwnerId);
        var department = await SeedSpaceAsync(stack, "Accounting", SpaceVisibility.Private, creator);

        var expected = new Dictionary<Guid, Guid[]>
        {
            [stack.OwnerId] = [open, department],
            [admin] = [open, department],
            [creator] = [open, department],
            [member] = [open],
            [guest] = [],
        };

        foreach (var (principal, visible) in expected)
        {
            using var provider = _host.CreateProvider(stack.AccountId, principal);
            var listed = await OrgTreeTestHost.QueryAsync(provider, new GetWorkspaceSpacesQuery(stack.WorkspaceId));
            listed.Select(s => s.Id).Should().BeEquivalentTo(visible, $"principal {principal} lists only visible Spaces");

            var single = new List<Guid>();
            foreach (var spaceId in new[] { open, department })
            {
                if (await CanReadAsync(provider, new GetSpaceQuery(stack.WorkspaceId, spaceId)))
                    single.Add(spaceId);
            }

            single.Should().BeEquivalentTo(visible, "a single Space read hides what the list hides (NotFound)");
            (await OrgTreeTestHost.QueryAsync(provider, new GetWorkspaceDashboardQuery(stack.WorkspaceId)))
                .SpaceCount.Should().Be(visible.Length, "the dashboard does not count hidden Spaces");
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private static async Task<bool> CanReadAsync(ServiceProvider provider, object request)
    {
        try
        {
            await OrgTreeTestHost.SendAsync(provider, request);
            return true;
        }
        catch (AppNotFound)
        {
            return false;
        }
        catch (AppForbidden)
        {
            return false;
        }
    }

    private Task<Guid> SeedSpaceAsync(OrgStack stack, string name, SpaceVisibility visibility, Guid createdBy) =>
        _host.SeedSpaceAsync(stack, name, visibility, createdBy);
}
