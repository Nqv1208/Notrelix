using Microsoft.Extensions.Options;
using Notrelix.Infrastructure.BackgroundJobs;

namespace Notrelix.Infrastructure.Tests;

/// <summary>The options of the Chat retention job (UC-CHT-H05) are validated on start.</summary>
public class ChatRetentionOptionsTests
{
    private static ValidateOptionsResult Validate(ChatRetentionOptions o) => new ChatRetentionOptionsValidator().Validate(null, o);

    [Fact]
    public void TheDefaultsAreValid() => Validate(new ChatRetentionOptions()).Succeeded.Should().BeTrue();

    [Fact]
    public void AnIntervalOutsideTheBoundsAndABatchCountOfZeroAreRefused()
    {
        Validate(new ChatRetentionOptions { Interval = TimeSpan.FromSeconds(1) }).Failed.Should().BeTrue();
        Validate(new ChatRetentionOptions { Interval = TimeSpan.FromDays(2) }).Failed.Should().BeTrue();
        Validate(new ChatRetentionOptions { MaxBatchesPerSpace = 0 }).Failed.Should().BeTrue();
    }
}
