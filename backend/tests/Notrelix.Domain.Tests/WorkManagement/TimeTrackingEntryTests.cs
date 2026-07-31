using FluentAssertions;
using Notrelix.Domain.WorkManagement.Items;
using Notrelix.Domain.Tests.Freeze;

namespace Notrelix.Domain.Tests.WorkManagement;

public class TimeTrackingEntryTests
{
    private readonly Guid _accountId = Guid.NewGuid();
    private readonly Guid _workspaceId = Guid.NewGuid();
    private readonly Guid _boardId = Guid.NewGuid();
    private readonly Guid _itemId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly DateTimeOffset _now = DateTimeOffset.UtcNow;

    [Fact]
    public void Start_WithValidData_ShouldSucceed()
    {
        var entry = TimeTrackingEntry.Start(_accountId, _workspaceId, _boardId, _itemId, _userId, _now, "Working on feature");

        entry.AccountId.Should().Be(_accountId);
        entry.WorkspaceId.Should().Be(_workspaceId);
        entry.BoardId.Should().Be(_boardId);
        entry.ItemId.Should().Be(_itemId);
        entry.UserId.Should().Be(_userId);
        entry.StartedAt.Should().Be(_now);
        entry.Status.Should().Be(TimeTrackingStatus.Running);
        entry.Note.Should().Be("Working on feature");
        entry.EndedAt.Should().BeNull();
        entry.DurationSeconds.Should().BeNull();
    }

    [Fact]
    public void Start_WithoutNote_ShouldSucceed()
    {
        var entry = TimeTrackingEntry.Start(_accountId, _workspaceId, _boardId, _itemId, _userId, _now);

        entry.Note.Should().BeNull();
    }

    [Fact]
    public void Start_WithEmptyWorkspaceId_ShouldThrow()
    {
        var act = () => TimeTrackingEntry.Start(_accountId, Guid.Empty, _boardId, _itemId, _userId, _now);

        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Start_WithEmptyItemId_ShouldThrow()
    {
        var act = () => TimeTrackingEntry.Start(_accountId, _workspaceId, _boardId, Guid.Empty, _userId, _now);

        act.Should().Throw<BusinessRuleException>();
    }

    [CoversMutation(typeof(TimeTrackingEntry), nameof(TimeTrackingEntry.Stop), MutationScenario.Valid, typeof(DateTimeOffset), typeof(Guid))]
    [Fact]
    public void Stop_WhenRunning_ShouldSucceed()
    {
        var entry = TimeTrackingEntry.Start(_accountId, _workspaceId, _boardId, _itemId, _userId, _now);
        var endedAt = _now.AddMinutes(30);

        entry.Stop(endedAt, _userId);

        entry.Status.Should().Be(TimeTrackingStatus.Stopped);
        entry.EndedAt.Should().Be(endedAt);
    }

    [CoversMutation(typeof(TimeTrackingEntry), nameof(TimeTrackingEntry.Stop), MutationScenario.Valid, typeof(DateTimeOffset), typeof(Guid))]
    [Fact]
    public void Stop_ShouldCalculateDurationSeconds()
    {
        var entry = TimeTrackingEntry.Start(_accountId, _workspaceId, _boardId, _itemId, _userId, _now);
        var endedAt = _now.AddMinutes(5);

        entry.Stop(endedAt, _userId);

        entry.DurationSeconds.Should().Be(300);
    }

    [CoversMutation(typeof(TimeTrackingEntry), nameof(TimeTrackingEntry.Stop), MutationScenario.Invalid, typeof(DateTimeOffset), typeof(Guid))]
    [Fact]
    public void Stop_WhenNotRunning_ShouldThrow()
    {
        var entry = TimeTrackingEntry.Start(_accountId, _workspaceId, _boardId, _itemId, _userId, _now);
        entry.Stop(_now.AddMinutes(10), _userId);

        var act = () => entry.Stop(_now.AddMinutes(20), _userId);

        act.Should().Throw<BusinessRuleException>()
            .WithMessage("*not running*");
    }

    [CoversMutation(typeof(TimeTrackingEntry), nameof(TimeTrackingEntry.Stop), MutationScenario.Invalid, typeof(DateTimeOffset), typeof(Guid))]
    [Fact]
    public void Stop_WithEndTimeBeforeStart_ShouldThrow()
    {
        var entry = TimeTrackingEntry.Start(_accountId, _workspaceId, _boardId, _itemId, _userId, _now);

        var act = () => entry.Stop(_now.AddMinutes(-5), _userId);

        act.Should().Throw<BusinessRuleException>()
            .WithMessage("*after start*");
    }

    [CoversMutation(typeof(TimeTrackingEntry), nameof(TimeTrackingEntry.Stop), MutationScenario.Version, typeof(DateTimeOffset), typeof(Guid))]
    [Fact]
    public void Stop_ShouldIncrementVersion()
    {
        var entry = TimeTrackingEntry.Start(_accountId, _workspaceId, _boardId, _itemId, _userId, _now);
        var versionBefore = entry.Version;

        entry.Stop(_now.AddMinutes(10), _userId);

        entry.Version.Should().Be(versionBefore + 1);
    }

    [CoversMutation(typeof(TimeTrackingEntry), nameof(TimeTrackingEntry.Stop), MutationScenario.Audit, typeof(DateTimeOffset), typeof(Guid))]
    [Fact]
    public void Stop_ShouldSetAudit()
    {
        var entry = TimeTrackingEntry.Start(_accountId, _workspaceId, _boardId, _itemId, _userId, _now);
        var stoppedBy = Guid.NewGuid();
        var endedAt = _now.AddMinutes(15);

        entry.Stop(endedAt, stoppedBy);

        entry.UpdatedBy.Should().Be(stoppedBy);
        entry.UpdatedAt.Should().Be(endedAt);
    }
}
