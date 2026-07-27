using FluentAssertions;
using Notrelix.Domain.Analytics.Snapshots;

namespace Notrelix.Domain.Tests.Analytics;

public class ReportingSnapshotTests
{
    [Fact]
    public void Capture_ShouldSetAllProperties()
    {
        var accountId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var data = JsonValue.Create("{\"total\":42}");

        var snapshot = ReportingSnapshot.Capture(accountId, workspaceId, "BoardSummary", data, now);

        snapshot.AccountId.Should().Be(accountId);
        snapshot.WorkspaceId.Should().Be(workspaceId);
        snapshot.ReportType.Should().Be("BoardSummary");
        snapshot.Data.Should().Be(data);
        snapshot.CapturedAt.Should().Be(now);
    }

    [Fact]
    public void Capture_WithEmptyWorkspaceId_ShouldThrow()
    {
        var act = () => ReportingSnapshot.Capture(Guid.NewGuid(), Guid.Empty, "Report",
            JsonValue.EmptyObject(), DateTimeOffset.UtcNow);

        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Capture_WithEmptyAccountId_ShouldThrow()
    {
        var act = () => ReportingSnapshot.Capture(Guid.Empty, Guid.NewGuid(), "Report",
            JsonValue.EmptyObject(), DateTimeOffset.UtcNow);

        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Capture_WithEmptyReportType_ShouldThrow()
    {
        var act = () => ReportingSnapshot.Capture(Guid.NewGuid(), Guid.NewGuid(), "",
            JsonValue.EmptyObject(), DateTimeOffset.UtcNow);

        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Capture_WithNullData_ShouldThrow()
    {
        var act = () => ReportingSnapshot.Capture(Guid.NewGuid(), Guid.NewGuid(), "Report",
            null!, DateTimeOffset.UtcNow);

        act.Should().Throw<BusinessRuleException>();
    }
}
