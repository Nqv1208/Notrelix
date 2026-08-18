using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Identity.ApiTokens.Commands.CreateApiToken;
using Notrelix.Application.Features.Identity.ApiTokens.Commands.RevokeApiToken;
using Notrelix.Application.Features.Identity.ApiTokens.DTOs;
using Notrelix.Application.Features.Identity.Security.Abstractions;
using Notrelix.Application.Features.Identity.Security.DTOs;
using Notrelix.Domain.Identity.Tokens;
using Notrelix.Domain.Identity.Users;
using Notrelix.Infrastructure.Auth.ApiTokens;
using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Security.ApiTokens;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;
using Notrelix.Testing.Integration;

namespace Notrelix.Integration.Tests.Auth;

/// <summary>
/// API token lifecycle certification (Phase 12): issuance returns the raw
/// secret exactly once, verification authenticates only active/valid tokens,
/// and revocation is effective immediately — against the real PostgreSQL
/// production graph (no RLS policies in the shared container; RLS isolation
/// for api_tokens is certified separately in RlsRuntimeEnforcementTests).
/// </summary>
[Collection("Cache")]
[Trait("Category", "Integration")]
public sealed class ApiTokenFlowTests : IAsyncLifetime
{
    private const string Email = "apitoken@example.com";

    private readonly CacheTestContainer _fixture;
    private User _user = null!;
    private Guid _accountId = Guid.NewGuid();
    private Guid _workspaceId = Guid.NewGuid();
    private readonly DateTimeOffset _now = DateTimeOffset.UtcNow;

    public ApiTokenFlowTests(CacheTestContainer fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        await _fixture.ResetAsync();
        await using var context = _fixture.CreatePostgresContext();
        _user = User.Create(Email, "API Token User", "hashed", _now, hasPasswordCredential: true);
        context.Users.Add(_user);
        await context.SaveChangesAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private ApplicationDbContext CreateSystemContext()
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        return _fixture.CreatePostgresContext(tenant);
    }

    private Mock<ICurrentRequestContext> Actor(Guid userId) =>
        Actor(userId, sessionId: null);

    private Mock<ICurrentRequestContext> Actor(Guid userId, Guid? sessionId)
    {
        var ctx = new Mock<ICurrentRequestContext>();
        ctx.Setup(x => x.UserId).Returns(userId);
        if (sessionId.HasValue)
        {
            ctx.Setup(x => x.SessionId).Returns(sessionId.Value);
        }
        ctx.Setup(x => x.RequireAccountId()).Returns(_accountId);
        ctx.Setup(x => x.RequireWorkspaceId()).Returns(_workspaceId);
        return ctx;
    }

    private async Task<Result<CreatedApiTokenDto>> SendCreateToken(
        Guid userId, string name, DateTimeOffset? expiresAt = null)
    {
        await using var context = CreateSystemContext();
        var stepUp = new Mock<ISecurityStepUpService>();
        stepUp.Setup(s => s.ConsumeAsync(
                It.IsAny<string>(), userId, It.IsAny<Guid>(),
                StepUpPurpose.IssueApiToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var time = new Mock<IDateTimeProvider>();
        time.Setup(t => t.UtcNow).Returns(() => DateTimeOffset.UtcNow);

        // Wire as a session-authenticated workspace actor with step-up proof.
        var tokenId = Guid.NewGuid();
        var handler = new CreateApiTokenCommandHandler(
            context, Actor(userId, sessionId: Guid.NewGuid()).Object, stepUp.Object,
            new ApiTokenSecretService(), time.Object,
            NullLogger<CreateApiTokenCommandHandler>.Instance);
        var result = await handler.Handle(new CreateApiTokenCommand(
            _workspaceId, name, expiresAt, "proof-token"), CancellationToken.None);
        await context.SaveChangesAsync();
        return result;
    }

    private async Task SendRevoke(Guid userId, Guid tokenId)
    {
        await using var context = CreateSystemContext();
        var time = new Mock<IDateTimeProvider>();
        time.Setup(t => t.UtcNow).Returns(() => DateTimeOffset.UtcNow);
        var handler = new RevokeApiTokenCommandHandler(
            context, Actor(userId).Object, time.Object,
            NullLogger<RevokeApiTokenCommandHandler>.Instance);
        var result = await handler.Handle(new RevokeApiTokenCommand(_workspaceId, tokenId), CancellationToken.None);
        result.Succeeded.Should().BeTrue();
        await context.SaveChangesAsync();
    }

    private async Task<AuthenticateResult> AuthenticateAsync(string rawToken)
    {
        var options = new ApiTokenAuthenticationOptions();
        var optionsMonitor = new Mock<IOptionsMonitor<ApiTokenAuthenticationOptions>>();
        optionsMonitor.Setup(o => o.CurrentValue).Returns(options);
        optionsMonitor.Setup(o => o.Get(ApiTokenAuthenticationOptions.SchemeName)).Returns(options);

        var time = new Mock<IDateTimeProvider>();
        time.Setup(t => t.UtcNow).Returns(() => DateTimeOffset.UtcNow);

        var handler = new ApiTokenAuthenticationHandler(
            optionsMonitor.Object,
            NullLoggerFactory.Instance,
            System.Text.Encodings.Web.UrlEncoder.Default,
            new SystemClock(),
            CreateOptions(),
            new ApiTokenSecretService(),
            time.Object);

        var context = new DefaultHttpContext();
        context.Request.Headers.Authorization = $"Bearer {rawToken}";
        handler.InitializeAsync(new AuthenticationScheme(
            ApiTokenAuthenticationOptions.SchemeName,
            ApiTokenAuthenticationOptions.SchemeName,
            typeof(ApiTokenAuthenticationHandler)), context).GetAwaiter().GetResult();

        return await handler.AuthenticateAsync();
    }

    private DbContextOptions<ApplicationDbContext> CreateOptions()
        => new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(_fixture.PostgresConnectionString, npgOptions =>
            {
                npgOptions.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
                npgOptions.MigrationsHistoryTable("__EFMigrationsHistory", "ops");
            })
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning))
            .UseSnakeCaseNamingConvention()
            .ReplaceService<IModelCacheKeyFactory, WorkspaceAwareModelCacheKeyFactory>()
            .Options;

    [Fact]
    public async Task Create_ReturnsRawSecretOnce_ThenAuthenticatesRequest()
    {
        var created = await SendCreateToken(_user.Id, "Deploy token");
        created.Succeeded.Should().BeTrue();
        created.Data!.RawSecret.Should().StartWith("ntk_v1.");

        await using var context = CreateSystemContext();
        var stored = context.ApiTokens.Single(t => t.Id == created.Data.Id);
        stored.TokenHash.Should().NotBe(created.Data.RawSecret,
            "only the digest is persisted");
        stored.Status.Should().Be(ApiTokenStatus.Active);

        var auth = await AuthenticateAsync(created.Data.RawSecret);
        auth.Succeeded.Should().BeTrue();
        auth.Principal!.FindFirstValue(JwtRegisteredClaimNames.Sub).Should().Be(_user.Id.ToString());
    }

    [Fact]
    public async Task Authenticate_UnknownSecret_IsRejected()
    {
        var auth = await AuthenticateAsync("ntk_v1.never-issued-secret");
        auth.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task Authenticate_GarbageToken_IsRejected()
    {
        var auth = await AuthenticateAsync("not-a-token");
        auth.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task Revoke_IsEffectiveImmediately_AndTokenCannotAuthenticateAnymore()
    {
        var created = await SendCreateToken(_user.Id, "Ephemeral token");
        created.Succeeded.Should().BeTrue();

        var before = await AuthenticateAsync(created.Data!.RawSecret);
        before.Succeeded.Should().BeTrue();

        await SendRevoke(_user.Id, created.Data.Id);

        var after = await AuthenticateAsync(created.Data.RawSecret);
        after.Succeeded.Should().BeFalse(
            "a revoked token must never authenticate again");
    }

    [Fact]
    public async Task Authenticate_ExpiredToken_IsRejected()
    {
        // Issuance rejects past expirations, so seed the expired token directly.
        var expired = DateTimeOffset.UtcNow.AddMinutes(-5);
        var secretService = new ApiTokenSecretService();
        var secret = secretService.Generate();
        var token = ApiToken.Create(
            _accountId, _workspaceId, _user.Id, "Expired token", secret.TokenHash,
            scopes: null, createdBy: _user.Id, createdAt: expired, expiresAt: expired);
        await using (var context = CreateSystemContext())
        {
            context.ApiTokens.Add(token);
            await context.SaveChangesAsync();
        }

        var auth = await AuthenticateAsync(secret.RawToken);
        auth.Succeeded.Should().BeFalse("an expired token must fail closed");
    }
}