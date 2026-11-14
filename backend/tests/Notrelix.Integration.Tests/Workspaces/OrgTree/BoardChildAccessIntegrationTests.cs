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
/// nothing and a Guest Viewer cannot comment; a child of an archived Board stays
/// readable and takes no write, and a person without access to it still gets NotFound.
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
    public async Task DeletedItemOfAPrivateBoard_IsThePlainNotFound_ForAnOutsider_AndNamesTheDeleter_ForTheBoardOwner()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var outsider = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Outsider");
        var board = await _host.SeedBoardAsync(stack, "Salaries", BoardVisibility.Private);
        var item = await _host.SeedAssignedItemAsync(stack, board, stack.OwnerId, "Raise for Q4");
        await DeleteItemAsync(item, stack.OwnerId);

        using (var provider = CreateProvider(stack.AccountId, outsider))
        {
            var plain = (await FluentActions.Awaiting(() => OrgTreeTestHost.SendAsync(provider, new GetBoardItemQuery(item)))
                .Should().ThrowAsync<AppNotFound>("the board is private to the caller, so a deleted item is hidden like a live one")).Which;
            plain.ErrorCode.Should().BeNull("no deleted code reaches a person without access");
            plain.Extensions.Should().BeEmpty("no deletedByName reaches a person without access");
        }

        using (var provider = CreateProvider(stack.AccountId, stack.OwnerId))
        {
            var deleted = (await FluentActions.Awaiting(() => OrgTreeTestHost.SendAsync(provider, new GetBoardItemQuery(item)))
                .Should().ThrowAsync<AppNotFound>()).Which;
            deleted.ErrorCode.Should().Be(GetBoardItemQueryHandler.DeletedCode);
            deleted.Extensions["deletedByName"].Should().Be("Org Owner");
        }
    }

    [Fact]
    public async Task DeletedItemOfAnotherWorkspace_IsThePlainNotFound()
    {
        var mine = await _host.SeedWorkspaceAsync();
        var other = await _host.SeedWorkspaceAsync();
        var board = await _host.SeedBoardAsync(other, "Elsewhere");
        var item = await _host.SeedAssignedItemAsync(other, board, other.OwnerId, "Not yours");
        await DeleteItemAsync(item, other.OwnerId);

        using var provider = CreateProvider(mine.AccountId, mine.OwnerId);

        var plain = (await FluentActions.Awaiting(() => OrgTreeTestHost.SendAsync(provider, new GetBoardItemQuery(item)))
            .Should().ThrowAsync<AppNotFound>()).Which;
        plain.ErrorCode.Should().BeNull();
        plain.Extensions.Should().BeEmpty();
    }

    private Task DeleteItemAsync(Guid itemId, Guid actorId) =>
        _host.WithSystemContextAsync(async c =>
            (await c.BoardItems.IgnoreQueryFilters().SingleAsync(i => i.Id == itemId)).Delete(actorId, OrgTreeTestHost.BaseTime));

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
    public async Task ItemOfAnArchivedBoard_StaysReadable_AndRefusesWrites_ForTheOwner_AndStaysNotFound_ForAnOutsider()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var outsider = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Outsider");
        var board = await _host.SeedBoardAsync(stack, "Old", BoardVisibility.Private);
        var item = await _host.SeedAssignedItemAsync(stack, board, stack.OwnerId, "Old work");
        await _host.WithSystemContextAsync(async c =>
            (await c.Boards.SingleAsync(b => b.Id == board)).Archive(stack.OwnerId, OrgTreeTestHost.BaseTime));

        using (var provider = CreateProvider(stack.AccountId, stack.OwnerId))
        {
            await OrgTreeTestHost.QueryAsync(provider, new GetBoardItemQuery(item));
            var refused = await FluentActions.Awaiting(() => UpdateItemAsync(provider, item, "Renamed"))
                .Should().ThrowAsync<AppForbidden>();
            refused.Which.Code.Should().Be("WorkManagement_Board_Archived");
        }

        using (var provider = CreateProvider(stack.AccountId, outsider))
        {
            await FluentActions.Awaiting(() => UpdateItemAsync(provider, item, "Renamed"))
                .Should().ThrowAsync<AppNotFound>("a caller without access never learns the Board is archived");
        }
    }

    [Fact]
    public async Task MentionOfAMemberWithoutBoardAccess_KeepsTheCommentText_ButCreatesNoMention_WhileABoardViewerIsMentioned()
    {
        // UC-WM-R03 (D42): a mention of someone who cannot view the Item's Board notifies no one; the Mention row is
        // the only source of the "mention.created" fact, so no row means no Inbox notification.
        var stack = await _host.SeedWorkspaceAsync();
        var outsider = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Outsider");
        var viewer = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Viewer");
        var guest = await _host.AddMemberAsync(stack, WorkspaceRole.Guest, "Guest");
        var board = await _host.SeedBoardAsync(stack, "Salaries", BoardVisibility.Private);
        var item = await _host.SeedAssignedItemAsync(stack, board, stack.OwnerId, "Raise for Q4");
        await _host.SeedResourcePermissionAsync(stack, BoardKind, board, PermissionSubjectType.User, viewer, PermissionLevel.Viewer);

        using var provider = CreateProvider(stack.AccountId, stack.OwnerId);
        await OrgTreeTestHost.SendIdempotentAsync(
            provider, CreateCommentCommand.ForBoardItem(item, "@Outsider @Viewer @Guest please check", null, [outsider, viewer, guest]));

        var comment = await _host.ReadAsync(c => c.Comments.Where(x => x.WorkspaceId == stack.WorkspaceId).Select(x => x.Content).SingleAsync());
        comment.Should().Contain("@Outsider", "the text of a mention without access stays in the comment");
        var mentioned = await _host.ReadAsync(c => c.PageMentions
            .Where(m => m.WorkspaceId == stack.WorkspaceId)
            .Select(m => m.MentionedId)
            .ToListAsync());
        mentioned.Should().Equal([viewer], "only the person who can view the Board is mentioned; the outsider and the Guest are not");
    }

    [Fact]
    public async Task BoardViewer_IsRefusedAnUploadAndALink_ButListsTheFiles()
    {
        // UC-WM-R04: upload and link need UpdateItem (Editor and above); viewing the list needs ViewBoard.
        var stack = await _host.SeedWorkspaceAsync();
        var viewer = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Viewer");
        var board = await _host.SeedBoardAsync(stack, "Roadmap", BoardVisibility.Private);
        var item = await _host.SeedAssignedItemAsync(stack, board, stack.OwnerId, "Ship it");
        await _host.SeedResourcePermissionAsync(stack, BoardKind, board, PermissionSubjectType.User, viewer, PermissionLevel.Viewer);

        using var provider = CreateProvider(stack.AccountId, viewer);

        await FluentActions.Awaiting(() => OrgTreeTestHost.SendIdempotentAsync(provider,
                new Notrelix.Application.Features.Collaboration.Attachments.Commands.UploadBoardItemAttachment.UploadBoardItemAttachmentCommand(
                    item, "plan.png", new MemoryStream([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]))))
            .Should().ThrowAsync<AppForbidden>("a board Viewer cannot upload a file");
        await FluentActions.Awaiting(() => OrgTreeTestHost.SendIdempotentAsync(provider,
                new Notrelix.Application.Features.Collaboration.Attachments.Commands.CreateBoardItemAttachment.CreateBoardItemAttachmentCommand(
                    item, "Spec", "https://example.com/spec", null, null, null)))
            .Should().ThrowAsync<AppForbidden>("a board Viewer cannot add a link");
        await OrgTreeTestHost.QueryAsync(provider,
            new Notrelix.Application.Features.Collaboration.Attachments.Queries.GetBoardItemAttachments.GetBoardItemAttachmentsQuery(item));

        var stored = await _host.ReadAsync(c => c.Attachments.CountAsync(a => a.WorkspaceId == stack.WorkspaceId));
        stored.Should().Be(0, "nothing was stored for the refused writes");
    }

    /// <summary>The org-tree host plus the read ports the Item handlers need.</summary>
    private ServiceProvider CreateProvider(Guid accountId, Guid userId) =>
        _host.CreateProvider(accountId, userId, services =>
        {
            services.AddScoped<Notrelix.Application.Features.Collaboration.Public.ResourceSummary.ICollaborationResourceSummary,
                Notrelix.Infrastructure.CrossContext.Collaboration.ResourceSummary.PostgresCollaborationResourceSummary>();
            services.AddScoped<Notrelix.Application.Features.Collaboration.Public.Watchers.IResourceFollowers,
                Notrelix.Infrastructure.CrossContext.Collaboration.Watchers.PostgresResourceFollowers>();
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
