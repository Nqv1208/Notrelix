using FluentAssertions;
using Notrelix.Domain.Analytics.Snapshots;

namespace Notrelix.Domain.Tests.Analytics;

public class ReportingSnapshotPayloadTests
{
    [Fact]
    public void Create_ShouldSetSchemaVersion1()
    {
        var data = JsonValue.Create("""{"total": 100}""");

        var payload = ReportSnapshotPayload.Create("BoardVelocity", data);

        payload.SchemaVersion.Should().Be(1);
        payload.ReportType.Should().Be("BoardVelocity");
    }

    [Fact]
    public void Create_ShouldTrimReportType()
    {
        var data = JsonValue.Create("""{"total": 100}""");

        var payload = ReportSnapshotPayload.Create("  BoardVelocity  ", data);

        payload.ReportType.Should().Be("BoardVelocity");
    }

    [Fact]
    public void Create_NullReportType_ShouldThrow()
    {
        var data = JsonValue.Create("""{"total": 100}""");
        var act = () => ReportSnapshotPayload.Create(null!, data);
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Create_EmptyReportType_ShouldThrow()
    {
        var data = JsonValue.Create("""{"total": 100}""");
        var act = () => ReportSnapshotPayload.Create("", data);
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Create_NullData_ShouldThrow()
    {
        var act = () => ReportSnapshotPayload.Create("Report", null!);
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Create_NonObjectJsonData_ShouldThrow()
    {
        var data = JsonValue.Create("[1,2,3]");
        var act = () => ReportSnapshotPayload.Create("Report", data);
        act.Should().Throw<BusinessRuleException>().WithMessage("*JSON object*");
    }

    [Fact]
    public void Create_EmptyObjectData_ShouldSucceed()
    {
        var data = JsonValue.Create("""{}""");

        var payload = ReportSnapshotPayload.Create("Report", data);

        payload.Data.Should().Be(data);
    }
}
