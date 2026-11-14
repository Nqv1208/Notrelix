using System.Net;
using System.Net.Http.Json;
using MediatR;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Notrelix.API.Tests.Contracts;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Identity.Registration.Commands.Register;

namespace Notrelix.API.Tests.Identity.Auth;

/// <summary>
/// Registering answers one neutral 200 with no body and no session, for a new address and for an address
/// that is already registered, so the answer never says which it was.
/// </summary>
public class RegisterEndpointContractTests
{
    private sealed class RaceLosingRegisterFactory : NotrelixApiFactory
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);

            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IRequestHandler<RegisterCommand, Result>>();
                services.AddScoped<IRequestHandler<RegisterCommand, Result>>(_ =>
                {
                    var handler = new Mock<IRequestHandler<RegisterCommand, Result>>();
                    handler.Setup(h => h.Handle(It.IsAny<RegisterCommand>(), It.IsAny<CancellationToken>()))
                        .ThrowsAsync(new ConflictException("duplicate key value violates unique constraint"));
                    return handler.Object;
                });
            });
        }
    }

    private static object Body(string email) =>
        new { Email = email, Password = "Test@123456", Name = "Neutral Answer" };

    [Fact]
    public async Task Register_Answers200WithNoBodyAndNoSessionCookie()
    {
        using var factory = new NotrelixApiFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/register", Body("neutral-new@test.com"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).Should().BeEmpty();
        response.Headers.Contains("Set-Cookie").Should().BeFalse("registering opens no session");
    }

    [Fact]
    public async Task Register_WhenALaterRegistrationLosesTheUniqueIndexRace_AnswersTheSameNeutral200()
    {
        using var factory = new RaceLosingRegisterFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/register", Body("neutral-race@test.com"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).Should().BeEmpty();
        response.Headers.Contains("Set-Cookie").Should().BeFalse();
    }
}
