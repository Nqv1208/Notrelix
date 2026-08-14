using System.Net;
using System.Net.Http.Json;
using Notrelix.API.Tests.Contracts;

namespace Notrelix.API.Tests.Identity.Auth;

public class ChangePasswordEndpointTests : IClassFixture<NotrelixApiFactory>
{
    private readonly NotrelixApiFactory _factory;

    public ChangePasswordEndpointTests(NotrelixApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ChangePassword_WithoutAuth_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/change-password", new
        {
            CurrentPassword = "OldPassword123!",
            NewPassword = "NewPassword123!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ChangePassword_WithAuth_WhenNewPasswordIsWeak_ReturnsBadRequest()
    {
        var client = _factory.CreateAuthenticatedClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/change-password", new
        {
            CurrentPassword = "OldPassword123!",
            NewPassword = "123"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ChangePassword_WithAuth_WhenCurrentPasswordMissing_ReturnsBadRequest()
    {
        var client = _factory.CreateAuthenticatedClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/change-password", new
        {
            CurrentPassword = "",
            NewPassword = "NewPassword123!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
