using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Notrelix.API.Tests.Contracts;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Identity.OAuth.Commands.StartOAuthStepUp;
using Notrelix.Application.Features.Identity.OAuth.DTOs;
using Notrelix.Application.Features.Identity.Security.DTOs;
using Notrelix.Infrastructure.Auth.Csrf;

namespace Notrelix.API.Tests.Identity.Auth;

/// <summary>
/// The provider re-authentication cannot be started by a cross-site link or form: it is a POST behind the
/// double-submit CSRF check, it only accepts an internal return path, and a handover proof must name its target.
/// </summary>
public sealed class OAuthStepUpStartTests : IDisposable
{
    private const string StartPath = "/api/v1/auth/oauth/google/step-up/start";
    private const string BootstrapPath = "/api/v1/auth/csrf";
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private sealed record IssueCsrfTokenResponse(string Token);

    private readonly WebApplicationFactory<Program> _factory;
    private StartOAuthStepUpCommand? _sent;

    public OAuthStepUpStartTests()
    {
        _factory = new CsrfEnabledApiFactory().WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IRequestHandler<StartOAuthStepUpCommand, Result<OAuthLoginStartResult>>>();
            services.AddScoped<IRequestHandler<StartOAuthStepUpCommand, Result<OAuthLoginStartResult>>>(_ =>
            {
                var handler = new Mock<IRequestHandler<StartOAuthStepUpCommand, Result<OAuthLoginStartResult>>>();
                handler.Setup(h => h.Handle(It.IsAny<StartOAuthStepUpCommand>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync((StartOAuthStepUpCommand command, CancellationToken _) =>
                    {
                        _sent = command;
                        return Result<OAuthLoginStartResult>.Success(new OAuthLoginStartResult("https://provider.test/authorize"));
                    });
                return handler.Object;
            });
        }));
    }

    public void Dispose() => _factory.Dispose();

    private HttpClient SignedInClient()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Add("X-Test-Auth", "true");
        client.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString("N"));
        return client;
    }

    private async Task<HttpResponseMessage> Post(HttpClient client, object body, bool withCsrf = true, string path = StartPath)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, path) { Content = JsonContent.Create(body) };
        if (withCsrf)
        {
            var bootstrap = await client.GetAsync(BootstrapPath);
            var token = (await bootstrap.Content.ReadFromJsonAsync<IssueCsrfTokenResponse>(Json))!.Token;
            request.Headers.Add("Cookie", $"csrf_token={token}");
            request.Headers.Add(CsrfProtector.HeaderName, token);
        }

        return await client.SendAsync(request);
    }

    [Fact]
    public async Task ACrossSiteNavigation_TheGetThatStartedItBefore_IsNotARoute()
    {
        using var client = SignedInClient();

        var response = await client.GetAsync($"{StartPath}?purpose=TransferWorkspaceOwnership&returnUrl=%2Fx");

        response.StatusCode.Should().Be(HttpStatusCode.MethodNotAllowed);
        _sent.Should().BeNull();
    }

    [Fact]
    public async Task APostWithoutTheCsrfPair_IsRefused_AndNothingStarts()
    {
        using var client = SignedInClient();

        var response = await Post(client, new { purpose = "RevokeOtherSessions", returnUrl = "/x" }, withCsrf: false);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(Json);
        problem.GetProperty("errorCode").GetString().Should().Be("security.csrf_validation_failed");
        _sent.Should().BeNull();
    }

    [Theory]
    [InlineData("https://evil.example/steal")]
    [InlineData("//evil.example/steal")]
    [InlineData("/\\evil.example")]
    [InlineData("javascript:alert(1)")]
    [InlineData("settings")]
    public async Task AReturnUrlThatIsNotAnInternalPath_IsRefused_AndNothingStarts(string returnUrl)
    {
        using var client = SignedInClient();

        var response = await Post(client, new { purpose = "RevokeOtherSessions", returnUrl });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        _sent.Should().BeNull();
    }

    [Fact]
    public async Task AHandoverProofWithoutItsTarget_IsRefused_AndNothingStarts()
    {
        using var client = SignedInClient();

        var response = await Post(client, new { purpose = "TransferWorkspaceOwnership", returnUrl = "/x" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        _sent.Should().BeNull();
    }

    [Fact]
    public async Task AHandoverStartedWithTheCsrfPair_AnswersTheProviderUrl_AndBindsTheTarget()
    {
        using var client = SignedInClient();
        var workspaceId = Guid.NewGuid();
        var newOwnerId = Guid.NewGuid();

        var response = await Post(client, new
        {
            purpose = "TransferWorkspaceOwnership",
            returnUrl = "/workspaces/w/settings",
            workspaceId,
            newOwnerId,
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(Json);
        body.GetProperty("authorizationUrl").GetString().Should().Be("https://provider.test/authorize");
        _sent!.Target.Should().Be(StepUpTarget.OwnershipTransfer(workspaceId, newOwnerId));
        _sent.ReturnUrl.Should().Be("/workspaces/w/settings");
    }

    [Theory]
    [InlineData("/api/v1/auth/security/step-up/complete-password", "password")]
    [InlineData("/api/v1/auth/security/step-up/complete-mfa", "challengeToken")]
    public async Task ACodeOrPasswordProofForAHandover_WithoutItsTarget_IsRefused(string path, string field)
    {
        using var client = SignedInClient();

        var response = await Post(
            client,
            new Dictionary<string, object> { ["purpose"] = "TransferWorkspaceOwnership", [field] = "x", ["code"] = "123456" },
            path: path);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
