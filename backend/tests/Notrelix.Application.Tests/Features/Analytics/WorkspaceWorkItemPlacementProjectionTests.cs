using Notrelix.Application.Features.Analytics.Projections.WorkItemPlacement;

namespace Notrelix.Application.Tests.Features.Analytics;

/// <summary>
/// TAC-AR-005 / TAC-AR-012 — the projection's single producer-revision
/// ordering semantics: live facts apply only when strictly newer, rebuild
/// may repair at an equal revision but never overwrite a newer live fact.
/// OccurredAt is carried as metadata; the aggregate version is the semantic
/// ordering authority.
/// </summary>
public class WorkspaceWorkItemPlacementProjectionTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid AccountId = Guid.NewGuid();
    private static readonly Guid WorkspaceId = Guid.NewGuid();
    private static readonly Guid ItemId = Guid.NewGuid();
    private static readonly Guid BoardId = Guid.NewGuid();

    private static WorkspaceWorkItemPlacementProjection Projected(long revision = 5) =>
        WorkspaceWorkItemPlacementProjection.Upsert(
            AccountId, WorkspaceId, ItemId, BoardId, Guid.NewGuid(), isArchived: false, revision, Now);

    [Fact]
    public void Upsert_StoresProducerRevision()
    {
        var projection = Projected(revision: 5);

        projection.SourceRevision.Should().Be(5,
            "TAC-AR-012: SourceRevision is the producer aggregate version — a single revision scale");
        projection.LastOccurredAt.Should().Be(Now);
    }

    [Fact]
    public void ApplyNewer_StrictlyNewerRevision_IsApplied()
    {
        var projection = Projected(revision: 5);
        var newerGroup = Guid.NewGuid();

        projection.ApplyNewer(BoardId, newerGroup, isArchived: false, revision: 6, Now.AddMilliseconds(1)).Should().BeTrue();

        projection.GroupId.Should().Be(newerGroup);
        projection.SourceRevision.Should().Be(6);
        projection.LastOccurredAt.Should().Be(Now.AddMilliseconds(1));
    }

    [Fact]
    public void ApplyNewer_EqualOrOlderRevision_IsIgnored()
    {
        var projection = Projected(revision: 5);
        var originalGroup = projection.GroupId;

        projection.ApplyNewer(BoardId, Guid.NewGuid(), false, revision: 5, Now).Should().BeFalse(
            "a duplicate delivery at the same revision must be a semantic no-op");
        projection.ApplyNewer(BoardId, Guid.NewGuid(), false, revision: 4, Now.AddSeconds(-1)).Should().BeFalse(
            "a stale out-of-order fact must not regress the projection");

        projection.GroupId.Should().Be(originalGroup);
        projection.SourceRevision.Should().Be(5);
    }

    [Fact]
    public void Reconcile_NewerRevision_IsApplied()
    {
        var projection = Projected(revision: 5);
        var producerGroup = Guid.NewGuid();

        projection.Reconcile(BoardId, producerGroup, isArchived: true, revision: 6, Now.AddMinutes(1)).Should().BeTrue();

        projection.GroupId.Should().Be(producerGroup);
        projection.IsArchived.Should().BeTrue();
    }

    [Fact]
    public void Reconcile_EqualRevision_RepairsDrift()
    {
        // Drifted local payload (e.g. a bad live projection) at the same
        // revision as the producer snapshot.
        var projection = WorkspaceWorkItemPlacementProjection.Upsert(
            AccountId, WorkspaceId, ItemId, BoardId, Guid.NewGuid(), isArchived: true, revision: 5, Now);
        var producerGroup = Guid.NewGuid();

        projection.Reconcile(BoardId, producerGroup, isArchived: false, revision: 5, Now).Should().BeTrue(
            "AR-FLOW-03: a rebuild may repair payload drift while the revision is unchanged");

        projection.GroupId.Should().Be(producerGroup);
        projection.IsArchived.Should().BeFalse();
        projection.SourceRevision.Should().Be(5);
    }

    [Fact]
    public void Reconcile_OlderSnapshot_PreservesNewerLocalState()
    {
        var projection = Projected(revision: 7);
        var liveGroup = projection.GroupId;

        projection.Reconcile(BoardId, Guid.NewGuid(), isArchived: false, revision: 2, Now).Should().BeFalse(
            "AR-FLOW-04: an older snapshot must never overwrite a newer live fact");

        projection.GroupId.Should().Be(liveGroup);
        projection.SourceRevision.Should().Be(7);
    }
}