using FluentAssertions;
using Notrelix.Domain.Analytics.Snapshots;

namespace Notrelix.Domain.Tests.Analytics;

public class ReportingSnapshotTests
{
    private static ReportSnapshotPayload ValidPayload() =>
        ReportSnapshotPayload.CreateV1("BoardSummary", JsonValue.Create("""{"total":42}"""));

    [Fact]
    public void Capture_ShouldSetAllProperties()
    {
        var accountId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var payload = ValidPayload();

        var snapshot = ReportingSnapshot.Capture(accountId, workspaceId, payload, now);

        snapshot.AccountId.Should().Be(accountId);
        snapshot.WorkspaceId.Should().Be(workspaceId);
        snapshot.ReportType.Should().Be("BoardSummary");
        snapshot.SchemaVersion.Should().Be(1);
        snapshot.Data.Should().Be(payload.Data);
        snapshot.Payload.Should().Be(payload);
        snapshot.CapturedAt.Should().Be(now);
    }

    [Fact]
    public void Capture_WithVersionedPayload_ShouldRetainSchemaVersion()
    {
        var payload = ReportSnapshotPayload.Create("BoardSummary", 2, JsonValue.Create("""{"total":42}"""));
        var snapshot = ReportingSnapshot.Capture(Guid.NewGuid(), Guid.NewGuid(), payload, DateTimeOffset.UtcNow);

        snapshot.SchemaVersion.Should().Be(2);
        snapshot.Payload.Should().Be(payload);
        snapshot.Payload.SchemaVersion.Should().Be(2);
    }

    [Fact]
    public void Capture_ShouldExposePayload()
    {
        var payload = ValidPayload();
        var snapshot = ReportingSnapshot.Capture(Guid.NewGuid(), Guid.NewGuid(), payload, DateTimeOffset.UtcNow);

        snapshot.Payload.Should().Be(payload);
        snapshot.ReportType.Should().Be(payload.ReportType);
        snapshot.Data.Should().Be(payload.Data);
    }

    [Fact]
    public void Snapshot_ShouldBeImmutable()
    {
        var snapshot = ReportingSnapshot.Capture(Guid.NewGuid(), Guid.NewGuid(), ValidPayload(), DateTimeOffset.UtcNow);

        var mutableProperties = typeof(ReportingSnapshot)
            .GetProperties()
            .Where(p => p.SetMethod?.IsPublic == true)
            .Select(p => p.Name)
            .ToList();

        mutableProperties.Should().BeEmpty(
            "ReportingSnapshot is append-only and must not expose public setters.");
    }

    [Fact]
    public void Capture_WithEmptyWorkspaceId_ShouldThrow()
    {
        var act = () => ReportingSnapshot.Capture(Guid.NewGuid(), Guid.Empty, ValidPayload(),
            DateTimeOffset.UtcNow);

        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Capture_WithEmptyAccountId_ShouldThrow()
    {
        var act = () => ReportingSnapshot.Capture(Guid.Empty, Guid.NewGuid(), ValidPayload(),
            DateTimeOffset.UtcNow);

        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Capture_WithNullPayload_ShouldThrow()
    {
        var act = () => ReportingSnapshot.Capture(Guid.NewGuid(), Guid.NewGuid(), null!,
            DateTimeOffset.UtcNow);

        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Capture_WithDefaultCapturedAt_ShouldThrow()
    {
        var act = () => ReportingSnapshot.Capture(Guid.NewGuid(), Guid.NewGuid(), ValidPayload(), default);

        act.Should().Throw<BusinessRuleException>()
            .WithMessage("*CapturedAt*default*");
    }
}
