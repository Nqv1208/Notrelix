using Notrelix.Infrastructure.Data.Ops.Entities;

namespace Notrelix.Infrastructure.Tests.Data.Ops;

public class ImportJobRecordTests
{
    [Fact]
    public void Create_sets_defaults()
    {
        var job = ImportJobRecord.Create(
            Guid.NewGuid(), Guid.NewGuid(), "CsvImport",
            "Pending", "{}", DateTimeOffset.UtcNow);

        job.TotalRecords.Should().Be(0);
        job.ProcessedRecords.Should().Be(0);
        job.SucceededRecords.Should().Be(0);
        job.FailedRecords.Should().Be(0);
        job.StartedAt.Should().BeNull();
        job.CompletedAt.Should().BeNull();
    }
}
