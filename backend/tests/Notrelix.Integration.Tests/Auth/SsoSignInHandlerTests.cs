using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Accounts.Abstractions.Records;
using Notrelix.Application.Features.Accounts.Security;
using Notrelix.Application.Features.Accounts.Sso;
using Notrelix.Application.Features.Identity.Sso;
using Notrelix.Application.Features.Identity.Sso.Commands.CompleteSsoSignIn;
using Notrelix.Application.Features.Identity.Sso.Commands.StartSsoSignIn;
using Notrelix.Domain.Accounts.Accounts;
using Notrelix.Domain.Accounts.Domains;
using Notrelix.Domain.Accounts.IdentityProviders;
using Notrelix.Domain.Accounts.Members;
using Notrelix.Domain.Identity.Mfa;
using Notrelix.Domain.Identity.Sso;
using Notrelix.Domain.Identity.Users;
using Notrelix.Domain.SharedKernel;
using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Data.Authz;
using Notrelix.Infrastructure.Identity.Sso;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Integration.Tests.Auth;

/// <summary>
/// The company sign-in callback over a real database (owner connection, no row-level security: the scope behavior has
/// its own application-role tests) with a fake provider that signs real ID tokens. Covers identity resolution (known
/// subject, link, just-in-time), every refusal, replay, and that MFA still applies afterwards.
/// </summary>
[Collection("Database")]
public class SsoSignInHandlerTests : IAsyncLifetime
{
    private const string Issuer = "https://login.acme.example";
    private const string ClientSecret = "client-secret-must-never-leak-77aa";
    private const string Callback = "https://api.example/api/v1/auth/sso/callback";

    private readonly PostgresTestContainer _db;
    private readonly ReversibleSecretEncryptor _encryptor = new();
    private readonly FakeDateTimeProvider _clock = FakeDateTimeProvider.WithFixedTime(DateTimeOffset.UtcNow);
    private readonly InMemorySsoStateStore _states = new();
    private readonly FakeOidcProvider _provider = new(Issuer, "client-1", ClientSecret);
    private readonly List<string> _logs = [];
    private Guid _accountId;

    public SsoSignInHandlerTests(PostgresTestContainer db) => _db = db;

    public async Task InitializeAsync() => await new DatabaseReset(_db.OwnerConnectionString).ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    // ── seeding ────────────────────────────────────────────────────────────

    private async Task SeedAccountAsync(bool jit = true, bool domainVerified = true, bool requireMfa = false, string domain = "acme.com")
    {
        await using var context = _db.CreateOwnerContext();
        var now = _clock.UtcNow;
        var owner = User.Create("owner@acme.com", "Owner", "hash", now, hasPasswordCredential: true);
        owner.ConfirmEmail(null, now);
        context.Users.Add(owner);
        var account = Account.Create("Acme Corp", "acme-corp", AccountType.Team, owner.Id, now);
        context.Accounts.Add(account);
        context.AccountMembers.Add(AccountMember.Create(account.Id, owner.Id, AccountRole.Owner, owner.Id, now));
        var row = AccountDomain.Create(account.Id, domain, owner.Id, now, AccountSsoSupport.HashToken("t"), _encryptor.Protect("t", AccountSsoSupport.DomainTokenPurpose));
        if (domainVerified) row.Verify(now, owner.Id);
        context.AccountDomains.Add(row);
        var provider = AccountIdentityProvider.CreateOidc(
            account.Id, "Acme SSO", Issuer, "client-1", _encryptor.Protect(ClientSecret, AccountSsoSupport.ClientSecretPurpose), jit, owner.Id, now);
        provider.MarkValidated(Issuer + "/authorize", owner.Id, now);
        context.AccountIdentityProviders.Add(provider);
        if (requireMfa)
            context.AccountSettingsEntities.Add(new AccountSettingRecord
            {
                Id = Guid.CreateVersion7(),
                AccountId = account.Id,
                SettingKey = AccountSecuritySettingKeys.RequireMfa,
                SettingValue = "true",
                CreatedAt = now,
            });
        await context.SaveChangesAsync();
        _accountId = account.Id;
    }

    private async Task<User> SeedUserAsync(string email, bool confirmed = true, bool suspended = false, bool activeMfa = false)
    {
        await using var context = _db.CreateOwnerContext();
        var user = User.Create(email, "Existing", "hash", _clock.UtcNow, hasPasswordCredential: true);
        if (confirmed) user.ConfirmEmail(null, _clock.UtcNow);
        if (suspended) user.Suspend(Guid.NewGuid(), _clock.UtcNow, "test");
        context.Users.Add(user);
        if (activeMfa)
        {
            var method = UserMfaMethod.Create(user.Id, MfaMethodType.AuthenticatorApp, _clock.UtcNow, SecretRef.Create("s"));
            method.Verify(_clock.UtcNow);
            context.UserMfaMethods.Add(method);
        }

        await context.SaveChangesAsync();
        return user;
    }

    private async Task BindAsync(Guid userId, string subject)
    {
        await using var context = _db.CreateOwnerContext();
        context.SsoIdentities.Add(SsoIdentity.Create(_accountId, Issuer, subject, userId, _clock.UtcNow));
        await context.SaveChangesAsync();
    }

    // ── running the flow ───────────────────────────────────────────────────

    private sealed class Factory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
    }

    private sealed class CapturingLogger<T>(List<string> sink) : ILogger<T>
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            sink.Add(formatter(state, exception) + (exception is null ? "" : " | " + exception));
    }

    private sealed record Run(Result<SsoSignInResult> Result, Mock<IAuthSessionIssuer> Sessions, string? State);

    private (CompleteSsoSignInCommandHandler Complete, StartSsoSignInCommandHandler Start, Mock<IAuthSessionIssuer> Sessions, ApplicationDbContext Context) Build()
    {
        var context = _db.CreateOwnerContext();
        var discovery = new FakeOidcDiscoveryClient().Serve(Issuer);
        var facts = new AccountSsoFactsProvider(
            context, _encryptor, new AccountGrantProjectionServiceAdapter(new AccessGrantProjectionService(context)));
        var sessions = new Mock<IAuthSessionIssuer>();
        sessions.Setup(s => s.IssueAsync(It.IsAny<User>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AuthResult { AccessToken = "access", RefreshToken = "refresh" });
        var hasher = new Mock<IPasswordHasher>();
        hasher.Setup(h => h.HashPassword(It.IsAny<string>())).Returns("sentinel-hash");

        var complete = new CompleteSsoSignInCommandHandler(
            _states, facts, new SsoCodeRedeemer(new Factory(_provider), discovery, _clock), context, new SsoPendingWrites(context),
            sessions.Object, new InMemoryMfaChallengeStore(_clock), new AccountSecurityPolicyFactsProvider(context), hasher.Object, _clock,
            new CapturingLogger<CompleteSsoSignInCommandHandler>(_logs));
        var start = new StartSsoSignInCommandHandler(facts, discovery, _states, _clock, NullLogger<StartSsoSignInCommandHandler>.Instance);
        return (complete, start, sessions, context);
    }

    /// <summary>Starts a sign-in for <paramref name="startEmail"/>, lets the provider answer as configured, and completes the callback like the pipeline would (handler, then save).</summary>
    private async Task<Run> SignInAsync(string providerEmail, string startEmail = "ann@acme.com", Action<FakeOidcProvider>? tweak = null, string? stateOverride = null)
    {
        var (complete, start, sessions, context) = Build();
        await using var _ = context;
        var started = await start.Handle(new StartSsoSignInCommand { Email = startEmail, RedirectUri = Callback, ReturnUrl = "/boards/1" }, CancellationToken.None);
        started.Succeeded.Should().BeTrue();

        _provider.Email = providerEmail;
        _provider.Name = "Ann Example";
        _provider.Follow(started.Data!.AuthorizationUrl);
        tweak?.Invoke(_provider);

        var result = await complete.Handle(
            new CompleteSsoSignInCommand { Code = _provider.Code, State = stateOverride ?? _provider.State }, CancellationToken.None);
        await context.SaveChangesAsync();
        return new Run(result, sessions, _provider.State);
    }

    private static string Code(Result<SsoSignInResult> result) => result.TypedErrors.Single().Code;

    private static void NoSession(Run run) =>
        run.Sessions.Verify(s => s.IssueAsync(It.IsAny<User>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()), Times.Never);

    // ── just-in-time ───────────────────────────────────────────────────────

    [Fact]
    public async Task ANewPerson_IsCreatedAsAMemberOnly_WithNoPasswordNoPersonalAccountAndNoWorkspace_AndSignsIn()
    {
        await SeedAccountAsync(jit: true);

        var run = await SignInAsync("Ann@Acme.com");

        run.Result.Succeeded.Should().BeTrue();
        run.Result.Data!.Auth.AccessToken.Should().Be("access");
        run.Result.Data.ReturnUrl.Should().Be("/boards/1");

        await using var context = _db.CreateOwnerContext();
        var user = await context.Users.SingleAsync(u => u.NormalizedEmail == "ann@acme.com");
        user.HasPasswordCredential.Should().BeFalse();
        user.EmailConfirmed.Should().BeTrue("the provider verified it and the Account verified the domain");
        user.Name.Should().Be("Ann Example");
        (await context.AccountMembers.Where(m => m.UserId == user.Id).ToListAsync()).Should().ContainSingle()
            .Which.Should().Match<AccountMember>(m => m.AccountId == _accountId && m.Role == AccountRole.Member && m.Status == AccountMemberStatus.Active);
        (await context.WorkspaceMembers.CountAsync(m => m.UserId == user.Id)).Should().Be(0, "a company sign-in never grants a workspace role");
        (await context.Accounts.CountAsync()).Should().Be(1, "no personal Account is created");
        (await context.SsoIdentities.SingleAsync(i => i.UserId == user.Id)).Should().Match<SsoIdentity>(i => i.AccountId == _accountId && i.Subject == "subject-1" && i.Issuer == Issuer);
    }

    [Fact]
    public async Task WithProvisioningOff_AnUnknownPersonIsRefused_AndNothingIsCreated()
    {
        await SeedAccountAsync(jit: false);

        var run = await SignInAsync("ann@acme.com");

        Code(run.Result).Should().Be(SsoErrorCodes.NotProvisioned);
        NoSession(run);
        await using var context = _db.CreateOwnerContext();
        (await context.Users.CountAsync(u => u.NormalizedEmail == "ann@acme.com")).Should().Be(0);
        (await context.SsoIdentities.CountAsync()).Should().Be(0);
    }

    // ── linking ────────────────────────────────────────────────────────────

    [Fact]
    public async Task AnExistingUserWithTheSameVerifiedEmail_IsLinked_AndJoinedAsAMember_AndSignsInWithoutANewUser()
    {
        await SeedAccountAsync();
        var existing = await SeedUserAsync("ann@acme.com");

        var run = await SignInAsync("ANN@acme.com");

        run.Result.Succeeded.Should().BeTrue();
        await using var context = _db.CreateOwnerContext();
        (await context.Users.CountAsync(u => u.NormalizedEmail == "ann@acme.com")).Should().Be(1);
        (await context.SsoIdentities.SingleAsync()).UserId.Should().Be(existing.Id);
        (await context.AccountMembers.SingleAsync(m => m.UserId == existing.Id && m.AccountId == _accountId)).Role.Should().Be(AccountRole.Member);
    }

    [Fact]
    public async Task WithProvisioningOff_ALinkedExistingUserSignsIn_ButIsNotJoinedToTheAccount()
    {
        await SeedAccountAsync(jit: false);
        var existing = await SeedUserAsync("ann@acme.com");

        var run = await SignInAsync("ann@acme.com");

        run.Result.Succeeded.Should().BeTrue();
        await using var context = _db.CreateOwnerContext();
        (await context.SsoIdentities.CountAsync(i => i.UserId == existing.Id)).Should().Be(1);
        (await context.AccountMembers.CountAsync(m => m.UserId == existing.Id && m.AccountId == _accountId)).Should().Be(0);
    }

    [Fact]
    public async Task AnExistingUserWhoseOwnEmailIsUnverified_IsNeverLinkedByEmail()
    {
        await SeedAccountAsync();
        await SeedUserAsync("ann@acme.com", confirmed: false);

        var run = await SignInAsync("ann@acme.com");

        Code(run.Result).Should().Be(SsoErrorCodes.IdentityConflict);
        NoSession(run);
        await using var context = _db.CreateOwnerContext();
        (await context.SsoIdentities.CountAsync()).Should().Be(0);
        (await context.AccountMembers.CountAsync(m => m.AccountId == _accountId)).Should().Be(1, "only the Owner");
    }

    [Fact]
    public async Task AUserAlreadyBoundToAnotherSubjectOfTheAccount_IsNotLinkedASecondTime()
    {
        await SeedAccountAsync();
        var existing = await SeedUserAsync("ann@acme.com");
        await BindAsync(existing.Id, "an-older-subject");

        var run = await SignInAsync("ann@acme.com");

        Code(run.Result).Should().Be(SsoErrorCodes.IdentityConflict);
        NoSession(run);
        await using var context = _db.CreateOwnerContext();
        (await context.SsoIdentities.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task AKnownSubject_SignsInAsItsOwnUser_WhateverEmailTheTokenCarries_AndLinksNobodyElse()
    {
        await SeedAccountAsync();
        var bound = await SeedUserAsync("bound@acme.com");
        var other = await SeedUserAsync("ann@acme.com");
        await BindAsync(bound.Id, "subject-1");

        var run = await SignInAsync("ann@acme.com");

        run.Result.Succeeded.Should().BeTrue();
        run.Sessions.Verify(s => s.IssueAsync(It.Is<User>(u => u.Id == bound.Id), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()), Times.Once);
        await using var context = _db.CreateOwnerContext();
        (await context.SsoIdentities.CountAsync(i => i.UserId == other.Id)).Should().Be(0);
        (await context.SsoIdentities.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task ASuspendedUser_IsRefusedWithTheGenericFailure()
    {
        await SeedAccountAsync();
        var suspended = await SeedUserAsync("ann@acme.com", suspended: true);
        await BindAsync(suspended.Id, "subject-1");

        var run = await SignInAsync("ann@acme.com");

        Code(run.Result).Should().Be(SsoErrorCodes.Failed);
        NoSession(run);
    }

    // ── what the provider and the Account must both vouch for ──────────────

    [Fact]
    public async Task AnEmailTheProviderDidNotVerify_IsRefused_AndNothingIsLinkedOrCreated()
    {
        await SeedAccountAsync();
        await SeedUserAsync("ann@acme.com");

        var run = await SignInAsync("ann@acme.com", tweak: p => p.EmailVerified = false);

        Code(run.Result).Should().Be(SsoErrorCodes.EmailUnverified);
        NoSession(run);
        await using var context = _db.CreateOwnerContext();
        (await context.SsoIdentities.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task AnEmailOnADomainThisAccountDidNotVerify_IsRefused_WithNoLinkNoJoinAndNoNewUser()
    {
        await SeedAccountAsync();
        var existing = await SeedUserAsync("ann@elsewhere.example");

        var run = await SignInAsync("ann@elsewhere.example");

        Code(run.Result).Should().Be(SsoErrorCodes.DomainMismatch);
        NoSession(run);
        await using var context = _db.CreateOwnerContext();
        (await context.SsoIdentities.CountAsync()).Should().Be(0);
        (await context.AccountMembers.CountAsync(m => m.UserId == existing.Id)).Should().Be(0);

        var fresh = await SignInAsync("new@elsewhere.example");
        Code(fresh.Result).Should().Be(SsoErrorCodes.DomainMismatch);
        (await _db.CreateOwnerContext().Users.CountAsync(u => u.NormalizedEmail == "new@elsewhere.example")).Should().Be(0);
    }

    [Fact]
    public async Task ADomainThatIsOnlyPendingForTheAccount_DoesNotLetAnyoneIn()
    {
        await SeedAccountAsync(domainVerified: false);
        var (_, start, _, context) = Build();
        await using var _ = context;

        // Pending routes nothing, so the start itself is refused.
        var started = await start.Handle(new StartSsoSignInCommand { Email = "ann@acme.com", RedirectUri = Callback }, CancellationToken.None);

        Code(started.Succeeded ? throw new InvalidOperationException("must refuse") : ToSso(started)).Should().Be(SsoErrorCodes.Unavailable);
    }

    private static Result<SsoSignInResult> ToSso(Result<SsoStartResult> failed) =>
        Result<SsoSignInResult>.Failure(failed.TypedErrors.Single());

    // ── state, replay, provider errors ─────────────────────────────────────

    [Fact]
    public async Task AReplayedCallback_FindsNoState_AndIssuesNoSecondSession()
    {
        await SeedAccountAsync();
        var (complete, start, sessions, context) = Build();
        await using var _ = context;
        var started = await start.Handle(new StartSsoSignInCommand { Email = "ann@acme.com", RedirectUri = Callback }, CancellationToken.None);
        _provider.Email = "ann@acme.com";
        _provider.Follow(started.Data!.AuthorizationUrl);
        var command = new CompleteSsoSignInCommand { Code = _provider.Code, State = _provider.State };

        var first = await complete.Handle(command, CancellationToken.None);
        await context.SaveChangesAsync();
        var replay = await complete.Handle(command, CancellationToken.None);

        first.Succeeded.Should().BeTrue();
        Code(replay).Should().Be(SsoErrorCodes.StateInvalid);
        sessions.Verify(s => s.IssueAsync(It.IsAny<User>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("not-a-state")]
    [InlineData("")]
    public async Task AStateThatWasNeverIssued_IsRefused(string state)
    {
        await SeedAccountAsync();

        var run = await SignInAsync("ann@acme.com", stateOverride: state);

        Code(run.Result).Should().Be(SsoErrorCodes.StateInvalid);
        NoSession(run);
        _provider.Requests.Should().BeEmpty("a refused state exchanges nothing");
    }

    [Fact]
    public async Task AProviderError_EndsWithDeniedOrFailed_AfterUsingUpTheState()
    {
        await SeedAccountAsync();
        var (complete, start, _, context) = Build();
        await using var _ = context;
        var started = await start.Handle(new StartSsoSignInCommand { Email = "ann@acme.com", RedirectUri = Callback }, CancellationToken.None);
        var state = new Uri(started.Data!.AuthorizationUrl).Query.Split('&').First(p => p.StartsWith("state=", StringComparison.Ordinal))[6..];

        var denied = await complete.Handle(new CompleteSsoSignInCommand { State = Uri.UnescapeDataString(state), Error = "access_denied", ErrorDescription = "x\r\ny" }, CancellationToken.None);
        var again = await complete.Handle(new CompleteSsoSignInCommand { State = Uri.UnescapeDataString(state), Error = "access_denied" }, CancellationToken.None);

        Code(denied).Should().Be(SsoErrorCodes.Denied);
        Code(again).Should().Be(SsoErrorCodes.StateInvalid);
        _logs.Should().NotContain(l => l.Contains('\n') || l.Contains('\r'));
        _provider.Requests.Should().BeEmpty();
    }

    [Fact]
    public async Task AProviderWhoseIssuerChangedSinceTheStart_IsRefused_BeforeAnythingIsExchanged()
    {
        await SeedAccountAsync();
        var (complete, start, _, context) = Build();
        await using var _ = context;
        var started = await start.Handle(new StartSsoSignInCommand { Email = "ann@acme.com", RedirectUri = Callback }, CancellationToken.None);
        _provider.Follow(started.Data!.AuthorizationUrl);
        var stored = _states.Stored.Single();
        await _states.ConsumeAsync(stored.State, CancellationToken.None);
        await _states.StoreAsync(stored with { Issuer = "https://old.example" }, TimeSpan.FromMinutes(1), CancellationToken.None);

        var result = await complete.Handle(new CompleteSsoSignInCommand { Code = _provider.Code, State = _provider.State }, CancellationToken.None);

        Code(result).Should().Be(SsoErrorCodes.Failed);
        _provider.Requests.Should().BeEmpty();
    }

    [Theory]
    [InlineData("nonce")]
    [InlineData("audience")]
    [InlineData("issuer")]
    [InlineData("signature")]
    public async Task AnIdTokenThatCannotBeTrusted_IsRefused_AndNothingIsCreated(string flaw)
    {
        await SeedAccountAsync();

        var run = await SignInAsync("ann@acme.com", tweak: p =>
        {
            switch (flaw)
            {
                case "nonce": p.NonceClaim = "not-mine"; break;
                case "audience": p.AudienceClaim = "someone-else"; break;
                case "issuer": p.IssuerClaim = "https://evil.example"; break;
                case "signature": p.SigningKeyOverride = System.Security.Cryptography.RSA.Create(2048); break;
            }
        });

        Code(run.Result).Should().Be(SsoErrorCodes.Failed);
        NoSession(run);
        await using var context = _db.CreateOwnerContext();
        (await context.Users.CountAsync(u => u.NormalizedEmail == "ann@acme.com")).Should().Be(0);
    }

    // ── MFA still applies ──────────────────────────────────────────────────

    [Fact]
    public async Task APersonWithASecondFactor_GetsAChallenge_AndNoSession()
    {
        await SeedAccountAsync();
        var existing = await SeedUserAsync("ann@acme.com", activeMfa: true);
        await BindAsync(existing.Id, "subject-1");

        var run = await SignInAsync("ann@acme.com");

        run.Result.Succeeded.Should().BeTrue();
        run.Result.Data!.Auth.MfaRequired.Should().BeTrue();
        run.Result.Data.Auth.MfaChallengeToken.Should().NotBeNullOrEmpty();
        NoSession(run);
    }

    [Fact]
    public async Task WhenTheAccountRequiresMfa_APersonWithoutAFactor_OnlyGetsTheEnrolmentStep()
    {
        await SeedAccountAsync(requireMfa: true);
        var existing = await SeedUserAsync("ann@acme.com");
        await BindAsync(existing.Id, "subject-1");
        await using (var context = _db.CreateOwnerContext())
        {
            context.AccountMembers.Add(AccountMember.Create(_accountId, existing.Id, AccountRole.Member, existing.Id, _clock.UtcNow));
            await context.SaveChangesAsync();
        }

        var run = await SignInAsync("ann@acme.com");

        run.Result.Data!.Auth.MfaEnrollmentRequired.Should().BeTrue();
        run.Result.Data.Auth.AccessToken.Should().BeNull();
        NoSession(run);
    }

    [Fact]
    public async Task ABrandNewPerson_OfAnAccountThatRequiresMfa_OnlyGetsTheEnrolmentStep()
    {
        await SeedAccountAsync(requireMfa: true);

        var run = await SignInAsync("ann@acme.com");

        run.Result.Data!.Auth.MfaEnrollmentRequired.Should().BeTrue();
        NoSession(run);
    }

    // ── secrets ────────────────────────────────────────────────────────────

    [Fact]
    public async Task NoLogLineAndNoResultEverCarriesTheClientSecret_TheCode_OrAToken()
    {
        await SeedAccountAsync();
        var success = await SignInAsync("ann@acme.com");
        var forged = await SignInAsync("ann2@acme.com", tweak: p => p.NonceClaim = "bad");
        var unverified = await SignInAsync("ann3@acme.com", tweak: p => p.EmailVerified = false);
        var token = _provider.BuildIdToken();

        var everything = string.Join('\n', _logs) + "\n" + System.Text.Json.JsonSerializer.Serialize(forged.Result.TypedErrors)
            + System.Text.Json.JsonSerializer.Serialize(unverified.Result.TypedErrors);
        everything.Should().NotContain(ClientSecret).And.NotContain(_provider.Code).And.NotContain(token.Split('.')[1]).And.NotContain("access-never-used");
        _logs.Should().NotBeEmpty();
        success.Result.Succeeded.Should().BeTrue();
    }

    // ── the database refuses what the handler also refuses ─────────────────

    [Fact]
    public async Task TheDatabase_RefusesASecondRowForTheSameSubjectOrTheSameUserInOneAccount()
    {
        await SeedAccountAsync();
        var a = await SeedUserAsync("a@acme.com");
        var b = await SeedUserAsync("b@acme.com");
        await BindAsync(a.Id, "s1");

        await FluentActions.Invoking(() => BindAsync(b.Id, "s1")).Should().ThrowAsync<DbUpdateException>("one subject is one person");
        await FluentActions.Invoking(() => BindAsync(a.Id, "s2")).Should().ThrowAsync<DbUpdateException>("one person has one identity per Account");
    }
}
