using System.Net;
using System.Net.Http.Json;
using MediatR;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Notrelix.API.Tests.Assertions;
using Notrelix.API.Tests.Contracts;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Identity.Auth.Commands.Login;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Notrelix.API.Tests.Identity.Auth;

public class LoginErrorContractTests
{
    private sealed class FailingLoginFactory : NotrelixApiFactory
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);

            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IRequestHandler<LoginCommand, Result<AuthResult>>>();
                services.AddScoped<IRequestHandler<LoginCommand, Result<AuthResult>>>(_ =>
                {
                    var handler = new Mock<IRequestHandler<LoginCommand, Result<AuthResult>>>();
                    handler.Setup(h => h.Handle(It.IsAny<LoginCommand>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync(Result<AuthResult>.Failure(new ApplicationError(
                            "identity.auth.invalid-credentials",
                            "Invalid email or password",
                            ApplicationErrorType.Authentication)));
                    return handler.Object;
                });
            });
        }
    }

    [Fact]
    public async Task Login_WithTypedAuthenticationFailure_Returns401ProblemDetails()
    {
        var factory = new FailingLoginFactory();
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new { Email = "user@test.com", Password = "Test@123456" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var json = await response.ReadAsJsonElementAsync();
        json.GetProperty("status").GetInt32().Should().Be(StatusCodes.Status401Unauthorized);
        json.GetProperty("type").GetString().Should().StartWith("https://");
        json.GetProperty("title").GetString().Should().Be("Unauthorized");
        json.GetProperty("detail").GetString().Should().Be("Invalid email or password");
        json.GetProperty("errorCode").GetString().Should().Be("auth.unauthorized");
        json.TryGetProperty("traceId", out _).Should().BeTrue();
    }
}
