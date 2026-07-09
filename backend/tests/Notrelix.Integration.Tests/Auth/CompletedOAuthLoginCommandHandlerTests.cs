using Microsoft.Extensions.Logging.Abstractions;
using Notrelix.Application.Features.Identity.OAuth.Abstractions;
using Notrelix.Application.Features.Identity.OAuth.Commands.CompleteOAuthLogin;
using Notrelix.Application.Features.Identity.OAuth.DTOs;
using Notrelix.Domain.Identity.OAuth;
using Notrelix.Domain.Identity.Users;
using Notrelix.Domain.SharedKernel;
using Notrelix.Infrastructure.Data;
using Notrelix.Integration.Tests.Containers;

namespace Notrelix.Integration.Tests.Auth;

[Collection("Database")]
public class CompleteOAuthLoginCommandHandlerTests : IAsyncLifetime
{
    private readonly PostgresTestContainer _db;
    private DatabaseReset _reset = null!;

    public CompleteOAuthLoginCommandHandlerTests(PostgresTestContainer db)
    {
        _db = db;
    }

    public async Task InitializeAsync()
    {
        _reset = new DatabaseReset(_db.ConnectionString);
        await _reset.ResetAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private static readonly JsonValue EmptyProfile = JsonValue.EmptyObject();
    private static readonly OAuthLoginState ValidState = new(
        "test-state", "test-nonce", "test-code-verifier",
        OAuthProvider.Google, null,
        DateTimeOffset.UtcNow.AddMinutes(10));

    private (Mock<IOAuthStateStore> StateStore, Mock<IOAuthProviderClient> ProviderClient,
            Mock<IOAuthOptionsProvider> OptionsProvider, Mock<IAuthSessionIssuer> SessionIssuer,
            Mock<IPasswordHasher> PasswordHasher, Mock<IDateTimeProvider> DateTimeProvider,
            Mock<IIntegrationEventCollector> EventCollector) CreateMocks()
    {
        var stateStore = new Mock<IOAuthStateStore>();
        var providerClient = new Mock<IOAuthProviderClient>();
        var optionsProvider = new Mock<IOAuthOptionsProvider>();
        var sessionIssuer = new Mock<IAuthSessionIssuer>();
        var passwordHasher = new Mock<IPasswordHasher>();
        var dateTimeProvider = new Mock<IDateTimeProvider>();
        var eventCollector = new Mock<IIntegrationEventCollector>();

        optionsProvider.Setup(x => x.GetRedirectUri(OAuthProvider.Google))
            .Returns("https://localhost/oauth/google/callback");

        return (stateStore, providerClient, optionsProvider, sessionIssuer, passwordHasher, dateTimeProvider, eventCollector);
    }

    private CompleteOAuthLoginCommandHandler CreateHandler(
        Mock<IOAuthStateStore> stateStore,
        Mock<IOAuthProviderClient> providerClient,
        Mock<IOAuthOptionsProvider> optionsProvider,
        Mock<IAuthSessionIssuer> sessionIssuer,
        Mock<IPasswordHasher> passwordHasher,
        Mock<IDateTimeProvider> dateTimeProvider,
        Mock<IIntegrationEventCollector> eventCollector,
        ApplicationDbContext context)
    {
        return new CompleteOAuthLoginCommandHandler(
            stateStore.Object,
            providerClient.Object,
            optionsProvider.Object,
            context,
            context,
            sessionIssuer.Object,
            passwordHasher.Object,
            dateTimeProvider.Object,
            eventCollector.Object,
            NullLogger<CompleteOAuthLoginCommandHandler>.Instance);
    }

    [Fact]
    public async Task Handle_WhenErrorReturned_ShouldReturnFailure()
    {
        await using var context = _db.CreateContext();
        var mocks = CreateMocks();
        var handler = CreateHandler(mocks.StateStore, mocks.ProviderClient, mocks.OptionsProvider,
            mocks.SessionIssuer, mocks.PasswordHasher, mocks.DateTimeProvider, mocks.EventCollector, context);

        mocks.DateTimeProvider.Setup(x => x.UtcNow).Returns(() => DateTimeOffset.UtcNow);

        var result = await handler.Handle(new CompleteOAuthLoginCommand
        {
            Provider = OAuthProvider.Google,
            Code = string.Empty,
            State = string.Empty,
            Error = "access_denied",
            ErrorDescription = "User denied access"
        }, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("error"));
    }

    [Fact]
    public async Task Handle_WhenStateInvalid_ShouldReturnFailure()
    {
        await using var context = _db.CreateContext();
        var mocks = CreateMocks();
        mocks.StateStore.Setup(x => x.ConsumeAsync("bad-state", It.IsAny<CancellationToken>()))
            .ReturnsAsync((OAuthLoginState?)null);
        mocks.DateTimeProvider.Setup(x => x.UtcNow).Returns(() => DateTimeOffset.UtcNow);

        var handler = CreateHandler(mocks.StateStore, mocks.ProviderClient, mocks.OptionsProvider,
            mocks.SessionIssuer, mocks.PasswordHasher, mocks.DateTimeProvider, mocks.EventCollector, context);

        var result = await handler.Handle(new CompleteOAuthLoginCommand
        {
            Provider = OAuthProvider.Google,
            Code = "auth-code",
            State = "bad-state"
        }, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Invalid") || e.Contains("expired"));
    }

    [Fact]
    public async Task Handle_WhenProviderMismatch_ShouldReturnFailure()
    {
        await using var context = _db.CreateContext();
        var mocks = CreateMocks();
        var mismatchedState = ValidState with { Provider = OAuthProvider.Apple };
        mocks.StateStore.Setup(x => x.ConsumeAsync("test-state", It.IsAny<CancellationToken>()))
            .ReturnsAsync(mismatchedState);
        mocks.DateTimeProvider.Setup(x => x.UtcNow).Returns(() => DateTimeOffset.UtcNow);

        var handler = CreateHandler(mocks.StateStore, mocks.ProviderClient, mocks.OptionsProvider,
            mocks.SessionIssuer, mocks.PasswordHasher, mocks.DateTimeProvider, mocks.EventCollector, context);

        var result = await handler.Handle(new CompleteOAuthLoginCommand
        {
            Provider = OAuthProvider.Google,
            Code = "auth-code",
            State = "test-state"
        }, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("mismatch"));
    }

    [Fact]
    public async Task Handle_WhenCodeRedemptionFails_ShouldReturnFailure()
    {
        await using var context = _db.CreateContext();
        var mocks = CreateMocks();
        mocks.StateStore.Setup(x => x.ConsumeAsync("test-state", It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidState);
        mocks.ProviderClient.Setup(x => x.RedeemCodeAsync(
                OAuthProvider.Google, It.IsAny<OAuthCodeRedemptionRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Invalid token"));
        mocks.DateTimeProvider.Setup(x => x.UtcNow).Returns(() => DateTimeOffset.UtcNow);

        var handler = CreateHandler(mocks.StateStore, mocks.ProviderClient, mocks.OptionsProvider,
            mocks.SessionIssuer, mocks.PasswordHasher, mocks.DateTimeProvider, mocks.EventCollector, context);

        var result = await handler.Handle(new CompleteOAuthLoginCommand
        {
            Provider = OAuthProvider.Google,
            Code = "auth-code",
            State = "test-state"
        }, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WhenOAuthAccountExists_ShouldLoginExistingLinkedAccount()
    {
        await using var context = _db.CreateContext();
        var now = DateTimeOffset.UtcNow;

        var user = User.Create("oauth-linked@example.com", "OAuth User", "hashed", now);
        user.LinkOAuthAccount(OAuthProvider.Google, "google-sub-123", EmptyProfile, null, now);
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var mocks = CreateMocks();
        mocks.StateStore.Setup(x => x.ConsumeAsync("test-state", It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidState);
        mocks.ProviderClient.Setup(x => x.RedeemCodeAsync(
                OAuthProvider.Google, It.IsAny<OAuthCodeRedemptionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExternalOAuthProfile(OAuthProvider.Google, "google-sub-123",
                "oauth-linked@example.com", true, "OAuth User", null, EmptyProfile));
        mocks.SessionIssuer.Setup(x => x.IssueAsync(user, It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AuthResult
            {
                AccessToken = "access",
                RefreshToken = "refresh",
                ExpiresAt = DateTime.UtcNow.AddHours(1),
                User = new UserDto { Id = user.Id, Email = "oauth-linked@example.com", Name = "OAuth User" }
            });
        mocks.DateTimeProvider.Setup(x => x.UtcNow).Returns(() => now);

        var handler = CreateHandler(mocks.StateStore, mocks.ProviderClient, mocks.OptionsProvider,
            mocks.SessionIssuer, mocks.PasswordHasher, mocks.DateTimeProvider, mocks.EventCollector, context);

        var result = await handler.Handle(new CompleteOAuthLoginCommand
        {
            Provider = OAuthProvider.Google,
            Code = "auth-code",
            State = "test-state"
        }, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data!.User.Id.Should().Be(user.Id);
    }

    [Fact]
    public async Task Handle_WhenSuspendedUserHasOAuthAccount_ShouldReturnFailure()
    {
        await using var context = _db.CreateContext();
        var now = DateTimeOffset.UtcNow;

        var user = User.Create("suspended@example.com", "Suspended User", "hashed", now);
        user.LinkOAuthAccount(OAuthProvider.Google, "google-sub-suspended", EmptyProfile, null, now);
        user.Suspend(Guid.NewGuid(), now, "Testing");
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var mocks = CreateMocks();
        mocks.StateStore.Setup(x => x.ConsumeAsync("test-state", It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidState);
        mocks.ProviderClient.Setup(x => x.RedeemCodeAsync(
                OAuthProvider.Google, It.IsAny<OAuthCodeRedemptionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExternalOAuthProfile(OAuthProvider.Google, "google-sub-suspended",
                "suspended@example.com", true, "Suspended User", null, EmptyProfile));
        mocks.DateTimeProvider.Setup(x => x.UtcNow).Returns(() => now);

        var handler = CreateHandler(mocks.StateStore, mocks.ProviderClient, mocks.OptionsProvider,
            mocks.SessionIssuer, mocks.PasswordHasher, mocks.DateTimeProvider, mocks.EventCollector, context);

        var result = await handler.Handle(new CompleteOAuthLoginCommand
        {
            Provider = OAuthProvider.Google,
            Code = "auth-code",
            State = "test-state"
        }, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("not active"));
    }

    [Fact]
    public async Task Handle_WhenUserExistsWithVerifiedEmail_ShouldAutoLinkAndLogin()
    {
        await using var context = _db.CreateContext();
        var now = DateTimeOffset.UtcNow;

        var user = User.Create("existing@example.com", "Existing User", "hashed", now);
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var mocks = CreateMocks();
        mocks.StateStore.Setup(x => x.ConsumeAsync("test-state", It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidState);
        mocks.ProviderClient.Setup(x => x.RedeemCodeAsync(
                OAuthProvider.Google, It.IsAny<OAuthCodeRedemptionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExternalOAuthProfile(OAuthProvider.Google, "google-sub-new",
                "existing@example.com", true, "Existing User", null, EmptyProfile));
        mocks.SessionIssuer.Setup(x => x.IssueAsync(user, It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AuthResult
            {
                AccessToken = "access",
                RefreshToken = "refresh",
                ExpiresAt = DateTime.UtcNow.AddHours(1),
                User = new UserDto { Id = user.Id, Email = "existing@example.com", Name = "Existing User" }
            });
        mocks.DateTimeProvider.Setup(x => x.UtcNow).Returns(() => now);

        var handler = CreateHandler(mocks.StateStore, mocks.ProviderClient, mocks.OptionsProvider,
            mocks.SessionIssuer, mocks.PasswordHasher, mocks.DateTimeProvider, mocks.EventCollector, context);

        var result = await handler.Handle(new CompleteOAuthLoginCommand
        {
            Provider = OAuthProvider.Google,
            Code = "auth-code",
            State = "test-state"
        }, CancellationToken.None);
        await context.SaveChangesAsync();

        result.Succeeded.Should().BeTrue();

        var linked = await context.OAuthAccounts
            .FirstOrDefaultAsync(a => a.Provider == OAuthProvider.Google && a.ProviderId == "google-sub-new");
        linked.Should().NotBeNull();
        linked!.UserId.Should().Be(user.Id);
    }

    [Fact]
    public async Task Handle_WhenEmailNotVerified_ShouldReturnFailure()
    {
        await using var context = _db.CreateContext();
        var mocks = CreateMocks();
        mocks.StateStore.Setup(x => x.ConsumeAsync("test-state", It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidState);
        mocks.ProviderClient.Setup(x => x.RedeemCodeAsync(
                OAuthProvider.Google, It.IsAny<OAuthCodeRedemptionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExternalOAuthProfile(OAuthProvider.Google, "google-sub-unverified",
                "unverified@example.com", false, "Unverified", null, EmptyProfile));
        mocks.DateTimeProvider.Setup(x => x.UtcNow).Returns(() => DateTimeOffset.UtcNow);

        var handler = CreateHandler(mocks.StateStore, mocks.ProviderClient, mocks.OptionsProvider,
            mocks.SessionIssuer, mocks.PasswordHasher, mocks.DateTimeProvider, mocks.EventCollector, context);

        var result = await handler.Handle(new CompleteOAuthLoginCommand
        {
            Provider = OAuthProvider.Google,
            Code = "auth-code",
            State = "test-state"
        }, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("email not verified"));
    }

    [Fact]
    public async Task Handle_WhenNoExistingUser_ShouldCreateNewUserAndAccount()
    {
        await using var context = _db.CreateContext();
        var now = DateTimeOffset.UtcNow;

        var mocks = CreateMocks();
        mocks.StateStore.Setup(x => x.ConsumeAsync("test-state", It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidState);
        mocks.ProviderClient.Setup(x => x.RedeemCodeAsync(
                OAuthProvider.Google, It.IsAny<OAuthCodeRedemptionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExternalOAuthProfile(OAuthProvider.Google, "google-sub-newuser",
                "newuser@example.com", true, "New OAuth User", null, EmptyProfile));
        mocks.PasswordHasher.Setup(x => x.HashPassword(It.IsAny<string>())).Returns("sentinel-hash");
        mocks.SessionIssuer.Setup(x => x.IssueAsync(It.IsAny<User>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AuthResult
            {
                AccessToken = "access",
                RefreshToken = "refresh",
                ExpiresAt = DateTime.UtcNow.AddHours(1),
                User = new UserDto { Id = Guid.NewGuid(), Email = "newuser@example.com", Name = "New OAuth User" }
            });
        mocks.DateTimeProvider.Setup(x => x.UtcNow).Returns(() => now);

        var handler = CreateHandler(mocks.StateStore, mocks.ProviderClient, mocks.OptionsProvider,
            mocks.SessionIssuer, mocks.PasswordHasher, mocks.DateTimeProvider, mocks.EventCollector, context);

        var result = await handler.Handle(new CompleteOAuthLoginCommand
        {
            Provider = OAuthProvider.Google,
            Code = "auth-code",
            State = "test-state"
        }, CancellationToken.None);
        await context.SaveChangesAsync();

        result.Succeeded.Should().BeTrue();

        var user = await context.Users.FirstOrDefaultAsync(u => u.Email.Value == "newuser@example.com");
        user.Should().NotBeNull();
        user!.Name.Should().Be("New OAuth User");

        var account = await context.Accounts.FirstOrDefaultAsync(a => a.CreatedBy == user.Id);
        account.Should().NotBeNull();
        account!.Name.Should().Be("New OAuth User's Account");

        var member = await context.AccountMembers.FirstOrDefaultAsync(m => m.UserId == user.Id);
        member.Should().NotBeNull();

        var linked = await context.OAuthAccounts
            .FirstOrDefaultAsync(a => a.UserId == user.Id && a.Provider == OAuthProvider.Google);
        linked.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_GitHubProvider_WithNullCodeVerifier_ShouldCreateNewUser()
    {
        await using var context = _db.CreateContext();
        var now = DateTimeOffset.UtcNow;

        var gitHubState = new OAuthLoginState(
            "github-state", "github-nonce", "github-code-verifier",
            OAuthProvider.GitHub, null,
            DateTimeOffset.UtcNow.AddMinutes(10));

        var mocks = CreateMocks();
        mocks.OptionsProvider.Setup(x => x.GetRedirectUri(OAuthProvider.GitHub))
            .Returns("http://localhost:8000/api/v1/auth/oauth/github/callback");
        mocks.StateStore.Setup(x => x.ConsumeAsync("github-state", It.IsAny<CancellationToken>()))
            .ReturnsAsync(gitHubState);
        mocks.ProviderClient.Setup(x => x.RedeemCodeAsync(
                OAuthProvider.GitHub, It.IsAny<OAuthCodeRedemptionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExternalOAuthProfile(OAuthProvider.GitHub, "github-user-123",
                "github@example.com", true, "GitHub User", "https://avatars.githubusercontent.com/u/123", EmptyProfile));
        mocks.PasswordHasher.Setup(x => x.HashPassword(It.IsAny<string>())).Returns("sentinel-hash");
        mocks.SessionIssuer.Setup(x => x.IssueAsync(It.IsAny<User>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AuthResult
            {
                AccessToken = "access",
                RefreshToken = "refresh",
                ExpiresAt = DateTime.UtcNow.AddHours(1),
                User = new UserDto { Id = Guid.NewGuid(), Email = "github@example.com", Name = "GitHub User" }
            });
        mocks.DateTimeProvider.Setup(x => x.UtcNow).Returns(() => now);

        var handler = CreateHandler(mocks.StateStore, mocks.ProviderClient, mocks.OptionsProvider,
            mocks.SessionIssuer, mocks.PasswordHasher, mocks.DateTimeProvider, mocks.EventCollector, context);

        var result = await handler.Handle(new CompleteOAuthLoginCommand
        {
            Provider = OAuthProvider.GitHub,
            Code = "github-code",
            State = "github-state"
        }, CancellationToken.None);
        await context.SaveChangesAsync();

        result.Succeeded.Should().BeTrue();

        var user = await context.Users.FirstOrDefaultAsync(u => u.Email.Value == "github@example.com");
        user.Should().NotBeNull();
        user!.Name.Should().Be("GitHub User");

        var linked = await context.OAuthAccounts
            .FirstOrDefaultAsync(a => a.UserId == user.Id && a.Provider == OAuthProvider.GitHub);
        linked.Should().NotBeNull();
        linked!.ProviderId.Should().Be("github-user-123");
    }
}
