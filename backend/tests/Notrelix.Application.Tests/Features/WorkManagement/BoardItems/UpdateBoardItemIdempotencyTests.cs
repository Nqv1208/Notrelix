using Notrelix.Application.Common.Diagnostics;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.BoardItems.Commands.UpdateBoardItem;

namespace Notrelix.Application.Tests.Features.WorkManagement.BoardItems;

/// <summary>
/// The item edit is retry-safe: it is an idempotent request with a stable operation, a retry under the same key
/// replays the stored result without running the handler (so the moved version is never looked at), and the same
/// key with another payload is refused.
/// </summary>
public class UpdateBoardItemIdempotencyTests
{
    private const string Operation = "work-management.board-items.update-board-item.v1";
    private static readonly Guid ItemId = Guid.CreateVersion7();
    private readonly Mock<IIdempotencyStore> _store = new();
    private readonly Mock<IIdempotencyExecutionContext> _executionContext = new();
    private readonly Mock<ICurrentTenantContext> _tenant = new();
    private readonly IdempotencyBehavior<UpdateBoardItemCommand, Result> _behavior;
    private readonly List<IdempotencyIdentity> _identities = [];

    public UpdateBoardItemIdempotencyTests()
    {
        _executionContext.Setup(c => c.RequireKey()).Returns("edit-retry-key-1");
        _tenant.SetupGet(t => t.AccountId).Returns(Guid.NewGuid());
        _tenant.SetupGet(t => t.UserId).Returns(Guid.NewGuid());
        _behavior = new IdempotencyBehavior<UpdateBoardItemCommand, Result>(
            _store.Object,
            new JsonIdempotencyRequestFingerprint(),
            new Mock<IIdempotencyReplayPolicy>().Object,
            new IdempotencyPartitionFactory(_tenant.Object),
            _executionContext.Object,
            new Mock<IIdempotencyExecutionContextWriter>().Object,
            new Mock<ILogger<IdempotencyBehavior<UpdateBoardItemCommand, Result>>>().Object,
            new PipelineMetrics());
    }

    private static UpdateBoardItemCommand Edit(string title, long version = 3) => new(ItemId, title, null, null, version);

    [Fact]
    public void TheCommand_IsIdempotent_WithAStableOperation()
    {
        typeof(IIdempotentRequest).IsAssignableFrom(typeof(UpdateBoardItemCommand)).Should().BeTrue();
        IdempotencyOperationMetadata.Resolve<UpdateBoardItemCommand>().Should().Be(Operation);
    }

    [Fact]
    public async Task FirstSend_RunsTheHandler_AndStoresTheResult()
    {
        _store.Setup(s => s.BeginAsync(It.IsAny<IdempotencyIdentity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IdempotencyBeginResult(IdempotencyBeginStatus.Started, null, null));
        var handlerRuns = 0;

        var result = await _behavior.Handle(Edit("Renamed"), _ => { handlerRuns++; return Task.FromResult(Result.Success()); }, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        handlerRuns.Should().Be(1);
        _store.Verify(s => s.CompleteAsync(It.IsAny<IdempotencyIdentity>(), It.IsAny<string>(), Operation, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RetryWithTheSameKeyAfterTheCommit_ReplaysTheFirstResult_WithoutRunningTheHandler()
    {
        var stored = System.Text.Json.JsonSerializer.Serialize(Result.Success(), IdempotencyJson.Options);
        _store.Setup(s => s.BeginAsync(It.IsAny<IdempotencyIdentity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IdempotencyBeginResult(IdempotencyBeginStatus.Completed, stored, Operation));
        var handlerRuns = 0;

        var replay = await _behavior.Handle(Edit("Renamed"), _ => { handlerRuns++; return Task.FromResult(Result.Success()); }, CancellationToken.None);

        replay.Succeeded.Should().BeTrue();
        handlerRuns.Should().Be(0, "the retry replays the first result and writes nothing");
    }

    [Fact]
    public async Task SameKeyWithAnotherPayload_IsRefused_AndTheHandlerNeverRuns()
    {
        _store.Setup(s => s.BeginAsync(It.IsAny<IdempotencyIdentity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IdempotencyBeginResult(IdempotencyBeginStatus.PayloadMismatch, null, null));
        var handlerRuns = 0;

        var act = () => _behavior.Handle(Edit("Another title"), _ => { handlerRuns++; return Task.FromResult(Result.Success()); }, CancellationToken.None);

        await act.Should().ThrowAsync<IdempotencyPayloadMismatchException>();
        handlerRuns.Should().Be(0);
    }

    [Fact]
    public async Task TheFingerprint_FollowsTheTitleAndTheVersion_SoOnlyAnIdenticalRetryReplays()
    {
        _store.Setup(s => s.BeginAsync(It.IsAny<IdempotencyIdentity>(), It.IsAny<CancellationToken>()))
            .Callback<IdempotencyIdentity, CancellationToken>((identity, _) => _identities.Add(identity))
            .ReturnsAsync(new IdempotencyBeginResult(IdempotencyBeginStatus.Started, null, null));

        foreach (var command in new[] { Edit("A"), Edit("A"), Edit("B"), Edit("A", version: 4) })
            await _behavior.Handle(command, _ => Task.FromResult(Result.Success()), CancellationToken.None);

        _identities[0].RequestHash.Should().Be(_identities[1].RequestHash);
        _identities[0].RequestHash.Should().NotBe(_identities[2].RequestHash);
        _identities[0].RequestHash.Should().NotBe(_identities[3].RequestHash);
    }
}
