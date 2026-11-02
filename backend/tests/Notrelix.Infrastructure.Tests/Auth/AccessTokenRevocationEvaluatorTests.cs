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

    // Watermark 12:00:00.600 inside second 12:00:00 (iat 12:00:00).
    private static readonly DateTimeOffset MidSecondWatermark = Watermark.AddMilliseconds(600);

    private static string Sec(DateTimeOffset t) => t.ToUnixTimeSeconds().ToString();
    private static string Ms(DateTimeOffset t) => t.ToUnixTimeMilliseconds().ToString();

    [Fact]
    public void ResolveIssuedAt_PreciseTokenSameSecondAfterWatermark_Accepts()
    {
        var issued = Watermark.AddMilliseconds(900);
        var resolved = AccessTokenRevocationEvaluator.ResolveIssuedAt(Sec(issued), Ms(issued));
        AccessTokenRevocationEvaluator.ShouldReject(resolved, MidSecondWatermark).Should().BeFalse();
    }

    [Fact]
    public void ResolveIssuedAt_PreciseTokenSameSecondBeforeWatermark_Rejects()
    {
        var issued = Watermark.AddMilliseconds(300);
        var resolved = AccessTokenRevocationEvaluator.ResolveIssuedAt(Sec(issued), Ms(issued));
        AccessTokenRevocationEvaluator.ShouldReject(resolved, MidSecondWatermark).Should().BeTrue();
    }

    [Fact]
    public void ResolveIssuedAt_PreciseTokenBeforeWatermarkSecond_Rejects()
    {
        var issued = Watermark.AddSeconds(-3);
        var resolved = AccessTokenRevocationEvaluator.ResolveIssuedAt(Sec(issued), Ms(issued));
        AccessTokenRevocationEvaluator.ShouldReject(resolved, MidSecondWatermark).Should().BeTrue();
    }

    [Fact]
    public void ResolveIssuedAt_WithoutPreciseClaim_KeepsConservativeWholeSecond()
    {
        var issued = Watermark.AddMilliseconds(900);
        var resolved = AccessTokenRevocationEvaluator.ResolveIssuedAt(Sec(issued), null);
        AccessTokenRevocationEvaluator.ShouldReject(resolved, MidSecondWatermark).Should().BeTrue();
    }

    [Fact]
    public void ResolveIssuedAt_PreciseClaimDisagreeingWithIat_FallsBackToIat()
    {
        // A precise value from another second must never lift a token over the watermark.
        var resolved = AccessTokenRevocationEvaluator.ResolveIssuedAt(Sec(Watermark), Ms(Watermark.AddSeconds(5)));
        resolved.Should().Be(Watermark);
        AccessTokenRevocationEvaluator.ShouldReject(resolved, MidSecondWatermark).Should().BeTrue();
    }

    [Fact]
    public void ResolveIssuedAt_MissingIat_IsNullSoWatermarkRejects()
    {
        var resolved = AccessTokenRevocationEvaluator.ResolveIssuedAt(null, Ms(Watermark.AddMinutes(5)));
        resolved.Should().BeNull();
        AccessTokenRevocationEvaluator.ShouldReject(resolved, MidSecondWatermark).Should().BeTrue();
    }
}
