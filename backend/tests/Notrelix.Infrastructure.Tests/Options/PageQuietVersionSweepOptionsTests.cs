using Microsoft.Extensions.Options;
using Notrelix.Infrastructure.BackgroundJobs;

namespace Notrelix.Infrastructure.Tests.Options;

/// <summary>The options of the quiet-version sweep (automatic version when editing goes quiet) are validated on start.</summary>
public class PageQuietVersionSweepOptionsTests
{
    private static ValidateOptionsResult Validate(PageQuietVersionSweepOptions o) =>
        new PageQuietVersionSweepOptionsValidator().Validate(null, o);

    [Fact]
    public void TheDefaultsAreValid() => Validate(new PageQuietVersionSweepOptions()).Succeeded.Should().BeTrue();

    [Fact]
    public void AnIntervalOrLookbackOutsideTheBoundsAndABatchOfZeroAreRefused()
    {
        Validate(new PageQuietVersionSweepOptions { Interval = TimeSpan.FromSeconds(1) }).Failed.Should().BeTrue();
        Validate(new PageQuietVersionSweepOptions { Interval = TimeSpan.FromHours(2) }).Failed.Should().BeTrue();
        Validate(new PageQuietVersionSweepOptions { Lookback = TimeSpan.FromSeconds(30) }).Failed.Should().BeTrue();
        Validate(new PageQuietVersionSweepOptions { Lookback = TimeSpan.FromDays(2) }).Failed.Should().BeTrue();
        Validate(new PageQuietVersionSweepOptions { BatchSize = 0 }).Failed.Should().BeTrue();
    }
}
