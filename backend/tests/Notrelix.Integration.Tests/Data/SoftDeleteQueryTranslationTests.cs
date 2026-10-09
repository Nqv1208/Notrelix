using Notrelix.Application.Features.Collaboration.Comments.Queries.GetComments;
using Notrelix.Application.Features.Documents.Pages.Queries.GetPage;
using Notrelix.Application.Features.WorkManagement.Boards.Queries.GetFullBoard;
using Notrelix.Application.Features.WorkManagement.Ports.Collaboration;
using Notrelix.Domain.Collaboration.Comments;
using Notrelix.Domain.Documents.Pages;
using Notrelix.Domain.SharedKernel;
using Notrelix.Domain.SharedKernel.Ordering;
using Notrelix.Domain.WorkManagement.BoardGroups;
using Notrelix.Domain.WorkManagement.Boards;
using Notrelix.Domain.WorkManagement.Fields;
using Notrelix.Domain.WorkManagement.Items;
using Notrelix.Domain.Workspaces.Workspaces;
using Notrelix.Infrastructure.Services;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Integration.Tests.Data;

/// <summary>
/// EF maps the soft-delete column <c>DeletedAt</c> and ignores the Domain-only
/// <c>IsDeleted</c> flag, so a query that filters or projects <c>IsDeleted</c>
/// cannot be translated. These tests run the real handlers against PostgreSQL
/// to prove the soft-delete predicates translate and exclude deleted rows.
/// </summary>
[Collection("Database")]
[Trait("Category", "Integration")]
public sealed class SoftDeleteQueryTranslationTests : IAsyncLifetime
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    private readonly PostgresTestContainer _db;
    private DatabaseReset _reset = null!;

    public SoftDeleteQueryTranslationTests(PostgresTestContainer db)
    {
        _db = db;
    }

    public async Task InitializeAsync()
    {
        _reset = new DatabaseReset(_db.ConnectionString);
        await _reset.ResetAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GetFullBoard_ExcludesDeletedItemsAndFields()
    {
        var accountId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var workspace = Workspace.Create(accountId, ownerId, "Soft Delete WS", $"sd-{Guid.NewGuid():N}", Now);
        var board = Board.Create(accountId, workspace.Id, ownerId, "Soft Delete Board", null, Now);
        var group = BoardGroup.Create(
            accountId, workspace.Id, board.Id, "Todo",
            Color.Create("#808080"), FractionalIndex.Create("a0"), ownerId, Now);
        var activeItem = BoardItem.CreateRoot(
            accountId, workspace.Id, board.Id, group.Id, "Active", FractionalIndex.Create("a0"), ownerId, Now);
        var deletedItem = BoardItem.CreateRoot(
            accountId, workspace.Id, board.Id, group.Id, "Deleted", FractionalIndex.Create("a1"), ownerId, Now);
        deletedItem.Delete(ownerId, Now);
        var activeField = BoardField.Create(
            accountId, workspace.Id, board.Id, "Notes", FieldType.Text, FieldSettings.Empty(),
            FractionalIndex.Create("a0"), ownerId, Now);
        var deletedField = BoardField.Create(
            accountId, workspace.Id, board.Id, "Retired", FieldType.Text, FieldSettings.Empty(),
            FractionalIndex.Create("a1"), ownerId, Now);
        deletedField.Delete(ownerId, Now);

        await using (var seed = _db.CreateContext(SystemTenant()))
        {
            seed.Workspaces.Add(workspace);
            seed.Boards.Add(board);
            seed.BoardGroups.Add(group);
            seed.BoardItems.AddRange(activeItem, deletedItem);
            seed.BoardFields.AddRange(activeField, deletedField);
            await seed.SaveChangesAsync();
        }

        await using var context = _db.CreateContext(SystemTenant());
        var handler = new GetFullBoardQueryHandler(
            context, new ActorLookupService(context), new EmptyCollaborationReadPort());

        var result = await handler.Handle(new GetFullBoardQuery(board.Id), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data!.Lists.Should().ContainSingle()
            .Which.BoardItems.Select(item => item.Id).Should().Equal(activeItem.Id);
        result.Data.Columns.Single(column => column.Id == activeField.Id).IsDeleted.Should().BeFalse();
        result.Data.Columns.Should().NotContain(column => column.Id == deletedField.Id);
    }

    [Fact]
    public async Task GetPage_ReturnsActivePage_AndHidesDeletedPage()
    {
        var accountId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var workspace = Workspace.Create(accountId, ownerId, "Soft Delete Docs", $"sd-{Guid.NewGuid():N}", Now);
        var activePage = Page.Create(accountId, workspace.Id, "Active", ownerId, Now);
        var deletedPage = Page.Create(accountId, workspace.Id, "Deleted", ownerId, Now);
        deletedPage.Delete(ownerId, Now);

        await using (var seed = _db.CreateContext(SystemTenant()))
        {
            seed.Workspaces.Add(workspace);
            seed.Pages.AddRange(activePage, deletedPage);
            await seed.SaveChangesAsync();
        }

        await using var context = _db.CreateContext(SystemTenant());
        var handler = new GetPageQueryHandler(context);

        var found = await handler.Handle(new GetPageQuery(activePage.Id), CancellationToken.None);
        found.Succeeded.Should().BeTrue();

        var act = () => handler.Handle(new GetPageQuery(deletedPage.Id), CancellationToken.None);
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetComments_ExcludesDeletedComments()
    {
        var accountId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        var authorId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        // Target is an EF owned type, so each comment needs its own instance.
        ResourceRef Target() => ResourceRef.Create(ResourceKind.Create("work-management.board-item"), itemId);
        var activeComment = Comment.Create(accountId, workspaceId, Target(), "kept", authorId, Now);
        var deletedComment = Comment.Create(accountId, workspaceId, Target(), "removed", authorId, Now.AddSeconds(1));
        deletedComment.Delete(authorId, Now.AddSeconds(2));

        await using (var seed = _db.CreateContext(SystemTenant()))
        {
            seed.Comments.AddRange(activeComment, deletedComment);
            await seed.SaveChangesAsync();
        }

        await using var context = _db.CreateContext(SystemTenant());
        var handler = new GetCommentsQueryHandler(context, new ActorLookupService(context));

        var result = await handler.Handle(GetCommentsQuery.ForBoardItem(itemId), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data!.Select(comment => comment.Id).Should().Equal(activeComment.Id);
    }

    private static ICurrentTenantContext SystemTenant()
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        return tenant;
    }

    private sealed class EmptyCollaborationReadPort : IWorkManagementCollaborationReadPort
    {
        public Task<IReadOnlyDictionary<Guid, WorkItemCollaborationCounts>> GetCountsAsync(
            IReadOnlyCollection<Guid> itemIds,
            CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyDictionary<Guid, WorkItemCollaborationCounts>>(
                new Dictionary<Guid, WorkItemCollaborationCounts>());
    }
}
