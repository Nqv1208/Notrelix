using Microsoft.Extensions.DependencyInjection;
using Notrelix.Application.Features.Collaboration.Comments.Commands.CreateComment;
using Notrelix.Application.Features.Collaboration.Comments.Queries.GetComments;
using Notrelix.Application.Features.Governance.ResourcePermissions.Queries.GetResourcePermissions;
using Notrelix.Application.Features.WorkManagement.BoardItems.Commands.UpdateBoardItem;
using Notrelix.Application.Features.WorkManagement.BoardItems.Queries.GetBoardItem;
using Notrelix.Application.Features.WorkManagement.Boards.Commands.AddBoardMember;
using Notrelix.Application.Features.WorkManagement.Boards.Commands.ArchiveBoard;
using Notrelix.Application.Features.WorkManagement.Boards.Commands.DeleteBoard;
using Notrelix.Application.Features.WorkManagement.Boards.Commands.UpdateBoardVisibility;
using Notrelix.Application.Features.WorkManagement.Boards.Commands.RemoveBoardMember;
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

    public async Task InitializeAsync() => await new DatabaseReset(_db.OwnerConnectionString).ResetAsync();

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

        // Managing members is Board management (Manager): previously an Editor
        // could add a member at or below Editor; now it is refused too.
        await FluentActions.Awaiting(() => OrgTreeTestHost.SendIdempotentAsync(provider, new AddBoardMemberCommand(board, colleague, BoardRole.Member)))
            .Should().ThrowAsync<AppForbidden>();
        (await _host.ReadAsync(c => c.BoardMembers.CountAsync(m => m.BoardId == board))).Should().Be(0, "a refused add changes nothing");
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
    public async Task Editor_CannotRemoveOrDemoteBoardMembersAboveTheirOwnLevel()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var editor = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Editor");
        var boardOwner = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "BoardOwner");
        var granted = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Granted");
        var plain = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Plain");
        var manager = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Manager");
        var board = await _host.SeedBoardAsync(stack, "Roadmap"); // Workspace default → Editor
        await _host.SeedBoardMemberAsync(board, boardOwner, BoardRole.Owner);
        await _host.SeedBoardMemberAsync(board, granted, BoardRole.Member);
        await _host.SeedBoardMemberAsync(board, plain, BoardRole.Member);
        await _host.SeedResourcePermissionAsync(stack, BoardKind, board, PermissionSubjectType.User, granted, PermissionLevel.Manager);
        await _host.SeedResourcePermissionAsync(stack, BoardKind, board, PermissionSubjectType.User, manager, PermissionLevel.Manager);

        using (var provider = CreateProvider(stack, editor))
        {
            await FluentActions.Awaiting(() => OrgTreeTestHost.SendIdempotentAsync(provider, new RemoveBoardMemberCommand(board, boardOwner)))
                .Should().ThrowAsync<AppForbidden>("a board Owner holds Manager, above the caller's Editor level");
            await FluentActions.Awaiting(() => OrgTreeTestHost.SendIdempotentAsync(provider, new RemoveBoardMemberCommand(board, granted)))
                .Should().ThrowAsync<AppForbidden>("the target's explicit Manager grant is above the caller's level");
            await FluentActions.Awaiting(() => OrgTreeTestHost.SendIdempotentAsync(provider, new UpdateBoardMemberRoleCommand(board, boardOwner, BoardRole.Observer)))
                .Should().ThrowAsync<AppForbidden>();

            // Removing members is Board management (Manager): previously an Editor
            // could remove a member at or below Editor; now it is refused too.
            await FluentActions.Awaiting(() => OrgTreeTestHost.SendIdempotentAsync(provider, new RemoveBoardMemberCommand(board, plain)))
                .Should().ThrowAsync<AppForbidden>();
        }

        using (var provider = CreateProvider(stack, manager))
        {
            await OrgTreeTestHost.SendIdempotentAsync(provider, new RemoveBoardMemberCommand(board, boardOwner));
        }

        var remaining = await _host.ReadAsync(c => c.BoardMembers.Where(m => m.BoardId == board).Select(m => m.UserId).ToListAsync());
        remaining.Should().BeEquivalentTo([granted, plain], "only the Manager's removal took effect");
    }

    [Fact]
    public async Task OnlyAManagerManagesTheBoard_EditorsEditContentOnly_AndGuestsOnlyRead()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var member = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Member");
        var guest = await _host.AddMemberAsync(stack, WorkspaceRole.Guest, "Guest");
        var colleague = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Colleague");
        var manager = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Manager");
        var board = await _host.SeedBoardAsync(stack, "Roadmap"); // unplaced, Workspace-visible: members are Editors
        var item = await _host.SeedAssignedItemAsync(stack, board, stack.OwnerId, "Ship it");
        await _host.SeedResourcePermissionAsync(stack, BoardKind, board, PermissionSubjectType.User, guest, PermissionLevel.Manager);
        await _host.SeedResourcePermissionAsync(stack, BoardKind, board, PermissionSubjectType.User, manager, PermissionLevel.Manager);

        foreach (var editor in new[] { member, guest })
        {
            using var provider = CreateProvider(stack, editor);

            await FluentActions.Awaiting(() => OrgTreeTestHost.SendIdempotentAsync(provider, new ArchiveBoardCommand(board)))
                .Should().ThrowAsync<AppForbidden>("archiving the Board is a Manager action");
            await FluentActions.Awaiting(() => OrgTreeTestHost.SendIdempotentAsync(provider, new DeleteBoardCommand(board)))
                .Should().ThrowAsync<AppForbidden>();
            await FluentActions.Awaiting(() => OrgTreeTestHost.SendIdempotentAsync(provider, new UpdateBoardVisibilityCommand(board, BoardVisibility.Private)))
                .Should().ThrowAsync<AppForbidden>();
            await FluentActions.Awaiting(() => OrgTreeTestHost.SendIdempotentAsync(provider, new AddBoardMemberCommand(board, colleague, BoardRole.Observer)))
                .Should().ThrowAsync<AppForbidden>("managing members is a Manager action");

            if (editor == guest)
            {
                await FluentActions.Awaiting(() => UpdateItemAsync(provider, item, "Edited by the guest"))
                    .Should().ThrowAsync<AppForbidden>("a guest is view-only (GOV-030)");
            }
            else
            {
                await UpdateItemAsync(provider, item, $"Edited by {editor}");
            }
        }

        var unchanged = await _host.ReadAsync(async c =>
        {
            var b = await c.Boards.IgnoreQueryFilters().SingleAsync(x => x.Id == board);
            var members = await c.BoardMembers.CountAsync(m => m.BoardId == board);
            return (b.IsArchived, b.IsDeleted, b.Visibility, members);
        });
        unchanged.Should().Be((false, false, BoardVisibility.Workspace, 0), "refused management changes nothing");

        using (var provider = CreateProvider(stack, manager))
        {
            await OrgTreeTestHost.SendIdempotentAsync(provider, new AddBoardMemberCommand(board, colleague, BoardRole.Observer));
            await OrgTreeTestHost.SendIdempotentAsync(provider, new ArchiveBoardCommand(board));
        }
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
            services.AddScoped<Notrelix.Application.Features.Collaboration.Public.Watchers.IResourceFollowers,
                Notrelix.Infrastructure.CrossContext.Collaboration.Watchers.PostgresResourceFollowers>();
            services.AddScoped<Notrelix.Application.Features.WorkManagement.Ports.Collaboration.IWorkManagementCollaborationReadPort,
                Notrelix.Infrastructure.CrossContext.WorkManagement.Collaboration.WorkManagementCollaborationReadAdapter>();
        });
}
