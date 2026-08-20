using Notrelix.Infrastructure.Auth.Jwt;

namespace Notrelix.Infrastructure.Tests.Auth;

public class AccessTokenRevocationEvaluatorTests
{
    private static readonly DateTimeOffset Watermark = new(2026, 8, 13, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void ShouldReject_TokenIssuedAfterWatermark_Accepts()
    {
        AccessTokenRevocationEvaluator.ShouldReject(Watermark.AddMinutes(5), Watermark).Should().BeFalse();
    }

    [Fact]
    public void ShouldReject_TokenIssuedBeforeWatermark_Rejects()
    {
        AccessTokenRevocationEvaluator.ShouldReject(Watermark.AddMinutes(-5), Watermark).Should().BeTrue();
    }

    [Fact]
    public void ShouldReject_TokenIssuedExactlyAtWatermark_Rejects()
    {
        AccessTokenRevocationEvaluator.ShouldReject(Watermark, Watermark).Should().BeTrue();
    }

    [Fact]
    public void ShouldReject_TokenMissingIatWithWatermark_RejectsFailClosed()
    {
        AccessTokenRevocationEvaluator.ShouldReject(null, Watermark).Should().BeTrue();
    }

    [Fact]
    public void ShouldReject_NoWatermark_Accepts()
    {
        AccessTokenRevocationEvaluator.ShouldReject(Watermark.AddMinutes(-5), null).Should().BeFalse();
        AccessTokenRevocationEvaluator.ShouldReject(null, null).Should().BeFalse();
    }
}