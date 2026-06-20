using Notrelix.Infrastructure.Data.Ops.Entities;

namespace Notrelix.Infrastructure.Tests.Data.Ops;

public class ExportJobRecordTests
{
    [Fact]
    public void Create_sets_defaults()
    {
        var job = ExportJobRecord.Create(
            Guid.NewGuid(), Guid.NewGuid(), "BoardExport",
            "Pending", "Csv", "{}", "{}", DateTimeOffset.UtcNow);

        job.Format.Should().Be("Csv");
        job.Status.Should().Be("Pending");
        job.RowCount.Should().BeNull();
        job.DownloadUrl.Should().BeNull();
    }
}
