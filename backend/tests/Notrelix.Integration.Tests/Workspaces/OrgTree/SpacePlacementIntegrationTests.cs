using Microsoft.Extensions.DependencyInjection;
using Notrelix.Application.Features.Documents.Pages.Commands.CreatePage;
using Notrelix.Application.Features.Documents.Pages.Commands.MovePageToSpace;
using Notrelix.Application.Features.Documents.Pages.Queries.GetPage;
using Notrelix.Application.Features.WorkManagement.Boards.Commands.CreateBoardInWorkspace;
using Notrelix.Application.Features.WorkManagement.Boards.Commands.MoveBoardToSpace;
using Notrelix.Application.Features.WorkManagement.Boards.Queries.GetBoard;
using Notrelix.Application.Features.WorkManagement.Boards.Queries.GetBoards;
using Notrelix.Application.Features.WorkManagement.Templates.Commands.CreateBoardFromTemplate;
using Notrelix.Application.Features.Workspaces.Spaces.Commands.ArchiveSpace;
using Notrelix.Domain.Documents;
using Notrelix.Domain.Governance.Permissions;
using Notrelix.Domain.WorkManagement.Templates;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Domain.Workspaces.Spaces;
using Notrelix.Integration.Tests.Containers;
using AppConflict = Notrelix.Application.Common.Exceptions.ConflictException;
using AppForbidden = Notrelix.Application.Common.Exceptions.ForbiddenException;
using AppNotFound = Notrelix.Application.Common.Exceptions.NotFoundException;
using DomainBusinessRuleException = Notrelix.Domain.Common.Exceptions.BusinessRuleException;
using PreconditionFailed = Notrelix.Application.Common.Exceptions.PreconditionFailedException;

namespace Notrelix.Integration.Tests.Workspaces.OrgTree;

/// <summary>
/// Placement of Boards and Pages in Spaces on PostgreSQL through the real
/// pipeline (docs/product/workspaces.md §79, GOV-028): creating in a Space is
/// authorized on the Space, moving is authorized on the source and on the
/// target, archived targets are refused, versions guard moves, a nested page
/// follows its root page, and a hidden Space's id is never disclosed.
/// </summary>
[Collection("Database")]
[Trait("Category", "Integration")]
public sealed class SpacePlacementIntegrationTests : IAsyncLifetime
{
    private readonly PostgresTestContainer _db;
    private readonly OrgTreeTestHost _host;

    public SpacePlacementIntegrationTests(PostgresTestContainer db)
    {
        _db = db;
        _host = new OrgTreeTestHost(db);
    }

    public async Task InitializeAsync() => await new DatabaseReset(_db.ConnectionString).ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    private sealed record Setup(OrgStack Stack, Guid Accountant, Guid Viewer, Guid Outsider, Guid Accounting, Guid Marketing, Guid Archived);

    /// <summary>
    /// Accounting (Private): Admin = Accountant, Viewer = Viewer.
    /// Marketing (Private): Member = Accountant, Viewer = Viewer.
    /// Old (Private, archived): Admin = Owner.
    /// Outsider: a workspace Member in no Space.
    /// </summary>
    private async Task<Setup> SeedAsync()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var accountant = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Accountant");
        var viewer = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Viewer");
        var outsider = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Outsider");

        var accounting = await _host.SeedSpaceAsync(stack, "Accounting", SpaceVisibility.Private, accountant, type: SpaceType.Department,
            transition: s => s.AddMember(SpaceMemberSubjectType.User, viewer, SpaceRole.Viewer, accountant, OrgTreeTestHost.BaseTime));
        var marketing = await _host.SeedSpaceAsync(stack, "Marketing", SpaceVisibility.Private, stack.OwnerId, type: SpaceType.Department,
            transition: s =>
            {
                s.AddMember(SpaceMemberSubjectType.User, accountant, SpaceRole.Member, stack.OwnerId, OrgTreeTestHost.BaseTime);
                s.AddMember(SpaceMemberSubjectType.User, viewer, SpaceRole.Viewer, stack.OwnerId, OrgTreeTestHost.BaseTime);
            });
        var archived = await _host.SeedSpaceAsync(stack, "Old", SpaceVisibility.Private, stack.OwnerId,
            transition: s => s.Archive(stack.OwnerId, OrgTreeTestHost.BaseTime, SpaceContentFact.Empty));

        return new Setup(stack, accountant, viewer, outsider, accounting, marketing, archived);
    }

    // ── Boards ────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateBoardInASpace_IsAuthorizedOnTheSpace_AndPlacesTheBoardThere()
    {
        var s = await SeedAsync();
        using var accountant = _host.CreateProvider(s.Stack.AccountId, s.Accountant);
        using var viewer = _host.CreateProvider(s.Stack.AccountId, s.Viewer);
        using var outsider = _host.CreateProvider(s.Stack.AccountId, s.Outsider);
        using var owner = _host.CreateProvider(s.Stack.AccountId, s.Stack.OwnerId);

        var boardId = await CreateBoardAsync(accountant, s.Stack, "Ledger", s.Accounting);
        (await SpaceOfBoardAsync(boardId)).Should().Be(s.Accounting);
        (await OrgTreeTestHost.QueryAsync(accountant, new GetBoardsQuery(s.Stack.WorkspaceId)))
            .Single(b => b.Id == boardId).SpaceId.Should().Be(s.Accounting);
        (await OrgTreeTestHost.QueryAsync(outsider, new GetBoardsQuery(s.Stack.WorkspaceId)))
            .Should().NotContain(b => b.Id == boardId, "a board placed in a Private Space is hidden from non-members");

        var asViewer = () => CreateBoardAsync(viewer, s.Stack, "Viewer board", s.Accounting);
        await asViewer.Should().ThrowAsync<AppForbidden>("creating needs Editor level on the Space");
        var asOutsider = () => CreateBoardAsync(outsider, s.Stack, "Outsider board", s.Accounting);
        await asOutsider.Should().ThrowAsync<AppNotFound>("the Space is invisible to a non-member");
        var inArchived = () => CreateBoardAsync(owner, s.Stack, "Late", s.Archived);
        await inArchived.Should().ThrowAsync<AppConflict>("an archived Space accepts no new boards");
    }

    [Fact]
    public async Task CreateBoardFromATemplate_InASpace_PlacesTheBoardThere()
    {
        var s = await SeedAsync();
        await _host.SeedBoardAsync(s.Stack, "Existing");
        var template = BoardTemplate.Create("Budget template", Notrelix.Domain.SharedKernel.JsonValue.Create("{}")!, OrgTreeTestHost.BaseTime, s.Stack.WorkspaceId);
        await _host.WithSystemContextAsync(c => { c.BoardTemplates.Add(template); return Task.CompletedTask; });
        using var accountant = _host.CreateProvider(s.Stack.AccountId, s.Accountant);

        var boardId = ((Notrelix.Application.Common.Models.Result<Guid>)(await OrgTreeTestHost.SendIdempotentAsync(
            accountant, new CreateBoardFromTemplateCommand(template.Id, s.Stack.WorkspaceId, "Budget 2027", s.Accounting)))!).Data;

        (await SpaceOfBoardAsync(boardId)).Should().Be(s.Accounting);

        using var viewer = _host.CreateProvider(s.Stack.AccountId, s.Viewer);
        var asViewer = () => OrgTreeTestHost.SendIdempotentAsync(viewer, new CreateBoardFromTemplateCommand(template.Id, s.Stack.WorkspaceId, "Nope", s.Accounting));
        await asViewer.Should().ThrowAsync<AppForbidden>();
    }

    [Fact]
    public async Task MoveBoardToSpace_IsAuthorizedOnSourceAndTarget_AndGuardedByVersion()
    {
        var s = await SeedAsync();
        var board = await _host.SeedBoardAsync(s.Stack, "Campaign plan", spaceId: s.Accounting);
        using var accountant = _host.CreateProvider(s.Stack.AccountId, s.Accountant);
        using var viewer = _host.CreateProvider(s.Stack.AccountId, s.Viewer);
        using var owner = _host.CreateProvider(s.Stack.AccountId, s.Stack.OwnerId);

        var stale = await VersionOfBoardAsync(board);
        var asViewer = () => MoveBoardAsync(viewer, board, s.Marketing, stale);
        await asViewer.Should().ThrowAsync<AppForbidden>("a Space Viewer cannot manage the source board");

        await MoveBoardAsync(accountant, board, s.Marketing, stale);
        (await SpaceOfBoardAsync(board)).Should().Be(s.Marketing, "Admin of the source and Member (Editor) of the target");

        var staleMove = () => MoveBoardAsync(owner, board, s.Accounting, stale);
        await staleMove.Should().ThrowAsync<PreconditionFailed>();
        (await SpaceOfBoardAsync(board)).Should().Be(s.Marketing);

        var toArchived = () => MoveBoardAsync(owner, board, s.Archived, null);
        await toArchived.Should().ThrowAsync<AppConflict>();

        var toRootAsMember = () => MoveBoardAsync(accountant, board, null, null);
        await toRootAsMember.Should().ThrowAsync<AppForbidden>("the target is the Workspace root, whose board creation right the member lacks");

        await MoveBoardAsync(owner, board, null, null);
        (await SpaceOfBoardAsync(board)).Should().BeNull();
    }

    [Fact]
    public async Task MovingIntoASpaceWhereTheActorIsOnlyAViewer_IsForbidden_WithoutChange()
    {
        var s = await SeedAsync();
        var board = await _host.SeedBoardAsync(s.Stack, "Brand book", spaceId: s.Marketing);
        await _host.WithSystemContextAsync(async c =>
        {
            var marketing = await c.Spaces.Include(x => x.Members).SingleAsync(x => x.Id == s.Marketing);
            marketing.ChangeMemberRole(SpaceMemberSubjectType.User, s.Accountant, SpaceRole.Admin, s.Stack.OwnerId, OrgTreeTestHost.BaseTime.AddMinutes(1));
            var accounting = await c.Spaces.Include(x => x.Members).SingleAsync(x => x.Id == s.Accounting);
            accounting.AddMember(SpaceMemberSubjectType.User, s.Stack.OwnerId, SpaceRole.Admin, s.Accountant, OrgTreeTestHost.BaseTime.AddMinutes(1));
            accounting.ChangeMemberRole(SpaceMemberSubjectType.User, s.Accountant, SpaceRole.Viewer, s.Stack.OwnerId, OrgTreeTestHost.BaseTime.AddMinutes(2));
        });
        using var accountant = _host.CreateProvider(s.Stack.AccountId, s.Accountant);

        var act = () => MoveBoardAsync(accountant, board, s.Accounting, null);

        await act.Should().ThrowAsync<AppForbidden>("the target Space level (Viewer) is below Editor");
        (await SpaceOfBoardAsync(board)).Should().Be(s.Marketing);
    }

    [Fact]
    public async Task ABoardReachedThroughAGrant_DoesNotDiscloseItsHiddenSpace()
    {
        var s = await SeedAsync();
        var board = await _host.SeedBoardAsync(s.Stack, "Shared audit", spaceId: s.Accounting);
        await _host.SeedResourcePermissionAsync(s.Stack, "work-management.board", board, PermissionSubjectType.User, s.Outsider, PermissionLevel.Viewer);
        using var outsider = _host.CreateProvider(s.Stack.AccountId, s.Outsider);

        var single = await OrgTreeTestHost.QueryAsync(outsider, new GetBoardQuery(board));
        var listed = (await OrgTreeTestHost.QueryAsync(outsider, new GetBoardsQuery(s.Stack.WorkspaceId))).Single(b => b.Id == board);

        single.SpaceId.Should().BeNull("the caller cannot see the Accounting Space");
        listed.SpaceId.Should().BeNull();
    }

    // ── Pages ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Pages_CreatedInASpace_AndUnderAParent_FollowTheRootSpace_AndAHiddenParentIsNotReachable()
    {
        var s = await SeedAsync();
        using var accountant = _host.CreateProvider(s.Stack.AccountId, s.Accountant);
        using var outsider = _host.CreateProvider(s.Stack.AccountId, s.Outsider);

        var root = await OrgTreeTestHost.QueryAsync(accountant, new CreatePageCommand(s.Stack.WorkspaceId, "Close process", null, s.Accounting));
        var child = await OrgTreeTestHost.QueryAsync(accountant, new CreatePageCommand(s.Stack.WorkspaceId, "Month end", root));

        (await SpaceOfPageAsync(root)).Should().Be(s.Accounting);
        (await SpaceOfPageAsync(child)).Should().Be(s.Accounting, "a nested page follows its root page's Space");
        (await OrgTreeTestHost.QueryAsync(accountant, new GetPageQuery(child))).SpaceId.Should().Be(s.Accounting);

        var hiddenParent = () => OrgTreeTestHost.QueryAsync(outsider, new CreatePageCommand(s.Stack.WorkspaceId, "Sneaky", root));
        await hiddenParent.Should().ThrowAsync<AppNotFound>("a page cannot be added under one the caller cannot reach");
        var readChild = () => OrgTreeTestHost.QueryAsync(outsider, new GetPageQuery(child));
        await readChild.Should().ThrowAsync<AppNotFound>();
    }

    [Fact]
    public async Task MovePageToSpace_MovesTheWholeSubtree_AndRejectsANestedPage()
    {
        var s = await SeedAsync();
        using var owner = _host.CreateProvider(s.Stack.AccountId, s.Stack.OwnerId);
        using var viewer = _host.CreateProvider(s.Stack.AccountId, s.Viewer);
        var root = await OrgTreeTestHost.QueryAsync(owner, new CreatePageCommand(s.Stack.WorkspaceId, "Playbook", null, s.Accounting));
        var child = await OrgTreeTestHost.QueryAsync(owner, new CreatePageCommand(s.Stack.WorkspaceId, "Chapter", root));
        var grandchild = await OrgTreeTestHost.QueryAsync(owner, new CreatePageCommand(s.Stack.WorkspaceId, "Section", child));

        var rootVersion = await VersionOfPageAsync(root);
        var asViewer = () => OrgTreeTestHost.SendAsync(viewer, new MovePageToSpaceCommand(root, s.Marketing, rootVersion));
        await asViewer.Should().ThrowAsync<AppForbidden>("a Space Viewer cannot update the source page");

        await OrgTreeTestHost.SendAsync(owner, new MovePageToSpaceCommand(root, s.Marketing, await VersionOfPageAsync(root)));

        (await SpaceOfPageAsync(root)).Should().Be(s.Marketing);
        (await SpaceOfPageAsync(child)).Should().Be(s.Marketing);
        (await SpaceOfPageAsync(grandchild)).Should().Be(s.Marketing, "the whole subtree follows its root page");

        var childVersion = await VersionOfPageAsync(child);
        var nested = () => OrgTreeTestHost.SendAsync(owner, new MovePageToSpaceCommand(child, s.Accounting, childVersion));
        (await nested.Should().ThrowAsync<DomainBusinessRuleException>()).Which.RuleCode.Should().Be(DocumentRuleCodes.Documents_Page_NestedPageFollowsRootSpace);
        (await SpaceOfPageAsync(child)).Should().Be(s.Marketing);
    }

    // ── Space lifecycle with placed resources (WSP-022) ───────────────────

    [Fact]
    public async Task ASpaceHoldingAPlacedPage_CannotBeArchived()
    {
        var s = await SeedAsync();
        using var owner = _host.CreateProvider(s.Stack.AccountId, s.Stack.OwnerId);
        await OrgTreeTestHost.QueryAsync(owner, new CreatePageCommand(s.Stack.WorkspaceId, "Brief", null, s.Marketing));

        var act = () => OrgTreeTestHost.SendAsync(owner, new ArchiveSpaceCommand(s.Stack.WorkspaceId, s.Marketing));

        await act.Should().ThrowAsync<DomainBusinessRuleException>();
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private static async Task<Guid> CreateBoardAsync(ServiceProvider provider, OrgStack stack, string title, Guid? spaceId)
    {
        var result = (Notrelix.Application.Common.Models.Result<Guid>)(await OrgTreeTestHost.SendIdempotentAsync(
            provider, new CreateBoardInWorkspaceCommand(stack.WorkspaceId, title, null, null, null, spaceId)))!;
        return result.Data;
    }

    private async Task MoveBoardAsync(ServiceProvider provider, Guid boardId, Guid? spaceId, long? expectedVersion) =>
        await OrgTreeTestHost.SendIdempotentAsync(provider, new MoveBoardToSpaceCommand(boardId, spaceId, expectedVersion ?? await VersionOfBoardAsync(boardId)));

    private Task<Guid?> SpaceOfBoardAsync(Guid boardId) =>
        _host.ReadAsync(c => c.Boards.IgnoreQueryFilters().Where(b => b.Id == boardId).Select(b => b.SpaceId).SingleAsync());

    private Task<long> VersionOfBoardAsync(Guid boardId) =>
        _host.ReadAsync(c => c.Boards.Where(b => b.Id == boardId).Select(b => b.Version).SingleAsync());

    private Task<Guid?> SpaceOfPageAsync(Guid pageId) =>
        _host.ReadAsync(c => c.Pages.Where(p => p.Id == pageId).Select(p => p.SpaceId).SingleAsync());

    private Task<long> VersionOfPageAsync(Guid pageId) =>
        _host.ReadAsync(c => c.Pages.Where(p => p.Id == pageId).Select(p => p.Version).SingleAsync());
}
