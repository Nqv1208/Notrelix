using FluentAssertions;
using Notrelix.Domain.Tests.Freeze;
using Notrelix.Domain.Automation.Scheduled;

namespace Notrelix.Domain.Tests.Automation;

[CoversAggregate(typeof(ScheduledJob))]
public class ScheduledJobTests
{
    [CoversMutation(typeof(ScheduledJob), "Complete(System.DateTimeOffset)", MutationScenario.Event)]
    [CoversMutation(typeof(ScheduledJob), "Fail(System.String,System.DateTimeOffset)", MutationScenario.Event)]
    [CoversMutation(typeof(ScheduledJob), "MarkRunCompleted(System.DateTimeOffset,System.DateTimeOffset)", MutationScenario.Event)]
    [CoversMutation(typeof(ScheduledJob), "UpdateSchedule(Notrelix.Domain.Automation.Scheduled.ScheduleDefinition,System.DateTimeOffset)", MutationScenario.Event)]
    [CoversMutation(typeof(ScheduledJob), "SoftDelete(System.Guid,System.DateTimeOffset,System.String)", MutationScenario.Lifecycle)]
    [CoversMutation(typeof(ScheduledJob), "Restore(System.Guid,System.DateTimeOffset)", MutationScenario.Lifecycle)]
    [Fact]
    public void Create_ShouldSucceed_AndRaiseEvent()
    {
        var schedule = ScheduleDefinition.Create("0 9 * * 1-5");
        var job = ScheduledJob.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), schedule, DateTimeOffset.UtcNow);

        job.Status.Should().Be(ScheduledJobStatus.Active);
        job.Schedule.Should().Be(schedule);
        job.DomainEvents.Should().ContainSingle(e => e is ScheduledJobCreatedDomainEvent);
    }

    [CoversMutation(typeof(ScheduledJob), "Pause(System.DateTimeOffset)", MutationScenario.Event)]
    [Fact]
    public void Pause_ShouldSetStatus_AndRaiseEvent()
    {
        var job = CreateJob();
        ((IHasDomainEvents)job).ClearDomainEvents();

        job.Pause(DateTimeOffset.UtcNow);

        job.Status.Should().Be(ScheduledJobStatus.Paused);
        job.DomainEvents.Should().ContainSingle(e => e is ScheduledJobPausedDomainEvent);
    }

    [CoversMutation(typeof(ScheduledJob), "Pause(System.DateTimeOffset)", MutationScenario.NoOp)]
    [Fact]
    public void Pause_WhenAlreadyPaused_ShouldBeNoOp()
    {
        var job = CreateJob();
        job.Pause(DateTimeOffset.UtcNow);
        ((IHasDomainEvents)job).ClearDomainEvents();

        job.Pause(DateTimeOffset.UtcNow);

        job.DomainEvents.Should().BeEmpty();
    }

    [CoversMutation(typeof(ScheduledJob), "Resume(System.DateTimeOffset)", MutationScenario.Valid)]
    [Fact]
    public void Resume_ShouldSetStatus()
    {
        var job = CreateJob();
        job.Pause(DateTimeOffset.UtcNow);

        job.Resume(DateTimeOffset.UtcNow);

        job.Status.Should().Be(ScheduledJobStatus.Active);
    }

    [CoversMutation(typeof(ScheduledJob), "Pause(System.DateTimeOffset)", MutationScenario.NoOp)]
    [Fact]
    public void Resume_WhenNotPaused_ShouldBeNoOp()
    {
        var job = CreateJob();

        job.Resume(DateTimeOffset.UtcNow);

        job.Status.Should().Be(ScheduledJobStatus.Active);
    }

    [CoversMutation(typeof(ScheduledJob), "Cancel(System.DateTimeOffset)", MutationScenario.Valid)]
    [Fact]
    public void Cancel_ShouldSetStatus()
    {
        var job = CreateJob();

        job.Cancel(DateTimeOffset.UtcNow);

        job.Status.Should().Be(ScheduledJobStatus.Cancelled);
    }

    [CoversMutation(typeof(ScheduledJob), "Cancel(System.DateTimeOffset)", MutationScenario.NoOp)]
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
