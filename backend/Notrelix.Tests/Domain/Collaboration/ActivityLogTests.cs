using FluentAssertions;
using Notrelix.Domain.Collaboration.Activity;
using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;
using Xunit;

namespace Notrelix.Domain.Tests.Collaboration;

public class ActivityLogTests
{
    [Fact]
    public void Record_ShouldCreateLog()
    {
        var workspaceId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var target = ResourceRef.Create("Board", Guid.NewGuid());

        var timestamp = DateTimeOffset.UtcNow;
        var log = ActivityLog.Record(workspaceId, actorId, ActivityType.Created, target, timestamp);

        log.WorkspaceId.Should().Be(workspaceId);
        log.ActorId.Should().Be(actorId);
        log.Type.Should().Be(ActivityType.Created);
        log.Target.Should().Be(target);
        log.Timestamp.Should().Be(timestamp);
    }
}
