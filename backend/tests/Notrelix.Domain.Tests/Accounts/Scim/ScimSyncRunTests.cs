using FluentAssertions;
using Notrelix.Domain.Accounts.Scim;

namespace Notrelix.Domain.Tests.Accounts.Scim;

public class ScimSyncRunTests
{
    private readonly Guid _accountId = Guid.NewGuid();
    private readonly Guid _directoryId = Guid.NewGuid();
    private readonly DateTimeOffset _now = DateTimeOffset.UtcNow;

    // ── Constructor ────────────────────────────────────────────────────────

    [Fact]
    public void Constructor_ShouldSetPendingStatus()
    {
        var run = new ScimSyncRun(_accountId, _directoryId);

        run.AccountId.Should().Be(_accountId);
        run.DirectoryId.Should().Be(_directoryId);
        run.Status.Should().Be("Pending");
        run.StartedAt.Should().BeNull();
        run.FinishedAt.Should().BeNull();
        run.UsersCreated.Should().Be(0);
        run.UsersUpdated.Should().Be(0);
        run.UsersDisabled.Should().Be(0);
        run.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithEmptyAccountId_ShouldThrow()
    {
        var act = () => new ScimSyncRun(Guid.Empty, _directoryId);

        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Constructor_WithEmptyDirectoryId_ShouldThrow()
    {
        var act = () => new ScimSyncRun(_accountId, Guid.Empty);

        act.Should().Throw<BusinessRuleException>();
    }

    // ── Start ──────────────────────────────────────────────────────────────

    [Fact]
    public void Start_ShouldSetRunningStatus()
    {
        var run = new ScimSyncRun(_accountId, _directoryId);

        run.Start(_now);

        run.Status.Should().Be("Running");
        run.StartedAt.Should().Be(_now);
    }

    // ── Complete ───────────────────────────────────────────────────────────

    [Fact]
    public void Complete_ShouldSetSucceededStatus()
    {
        var run = new ScimSyncRun(_accountId, _directoryId);
        run.Start(_now);

        run.Complete(5, 3, 1, _now);

        run.Status.Should().Be("Succeeded");
        run.UsersCreated.Should().Be(5);
        run.UsersUpdated.Should().Be(3);
        run.UsersDisabled.Should().Be(1);
        run.FinishedAt.Should().Be(_now);
    }

    // ── Fail ───────────────────────────────────────────────────────────────

    [Fact]
    public void Fail_ShouldSetFailedStatus()
    {
        var run = new ScimSyncRun(_accountId, _directoryId);
        run.Start(_now);

        run.Fail("Connection timeout", _now);

        run.Status.Should().Be("Failed");
        run.ErrorMessage.Should().Be("Connection timeout");
        run.FinishedAt.Should().Be(_now);
    }

    // ── Cancel ─────────────────────────────────────────────────────────────

    [Fact]
    public void Cancel_ShouldSetCancelledStatus()
    {
        var run = new ScimSyncRun(_accountId, _directoryId);
        run.Start(_now);

        run.Cancel(_now);

        run.Status.Should().Be("Cancelled");
        run.FinishedAt.Should().Be(_now);
    }

    // ── Complete with negative counts ──────────────────────────────────────

    [Fact]
    public void Complete_WithNegativeCount_ShouldAccept_IfNoValidation()
    {
        var run = new ScimSyncRun(_accountId, _directoryId);
        run.Start(_now);

        run.Complete(-1, 0, 0, _now);

        run.UsersCreated.Should().Be(-1);
    }

    // ── State machine ──────────────────────────────────────────────────────

    [Fact]
    public void Start_AfterComplete_ShouldStillSetRunning()
    {
        var run = new ScimSyncRun(_accountId, _directoryId);
        run.Start(_now);
        run.Complete(1, 0, 0, _now);

        run.Start(_now);

        run.Status.Should().Be("Running");
    }

    [Fact]
    public void Fail_AfterComplete_ShouldStillSetFailed()
    {
        var run = new ScimSyncRun(_accountId, _directoryId);
        run.Start(_now);
        run.Complete(1, 0, 0, _now);

        run.Fail("Late error", _now);

        run.Status.Should().Be("Failed");
        run.ErrorMessage.Should().Be("Late error");
    }
}
