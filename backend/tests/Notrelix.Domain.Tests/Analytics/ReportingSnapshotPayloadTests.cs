using FluentAssertions;
using Notrelix.Domain.Analytics.Snapshots;

namespace Notrelix.Domain.Tests.Analytics;

public class ReportingSnapshotPayloadTests
{
    [Fact]
    public void Create_ShouldSetSchemaVersion()
    {
        var data = JsonValue.Create("""{"total": 100}""");

        var payload = ReportSnapshotPayload.Create("BoardVelocity", 2, data);

        payload.SchemaVersion.Should().Be(2);
        payload.ReportType.Should().Be("BoardVelocity");
    }

    [Fact]
    public void Create_ShouldTrimReportType()
    {
        var data = JsonValue.Create("""{"total": 100}""");

        var payload = ReportSnapshotPayload.Create("  BoardVelocity  ", 3, data);

        payload.ReportType.Should().Be("BoardVelocity");
    }

    [Fact]
    public void Create_WithZeroSchemaVersion_ShouldThrow()
    {
        var data = JsonValue.Create("""{"total": 100}""");
        var act = () => ReportSnapshotPayload.Create("BoardVelocity", 0, data);

        act.Should().Throw<BusinessRuleException>()
            .WithMessage("*schema version*greater than zero*");
    }

    [Fact]
    public void Create_WithNegativeSchemaVersion_ShouldThrow()
    {
        var data = JsonValue.Create("""{"total": 100}""");
        var act = () => ReportSnapshotPayload.Create("BoardVelocity", -1, data);

        act.Should().Throw<BusinessRuleException>()
            .WithMessage("*schema version*greater than zero*");
    }

    [Fact]
    public void Create_NullReportType_ShouldThrow()
    {
        var data = JsonValue.Create("""{"total": 100}""");
        var act = () => ReportSnapshotPayload.Create(null!, 1, data);
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Create_EmptyReportType_ShouldThrow()
    {
        var data = JsonValue.Create("""{"total": 100}""");
        var act = () => ReportSnapshotPayload.Create("", 1, data);
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Create_NullData_ShouldThrow()
    {
        var act = () => ReportSnapshotPayload.Create("Report", 1, null!);
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Create_NonObjectJsonData_ShouldThrow()
    {
        var data = JsonValue.Create("[1,2,3]");
        var act = () => ReportSnapshotPayload.Create("Report", 1, data);
        act.Should().Throw<BusinessRuleException>().WithMessage("*JSON object*");
    }

    [Fact]
    public void Create_EmptyObjectData_ShouldSucceed()
    {
        var data = JsonValue.Create("""{}""");

        var payload = ReportSnapshotPayload.Create("Report", 1, data);

        payload.Data.Should().Be(data);
    }

    [Fact]
    public void CreateV1_ShouldSetSchemaVersion1()
    {
        var data = JsonValue.Create("""{"total": 100}""");

        var payload = ReportSnapshotPayload.CreateV1("BoardVelocity", data);

        payload.SchemaVersion.Should().Be(1);
        payload.ReportType.Should().Be("BoardVelocity");
        payload.Data.Should().Be(data);
    }

    [Fact]
    public void CreateV1_NullReportType_ShouldThrow()
    {
        var data = JsonValue.Create("""{"total": 100}""");
        var act = () => ReportSnapshotPayload.CreateV1(null!, data);
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void CreateV1_NonObjectJsonData_ShouldThrow()
    {
        var data = JsonValue.Create("[1,2,3]");
        var act = () => ReportSnapshotPayload.CreateV1("Report", data);
        act.Should().Throw<BusinessRuleException>().WithMessage("*JSON object*");
    }
}
