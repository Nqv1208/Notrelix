using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using Notrelix.API.Tests.Assertions;
using Notrelix.API.Tests.Contracts;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Identity.Auth.Queries.GetCurrentUser;
using Notrelix.Infrastructure.Auth.Jwt;
using StackExchange.Redis;
using JwtClaims = System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames;

namespace Notrelix.API.Tests.Identity.Auth;

/// <summary>
/// The real JwtBearer pipeline, with a real revocation service over a Redis that is down: a request
/// that cannot be checked against revocation is refused with a retryable 503 and reaches no handler.
/// </summary>
public class RevocationStoreUnavailableTests
{
    private const string Secret = "ci-test-secret-key-must-be-at-least-32-characters";

    private sealed class RealJwtFactory(bool redisDown) : NotrelixApiFactory
    {
        public Mock<IRequestHandler<GetCurrentUserQuery, Result<UserDto>>> Handler { get; } = new();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);

            builder.ConfigureTestServices(services =>
            {
                var database = new Mock<IDatabase>();
                if (redisDown)
                {
                    database.Setup(d => d.KeyExistsAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
                        .ThrowsAsync(new RedisConnectionException(ConnectionFailureType.UnableToConnect, "redis is down"));
                }
                else
                {
                    database.Setup(d => d.KeyExistsAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
                        .ReturnsAsync(false);
                    database.Setup(d => d.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
                        .ReturnsAsync(RedisValue.Null);
                }

                var redis = new Mock<IConnectionMultiplexer>();
                redis.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(database.Object);

                services.RemoveAll<IJwtBlacklistService>();
                services.AddSingleton<IJwtBlacklistService>(new JwtBlacklistService(redis.Object));

                Handler.Setup(h => h.Handle(It.IsAny<GetCurrentUserQuery>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result<UserDto>.Success(new UserDto { Id = Guid.NewGuid(), Email = "a@b.c", Name = "A" }));
                services.RemoveAll<IRequestHandler<GetCurrentUserQuery, Result<UserDto>>>();
                services.AddScoped(_ => Handler.Object);

                // The base factory forces the Test scheme; restore the production composite scheme.
                services.PostConfigure<AuthenticationOptions>(options =>
                {
                    options.DefaultAuthenticateScheme = "NotrelixAuth";
                    options.DefaultChallengeScheme = "NotrelixAuth";
                });
            });
        }
    }

    private static HttpClient BearerClient(RealJwtFactory factory)
    {
        var now = DateTime.UtcNow;
        var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().WriteToken(
            new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
                issuer: "Notrelix.Tests",
                audience: "Notrelix.Tests",
                claims:
                [
                    new Claim(JwtClaims.Sub, Guid.NewGuid().ToString()),
                    new Claim(JwtClaims.Jti, Guid.NewGuid().ToString()),
                    new Claim(JwtClaims.Iat, new DateTimeOffset(now).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
                ],
                notBefore: now.AddMinutes(-1),
                expires: now.AddMinutes(30),
                signingCredentials: new SigningCredentials(
                    new SymmetricSecurityKey(JwtKeyMaterial.DeriveKeyBytes(Secret)),
                    SecurityAlgorithms.HmacSha256)));

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    [Fact]
    public async Task RedisDown_RefusesTheRequestWith503AndRunsNoHandler()
    {
        using var factory = new RealJwtFactory(redisDown: true);

        var response = await BearerClient(factory).GetAsync("/api/v1/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        response.Headers.RetryAfter.Should().NotBeNull();
        var json = await response.ReadAsJsonElementAsync();
        json.GetProperty("status").GetInt32().Should().Be(503);
        json.GetProperty("errorCode").GetString().Should().Be("auth.revocation_unavailable");
        factory.Handler.Verify(
            h => h.Handle(It.IsAny<GetCurrentUserQuery>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RedisUp_SameToken_ReachesTheHandler()
    {
        using var factory = new RealJwtFactory(redisDown: false);

        var response = await BearerClient(factory).GetAsync("/api/v1/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        factory.Handler.Verify(
            h => h.Handle(It.IsAny<GetCurrentUserQuery>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
