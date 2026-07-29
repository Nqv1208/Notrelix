using FluentAssertions;
using Notrelix.Domain.Integrations.Connections;
using Notrelix.Domain.Integrations.Sync;
using Xunit;

namespace Notrelix.Domain.Tests.Integrations.Sync;

public class SyncTransitionMatrixTests
{
    [Fact]
    public void Create_ShouldSetInitialCursor()
    {
        var cursor = IntegrationSyncCursor.Create(Guid.NewGuid(), "BoardItem",
            SyncCursorValue.Create("abc"), DateTimeOffset.UtcNow);
        cursor.Cursor.Value.Should().Be("abc");
    }

    [Fact]
    public void UpdateCursor_ShouldChangeValue()
    {
        var cursor = IntegrationSyncCursor.Create(Guid.NewGuid(), "BoardItem",
            SyncCursorValue.Create("abc"), DateTimeOffset.UtcNow);
        cursor.UpdateCursor(SyncCursorValue.Create("def"), DateTimeOffset.UtcNow);
        cursor.Cursor.Value.Should().Be("def");
    }

    [Fact]
    public void UpdateCursor_ShouldUpdateTimestamp()
    {
        var now = DateTimeOffset.UtcNow;
        var later = now.AddMinutes(5);
        var cursor = IntegrationSyncCursor.Create(Guid.NewGuid(), "BoardItem",
            SyncCursorValue.Create("abc"), now);
        cursor.UpdateCursor(SyncCursorValue.Create("def"), later);
        cursor.LastSyncedAt.Should().Be(later);
    }

    [Fact]
    public void Create_WithEmptyConnectionId_ShouldThrow()
    {
        var act = () => IntegrationSyncCursor.Create(Guid.Empty, "BoardItem",
            SyncCursorValue.Create("abc"), DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Create_WithEmptyResourceType_ShouldThrow()
    {
        var act = () => IntegrationSyncCursor.Create(Guid.NewGuid(), "",
            SyncCursorValue.Create("abc"), DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void SyncCursorValue_Equality()
    {
        var a = SyncCursorValue.Create("abc");
        var b = SyncCursorValue.Create("abc");
        var c = SyncCursorValue.Create("def");
        a.Should().Be(b);
        a.Should().NotBe(c);
    }
}
