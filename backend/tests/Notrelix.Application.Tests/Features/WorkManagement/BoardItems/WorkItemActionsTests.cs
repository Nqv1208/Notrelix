using Notrelix.Application.Features.WorkManagement.BoardItems.Services;
using Notrelix.Application.Features.WorkManagement.Public.ItemMovement;

using Notrelix.Domain.SharedKernel.Ordering;

namespace Notrelix.Application.Tests.Features.WorkManagement.BoardItems;

/// <summary>
/// TAC-WM-006 — the WorkManagement Public move action is governed by the
/// canonical MoveItem authorization before anything else, then delegates to
/// the single producer-local use case: valid moves succeed, unknown items and
/// scope/authorization failures are rejected before the dedup store, a
/// completed operation replays its stored result, and a conflicting payload
/// fails deterministically.
/// </summary>
public class WorkItemActionsTests : WorkManagementHandlerTestBase
{
    private readonly Mock<IWorkItemActionAuthorizer> AuthorizerMock = new();
    private readonly Mock<IIdempotencyStore> IdempotencyStoreMock = new();
    private readonly WorkItemActions _sut;

    public WorkItemActionsTests()
    {
        AuthorizerMock
            .Setup(a => a.AuthorizeMoveItemAsync(
                It.IsAny<WorkItemActionIdentity>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        IdempotencyStoreMock
            .Setup(s => s.BeginAsync(It.IsAny<IdempotencyIdentity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IdempotencyBeginResult(IdempotencyBeginStatus.Started, null, null));

        _sut = new WorkItemActions(
            new MoveBoardItemUseCase(DbContextMock.Object, DateTimeProviderMock.Object),
            DbContextMock.Object,
            AuthorizerMock.Object,
            IdempotencyStoreMock.Object);
    }

    private WorkItemMoveRequest MoveRequest(Guid itemId, Guid targetGroupId, Guid? operationId = null) =>
        new(
            new WorkItemActionIdentity(
                OperationId: operationId ?? Guid.CreateVersion7(),
                AccountId: TestAccountId,
                WorkspaceId: TestWorkspaceId,
                ExecutorUserId: TestUserId),
            itemId,
            targetGroupId);

    [Fact]
    public async Task MoveItem_CanonicalAuthorizationDenies_NeverReachesDedupStoreOrMutation()
    {
        var itemId = Guid.CreateVersion7();
        var groupId = Guid.CreateVersion7();
        AuthorizerMock
            .Setup(a => a.AuthorizeMoveItemAsync(
                It.IsAny<WorkItemActionIdentity>(), itemId, groupId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ForbiddenException("You do not have permission to perform this action."));

        await _sut.Invoking(s => s.MoveItemAsync(MoveRequest(itemId, groupId), CancellationToken.None))
            .Should().ThrowAsync<ForbiddenException>(
                "the canonical MoveItem decision governs the public action");

        IdempotencyStoreMock.Verify(
            s => s.BeginAsync(It.IsAny<IdempotencyIdentity>(), It.IsAny<CancellationToken>()),
            Times.Never, "a deny must never begin a dedup record");
        IdempotencyStoreMock.Verify(
            s => s.CompleteAsync(It.IsAny<IdempotencyIdentity>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never, "a deny must never complete a dedup record");
    }

    [Fact]
    public async Task MoveItem_ValidRequest_MovesItemToTargetGroup()
    {
        var board = CreateBoard();
        var sourceGroup = BoardGroup.Create(
            TestAccountId, TestWorkspaceId, board.Id, "Source",
            Color.Create("#FF0000"), FractionalIndex.Create("a0"), TestUserId, TestNow);
        var targetGroup = BoardGroup.Create(
            TestAccountId, TestWorkspaceId, board.Id, "Target",
            Color.Create("#00FF00"), FractionalIndex.Create("a1"), TestUserId, TestNow);
        var item = CreateBoardItem(boardId: board.Id, groupId: sourceGroup.Id);
        SetupBoards(board);
        SetupBoardGroups(sourceGroup, targetGroup);
        SetupBoardItems(item);

        var result = await _sut.MoveItemAsync(MoveRequest(item.Id, targetGroup.Id), CancellationToken.None);

        result.ItemId.Should().Be(item.Id);
        result.GroupId.Should().Be(targetGroup.Id);
        result.Position.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task MoveItem_UnknownItem_ThrowsNotFound()
    {
        var request = MoveRequest(Guid.CreateVersion7(), Guid.CreateVersion7());

        await _sut.Invoking(s => s.MoveItemAsync(request, CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task MoveItem_ItemOutsideDeclaredWorkspace_IsForbidden()
    {
        var otherWorkspaceId = Guid.CreateVersion7();
        var board = CreateBoard(workspaceId: otherWorkspaceId);
        var group = BoardGroup.Create(
            TestAccountId, otherWorkspaceId, board.Id, "Other Workspace Group",
            Color.Create("#00FF00"), FractionalIndex.Create("a0"), TestUserId, TestNow);
        var item = BoardItem.CreateRoot(
            TestAccountId, otherWorkspaceId, board.Id, group.Id,
            "Foreign Item", FractionalIndex.Create("a0"), TestUserId, TestNow);
        SetupBoards(board);
        SetupBoardGroups(group);
        SetupBoardItems(item);

        await _sut.Invoking(s => s.MoveItemAsync(MoveRequest(item.Id, group.Id), CancellationToken.None))
            .Should().ThrowAsync<ForbiddenException>(
                "the item's workspace must match the declared execution workspace");
    }

    [Fact]
    public async Task MoveItem_NonMemberExecutor_IsForbidden()
    {
        var board = CreateBoard();
        var group = BoardGroup.Create(
            TestAccountId, TestWorkspaceId, board.Id, "Group",
            Color.Create("#00FF00"), FractionalIndex.Create("a0"), TestUserId, TestNow);
        var item = CreateBoardItem(boardId: board.Id, groupId: group.Id);
        SetupBoards(board);
        SetupBoardGroups(group);
        SetupBoardItems(item);
        var outsider = Guid.CreateVersion7();
        AuthorizerMock
            .Setup(a => a.AuthorizeMoveItemAsync(
                It.Is<WorkItemActionIdentity>(i => i.ExecutorUserId == outsider),
                item.Id, group.Id, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ForbiddenException("The executor is not an active member of the workspace."));

        await _sut.Invoking(s => s.MoveItemAsync(
                new WorkItemMoveRequest(
                    new WorkItemActionIdentity(
                        Guid.CreateVersion7(), TestAccountId, TestWorkspaceId, outsider),
                    item.Id,
                    group.Id),
                CancellationToken.None))
            .Should().ThrowAsync<ForbiddenException>(
                "the canonical authorization denies the non-member executor");
        IdempotencyStoreMock.Verify(
            s => s.BeginAsync(It.IsAny<IdempotencyIdentity>(), It.IsAny<CancellationToken>()),
            Times.Never, "a denied executor must never begin a dedup record");
    }

    [Fact]
    public async Task MoveItem_SameOperationIdDifferentExecutor_IsDeterministicConflict()
    {
        var board = CreateBoard();
        var group = BoardGroup.Create(
            TestAccountId, TestWorkspaceId, board.Id, "Group",
            Color.Create("#00FF00"), FractionalIndex.Create("a0"), TestUserId, TestNow);
        var item = CreateBoardItem(boardId: board.Id, groupId: group.Id);
        SetupBoards(board);
        SetupBoardGroups(group);
        SetupBoardItems(item);
        var operationId = Guid.CreateVersion7();
        var otherExecutor = Guid.CreateVersion7();

        // The first executor's operation committed with this fingerprint.
        IdempotencyStoreMock
            .Setup(s => s.BeginAsync(It.Is<IdempotencyIdentity>(i => i.RequestHash == Sha256($"{item.Id:N}:{group.Id:N}:{TestUserId:N}")), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IdempotencyBeginResult(
                IdempotencyBeginStatus.Completed,
                System.Text.Json.JsonSerializer.Serialize(new WorkItemMoveResult(item.Id, group.Id, "a0")),
                nameof(WorkItemMoveResult)));

        // A second actor reusing the same OperationId over the same item and
        // target is an actor-swap: deterministic conflict, never a replay of
        // someone else's move.
        IdempotencyStoreMock
            .Setup(s => s.BeginAsync(It.Is<IdempotencyIdentity>(i => i.RequestHash == Sha256($"{item.Id:N}:{group.Id:N}:{otherExecutor:N}")), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IdempotencyBeginResult(IdempotencyBeginStatus.PayloadMismatch, null, null));

        AuthorizerMock
            .Setup(a => a.AuthorizeMoveItemAsync(
                It.Is<WorkItemActionIdentity>(i => i.ExecutorUserId == otherExecutor),
                item.Id, group.Id, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await _sut.Invoking(s => s.MoveItemAsync(
                new WorkItemMoveRequest(
                    new WorkItemActionIdentity(
                        operationId, TestAccountId, TestWorkspaceId, otherExecutor),
                    item.Id,
                    group.Id),
                CancellationToken.None))
            .Should().ThrowAsync<WorkItemOperationConflictException>(
                "the executor is part of the execution semantics");
    }

    [Fact]
    public async Task MoveItem_CompletedOperationId_WithSamePayload_ReplaysResult()
    {
        var board = CreateBoard();
        var group = BoardGroup.Create(
            TestAccountId, TestWorkspaceId, board.Id, "Group",
            Color.Create("#00FF00"), FractionalIndex.Create("a0"), TestUserId, TestNow);
        var item = CreateBoardItem(boardId: board.Id, groupId: group.Id);
        SetupBoards(board);
        SetupBoardGroups(group);
        SetupBoardItems(item);
        var operationId = Guid.CreateVersion7();
        var replayed = new WorkItemMoveResult(item.Id, group.Id, "a0");
        IdempotencyStoreMock
            .Setup(s => s.BeginAsync(It.IsAny<IdempotencyIdentity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IdempotencyBeginResult(
                IdempotencyBeginStatus.Completed,
                System.Text.Json.JsonSerializer.Serialize(replayed),
                nameof(WorkItemMoveResult)));

        var result = await _sut.MoveItemAsync(MoveRequest(item.Id, group.Id, operationId), CancellationToken.None);

        result.Should().Be(replayed, "a committed duplicate replays the stored result");
    }

    [Fact]
    public async Task MoveItem_CompletedOperationId_WithConflictingPayload_FailsDeterministically()
    {
        var board = CreateBoard();
        var group = BoardGroup.Create(
            TestAccountId, TestWorkspaceId, board.Id, "Group",
            Color.Create("#00FF00"), FractionalIndex.Create("a0"), TestUserId, TestNow);
        var item = CreateBoardItem(boardId: board.Id, groupId: group.Id);
        SetupBoards(board);
        SetupBoardGroups(group);
        SetupBoardItems(item);
        var operationId = Guid.CreateVersion7();
        var executedForItem = Guid.CreateVersion7();
        IdempotencyStoreMock
            .Setup(s => s.BeginAsync(It.Is<IdempotencyIdentity>(i => i.RequestHash == Sha256($"{executedForItem:N}:{group.Id:N}:{TestUserId:N}")), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IdempotencyBeginResult(
                IdempotencyBeginStatus.Completed,
                System.Text.Json.JsonSerializer.Serialize(new WorkItemMoveResult(executedForItem, group.Id, "a0")),
                nameof(WorkItemMoveResult)));

        // The store reports a hash mismatch between the executed payload and
        // this conflicting retry.
        IdempotencyStoreMock
            .Setup(s => s.BeginAsync(It.Is<IdempotencyIdentity>(i => i.RequestHash == Sha256($"{item.Id:N}:{group.Id:N}:{TestUserId:N}")), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IdempotencyBeginResult(IdempotencyBeginStatus.PayloadMismatch, null, null));

        await _sut.Invoking(s => s.MoveItemAsync(MoveRequest(item.Id, group.Id, operationId), CancellationToken.None))
            .Should().ThrowAsync<WorkItemOperationConflictException>(
                "the same OperationId with a different payload is a deterministic conflict");
    }

    private static string Sha256(string value) =>
        Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(value)));
}