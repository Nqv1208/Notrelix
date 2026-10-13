using Microsoft.Extensions.DependencyInjection;
using Notrelix.Application.Features.Collaboration.Comments.Commands.CreateComment;
using Notrelix.Application.Features.Collaboration.Comments.Queries.GetComments;
using Notrelix.Application.Features.Governance.ResourcePermissions.Queries.GetResourcePermissions;
using Notrelix.Application.Features.WorkManagement.BoardItems.Commands.UpdateBoardItem;
using Notrelix.Application.Features.WorkManagement.BoardItems.Queries.GetBoardItem;
using Notrelix.Application.Features.WorkManagement.Boards.Commands.AddBoardMember;
using Notrelix.Application.Features.WorkManagement.Boards.Commands.UpdateBoardMemberRole;
using Notrelix.Application.Features.WorkManagement.BoardViews.Commands.SaveBoardView;
using Notrelix.Domain.Governance.Permissions;
using Notrelix.Domain.WorkManagement.Boards;
using Notrelix.Domain.WorkManagement;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Integration.Tests.Containers;
using AppForbidden = Notrelix.Application.Common.Exceptions.ForbiddenException;

namespace Notrelix.Integration.Tests.Workspaces.OrgTree;

/// <summary>
/// Board authorization hardening on PostgreSQL through the real pipeline:
/// an Editor cannot grant a board role above their own level or raise their
/// own role (board Owner/Admin carry Manager); permission rules scoped to a
/// Board also decide its children (a Deny on the Board blocks its Items and
/// their comments); and saving the Board's shared view needs Editor.
/// </summary>
[Collection("Database")]
[Trait("Category", "Integration")]
public sealed class BoardAuthorizationHardeningIntegrationTests : IAsyncLifetime
{
    private const string BoardKind = "work-management.board";

    private readonly PostgresTestContainer _db;
    private readonly OrgTreeTestHost _host;

    public BoardAuthorizationHardeningIntegrationTests(PostgresTestContainer db)
    {
        _db = db;
        _host = new OrgTreeTestHost(db);
    }

    public async Task InitializeAsync() => await new DatabaseReset(_db.ConnectionString).ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Editor_CannotMakeThemselvesBoardOwner_AndStaysUnableToManagePermissions()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var editor = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Editor");
        var colleague = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Colleague");
        var board = await _host.SeedBoardAsync(stack, "Roadmap"); // Workspace default → Editor

        using var provider = CreateProvider(stack, editor);

        await FluentActions.Awaiting(() => OrgTreeTestHost.SendIdempotentAsync(provider, new AddBoardMemberCommand(board, editor, BoardRole.Owner)))
            .Should().ThrowAsync<AppForbidden>("Owner carries Manager, above the caller's Editor level");
        await FluentActions.Awaiting(() => OrgTreeTestHost.SendIdempotentAsync(provider, new AddBoardMemberCommand(board, colleague, BoardRole.Admin)))
            .Should().ThrowAsync<AppForbidden>();
        await FluentActions.Awaiting(() => OrgTreeTestHost.QueryAsync(provider, new GetResourcePermissionsQuery(BoardKind, board)))
            .Should().ThrowAsync<AppForbidden>("no path to Manager remains");

        await OrgTreeTestHost.SendIdempotentAsync(provider, new AddBoardMemberCommand(board, colleague, BoardRole.Member));
    }

    [Fact]
    public async Task BoardObserver_CannotRaiseTheirOwnRole()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var observer = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Observer");
        var board = await _host.SeedBoardAsync(stack, "Roadmap");
        await _host.SeedBoardMemberAsync(board, observer, BoardRole.Observer);

        using var provider = CreateProvider(stack, observer);

        await FluentActions.Awaiting(() => OrgTreeTestHost.SendIdempotentAsync(provider, new UpdateBoardMemberRoleCommand(board, observer, BoardRole.Member)))
            .Should().ThrowAsync<AppForbidden>();
    }

    [Fact]
    public async Task ADenyRuleOnTheBoard_BlocksItsItemsAndTheirComments()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var member = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Member");
        var board = await _host.SeedBoardAsync(stack, "Roadmap"); // Editor via Workspace default
        var item = await _host.SeedAssignedItemAsync(stack, board, stack.OwnerId, "Ship it");
        await _host.SeedRuleAsync(stack, PermissionSubjectType.User, member, PermissionAction.UpdateItem, PermissionEffect.Deny, BoardKind, board);
        await _host.SeedRuleAsync(stack, PermissionSubjectType.User, member, PermissionAction.CreateComment, PermissionEffect.Deny, BoardKind, board);
        await _host.SeedRuleAsync(stack, PermissionSubjectType.User, member, PermissionAction.ViewBoard, PermissionEffect.Deny, BoardKind, board);

        using var provider = CreateProvider(stack, member);

        await FluentActions.Awaiting(() => UpdateItemAsync(provider, item, "Renamed"))
            .Should().ThrowAsync<AppForbidden>("a Deny rule on the Board decides its Items");
        await FluentActions.Awaiting(() => OrgTreeTestHost.SendAsync(provider, CreateCommentCommand.ForBoardItem(item, "Hi", null)))
            .Should().ThrowAsync<AppForbidden>();
        await FluentActions.Awaiting(() => OrgTreeTestHost.SendAsync(provider, new GetBoardItemQuery(item)))
            .Should().ThrowAsync<AppForbidden>();
        await FluentActions.Awaiting(() => OrgTreeTestHost.SendAsync(provider, GetCommentsQuery.ForBoardItem(item)))
            .Should().ThrowAsync<AppForbidden>();
    }

    [Fact]
    public async Task AViewer_CannotSaveTheBoardsSharedView()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var viewer = await _host.AddMemberAsync(stack, WorkspaceRole.Guest, "Viewer");
        var board = await _host.SeedBoardAsync(stack, "Shared");
        await _host.SeedResourcePermissionAsync(stack, BoardKind, board, PermissionSubjectType.User, viewer, PermissionLevel.Viewer);

        using var provider = CreateProvider(stack, viewer);

        await FluentActions.Awaiting(() => OrgTreeTestHost.SendIdempotentAsync(provider, new SaveBoardViewCommand(board, ViewMode.Kanban, "{\"status\":[]}")))
            .Should().ThrowAsync<AppForbidden>("the shared view is changed for everyone");
    }

    private async Task UpdateItemAsync(ServiceProvider provider, Guid itemId, string title)
    {
        var version = await _host.ReadAsync(c => c.BoardItems.Where(i => i.Id == itemId).Select(i => i.Version).SingleAsync());
        await OrgTreeTestHost.SendAsync(provider, new UpdateBoardItemCommand(itemId, title, null, null, version));
    }

    /// <summary>The org-tree host plus the read ports the Item handlers need.</summary>
    private ServiceProvider CreateProvider(OrgStack stack, Guid userId) =>
        _host.CreateProvider(stack.AccountId, userId, services =>
        {
            services.AddScoped<Notrelix.Application.Features.Collaboration.Public.ResourceSummary.ICollaborationResourceSummary,
                Notrelix.Infrastructure.CrossContext.Collaboration.ResourceSummary.PostgresCollaborationResourceSummary>();
            services.AddScoped<Notrelix.Application.Features.WorkManagement.Ports.Collaboration.IWorkManagementCollaborationReadPort,
                Notrelix.Infrastructure.CrossContext.WorkManagement.Collaboration.WorkManagementCollaborationReadAdapter>();
        });
}
