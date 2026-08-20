using Microsoft.Extensions.Logging.Abstractions;
using Notrelix.Application.Features.Identity.OAuth.Abstractions;
using Notrelix.Application.Features.Identity.OAuth.Commands.CompleteOAuthLink;
using Notrelix.Application.Features.Identity.OAuth.DTOs;
using Notrelix.Domain.Identity.OAuth;
using Notrelix.Domain.Identity.Users;
using Notrelix.Domain.SharedKernel;
using Notrelix.Infrastructure.Data;
using Notrelix.Integration.Tests.Containers;

namespace Notrelix.Integration.Tests.Auth;

[Collection("Database")]
public class CompleteOAuthLinkCommandHandlerTests : IAsyncLifetime
{
    private readonly PostgresTestContainer _db;
    private DatabaseReset _reset = null!;

    public CompleteOAuthLinkCommandHandlerTests(PostgresTestContainer db)
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

    private static OAuthLoginState LinkState(Guid userId) => new(
        "link-state", "link-nonce", "link-code-verifier",
        OAuthProvider.Google, null,
        DateTimeOffset.UtcNow.AddMinutes(10),
        OAuthFlowKind.Link, userId);

    private static OAuthProfileSnapshot EmptySnapshot(OAuthProvider provider) =>
        OAuthProfileSnapshot.Create(provider, 1, JsonValue.EmptyObject());

    private (Mock<IOAuthStateStore> StateStore, Mock<IOAuthProviderClient> ProviderClient,
            Mock<IOAuthOptionsProvider> OptionsProvider, Mock<IDateTimeProvider> DateTimeProvider)
        CreateMocks()
    {
        var stateStore = new Mock<IOAuthStateStore>();
        var providerClient = new Mock<IOAuthProviderClient>();
        var optionsProvider = new Mock<IOAuthOptionsProvider>();
        var dateTimeProvider = new Mock<IDateTimeProvider>();

        optionsProvider.Setup(x => x.GetRedirectUri(OAuthProvider.Google))
            .Returns("https://localhost/oauth/google/callback");

        return (stateStore, providerClient, optionsProvider, dateTimeProvider);
    }

    private CompleteOAuthLinkCommandHandler CreateHandler(
        Mock<IOAuthStateStore> stateStore,
        Mock<IOAuthProviderClient> providerClient,
        Mock<IOAuthOptionsProvider> optionsProvider,
        Mock<IDateTimeProvider> dateTimeProvider,
        ApplicationDbContext context,
        Guid currentUserId)
    {
        var currentUser = new Mock<ICurrentRequestContext>();
        currentUser.Setup(x => x.UserId).Returns(currentUserId);
        currentUser.Setup(x => x.IsAuthenticated).Returns(true);

        return new CompleteOAuthLinkCommandHandler(
            stateStore.Object,
            providerClient.Object,
            optionsProvider.Object,
            context,
            currentUser.Object,
            dateTimeProvider.Object,
            NullLogger<CompleteOAuthLinkCommandHandler>.Instance);
    }

    private static CompleteOAuthLinkCommand Command(string? error = null) => new()
    {
        Provider = OAuthProvider.Google,
        Code = "auth-code",
        State = "link-state",
        Error = error
    };

    [Fact]
    public async Task Handle_WhenErrorReturned_ShouldReturnFailure()
    {
        await using var context = _db.CreateContext();
        var mocks = CreateMocks();
        var handler = CreateHandler(mocks.StateStore, mocks.ProviderClient, mocks.OptionsProvider,
            mocks.DateTimeProvider, context, Guid.NewGuid());

        var result = await handler.Handle(Command(error: "access_denied"), CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("error"));
    }

    [Fact]
    public async Task Handle_WhenStateInvalid_ShouldReturnFailure()
    {
        await using var context = _db.CreateContext();
        var mocks = CreateMocks();
        mocks.StateStore.Setup(x => x.ConsumeAsync("link-state", It.IsAny<CancellationToken>()))
            .ReturnsAsync((OAuthLoginState?)null);
        var handler = CreateHandler(mocks.StateStore, mocks.ProviderClient, mocks.OptionsProvider,
            mocks.DateTimeProvider, context, Guid.NewGuid());

        var result = await handler.Handle(Command(), CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Invalid") || e.Contains("expired"));
    }

    [Fact]
    public async Task Handle_WhenStateBoundToLoginFlow_ShouldReturnFailure()
    {
        await using var context = _db.CreateContext();
        var currentUserId = Guid.NewGuid();
        var mocks = CreateMocks();
        var loginState = new OAuthLoginState(
            "link-state", "link-nonce", "link-code-verifier",
            OAuthProvider.Google, null,
            DateTimeOffset.UtcNow.AddMinutes(10));
        mocks.StateStore.Setup(x => x.ConsumeAsync("link-state", It.IsAny<CancellationToken>()))
            .ReturnsAsync(loginState);
        var handler = CreateHandler(mocks.StateStore, mocks.ProviderClient, mocks.OptionsProvider,
            mocks.DateTimeProvider, context, currentUserId);

        var result = await handler.Handle(Command(), CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("link flow"));
    }

    [Fact]
    public async Task Handle_WhenStateBoundToAnotherUser_ShouldReturnFailure()
    {
        await using var context = _db.CreateContext();
        var mocks = CreateMocks();
        mocks.StateStore.Setup(x => x.ConsumeAsync("link-state", It.IsAny<CancellationToken>()))
            .ReturnsAsync(LinkState(Guid.NewGuid()));
        var handler = CreateHandler(mocks.StateStore, mocks.ProviderClient, mocks.OptionsProvider,
            mocks.DateTimeProvider, context, Guid.NewGuid());

        var result = await handler.Handle(Command(), CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("another user"));
    }

    [Fact]
    public async Task Handle_WhenProviderMismatch_ShouldReturnFailure()
    {
        await using var context = _db.CreateContext();
        var currentUserId = Guid.NewGuid();
        var mocks = CreateMocks();
        var mismatchedState = LinkState(currentUserId) with { Provider = OAuthProvider.Apple };
        mocks.StateStore.Setup(x => x.ConsumeAsync("link-state", It.IsAny<CancellationToken>()))
            .ReturnsAsync(mismatchedState);
        var handler = CreateHandler(mocks.StateStore, mocks.ProviderClient, mocks.OptionsProvider,
            mocks.DateTimeProvider, context, currentUserId);

        var result = await handler.Handle(Command(), CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("mismatch"));
    }

    [Fact]
    public async Task Handle_WhenCodeRedemptionFails_ShouldReturnFailure()
    {
        await using var context = _db.CreateContext();
        var currentUserId = Guid.NewGuid();
        var mocks = CreateMocks();
        mocks.StateStore.Setup(x => x.ConsumeAsync("link-state", It.IsAny<CancellationToken>()))
            .ReturnsAsync(LinkState(currentUserId));
        mocks.ProviderClient.Setup(x => x.RedeemCodeAsync(
                OAuthProvider.Google, It.IsAny<OAuthCodeRedemptionRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Invalid token"));
        var handler = CreateHandler(mocks.StateStore, mocks.ProviderClient, mocks.OptionsProvider,
            mocks.DateTimeProvider, context, currentUserId);

        var result = await handler.Handle(Command(), CancellationToken.None);

        result.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ShouldReturnFailure()
    {
        await using var context = _db.CreateContext();
        var currentUserId = Guid.NewGuid();
        var mocks = CreateMocks();
        mocks.StateStore.Setup(x => x.ConsumeAsync("link-state", It.IsAny<CancellationToken>()))
            .ReturnsAsync(LinkState(currentUserId));
        mocks.ProviderClient.Setup(x => x.RedeemCodeAsync(
                OAuthProvider.Google, It.IsAny<OAuthCodeRedemptionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExternalOAuthProfile(OAuthProvider.Google, "google-sub-1",
                "linked@example.com", true, "Linked User", null, EmptyProfile));
        mocks.DateTimeProvider.Setup(x => x.UtcNow).Returns(() => DateTimeOffset.UtcNow);
        var handler = CreateHandler(mocks.StateStore, mocks.ProviderClient, mocks.OptionsProvider,
            mocks.DateTimeProvider, context, currentUserId);

        var result = await handler.Handle(Command(), CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("User not found"));
    }

    [Fact]
    public async Task Handle_WhenSuspendedUser_ShouldReturnFailure()
    {
        await using var context = _db.CreateContext();
        var now = DateTimeOffset.UtcNow;
        var user = User.Create("suspended-link@example.com", "Suspended", "hashed", now, hasPasswordCredential: true);
        user.Suspend(Guid.NewGuid(), now, "Testing");
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var mocks = CreateMocks();
        mocks.StateStore.Setup(x => x.ConsumeAsync("link-state", It.IsAny<CancellationToken>()))
            .ReturnsAsync(LinkState(user.Id));
        mocks.ProviderClient.Setup(x => x.RedeemCodeAsync(
                OAuthProvider.Google, It.IsAny<OAuthCodeRedemptionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExternalOAuthProfile(OAuthProvider.Google, "google-sub-suspended",
                "suspended-link@example.com", true, "Suspended", null, EmptyProfile));
        mocks.DateTimeProvider.Setup(x => x.UtcNow).Returns(() => now);
        var handler = CreateHandler(mocks.StateStore, mocks.ProviderClient, mocks.OptionsProvider,
            mocks.DateTimeProvider, context, user.Id);

        var result = await handler.Handle(Command(), CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("not active"));
    }

    [Fact]
    public async Task Handle_WhenProviderSubjectFree_ShouldLinkToCurrentUser()
    {
        await using var context = _db.CreateContext();
        var now = DateTimeOffset.UtcNow;
        var user = User.Create("link-target@example.com", "Link Target", "hashed", now, hasPasswordCredential: true);
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var mocks = CreateMocks();
        mocks.StateStore.Setup(x => x.ConsumeAsync("link-state", It.IsAny<CancellationToken>()))
            .ReturnsAsync(LinkState(user.Id));
        mocks.ProviderClient.Setup(x => x.RedeemCodeAsync(
                OAuthProvider.Google, It.IsAny<OAuthCodeRedemptionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExternalOAuthProfile(OAuthProvider.Google, "google-sub-free",
                "link-target@example.com", true, "Link Target", null, EmptyProfile));
        mocks.DateTimeProvider.Setup(x => x.UtcNow).Returns(() => now);
        var handler = CreateHandler(mocks.StateStore, mocks.ProviderClient, mocks.OptionsProvider,
            mocks.DateTimeProvider, context, user.Id);

        var result = await handler.Handle(Command(), CancellationToken.None);
        await context.SaveChangesAsync();

        result.Succeeded.Should().BeTrue();
        result.Data!.AlreadyLinked.Should().BeFalse();
        result.Data.Provider.Should().Be(OAuthProvider.Google);
        result.Data.ProviderId.Should().Be("google-sub-free");

        var linked = await context.OAuthAccounts
            .FirstOrDefaultAsync(a => a.UserId == user.Id && a.Provider == OAuthProvider.Google);
        linked.Should().NotBeNull();
        linked!.ProviderId.Should().Be("google-sub-free");
    }

    [Fact]
    public async Task Handle_WhenProviderAlreadyLinkedToSelf_ShouldReturnAlreadyLinkedNoOp()
    {
        await using var context = _db.CreateContext();
        var now = DateTimeOffset.UtcNow;
        var user = User.Create("self-linked@example.com", "Self Linked", "hashed", now, hasPasswordCredential: true);
        user.LinkOAuthAccount(OAuthProvider.Google, "google-sub-self",
            EmptySnapshot(OAuthProvider.Google), null, user.Id, now);
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var mocks = CreateMocks();
        mocks.StateStore.Setup(x => x.ConsumeAsync("link-state", It.IsAny<CancellationToken>()))
            .ReturnsAsync(LinkState(user.Id));
        mocks.ProviderClient.Setup(x => x.RedeemCodeAsync(
                OAuthProvider.Google, It.IsAny<OAuthCodeRedemptionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExternalOAuthProfile(OAuthProvider.Google, "google-sub-self",
                "self-linked@example.com", true, "Self Linked", null, EmptyProfile));
        mocks.DateTimeProvider.Setup(x => x.UtcNow).Returns(() => now);
        var handler = CreateHandler(mocks.StateStore, mocks.ProviderClient, mocks.OptionsProvider,
            mocks.DateTimeProvider, context, user.Id);

        var result = await handler.Handle(Command(), CancellationToken.None);
        await context.SaveChangesAsync();

        result.Succeeded.Should().BeTrue();
        result.Data!.AlreadyLinked.Should().BeTrue();

        var accounts = await context.OAuthAccounts
            .Where(a => a.UserId == user.Id && a.Provider == OAuthProvider.Google).ToListAsync();
        accounts.Should().ContainSingle();
    }

    [Fact]
    public async Task Handle_WhenProviderSubjectLinkedToAnotherUser_ShouldReturnConflict()
    {
        await using var context = _db.CreateContext();
        var now = DateTimeOffset.UtcNow;
        var otherUser = User.Create("other@example.com", "Other User", "hashed", now, hasPasswordCredential: true);
        otherUser.LinkOAuthAccount(OAuthProvider.Google, "google-sub-taken",
            EmptySnapshot(OAuthProvider.Google), null, otherUser.Id, now);
        context.Users.Add(otherUser);
        var currentUser = User.Create("current@example.com", "Current User", "hashed", now, hasPasswordCredential: true);
        context.Users.Add(currentUser);
        await context.SaveChangesAsync();

        var mocks = CreateMocks();
        mocks.StateStore.Setup(x => x.ConsumeAsync("link-state", It.IsAny<CancellationToken>()))
            .ReturnsAsync(LinkState(currentUser.Id));
        mocks.ProviderClient.Setup(x => x.RedeemCodeAsync(
                OAuthProvider.Google, It.IsAny<OAuthCodeRedemptionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExternalOAuthProfile(OAuthProvider.Google, "google-sub-taken",
                "other@example.com", true, "Other User", null, EmptyProfile));
        mocks.DateTimeProvider.Setup(x => x.UtcNow).Returns(() => now);
        var handler = CreateHandler(mocks.StateStore, mocks.ProviderClient, mocks.OptionsProvider,
            mocks.DateTimeProvider, context, currentUser.Id);

        var result = await handler.Handle(Command(), CancellationToken.None);
        await context.SaveChangesAsync();

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("another account"));

        var currentLinked = await context.OAuthAccounts
            .FirstOrDefaultAsync(a => a.UserId == currentUser.Id && a.Provider == OAuthProvider.Google);
        currentLinked.Should().BeNull();
    }
}
