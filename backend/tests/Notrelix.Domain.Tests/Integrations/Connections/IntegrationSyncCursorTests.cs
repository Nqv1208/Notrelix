using FluentAssertions;
using Notrelix.Domain.Integrations.Sync;

namespace Notrelix.Domain.Tests.Integrations;

public class SyncCursorValueTests
{
    [Fact]
    public void Create_ShouldSucceed()
    {
        var cursor = SyncCursorValue.Create("abc123");
        cursor.Value.Should().Be("abc123");
    }

    [Fact]
    public void Create_WithEmpty_ShouldThrow()
    {
        var act = () => SyncCursorValue.Create("");
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Equality_SameValue_ShouldBeEqual()
    {
        var c1 = SyncCursorValue.Create("token");
        var c2 = SyncCursorValue.Create("token");
        c1.Should().Be(c2);
    }
}

public class IntegrationSyncCursorTests
{
    [Fact]
    public void Create_ShouldSucceed()
    {
        var cursorValue = SyncCursorValue.Create("cursor123");
        var cursor = IntegrationSyncCursor.Create(Guid.NewGuid(), "boards", cursorValue, DateTimeOffset.UtcNow);

        cursor.Cursor.Should().Be(cursorValue);
        cursor.ResourceKind.Should().Be("boards");
    }

    [Fact]
    public void UpdateCursor_ShouldUpdate()
    {
        var cursor = IntegrationSyncCursor.Create(Guid.NewGuid(), "items", SyncCursorValue.Create("old"), DateTimeOffset.UtcNow);
        var newCursor = SyncCursorValue.Create("new");

        cursor.UpdateCursor(newCursor, DateTimeOffset.UtcNow);

        cursor.Cursor.Should().Be(newCursor);
    }

    [Fact]
    public void UpdateCursor_WithNull_ShouldThrow()
    {
        var cursor = IntegrationSyncCursor.Create(Guid.NewGuid(), "items", SyncCursorValue.Create("val"), DateTimeOffset.UtcNow);
        var act = () => cursor.UpdateCursor(null!, DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>();
    }
}
