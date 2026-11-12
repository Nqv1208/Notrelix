using FluentAssertions;
using Notrelix.Domain.Chat.Exports;
using Notrelix.Domain.Chat.Rules;
using BusinessRuleException = Notrelix.Domain.Common.Exceptions.BusinessRuleException;

namespace Notrelix.Domain.Tests.Chat.Exports;

/// <summary>UC-CHT-H06: the export job's lifecycle and its invariants.</summary>
public class ChatExportJobTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 12, 9, 0, 0, TimeSpan.Zero);
    private static readonly TimeSpan Life = TimeSpan.FromHours(ChatLimits.ExportLifetimeHours);
    private readonly Guid _account = Guid.NewGuid();
    private readonly Guid _workspace = Guid.NewGuid();
    private readonly Guid _admin = Guid.NewGuid();
    private readonly Guid _subject = Guid.NewGuid();

    private ChatExportJob Job(DateTimeOffset? from = null, DateTimeOffset? to = null) =>
        ChatExportJob.Request(_account, _workspace, _admin, _subject, from, to, Now);

    [Fact]
    public void ARequestIsQueued_WithWhoAskedAndWhoseData()
    {
        var job = Job(Now.AddDays(-30), Now);

        job.Status.Should().Be(ChatExportStatus.Queued);
        job.RequestedBy.Should().Be(_admin);
        job.SubjectUserId.Should().Be(_subject);
        job.RangeFrom.Should().Be(Now.AddDays(-30));
        job.IsDownloadableAt(Now).Should().BeFalse();
    }

    [Fact]
    public void ARangeThatEndsBeforeItStartsIsRefused()
    {
        var act = () => Job(Now, Now.AddDays(-1));

        act.Should().Throw<BusinessRuleException>().Which.RuleCode.Should().Be(ChatRuleCodes.ExportInvalid);
    }

    [Fact]
    public void ARangeOfZeroLengthIsRefused()
    {
        var act = () => Job(Now, Now);

        act.Should().Throw<BusinessRuleException>().Which.RuleCode.Should().Be(ChatRuleCodes.ExportInvalid);
    }

    [Fact]
    public void ARunThatSucceedsIsReadyWithCountsAndAnExpiry_ThenDownloadableUntilItExpires()
    {
        var job = Job();
        job.Start(Now).Should().BeTrue();
        job.Status.Should().Be(ChatExportStatus.Running);

        job.Complete("acc/ws/chat-exports/x", 1234, 10, 2, 1, 3, Now.AddMinutes(1), Life);

        job.Status.Should().Be(ChatExportStatus.Ready);
        job.MessageCount.Should().Be(10);
        job.SpaceCount.Should().Be(2);
        job.ExcludedSpaceCount.Should().Be(1);
        job.TombstoneCount.Should().Be(3);
        job.ExpiresAt.Should().Be(Now.AddMinutes(1) + Life);
        job.IsDownloadableAt(Now.AddHours(23)).Should().BeTrue();
        job.IsDownloadableAt(Now.AddHours(25)).Should().BeFalse("the file is past its lifetime");
    }

    [Fact]
    public void OnlyAQueuedJobIsTaken_AndOnlyARunningOneCompletesOrIsQueuedAgain()
    {
        var job = Job();
        job.Start(Now);

        job.Start(Now).Should().BeFalse();
        var queued = Job();
        queued.Requeue(Now).Should().BeFalse();
        var act = () => queued.Complete("k", 1, 0, 0, 0, 0, Now, Life);
        act.Should().Throw<BusinessRuleException>().Which.RuleCode.Should().Be(ChatRuleCodes.ExportInvalid);

        job.Requeue(Now.AddMinutes(20)).Should().BeTrue();
        job.Status.Should().Be(ChatExportStatus.Queued);
        job.StartedAt.Should().BeNull();
    }

    [Fact]
    public void AFailedJobKeepsItsCodeAndStoresNoFile()
    {
        var job = Job();
        job.Start(Now);

        job.Fail(ChatRuleCodes.ExportTooLarge, Now);

        job.Status.Should().Be(ChatExportStatus.Failed);
        job.FailureCode.Should().Be("export-too-large");
        job.StorageKey.Should().BeNull();
        job.IsDownloadableAt(Now).Should().BeFalse();
    }

    [Fact]
    public void AFinishedExportDoesNotFail()
    {
        var job = Job();
        job.Start(Now);
        job.Complete("k", 1, 0, 0, 0, 0, Now, Life);

        var act = () => job.Fail("error", Now);

        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void ExpiringAReadyExportDropsTheKey_AndOnlyAReadyOneExpires()
    {
        var job = Job();
        job.Expire(Now).Should().BeFalse();
        job.Start(Now);
        job.Complete("k", 1, 0, 0, 0, 0, Now, Life);

        job.Expire(Now.AddDays(2)).Should().BeTrue();

        job.Status.Should().Be(ChatExportStatus.Expired);
        job.StorageKey.Should().BeNull();
        job.IsDownloadableAt(Now.AddHours(1)).Should().BeFalse();
    }

    [Fact]
    public void NegativeCountsAreRefused()
    {
        var job = Job();
        job.Start(Now);

        var act = () => job.Complete("k", 1, -1, 0, 0, 0, Now, Life);

        act.Should().Throw<BusinessRuleException>();
    }

    [Theory]
    [InlineData(ChatExportStatus.Queued, "queued")]
    [InlineData(ChatExportStatus.Running, "running")]
    [InlineData(ChatExportStatus.Ready, "ready")]
    [InlineData(ChatExportStatus.Failed, "failed")]
    [InlineData(ChatExportStatus.Expired, "expired")]
    public void TheStatusRoundTripsItsWireName(ChatExportStatus status, string wire)
    {
        status.ToWire().Should().Be(wire);
        ChatExportStatusWire.FromWire(wire).Should().Be(status);
    }
}
