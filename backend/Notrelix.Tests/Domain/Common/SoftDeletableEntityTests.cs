using FluentAssertions;
using Notrelix.Domain.Common;
using Xunit;

namespace Notrelix.Domain.Tests.Common;

public class SoftDeletableEntityTests
{
    private class TestEntity : SoftDeletableEntity
    {
    }

    [Fact]
    public void SoftDelete_ShouldSetDeletedProperties()
    {
        var entity = new TestEntity();
        var deletedBy = Guid.NewGuid();
        var deletedAt = DateTimeOffset.UtcNow;

        entity.SoftDelete(deletedBy, deletedAt, "Cleanup");

        entity.IsDeleted.Should().BeTrue();
        entity.DeletedAt.Should().Be(deletedAt);
        entity.DeletedBy.Should().Be(deletedBy);
        entity.DeleteReason.Should().Be("Cleanup");
    }

    [Fact]
    public void Restore_ShouldClearDeletedProperties()
    {
        var entity = new TestEntity();
        var userId = Guid.NewGuid();
        var time = DateTimeOffset.UtcNow;
        
        entity.SoftDelete(userId, time);
        entity.Restore(userId, time.AddMinutes(1));

        entity.IsDeleted.Should().BeFalse();
        entity.DeletedAt.Should().BeNull();
        entity.DeletedBy.Should().BeNull();
        entity.DeleteReason.Should().BeNull();
        entity.UpdatedBy.Should().Be(userId);
        entity.UpdatedAt.Should().Be(time.AddMinutes(1));
    }
}
