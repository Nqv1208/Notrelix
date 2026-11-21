using Microsoft.Extensions.Logging.Abstractions;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Accounts.Provisioning;
using Notrelix.Application.Features.Accounts.Public.Sso;
using Notrelix.Application.Features.Identity.Mfa.Abstractions;
using Notrelix.Application.Features.Identity.OAuth;
using Notrelix.Application.Features.Identity.OAuth.Abstractions;
using Notrelix.Application.Features.Identity.OAuth.Commands.CompleteOAuthLogin;
using Notrelix.Application.Features.Identity.OAuth.DTOs;
using Notrelix.Domain.Identity.OAuth;
using Notrelix.Domain.Identity.Users;
using Notrelix.Domain.SharedKernel;
using Notrelix.Infrastructure.Data.Authz;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Integration.Tests.Auth;

/// <summary>A social (Google) sign-in is refused, after the provider identity is resolved and before any session, challenge or new user, when the email's Account requires company sign-in. Nobody is exempt.</summary>
[Collection("Database")]
public class GoogleLoginSsoEnforcementTests : IAsyncLifetime
{
    private static readonly OAuthLoginState State = new("state", "nonce", "verifier", OAuthProvider.Google, null, DateTimeOffset.UtcNow.AddMinutes(10), BindingHash: StateBinding.Hash("binding"));

    private readonly PostgresTestContainer _db;
    private readonly FakeAccountSsoFacts _sso = new();
    private readonly Mock<IAuthSessionIssuer> _sessions = new();
    private readonly Mock<IOAuthProviderClient> _provider = new();

    public GoogleLoginSsoEnforcementTests(PostgresTestContainer db) => _db = db;

    public async Task InitializeAsync() => await new DatabaseReset(_db.OwnerConnectionString).ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task<Result<AuthResult>> SignInAsync(string email, string subject)
    {
        await using var context = _db.CreateOwnerContext();
        var state = new Mock<IOAuthStateStore>();
        state.Setup(s => s.ConsumeAsync("state", It.IsAny<CancellationToken>())).ReturnsAsync(State);
        _provider.Setup(p => p.RedeemCodeAsync(OAuthProvider.Google, It.IsAny<OAuthCodeRedemptionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExternalOAuthProfile(OAuthProvider.Google, subject, email, true, "Gina", null, JsonValue.EmptyObject()));
        _sessions.Setup(s => s.IssueAsync(It.IsAny<User>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AuthResult { AccessToken = "a", RefreshToken = "r" });
        var options = new Mock<IOAuthOptionsProvider>();
        options.Setup(o => o.GetRedirectUri(OAuthProvider.Google)).Returns("https://localhost/cb");
        var clock = new Mock<IDateTimeProvider>();
        clock.Setup(c => c.UtcNow).Returns(() => DateTimeOffset.UtcNow);

        var handler = new CompleteOAuthLoginCommandHandler(
            state.Object, _provider.Object, options.Object, context,
            new AccountProvisioningService(context, new AccountGrantProjectionServiceAdapter(new AccessGrantProjectionService(context))),
            _sessions.Object, Mock.Of<IMfaChallengeStore>(),
            new Notrelix.Application.Features.Accounts.Security.AccountSecurityPolicyFactsProvider(context),
            Mock.Of<IPasswordHasher>(h => h.HashPassword(It.IsAny<string>()) == "hash"), clock.Object, Mock.Of<IIntegrationEventCollector>(),
            NullLogger<CompleteOAuthLoginCommandHandler>.Instance, _sso);
        var result = await handler.Handle(new CompleteOAuthLoginCommand { Provider = OAuthProvider.Google, Code = "c", State = "state", BrowserBinding = "binding" }, CancellationToken.None);
        await context.SaveChangesAsync();
        return result;
    }

    private async Task<User> LinkedGoogleUserAsync(string email, string subject)
    {
        await using var context = _db.CreateOwnerContext();
        var now = DateTimeOffset.UtcNow;
        var user = User.Create(email, "Gina", "hash", now, hasPasswordCredential: true);
        user.LinkOAuthAccount(OAuthProvider.Google, subject, OAuthProfileSnapshot.Create(OAuthProvider.Google, 1, JsonValue.EmptyObject()), null, user.Id, now);
        context.Users.Add(user);
        await context.SaveChangesAsync();
        return user;
    }

    [Fact]
    public async Task AGoogleSignInOfALinkedMemberOfAnEnforcingAccount_IsRefused_WithNoSession()
    {
        var user = await LinkedGoogleUserAsync("gina@acme.com", "g-1");
        _sso.Enforcement = new(true, Guid.NewGuid());

        var result = await SignInAsync("gina@acme.com", "g-1");

        result.TypedErrors.Single().Code.Should().Be("identity.oauth.sso-required");
        _sso.EnforcementChecks.Should().ContainSingle().Which.Should().Be(("gina@acme.com", (Guid?)user.Id, AccountSsoSignInMethod.Social));
        _sessions.Verify(s => s.IssueAsync(It.IsAny<User>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ABrandNewGooglePersonOnAnEnforcedDomain_IsRefused_BeforeAnyUserIsCreated()
    {
        _sso.Enforcement = new(true, Guid.NewGuid());

        var result = await SignInAsync("new@acme.com", "g-new");

        result.TypedErrors.Single().Code.Should().Be("identity.oauth.sso-required");
        await using var context = _db.CreateOwnerContext();
        (await context.Users.CountAsync(u => u.NormalizedEmail == "new@acme.com")).Should().Be(0);
        _sessions.Verify(s => s.IssueAsync(It.IsAny<User>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task WhenSsoIsNotRequired_AGoogleSignInIsUnchanged()
    {
        await LinkedGoogleUserAsync("gina@acme.com", "g-1");

        var result = await SignInAsync("gina@acme.com", "g-1");

        result.Succeeded.Should().BeTrue();
        _sso.EnforcementChecks.Should().ContainSingle();
    }

    [Fact]
    public void TheEnforcementCodeIsOneOfTheNormalizedGoogleErrorCodes() =>
        OAuthLoginErrorCodes.Normalize("identity.oauth.sso-required").Should().Be("identity.oauth.sso-required");
}
