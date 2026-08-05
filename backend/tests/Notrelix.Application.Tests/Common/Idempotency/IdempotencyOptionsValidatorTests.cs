using Microsoft.Extensions.Options;

namespace Notrelix.Application.Tests.Common.Idempotency;

/// <summary>
/// Spec 3.6: canonical validated idempotency options defaults. The Infrastructure
/// store owns every expiry calculation, so invalid values must fail at startup.
/// </summary>
public class IdempotencyOptionsValidatorTests
{
    private readonly IdempotencyOptionsValidator _validator = new();

    private ValidateOptionsResult Validate(IdempotencyOptions options) =>
        _validator.Validate(Options.DefaultName, options);

    [Fact]
    public void Defaults_Are_Valid()
    {
        var result = Validate(new IdempotencyOptions());

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public void Defaults_Match_Spec()
    {
        var options = new IdempotencyOptions();

        options.ProcessingExpiry.Should().Be(TimeSpan.FromMinutes(5));
        options.ResultExpiry.Should().Be(TimeSpan.FromDays(1));
        options.MaxResultBytes.Should().Be(1_048_576);
        options.IncompleteStateRetryAfter.Should().Be(TimeSpan.FromSeconds(3));
    }

    [Theory]
    [InlineData(0, 0, 0)]         // zero
    [InlineData(0, 0, -1)]        // negative
    [InlineData(1, 0, 1)]         // > 1 hour
    public void Invalid_ProcessingExpiry_Fails(int hours, int minutes, int seconds)
    {
        var options = new IdempotencyOptions
        {
            ProcessingExpiry = new TimeSpan(hours, minutes, seconds),
        };

        var result = Validate(options);

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("ProcessingExpiry");
    }

    [Fact]
    public void ResultExpiry_NotGreaterThan_ProcessingExpiry_Fails()
    {
        var options = new IdempotencyOptions
        {
            ProcessingExpiry = TimeSpan.FromMinutes(5),
            ResultExpiry = TimeSpan.FromMinutes(5),
        };

        var result = Validate(options);

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("ResultExpiry");
    }

    [Fact]
    public void ResultExpiry_Above_ThirtyDays_Fails()
    {
        var options = new IdempotencyOptions
        {
            ResultExpiry = TimeSpan.FromDays(31),
        };

        var result = Validate(options);

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("ResultExpiry");
    }

    [Theory]
    [InlineData(1023)]            // below 1 KiB
    [InlineData(4 * 1024 * 1024 + 1)] // above 4 MiB
    public void Invalid_MaxResultBytes_Fails(int bytes)
    {
        var options = new IdempotencyOptions { MaxResultBytes = bytes };

        var result = Validate(options);

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("MaxResultBytes");
    }

    [Theory]
    [InlineData(0)]   // below 1 second
    [InlineData(31)]  // above 30 seconds
    public void Invalid_IncompleteStateRetryAfter_Fails(int seconds)
    {
        var options = new IdempotencyOptions
        {
            IncompleteStateRetryAfter = TimeSpan.FromSeconds(seconds),
        };

        var result = Validate(options);

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("IncompleteStateRetryAfter");
    }

    [Fact]
    public void Boundary_Values_Are_Valid()
    {
        var options = new IdempotencyOptions
        {
            ProcessingExpiry = TimeSpan.FromHours(1),
            ResultExpiry = TimeSpan.FromDays(30),
            MaxResultBytes = 4 * 1024 * 1024,
            IncompleteStateRetryAfter = TimeSpan.FromSeconds(30),
        };

        var result = Validate(options);

        result.Succeeded.Should().BeTrue();
    }
}
