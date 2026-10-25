using Microsoft.Extensions.DependencyInjection;
using Notrelix.Application.Features.Collaboration.Comments.Commands.CreateComment;
using Notrelix.Application.Features.Collaboration.Comments.Queries.GetComments;
using Notrelix.Application.Features.WorkManagement.BoardFields.Commands.UpdateBoardField;
using Notrelix.Application.Features.WorkManagement.BoardGroups.Commands.UpdateBoardGroup;
using Notrelix.Application.Features.WorkManagement.BoardItems.Commands.UpdateBoardItem;
using Notrelix.Application.Features.WorkManagement.BoardItems.Queries.GetBoardItem;
using Notrelix.Domain.Governance.Permissions;
using Notrelix.Domain.SharedKernel.Ordering;
using Notrelix.Domain.WorkManagement.Boards;
using Notrelix.Domain.WorkManagement.Fields;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Integration.Tests.Containers;
using AppForbidden = Notrelix.Application.Common.Exceptions.ForbiddenException;
using AppNotFound = Notrelix.Application.Common.Exceptions.NotFoundException;

namespace Notrelix.Integration.Tests.Workspaces.OrgTree;

/// <summary>
/// Board child resources (Items, Fields, Groups) are authorized through their
/// parent Board on PostgreSQL through the real pipeline (GOV-033): a member
/// without access to a Private Board gets NotFound for its Items and cannot
/// comment; a Board Editor can update Items, Fields and Groups; a Commenter
/// comments but cannot change anything; a Guest without explicit access sees
/// nothing and a Guest Viewer cannot comment; a child of an archived Board is
/// NotFound even for the Owner.
/// </summary>
[Collection("Database")]
[Trait("Category", "Integration")]
public sealed class BoardChildAccessIntegrationTests : IAsyncLifetime
{
    private const string BoardKind = "work-management.board";

    private readonly PostgresTestContainer _db;
    private readonly OrgTreeTestHost _host;

    public BoardChildAccessIntegrationTests(PostgresTestContainer db)
    {
        _db = db;
        _host = new OrgTreeTestHost(db);
    }

    public async Task InitializeAsync() => await new DatabaseReset(_db.OwnerConnectionString).ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task MemberWithoutBoardAccess_GetsNotFound_ForItemsOfAPrivateBoard_AndCannotComment()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var outsider = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Outsider");
        var board = await _host.SeedBoardAsync(stack, "Salaries", BoardVisibility.Private);
        var item = await _host.SeedAssignedItemAsync(stack, board, stack.OwnerId, "Raise for Q4");

        using var provider = CreateProvider(stack.AccountId, outsider);

        await FluentActions.Awaiting(() => OrgTreeTestHost.SendAsync(provider, new GetBoardItemQuery(item)))
            .Should().ThrowAsync<AppNotFound>("an Item of a Private Board the caller cannot see is hidden (GOV-025)");
        await FluentActions.Awaiting(() => OrgTreeTestHost.SendAsync(provider, GetCommentsQuery.ForBoardItem(item)))
            .Should().ThrowAsync<AppNotFound>();
        await FluentActions.Awaiting(() => OrgTreeTestHost.SendAsync(provider, CreateCommentCommand.ForBoardItem(item, "Hello", null)))
            .Should().ThrowAsync<AppNotFound>();
    }

    [Fact]
    public async Task BoardEditor_UpdatesItemsFieldsAndGroups()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var editor = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Editor");
        var board = await _host.SeedBoardAsync(stack, "Roadmap", BoardVisibility.Private);
        var item = await _host.SeedAssignedItemAsync(stack, board, stack.OwnerId, "Ship it");
        var field = await SeedFieldAsync(stack, board);
        await _host.SeedResourcePermissionAsync(stack, BoardKind, board, PermissionSubjectType.User, editor, PermissionLevel.Editor);

        using var provider = CreateProvider(stack.AccountId, editor);

        await OrgTreeTestHost.QueryAsync(provider, new GetBoardItemQuery(item));
        await UpdateItemAsync(provider, item, "Ship it now");
        await UpdateFieldAsync(provider, board, field, "Estimate");
        await UpdateGroupAsync(provider, item, "Doing");
    }

    [Fact]
    public async Task BoardCommenter_Comments_ButCannotChangeItemsFieldsOrGroups()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var commenter = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Commenter");
        var board = await _host.SeedBoardAsync(stack, "Roadmap", BoardVisibility.Private);
        var item = await _host.SeedAssignedItemAsync(stack, board, stack.OwnerId, "Ship it");
        var field = await SeedFieldAsync(stack, board);
        await _host.SeedResourcePermissionAsync(stack, BoardKind, board, PermissionSubjectType.User, commenter, PermissionLevel.Commenter);

        using var provider = CreateProvider(stack.AccountId, commenter);

        await OrgTreeTestHost.QueryAsync(provider, new GetBoardItemQuery(item));
        await OrgTreeTestHost.SendAsync(provider, CreateCommentCommand.ForBoardItem(item, "Looks good", null));
        await FluentActions.Awaiting(() => UpdateItemAsync(provider, item, "Renamed")).Should().ThrowAsync<AppForbidden>();
        await FluentActions.Awaiting(() => UpdateFieldAsync(provider, board, field, "Renamed")).Should().ThrowAsync<AppForbidden>();
        await FluentActions.Awaiting(() => UpdateGroupAsync(provider, item, "Renamed")).Should().ThrowAsync<AppForbidden>();
    }

    [Fact]
    public async Task Guests_SeeOnlyWhatTheBoardGrants_AndAGuestViewerCannotComment()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var stranger = await _host.AddMemberAsync(stack, WorkspaceRole.Guest, "Stranger");
        var viewer = await _host.AddMemberAsync(stack, WorkspaceRole.Guest, "Viewer");
        var board = await _host.SeedBoardAsync(stack, "Shared");
        var item = await _host.SeedAssignedItemAsync(stack, board, stack.OwnerId, "Deliverable");
        await _host.SeedResourcePermissionAsync(stack, BoardKind, board, PermissionSubjectType.User, viewer, PermissionLevel.Viewer);

        using (var provider = CreateProvider(stack.AccountId, stranger))
        {
            await FluentActions.Awaiting(() => OrgTreeTestHost.SendAsync(provider, new GetBoardItemQuery(item)))
                .Should().ThrowAsync<AppNotFound>("a Guest needs explicit access to the Board (GOV-025 step 6)");
        }

        using (var provider = CreateProvider(stack.AccountId, viewer))
        {
            await OrgTreeTestHost.QueryAsync(provider, new GetBoardItemQuery(item));
            await FluentActions.Awaiting(() => OrgTreeTestHost.SendAsync(provider, CreateCommentCommand.ForBoardItem(item, "Hi", null)))
                .Should().ThrowAsync<AppForbidden>("commenting needs Commenter on the Board");
        }
    }

    [Fact]
    public async Task ItemOfAnArchivedBoard_IsNotFound_EvenForTheOwner()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var board = await _host.SeedBoardAsync(stack, "Old");
        var item = await _host.SeedAssignedItemAsync(stack, board, stack.OwnerId, "Old work");
        await _host.WithSystemContextAsync(async c =>
            (await c.Boards.SingleAsync(b => b.Id == board)).Archive(stack.OwnerId, OrgTreeTestHost.BaseTime));

        using var provider = CreateProvider(stack.AccountId, stack.OwnerId);

        await FluentActions.Awaiting(() => OrgTreeTestHost.SendAsync(provider, new GetBoardItemQuery(item)))
            .Should().ThrowAsync<AppNotFound>();
    }

    /// <summary>The org-tree host plus the read ports the Item handlers need.</summary>
    private ServiceProvider CreateProvider(Guid accountId, Guid userId) =>
        _host.CreateProvider(accountId, userId, services =>
        {
            services.AddScoped<Notrelix.Application.Features.Collaboration.Public.ResourceSummary.ICollaborationResourceSummary,
                Notrelix.Infrastructure.CrossContext.Collaboration.ResourceSummary.PostgresCollaborationResourceSummary>();
            services.AddScoped<Notrelix.Application.Features.WorkManagement.Ports.Collaboration.IWorkManagementCollaborationReadPort,
                Notrelix.Infrastructure.CrossContext.WorkManagement.Collaboration.WorkManagementCollaborationReadAdapter>();
        });

    private async Task<Guid> SeedFieldAsync(OrgStack stack, Guid boardId)
    {
        var field = BoardField.Create(stack.AccountId, stack.WorkspaceId, boardId, "Points", FieldType.Text,
            FieldSettings.Empty(), FractionalIndex.Initial(), stack.OwnerId, OrgTreeTestHost.BaseTime);
        await _host.WithSystemContextAsync(c => { c.BoardFields.Add(field); return Task.CompletedTask; });
        return field.Id;
    }

    private async Task UpdateItemAsync(ServiceProvider provider, Guid itemId, string title)
    {
        var version = await _host.ReadAsync(c => c.BoardItems.Where(i => i.Id == itemId).Select(i => i.Version).SingleAsync());
        await OrgTreeTestHost.SendAsync(provider, new UpdateBoardItemCommand(itemId, title, null, null, version));
    }

    private async Task UpdateFieldAsync(ServiceProvider provider, Guid boardId, Guid fieldId, string name)
    {
        var version = await _host.ReadAsync(c => c.BoardFields.Where(f => f.Id == fieldId).Select(f => f.Version).SingleAsync());
        await OrgTreeTestHost.SendIdempotentAsync(provider, new UpdateBoardFieldCommand(boardId, fieldId, name, null, version));
    }

    private async Task UpdateGroupAsync(ServiceProvider provider, Guid itemId, string title)
    {
        var (groupId, version) = await _host.ReadAsync(async c =>
        {
            var groupId = await c.BoardItems.Where(i => i.Id == itemId).Select(i => i.GroupId).SingleAsync();
            var version = await c.BoardGroups.Where(g => g.Id == groupId).Select(g => g.Version).SingleAsync();
            return (groupId, version);
        });
        await OrgTreeTestHost.SendIdempotentAsync(provider, new UpdateBoardGroupCommand(groupId, title, version));
    }
}
