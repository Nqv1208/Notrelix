using FluentAssertions;
using Notrelix.Domain.Automation.Scheduled;

namespace Notrelix.Domain.Tests.Automation;

public class ScheduledJobTests
{
    [Fact]
    public void Create_ShouldSucceed_AndRaiseEvent()
    {
        var schedule = ScheduleDefinition.Create("0 9 * * 1-5");
        var job = ScheduledJob.Create(Guid.NewGuid(), Guid.NewGuid(), schedule, DateTimeOffset.UtcNow);

        job.Status.Should().Be(ScheduledJobStatus.Active);
        job.Schedule.Should().Be(schedule);
        job.DomainEvents.Should().ContainSingle(e => e is ScheduledJobCreatedDomainEvent);
    }

    [Fact]
    public void Pause_ShouldSetStatus_AndRaiseEvent()
    {
        var job = CreateJob();
        job.ClearDomainEvents();

        job.Pause(DateTimeOffset.UtcNow);

        job.Status.Should().Be(ScheduledJobStatus.Paused);
        job.DomainEvents.Should().ContainSingle(e => e is ScheduledJobPausedDomainEvent);
    }

    [Fact]
    public void Pause_WhenAlreadyPaused_ShouldBeNoOp()
    {
        var job = CreateJob();
        job.Pause(DateTimeOffset.UtcNow);
        job.ClearDomainEvents();

        job.Pause(DateTimeOffset.UtcNow);

        job.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Resume_ShouldSetStatus()
    {
        var job = CreateJob();
        job.Pause(DateTimeOffset.UtcNow);

        job.Resume(DateTimeOffset.UtcNow);

        job.Status.Should().Be(ScheduledJobStatus.Active);
    }

    [Fact]
    public void Resume_WhenNotPaused_ShouldBeNoOp()
    {
        var job = CreateJob();

        job.Resume(DateTimeOffset.UtcNow);

        job.Status.Should().Be(ScheduledJobStatus.Active);
    }

    [Fact]
    public void Cancel_ShouldSetStatus()
    {
        var job = CreateJob();

        job.Cancel(DateTimeOffset.UtcNow);

        job.Status.Should().Be(ScheduledJobStatus.Cancelled);
    }

    [Fact]
    public void Cancel_WhenAlreadyCancelled_ShouldBeNoOp()
    {
        var job = CreateJob();
        job.Cancel(DateTimeOffset.UtcNow);

        job.Cancel(DateTimeOffset.UtcNow);

        job.Status.Should().Be(ScheduledJobStatus.Cancelled);
    }

    private static ScheduledJob CreateJob()
    {
        return ScheduledJob.Create(Guid.NewGuid(), Guid.NewGuid(), ScheduleDefinition.Create("0 0 * * *"), DateTimeOffset.UtcNow);
    }
}
