using Notrelix.Application.Features.Analytics.Projections.WorkItemPlacement;

namespace Notrelix.Application.Tests.Features.Analytics;

/// <summary>
/// TAC-AR-005 / TAC-AR-012 — the projection's single producer-timestamp
/// watermark semantics: live facts apply only when strictly newer, rebuild
/// may repair at an equal watermark but never overwrite a newer live fact.
/// </summary>
public class WorkspaceWorkItemPlacementProjectionTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid AccountId = Guid.NewGuid();
    private static readonly Guid WorkspaceId = Guid.NewGuid();
    private static readonly Guid ItemId = Guid.NewGuid();
    private static readonly Guid BoardId = Guid.NewGuid();

    private static WorkspaceWorkItemPlacementProjection Projected(DateTimeOffset occurredAt) =>
        WorkspaceWorkItemPlacementProjection.Upsert(
            AccountId, WorkspaceId, ItemId, BoardId, Guid.NewGuid(), isArchived: false, occurredAt);

    [Fact]
    public void Upsert_StoresProducerTimestampWatermark()
    {
        var projection = Projected(Now);

        projection.SourceRevision.Should().Be(Now.UtcTicks,
            "TAC-AR-012: SourceRevision is the LastOccurredAt tick watermark — a single scale");
        projection.LastOccurredAt.Should().Be(Now);
    }

    [Fact]
    public void ApplyNewer_StrictlyNewerWatermark_IsApplied()
    {
        var projection = Projected(Now);
        var newerGroup = Guid.NewGuid();

        projection.ApplyNewer(BoardId, newerGroup, isArchived: false, Now.AddMilliseconds(1)).Should().BeTrue();

        projection.GroupId.Should().Be(newerGroup);
        projection.SourceRevision.Should().Be(Now.AddMilliseconds(1).UtcTicks);
    }

    [Fact]
    public void ApplyNewer_EqualOrOlderWatermark_IsIgnored()
    {
        var projection = Projected(Now);
        var originalGroup = projection.GroupId;

        projection.ApplyNewer(BoardId, Guid.NewGuid(), false, Now).Should().BeFalse(
            "a duplicate delivery at the same watermark must be a semantic no-op");
        projection.ApplyNewer(BoardId, Guid.NewGuid(), false, Now.AddSeconds(-1)).Should().BeFalse(
            "a stale out-of-order fact must not regress the projection");

        projection.GroupId.Should().Be(originalGroup);
        projection.SourceRevision.Should().Be(Now.UtcTicks);
    }

    [Fact]
    public void Reconcile_NewerWatermark_IsApplied()
    {
        var projection = Projected(Now);
        var producerGroup = Guid.NewGuid();

        projection.Reconcile(BoardId, producerGroup, isArchived: true, Now.AddMinutes(1)).Should().BeTrue();

        projection.GroupId.Should().Be(producerGroup);
        projection.IsArchived.Should().BeTrue();
    }

    [Fact]
    public void Reconcile_EqualWatermark_RepairsDrift()
    {
        // Drifted local payload (e.g. a bad live projection) at the same
        // watermark as the producer snapshot.
        var projection = WorkspaceWorkItemPlacementProjection.Upsert(
            AccountId, WorkspaceId, ItemId, BoardId, Guid.NewGuid(), isArchived: true, Now);
        var producerGroup = Guid.NewGuid();

        projection.Reconcile(BoardId, producerGroup, isArchived: false, Now).Should().BeTrue(
            "AR-FLOW-03: a rebuild may repair payload drift while the watermark is unchanged");

        projection.GroupId.Should().Be(producerGroup);
        projection.IsArchived.Should().BeFalse();
        projection.SourceRevision.Should().Be(Now.UtcTicks);
    }

    [Fact]
    public void Reconcile_OlderSnapshot_PreservesNewerLocalState()
    {
        var projection = Projected(Now.AddMinutes(1));
        var liveGroup = projection.GroupId;

        projection.Reconcile(BoardId, Guid.NewGuid(), isArchived: false, Now).Should().BeFalse(
            "AR-FLOW-04: an older snapshot must never overwrite a newer live fact");

        projection.GroupId.Should().Be(liveGroup);
        projection.SourceRevision.Should().Be(Now.AddMinutes(1).UtcTicks);
    }
}
