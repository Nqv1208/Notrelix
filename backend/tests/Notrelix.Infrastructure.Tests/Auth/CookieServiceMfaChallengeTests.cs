using Microsoft.AspNetCore.Http;
using Notrelix.Infrastructure.Auth.Cookies;
using Notrelix.Infrastructure.Auth.Jwt;

namespace Notrelix.Infrastructure.Tests.Auth;

public class CookieServiceMfaChallengeTests
{
    private static (CookieService Service, DefaultHttpContext Context) Create(bool https = false, string? requestCookie = null)
    {
        var context = new DefaultHttpContext();
        context.Request.Scheme = https ? "https" : "http";
        if (requestCookie is not null)
            context.Request.Headers.Cookie = requestCookie;

        var accessor = new Mock<IHttpContextAccessor>();
        accessor.Setup(a => a.HttpContext).Returns(context);
        var environment = new Mock<IHostEnvironment>();
        environment.Setup(e => e.EnvironmentName).Returns("Production");
        var jwt = Microsoft.Extensions.Options.Options.Create(new JwtSettings { ExpireMinutes = 60, RefreshTokenExpireDays = 7 });
        return (new CookieService(accessor.Object, environment.Object, jwt), context);
    }

    private static string SetCookie(DefaultHttpContext context) =>
        context.Response.Headers.SetCookie.ToString();

    [Fact]
    public void SetMfaChallengeCookie_IsHttpOnlyLaxAndScopedToTheMfaEndpoints()
    {
        var (service, context) = Create();

        service.SetMfaChallengeCookie("challenge", DateTimeOffset.UtcNow.AddMinutes(5));

        var cookie = SetCookie(context);
        cookie.Should().StartWith("mfaChallenge=challenge");
        cookie.Should().ContainEquivalentOf("path=/api/v1/auth/mfa");
        cookie.Should().ContainEquivalentOf("httponly");
        cookie.Should().ContainEquivalentOf("samesite=lax");
        cookie.Should().NotContainEquivalentOf("secure");
    }

    [Fact]
    public void SetMfaChallengeCookie_IsSecureOnHttps()
    {
        var (service, context) = Create(https: true);

        service.SetMfaChallengeCookie("challenge", DateTimeOffset.UtcNow.AddMinutes(5));

        SetCookie(context).Should().ContainEquivalentOf("secure");
    }

    [Fact]
    public void SetMfaChallengeCookie_NeverOutlivesFiveMinutes_OrTheChallenge()
    {
        var (longLived, longContext) = Create();
        longLived.SetMfaChallengeCookie("challenge", DateTimeOffset.UtcNow.AddHours(1));
        MaxAgeSeconds(SetCookie(longContext)).Should().BeInRange(290, 300);

        var (shortLived, shortContext) = Create();
        shortLived.SetMfaChallengeCookie("challenge", DateTimeOffset.UtcNow.AddSeconds(45));
        MaxAgeSeconds(SetCookie(shortContext)).Should().BeInRange(40, 45);
    }

    [Fact]
    public void SetMfaChallengeCookie_ForAChallengeThatAlreadyExpired_SetsNoCookie()
    {
        var (service, context) = Create();

        service.SetMfaChallengeCookie("challenge", DateTimeOffset.UtcNow.AddSeconds(-1));

        SetCookie(context).Should().NotContain("challenge");
    }

    [Fact]
    public void GetMfaChallengeCookie_ReadsTheRequestCookie_OrNull()
    {
        Create(requestCookie: "mfaChallenge=abc123").Service.GetMfaChallengeCookie().Should().Be("abc123");
        Create().Service.GetMfaChallengeCookie().Should().BeNull();
    }

    [Fact]
    public void DeleteMfaChallengeCookie_ExpiresItOnTheSamePath()
    {
        var (service, context) = Create();

        service.DeleteMfaChallengeCookie();

        var cookie = SetCookie(context);
        cookie.Should().StartWith("mfaChallenge=;");
        cookie.Should().ContainEquivalentOf("path=/api/v1/auth/mfa");
        cookie.Should().ContainEquivalentOf("expires=Thu, 01 Jan 1970");
    }

    private static int MaxAgeSeconds(string cookie) =>
        int.Parse(System.Text.RegularExpressions.Regex.Match(cookie, "max-age=(\\d+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase).Groups[1].Value);
}
