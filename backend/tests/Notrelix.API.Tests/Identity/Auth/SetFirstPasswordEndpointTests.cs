using System.Net;
using System.Net.Http.Json;
using Notrelix.API.Tests.Contracts;

namespace Notrelix.API.Tests.Identity.Auth;

public class SetFirstPasswordEndpointTests : IClassFixture<NotrelixApiFactory>
{
    private readonly NotrelixApiFactory _factory;

    public SetFirstPasswordEndpointTests(NotrelixApiFactory factory) => _factory = factory;

    [Fact]
    public async Task SetPassword_WithoutAuth_ReturnsUnauthorized()
    {
        var response = await _factory.CreateClient().PostAsJsonAsync(
            "/api/v1/auth/set-password", new { NewPassword = "NewPassword123!", StepUpToken = "x" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task SetPassword_WithAuth_WhenPasswordIsShort_ReturnsBadRequest()
    {
        var response = await _factory.CreateAuthenticatedClient().PostAsJsonAsync(
            "/api/v1/auth/set-password", new { NewPassword = "123", StepUpToken = "x" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
