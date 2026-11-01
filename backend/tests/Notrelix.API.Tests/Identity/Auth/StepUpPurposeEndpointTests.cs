using System.Net;
using System.Net.Http.Json;
using Notrelix.API.Tests.Contracts;

namespace Notrelix.API.Tests.Identity.Auth;

/// <summary>The three step-up calls read the purpose by name only; a number or an unknown name is a 400, never a mapped purpose.</summary>
public class StepUpPurposeEndpointTests : IClassFixture<NotrelixApiFactory>
{
    private readonly NotrelixApiFactory _factory;

    public StepUpPurposeEndpointTests(NotrelixApiFactory factory) => _factory = factory;

    public static TheoryData<string, object> Calls => new()
    {
        { "/api/v1/auth/security/step-up/requirement", new { Purpose = "0" } },
        { "/api/v1/auth/security/step-up/requirement", new { Purpose = "NotAPurpose" } },
        { "/api/v1/auth/security/step-up/requirement", new { Purpose = "" } },
        { "/api/v1/auth/security/step-up/complete-mfa", new { Purpose = "7", ChallengeToken = "t", Code = "123456" } },
        { "/api/v1/auth/security/step-up/complete-mfa", new { Purpose = "NotAPurpose", ChallengeToken = "t", Code = "123456" } },
        { "/api/v1/auth/security/step-up/complete-password", new { Purpose = "1", Password = "x" } },
        { "/api/v1/auth/security/step-up/complete-password", new { Purpose = "NotAPurpose", Password = "x" } },
    };

    [Theory]
    [MemberData(nameof(Calls))]
    public async Task AnUnknownOrNumericPurpose_IsRefusedWith400(string path, object body)
    {
        var response = await _factory.CreateAuthenticatedClient().PostAsJsonAsync(path, body);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData("/api/v1/auth/security/step-up/requirement")]
    [InlineData("/api/v1/auth/security/step-up/complete-mfa")]
    [InlineData("/api/v1/auth/security/step-up/complete-password")]
    [InlineData("/api/v1/auth/mfa/enrollment/start")]
    [InlineData("/api/v1/auth/mfa/enrollment/verify")]
    [InlineData("/api/v1/auth/mfa/disable")]
    public async Task EveryCall_WithoutASession_IsUnauthorized(string path)
    {
        var response = await _factory.CreateClient().PostAsJsonAsync(path, new { Purpose = "DisableMfa" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RevokeSession_WithoutASession_IsUnauthorized()
    {
        var response = await _factory.CreateClient().PostAsync($"/api/v1/auth/sessions/{Guid.NewGuid()}/revoke", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
