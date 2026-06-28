using FluentAssertions;
using Notrelix.Domain.Collaboration.Activity;

namespace Notrelix.Domain.Tests.Collaboration;

public class ActivityLogTests
{
    [Fact]
    public void Record_ShouldCreateLog()
    {
        var workspaceId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var target = ResourceRef.Create(ResourceType.Board, Guid.NewGuid());

        var timestamp = DateTimeOffset.UtcNow;
        var log = ActivityLog.Record(workspaceId, actorId, ActivityType.Created, target, timestamp);

        log.WorkspaceId.Should().Be(workspaceId);
        log.ActorId.Should().Be(actorId);
        log.Type.Should().Be(ActivityType.Created);
        log.Target.Should().Be(target);
        log.Timestamp.Should().Be(timestamp);
    }

    [Fact]
    public void Record_WithMetadata_ShouldSetMetadata()
    {
        var metadata = ActivityMetadata.Create(JsonValue.Create("{\"source\":\"api\"}"));

        var log = ActivityLog.Record(
            Guid.NewGuid(), Guid.NewGuid(), ActivityType.Updated,
            ResourceRef.Create(ResourceType.BoardItem, Guid.NewGuid()),
            DateTimeOffset.UtcNow, metadata: metadata);

        log.Metadata.Should().Be(metadata);
    }

    [Fact]
    public void Record_WithWorkspaceMismatchTarget_ShouldThrow()
    {
        var workspaceId = Guid.NewGuid();
        var target = ResourceRef.Create(ResourceType.Page, Guid.NewGuid(), Guid.NewGuid());

        var act = () => ActivityLog.Record(workspaceId, Guid.NewGuid(), ActivityType.Created, target, DateTimeOffset.UtcNow);
        act.Should().Throw<WorkspaceMismatchException>();
    }

    [Fact]
    public void Record_WithEmptyWorkspaceId_ShouldThrow()
    {
        var act = () => ActivityLog.Record(Guid.Empty, Guid.NewGuid(), ActivityType.Created, ResourceRef.Create(ResourceType.Page, Guid.NewGuid()), DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Record_WithEmptyActorId_ShouldThrow()
    {
        var act = () => ActivityLog.Record(Guid.NewGuid(), Guid.Empty, ActivityType.Created, ResourceRef.Create(ResourceType.Page, Guid.NewGuid()), DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Record_ShouldSetTimestamp()
    {
        var timestamp = DateTimeOffset.UtcNow;
        var log = ActivityLog.Record(Guid.NewGuid(), Guid.NewGuid(), ActivityType.Deleted,
            ResourceRef.Create(ResourceType.Board, Guid.NewGuid()), timestamp);

        log.Timestamp.Should().Be(timestamp);
    }
}
