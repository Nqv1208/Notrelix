using FluentAssertions;
using Notrelix.Domain.Tests.Freeze;
using Notrelix.Domain.Automation.Scheduled;

namespace Notrelix.Domain.Tests.Automation;

[CoversAggregate(typeof(ScheduledJob))]
public class ScheduledJobTests
{
    [CoversMutation(typeof(ScheduledJob), nameof(ScheduledJob.Complete), MutationScenario.Event, typeof(DateTimeOffset))]
    [CoversMutation(typeof(ScheduledJob), nameof(ScheduledJob.Fail), MutationScenario.Event, typeof(string), typeof(DateTimeOffset))]
    [CoversMutation(typeof(ScheduledJob), nameof(ScheduledJob.MarkRunCompleted), MutationScenario.Event, typeof(DateTimeOffset), typeof(DateTimeOffset))]
    [CoversMutation(typeof(ScheduledJob), nameof(ScheduledJob.UpdateSchedule), MutationScenario.Event, typeof(ScheduleDefinition), typeof(DateTimeOffset))]
    [CoversMutation(typeof(ScheduledJob), nameof(ScheduledJob.Delete), MutationScenario.Lifecycle, typeof(Guid), typeof(DateTimeOffset), typeof(string))]
    [CoversMutation(typeof(ScheduledJob), nameof(ScheduledJob.Restore), MutationScenario.Lifecycle, typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void Create_ShouldSucceed_AndRaiseEvent()
    {
        var schedule = ScheduleDefinition.Create("0 9 * * 1-5");
        var job = ScheduledJob.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), schedule, DateTimeOffset.UtcNow);

        job.Status.Should().Be(ScheduledJobStatus.Active);
        job.Schedule.Should().Be(schedule);
        job.DomainEvents.Should().ContainSingle(e => e is ScheduledJobCreatedDomainEvent);
    }

    [CoversMutation(typeof(ScheduledJob), nameof(ScheduledJob.Pause), MutationScenario.Event, typeof(DateTimeOffset))]
    [Fact]
    public void Pause_ShouldSetStatus_AndRaiseEvent()
    {
        var job = CreateJob();
        ((IHasDomainEvents)job).ClearDomainEvents();

        job.Pause(DateTimeOffset.UtcNow);

        job.Status.Should().Be(ScheduledJobStatus.Paused);
        job.DomainEvents.Should().ContainSingle(e => e is ScheduledJobPausedDomainEvent);
    }

    [CoversMutation(typeof(ScheduledJob), nameof(ScheduledJob.Pause), MutationScenario.NoOp, typeof(DateTimeOffset))]
    [Fact]
    public void Pause_WhenAlreadyPaused_ShouldBeNoOp()
    {
        var job = CreateJob();
        job.Pause(DateTimeOffset.UtcNow);
        ((IHasDomainEvents)job).ClearDomainEvents();

        job.Pause(DateTimeOffset.UtcNow);

        job.DomainEvents.Should().BeEmpty();
    }

    [CoversMutation(typeof(ScheduledJob), nameof(ScheduledJob.Resume), MutationScenario.Valid, typeof(DateTimeOffset))]
    [Fact]
    public void Resume_ShouldSetStatus()
    {
        var job = CreateJob();
        job.Pause(DateTimeOffset.UtcNow);

        job.Resume(DateTimeOffset.UtcNow);

        job.Status.Should().Be(ScheduledJobStatus.Active);
    }

    [CoversMutation(typeof(ScheduledJob), nameof(ScheduledJob.Pause), MutationScenario.NoOp, typeof(DateTimeOffset))]
    [Fact]
    public void Resume_WhenNotPaused_ShouldBeNoOp()
    {
        var job = CreateJob();

        job.Resume(DateTimeOffset.UtcNow);

        job.Status.Should().Be(ScheduledJobStatus.Active);
    }

    [CoversMutation(typeof(ScheduledJob), nameof(ScheduledJob.Cancel), MutationScenario.Valid, typeof(DateTimeOffset))]
    [Fact]
    public void Cancel_ShouldSetStatus()
    {
        var job = CreateJob();

        job.Cancel(DateTimeOffset.UtcNow);

        job.Status.Should().Be(ScheduledJobStatus.Cancelled);
    }

    [CoversMutation(typeof(ScheduledJob), nameof(ScheduledJob.Cancel), MutationScenario.NoOp, typeof(DateTimeOffset))]
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
        return ScheduledJob.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), ScheduleDefinition.Create("0 0 * * *"), DateTimeOffset.UtcNow);
    }
}
