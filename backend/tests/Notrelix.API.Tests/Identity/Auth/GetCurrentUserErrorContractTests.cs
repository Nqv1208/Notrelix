using System.Net;
using MediatR;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Notrelix.API.Tests.Assertions;
using Notrelix.API.Tests.Contracts;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Identity.Auth.Queries.GetCurrentUser;

namespace Notrelix.API.Tests.Identity.Auth;

public class GetCurrentUserErrorContractTests
{
    private sealed class SessionUserGoneFactory : NotrelixApiFactory
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);

            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IRequestHandler<GetCurrentUserQuery, Result<UserDto>>>();
                services.AddScoped<IRequestHandler<GetCurrentUserQuery, Result<UserDto>>>(_ =>
                {
                    var handler = new Mock<IRequestHandler<GetCurrentUserQuery, Result<UserDto>>>();
                    handler.Setup(h => h.Handle(It.IsAny<GetCurrentUserQuery>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync(Result<UserDto>.Failure(new ApplicationError(
                            "identity.auth.session-invalid",
                            "Session is invalid or expired",
                            ApplicationErrorType.Authentication)));
                    return handler.Object;
                });
            });
        }
    }

    [Fact]
    public async Task GetCurrentUser_WhenSessionUserIsGone_Returns401Problem()
    {
        var factory = new SessionUserGoneFactory();
        var client = factory.CreateAuthenticatedClient();

        var response = await client.GetAsync("/api/v1/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var json = await response.ReadAsJsonElementAsync();
        json.GetProperty("status").GetInt32().Should().Be(StatusCodes.Status401Unauthorized);
        json.GetProperty("errorCode").GetString().Should().Be("identity.auth.session-invalid");
        json.GetProperty("detail").GetString().Should().Be("Session is invalid or expired");
    }
}
