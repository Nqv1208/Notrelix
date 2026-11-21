using Notrelix.Infrastructure.BackgroundJobs;

namespace Notrelix.Infrastructure.Tests;

/// <summary>UC-CHT-H05: the purge of deleted channels is off by default and its options are bounded.</summary>
public class DeletedChatSpacePurgeOptionsTests
{
    private static readonly DeletedChatSpacePurgeOptionsValidator Validator = new();

    [Fact]
    public void The_purge_is_off_by_default()
    {
        new DeletedChatSpacePurgeOptions().Enabled.Should().BeFalse();
        Validator.Validate(null, new DeletedChatSpacePurgeOptions()).Succeeded.Should().BeTrue();
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1_001, 1)]
    [InlineData(20, 0)]
    public void An_interval_or_batch_out_of_range_is_refused(int batch, int seconds)
    {
        var options = new DeletedChatSpacePurgeOptions { BatchSize = batch, Interval = TimeSpan.FromSeconds(seconds == 0 ? 1 : 3600) };

        Validator.Validate(null, options).Failed.Should().BeTrue();
    }
}
