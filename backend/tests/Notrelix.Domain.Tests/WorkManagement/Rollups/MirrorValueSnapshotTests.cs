using FluentAssertions;
using Notrelix.Domain.WorkManagement.Relations;

namespace Notrelix.Domain.Tests.WorkManagement;

public class MirrorValueSnapshotTests
{
    [Fact]
    public void Create_ShouldSucceed()
    {
        var workspaceId = Guid.NewGuid();
        var relationId = Guid.NewGuid();
        var connectionId = Guid.NewGuid();
        var sourceFieldId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var snapshot = MirrorValueSnapshot.Create(workspaceId, relationId, connectionId, sourceFieldId, null, "{\"val\":1}", "hash123", now);

        snapshot.WorkspaceId.Should().Be(workspaceId);
        snapshot.RelationId.Should().Be(relationId);
        snapshot.ConnectionId.Should().Be(connectionId);
        snapshot.SourceFieldId.Should().Be(sourceFieldId);
        snapshot.ValueJson.Should().Be("{\"val\":1}");
        snapshot.ValueHash.Should().Be("hash123");
        snapshot.IsStale.Should().BeFalse();
        snapshot.ComputedAt.Should().Be(now);
    }

    [Fact]
    public void Create_WithEmptyWorkspaceId_ShouldThrow()
    {
        var act = () => MirrorValueSnapshot.Create(Guid.Empty, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null, null, null, DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Create_WithEmptyRelationId_ShouldThrow()
    {
        var act = () => MirrorValueSnapshot.Create(Guid.NewGuid(), Guid.Empty, Guid.NewGuid(), Guid.NewGuid(), null, null, null, DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Create_WithEmptyConnectionId_ShouldThrow()
    {
        var act = () => MirrorValueSnapshot.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.Empty, Guid.NewGuid(), null, null, null, DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Create_WithEmptySourceFieldId_ShouldThrow()
    {
        var act = () => MirrorValueSnapshot.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.Empty, null, null, null, DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void MarkStale_ShouldSetIsStale()
    {
        var snapshot = CreateSnapshot();
        snapshot.MarkStale();
        snapshot.IsStale.Should().BeTrue();
    }

    [Fact]
    public void UpdateValue_ShouldResetStaleAndUpdate()
    {
        var snapshot = CreateSnapshot();
        snapshot.MarkStale();

        var now = DateTimeOffset.UtcNow;
        snapshot.UpdateValue("{\"val\":2}", "hash456", now);

        snapshot.ValueJson.Should().Be("{\"val\":2}");
        snapshot.ValueHash.Should().Be("hash456");
        snapshot.IsStale.Should().BeFalse();
        snapshot.ComputedAt.Should().Be(now);
    }

    [Fact]
    public void UpdateValue_WithNulls_ShouldClearValues()
    {
        var snapshot = CreateSnapshot();
        var now = DateTimeOffset.UtcNow;

        snapshot.UpdateValue(null, null, now);

        snapshot.ValueJson.Should().BeNull();
        snapshot.ValueHash.Should().BeNull();
        snapshot.IsStale.Should().BeFalse();
    }

    private static MirrorValueSnapshot CreateSnapshot()
    {
        return MirrorValueSnapshot.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            null, "{\"val\":1}", "hash123", DateTimeOffset.UtcNow);
    }
}
