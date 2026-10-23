using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Notrelix.API.Middleware;
using Notrelix.Infrastructure.Data.Audit;

namespace Notrelix.API.Tests.Middleware;

/// <summary>
/// The audit list used to name <c>/api/auth/*</c>, a spelling no route has (the real routes are
/// <c>/api/v1/auth/*</c>), so failed sign-ins were never classified as such.
/// </summary>
public class SecurityAuditMiddlewareTests
{
    [Theory]
    [InlineData("/api/v1/auth/login")]
    [InlineData("/api/v1/auth/register")]
    [InlineData("/api/v1/auth/refresh")]
    [InlineData("/api/v1/auth/forgot-password")]
    [InlineData("/api/v1/auth/reset-password")]
    [InlineData("/api/v1/auth/mfa/verify")]
    [InlineData("/api/v1/auth/oauth/google/callback")]
    [InlineData("/API/V1/AUTH/LOGIN")]
    [InlineData("/api/v2/auth/login")]
    [InlineData("/api/v1/auth/login/")]
    public void RealSignInRoutes_AreSensitiveSignInRoutes(string path)
    {
        SecuritySensitiveRoutes.IsSensitive(path).Should().BeTrue();
        SecuritySensitiveRoutes.IsSignInRoute(path).Should().BeTrue();
    }

    [Theory]
    [InlineData("/api/v1/auth/change-password")]
    [InlineData("/api/v1/auth/mfa/disable")]
    [InlineData("/api/v1/auth/mfa/enrollment/verify")]
    [InlineData("/api/v1/auth/mfa/recovery-codes/regenerate")]
    [InlineData("/api/v1/auth/security/step-up/complete-mfa")]
    [InlineData("/api/v1/auth/security/step-up/complete-password")]
    [InlineData("/api/v1/auth/oauth/google/step-up/callback")]
    public void CredentialChangeRoutes_AreSensitiveButNotSignIn(string path)
    {
        SecuritySensitiveRoutes.IsSensitive(path).Should().BeTrue();
        SecuritySensitiveRoutes.IsSignInRoute(path).Should().BeFalse(
            "a 401 on a credential change by a signed-in user is not a failed sign-in");
    }

    [Theory]
    [InlineData("/api/auth/login")]
    [InlineData("/api/v1/auth")]
    [InlineData("/api/v1/auth/")]
    [InlineData("/api/vx/auth/login")]
    [InlineData("/api/v1/auth/sessions")]
    [InlineData("/api/v1/auth/me")]
    [InlineData("/api/v1/workspaces/login")]
    [InlineData("/login")]
    [InlineData("")]
    public void OtherRoutes_AreNotSensitive(string path)
    {
        SecuritySensitiveRoutes.IsSensitive(path).Should().BeFalse();
    }

    [Fact]
    public void EveryCheckedInIdentityCredentialRoute_IsClassified()
    {
        // The checked-in producer contract is the route inventory (it is regenerated and diffed by CI).
        var document = System.Text.Json.JsonDocument.Parse(File.ReadAllText(ContractPath()));
        var paths = document.RootElement.GetProperty("paths").EnumerateObject().Select(p => p.Name).ToHashSet();

        string[] mustBeSensitive =
        [
            "/api/v1/auth/login", "/api/v1/auth/register", "/api/v1/auth/refresh",
            "/api/v1/auth/forgot-password", "/api/v1/auth/reset-password", "/api/v1/auth/mfa/verify",
            "/api/v1/auth/change-password", "/api/v1/auth/mfa/disable",
            "/api/v1/auth/security/step-up/complete-mfa", "/api/v1/auth/security/step-up/complete-password",
        ];

        mustBeSensitive.Should().OnlyContain(route => paths.Contains(route), "the list must track real routes");
        mustBeSensitive.Where(route => !SecuritySensitiveRoutes.IsSensitive(route)).Should().BeEmpty();
    }

    [Theory]
    [InlineData("/api/v1/auth/login", 401, "LoginFailed")]
    [InlineData("/api/v1/auth/login", 400, "SecurityObservation")]
    [InlineData("/api/v1/auth/mfa/verify", 400, "SecurityObservation")]
    [InlineData("/api/v1/auth/change-password", 401, "UnauthorizedAccess")]
    [InlineData("/api/v1/auth/change-password", 400, "SecurityObservation")]
    [InlineData("/api/v1/auth/sessions", 401, "UnauthorizedAccess")]
    [InlineData("/api/v1/auth/login", 429, "RateLimitExceeded")]
    [InlineData("/api/v1/auth/mfa/verify", 403, "ForbiddenAccess")]
    public async Task Middleware_RecordsTheFailureOfARealAuthRoute(string path, int status, string expectedEventType)
    {
        var (context, events) = CreateContext(path, status);

        await new SecurityAuditMiddleware(_ => Task.CompletedTask).InvokeAsync(context);

        var recorded = events.Single();
        recorded.EventType.Should().Be(expectedEventType);
    }

    [Theory]
    [InlineData("/api/v1/auth/login", 200)]
    [InlineData("/api/v1/auth/login", 302)]
    [InlineData("/api/v1/auth/me", 400)]
    [InlineData("/api/auth/login", 400)]
    public async Task Middleware_IgnoresSuccessesAndUnrelatedClientErrors(string path, int status)
    {
        var (context, events) = CreateContext(path, status);

        await new SecurityAuditMiddleware(_ => Task.CompletedTask).InvokeAsync(context);

        events.Should().BeEmpty();
    }

    [Fact]
    public async Task Middleware_WhenThePersistencePathFails_DoesNotFailTheRequest()
    {
        var sink = new Mock<ISecurityAuditSink>();
        sink.Setup(a => a.WriteAsync(It.IsAny<SecurityEvent>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("database down"));
        var context = new DefaultHttpContext
        {
            RequestServices = new ServiceCollection().AddSingleton(sink.Object).BuildServiceProvider()
        };
        context.Request.Path = "/api/v1/auth/login";
        context.Response.StatusCode = 401;

        var act = () => new SecurityAuditMiddleware(_ => Task.CompletedTask).InvokeAsync(context);

        await act.Should().NotThrowAsync();
        context.Response.StatusCode.Should().Be(401);
    }

    private static (DefaultHttpContext Context, List<SecurityEvent> Events) CreateContext(string path, int status)
    {
        var events = new List<SecurityEvent>();
        var sink = new Mock<ISecurityAuditSink>();
        sink.Setup(a => a.WriteAsync(It.IsAny<SecurityEvent>(), It.IsAny<CancellationToken>()))
            .Callback<SecurityEvent, CancellationToken>((e, _) => events.Add(e))
            .Returns(Task.CompletedTask);

        var services = new ServiceCollection().AddSingleton(sink.Object).BuildServiceProvider();
        var context = new DefaultHttpContext { RequestServices = services };
        context.Request.Path = path;
        context.Request.Method = "POST";
        context.Response.StatusCode = status;
        return (context, events);
    }

    private static string ContractPath()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "contracts", "openapi", "notrelix.v1.json")))
            dir = dir.Parent;
        return Path.Combine(dir!.FullName, "contracts", "openapi", "notrelix.v1.json");
    }
}
