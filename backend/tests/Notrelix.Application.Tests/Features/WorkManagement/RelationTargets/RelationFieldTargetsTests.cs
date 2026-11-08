using Moq;
using Notrelix.Application.Features.Governance.Public.Visibility;
using Notrelix.Application.Features.WorkManagement.RelationTargets;
using Notrelix.Domain.WorkManagement;

namespace Notrelix.Application.Tests.Features.WorkManagement.RelationTargets;

/// <summary>The Application's checks of a Relation field: the target board and the linked items (product sections 64-66).</summary>
public sealed class RelationFieldTargetsTests : WorkManagementHandlerTestBase
{
    private readonly Mock<IResourceVisibility> _visibility = new();

    private Board TargetBoard(Guid? accountId = null, Guid? workspaceId = null, bool archived = false)
    {
        var board = Board.Create(accountId ?? TestAccountId, workspaceId ?? TestWorkspaceId, TestUserId, "Target", null, TestNow, BoardVisibility.Workspace);
        if (archived) board.Archive(TestUserId, TestNow);
        return board;
    }

    private void Readable(params Guid[] boardIds) =>
        _visibility.Setup(v => v.VisibleBoardIdsAsync(TestWorkspaceId, TestUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(boardIds.ToList());

    private Task CheckTarget(Guid targetBoardId) =>
        RelationFieldTargets.EnsureTargetBoardAsync(DbContextMock.Object, _visibility.Object, TestAccountId, TestWorkspaceId, TestUserId, targetBoardId, CancellationToken.None);

    private Task CheckLinks(Guid targetBoardId, IReadOnlyCollection<Guid> requested, IReadOnlyCollection<Guid>? stored = null) =>
        RelationFieldTargets.EnsureLinksAsync(DbContextMock.Object, _visibility.Object, TestAccountId, TestWorkspaceId, TestUserId, targetBoardId,
            requested, stored ?? [], CancellationToken.None);

    private static string RuleOf(Func<Task> act) => act.Should().ThrowAsync<Notrelix.Domain.Common.Exceptions.BusinessRuleException>().Result.Which.RuleCode;

    [Fact]
    public async Task Target_OnAnotherWorkspace_IsRefusedAsMissing()
    {
        var board = TargetBoard(workspaceId: Guid.CreateVersion7());
        SetupBoards(board);
        Readable(board.Id);

        var code = RuleOf(() => CheckTarget(board.Id));

        code.Should().Be(WorkManagementRuleCodes.WorkManagement_Relation_TargetBoardNotFound);
    }

    [Fact]
    public async Task Target_OnAnotherAccount_IsRefusedAsMissing()
    {
        var board = TargetBoard(accountId: Guid.CreateVersion7());
        SetupBoards(board);
        Readable(board.Id);

        RuleOf(() => CheckTarget(board.Id)).Should().Be(WorkManagementRuleCodes.WorkManagement_Relation_TargetBoardNotFound);
    }

    [Fact]
    public async Task Target_Archived_IsRefused()
    {
        var board = TargetBoard(archived: true);
        SetupBoards(board);
        Readable(board.Id);

        RuleOf(() => CheckTarget(board.Id)).Should().Be(WorkManagementRuleCodes.WorkManagement_Relation_TargetBoardNotFound);
    }

    [Fact]
    public async Task Target_Missing_IsRefused()
    {
        SetupBoards();
        Readable();

        RuleOf(() => CheckTarget(Guid.CreateVersion7())).Should().Be(WorkManagementRuleCodes.WorkManagement_Relation_TargetBoardNotFound);
    }

    [Fact]
    public async Task Target_NotReadableByWriter_IsRefusedAsMissing()
    {
        var board = TargetBoard();
        SetupBoards(board);
        Readable();

        RuleOf(() => CheckTarget(board.Id)).Should().Be(WorkManagementRuleCodes.WorkManagement_Relation_TargetBoardNotFound);
    }

    [Fact]
    public async Task Target_ActiveReadableSameWorkspace_IsAccepted()
    {
        var board = TargetBoard();
        SetupBoards(board);
        Readable(board.Id);

        var act = () => CheckTarget(board.Id);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Links_ToAnItemOfAnotherBoard_AreRefused()
    {
        var target = TargetBoard();
        SetupBoards(target);
        Readable(target.Id);
        var foreign = CreateBoardItem(boardId: Guid.CreateVersion7());
        SetupBoardItems(foreign);

        RuleOf(() => CheckLinks(target.Id, [foreign.Id])).Should().Be(WorkManagementRuleCodes.WorkManagement_Relation_TargetItemNotFound);
    }

    [Fact]
    public async Task Links_ToAMissingItem_AreRefused()
    {
        var target = TargetBoard();
        SetupBoards(target);
        Readable(target.Id);
        SetupBoardItems();

        RuleOf(() => CheckLinks(target.Id, [Guid.CreateVersion7()])).Should().Be(WorkManagementRuleCodes.WorkManagement_Relation_TargetItemNotFound);
    }

    [Fact]
    public async Task Links_ToATrashedItem_AreRefused()
    {
        var target = TargetBoard();
        SetupBoards(target);
        Readable(target.Id);
        var item = CreateBoardItem(boardId: target.Id);
        item.Delete(TestUserId, TestNow);
        SetupBoardItems(item);

        RuleOf(() => CheckLinks(target.Id, [item.Id])).Should().Be(WorkManagementRuleCodes.WorkManagement_Relation_TargetItemNotFound);
    }

    [Fact]
    public async Task Links_ToAnArchivedItem_AreRefusedWhenNewlyAdded()
    {
        var target = TargetBoard();
        SetupBoards(target);
        Readable(target.Id);
        var item = CreateBoardItem(boardId: target.Id);
        item.Archive(TestUserId, TestNow);
        SetupBoardItems(item);

        RuleOf(() => CheckLinks(target.Id, [item.Id])).Should().Be(WorkManagementRuleCodes.WorkManagement_Relation_TargetItemNotFound);
    }

    [Fact]
    public async Task Links_ToABoardTheWriterCannotRead_AreRefused()
    {
        var target = TargetBoard();
        SetupBoards(target);
        Readable();
        var item = CreateBoardItem(boardId: target.Id);
        SetupBoardItems(item);

        RuleOf(() => CheckLinks(target.Id, [item.Id])).Should().Be(WorkManagementRuleCodes.WorkManagement_Relation_TargetBoardNotFound);
    }

    [Fact]
    public async Task Links_AStoredLink_IsKeptWithoutANewReadCheck()
    {
        var target = TargetBoard();
        SetupBoards(target);
        Readable();
        var item = CreateBoardItem(boardId: target.Id);
        SetupBoardItems(item);

        var act = () => CheckLinks(target.Id, [item.Id], stored: [item.Id]);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Links_AValidLinkOnTheTargetBoard_IsAccepted()
    {
        var target = TargetBoard();
        SetupBoards(target);
        Readable(target.Id);
        var item = CreateBoardItem(boardId: target.Id);
        SetupBoardItems(item);

        var act = () => CheckLinks(target.Id, [item.Id]);

        await act.Should().NotThrowAsync();
    }
}
