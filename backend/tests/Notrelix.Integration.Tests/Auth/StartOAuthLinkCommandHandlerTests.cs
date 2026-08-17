using Notrelix.Application.Features.Identity.Abstractions;
using Notrelix.Application.Features.Identity.OAuth.Abstractions;
using Notrelix.Application.Features.Identity.OAuth.Commands.StartOAuthLink;
using Notrelix.Application.Features.Identity.OAuth.DTOs;
using Notrelix.Application.Features.Identity.Mfa.Abstractions;
using Notrelix.Application.Features.Identity.Security.Abstractions;
using Notrelix.Application.Features.Identity.Security.DTOs;
using Notrelix.Application.Features.Identity.Security.Services;
using Notrelix.Domain.Identity.OAuth;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Integration.Tests.Auth;

public class StartOAuthLinkCommandHandlerTests
{
    private static readonly Guid CurrentUserId = Guid.NewGuid();
    private static readonly Guid CurrentSessionId = Guid.CreateVersion7();

    private static Mock<ICurrentRequestContext> CreateCurrentUser()
    {
        var currentUser = new Mock<ICurrentRequestContext>();
        currentUser.Setup(x => x.UserId).Returns(CurrentUserId);
        currentUser.Setup(x => x.SessionId).Returns(CurrentSessionId);
        currentUser.Setup(x => x.IsAuthenticated).Returns(true);
        return currentUser;
    }

    private static ISecurityStepUpService CreateStepUp(DateTimeOffset now)
    {
        var clock = FakeDateTimeProvider.WithFixedTime(now);
        return new SecurityStepUpService(
            new Mock<IIdentityDbContext>().Object,
            new InMemoryMfaChallengeStore(clock),
            new InMemoryStepUpProofStore(clock),
            new InMemoryRateLimitService(),
            new Mock<IMfaCodeVerifier>().Object,
            new Mock<IPasswordHasher>().Object,
            clock);
    }

    private static async Task<string> IssueStepUpProof(ISecurityStepUpService stepUp)
    {
        var proof = await stepUp.GrantOAuthProofAsync(
            CurrentUserId, CurrentSessionId, StepUpPurpose.LinkOAuth, CancellationToken.None);
        proof.Succeeded.Should().BeTrue();
        return proof.Data!.ProofToken;
    }

    [Fact]
    public async Task Handle_WhenProviderDisabled_ShouldReturnFailure()
    {
        var optionsProvider = new Mock<IOAuthOptionsProvider>();
        optionsProvider.Setup(x => x.IsProviderEnabled(OAuthProvider.Google)).Returns(false);
        var providerClient = new Mock<IOAuthProviderClient>();
        var stateStore = new Mock<IOAuthStateStore>();
        var dateTimeProvider = new Mock<IDateTimeProvider>();
        var currentUser = CreateCurrentUser();
        var stepUp = CreateStepUp(new DateTimeOffset(2026, 7, 7, 12, 0, 0, TimeSpan.Zero));

        var handler = new StartOAuthLinkCommandHandler(
            optionsProvider.Object, providerClient.Object, stateStore.Object,
            currentUser.Object, stepUp, dateTimeProvider.Object);

        var result = await handler.Handle(new StartOAuthLinkCommand
        {
            Provider = OAuthProvider.Google,
            StepUpToken = string.Empty,
            ReturnUrl = null
        }, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("not enabled"));
    }

    [Fact]
    public async Task Handle_WhenValid_ShouldReturnSuccessWithAuthorizationUrl()
    {
        var optionsProvider = new Mock<IOAuthOptionsProvider>();
        optionsProvider.Setup(x => x.IsProviderEnabled(OAuthProvider.Google)).Returns(true);
        optionsProvider.Setup(x => x.GetRedirectUri(OAuthProvider.Google)).Returns("https://localhost/callback");
        var providerClient = new Mock<IOAuthProviderClient>();
        providerClient.Setup(x => x.BuildAuthorizationUrlAsync(
                OAuthProvider.Google, It.IsAny<OAuthAuthorizationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OAuthAuthorizationUrlResult("https://accounts.google.com/o/oauth2/auth?state=xyz"));
        var stateStore = new Mock<IOAuthStateStore>();
        stateStore.Setup(x => x.StoreAsync(It.IsAny<OAuthLoginState>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var dateTimeProvider = new Mock<IDateTimeProvider>();
        dateTimeProvider.Setup(x => x.UtcNow).Returns(new DateTimeOffset(2026, 7, 7, 12, 0, 0, TimeSpan.Zero));
        var currentUser = CreateCurrentUser();
        var stepUp = CreateStepUp(new DateTimeOffset(2026, 7, 7, 12, 0, 0, TimeSpan.Zero));
        var stepUpToken = await IssueStepUpProof(stepUp);

        var handler = new StartOAuthLinkCommandHandler(
            optionsProvider.Object, providerClient.Object, stateStore.Object,
            currentUser.Object, stepUp, dateTimeProvider.Object);

        var result = await handler.Handle(new StartOAuthLinkCommand
        {
            Provider = OAuthProvider.Google,
            StepUpToken = stepUpToken,
            ReturnUrl = null
        }, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data!.AuthorizationUrl.Should().Be("https://accounts.google.com/o/oauth2/auth?state=xyz");
    }

    [Fact]
    public async Task Handle_WhenValid_ShouldStoreStateBoundToCurrentUser()
    {
        var optionsProvider = new Mock<IOAuthOptionsProvider>();
        optionsProvider.Setup(x => x.IsProviderEnabled(OAuthProvider.Google)).Returns(true);
        optionsProvider.Setup(x => x.GetRedirectUri(OAuthProvider.Google)).Returns("https://localhost/callback");
        var providerClient = new Mock<IOAuthProviderClient>();
        providerClient.Setup(x => x.BuildAuthorizationUrlAsync(
                OAuthProvider.Google, It.IsAny<OAuthAuthorizationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OAuthAuthorizationUrlResult("https://accounts.google.com/oauth2/auth"));
        var stateStore = new Mock<IOAuthStateStore>();
        OAuthLoginState? storedState = null;
        stateStore.Setup(x => x.StoreAsync(It.IsAny<OAuthLoginState>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .Callback<OAuthLoginState, TimeSpan, CancellationToken>((s, _, _) => storedState = s)
            .Returns(Task.CompletedTask);
        var now = new DateTimeOffset(2026, 7, 7, 12, 0, 0, TimeSpan.Zero);
        var dateTimeProvider = new Mock<IDateTimeProvider>();
        dateTimeProvider.Setup(x => x.UtcNow).Returns(now);
        var currentUser = CreateCurrentUser();
        var stepUp = CreateStepUp(now);
        var stepUpToken = await IssueStepUpProof(stepUp);

        var handler = new StartOAuthLinkCommandHandler(
            optionsProvider.Object, providerClient.Object, stateStore.Object,
            currentUser.Object, stepUp, dateTimeProvider.Object);

        var result = await handler.Handle(new StartOAuthLinkCommand
        {
            Provider = OAuthProvider.Google,
            StepUpToken = stepUpToken,
            ReturnUrl = "/settings/security"
        }, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        storedState.Should().NotBeNull();
        storedState!.Flow.Should().Be(OAuthFlowKind.Link);
        storedState.BoundUserId.Should().Be(CurrentUserId);
        storedState.Provider.Should().Be(OAuthProvider.Google);
        storedState.ExpiresAt.Should().Be(now.AddMinutes(10));
        storedState.ReturnUrl.Should().Be("/settings/security");
        storedState.State.Should().NotBeNullOrWhiteSpace();
        storedState.Nonce.Should().NotBeNullOrWhiteSpace();
        storedState.CodeVerifier.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Handle_GitHubProvider_ShouldNotGeneratePKCE()
    {
        var optionsProvider = new Mock<IOAuthOptionsProvider>();
        optionsProvider.Setup(x => x.IsProviderEnabled(OAuthProvider.GitHub)).Returns(true);
        optionsProvider.Setup(x => x.GetRedirectUri(OAuthProvider.GitHub)).Returns("https://localhost/github/callback");
        var providerClient = new Mock<IOAuthProviderClient>();
        providerClient.Setup(x => x.BuildAuthorizationUrlAsync(
                OAuthProvider.GitHub, It.IsAny<OAuthAuthorizationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OAuthAuthorizationUrlResult("https://github.com/login/oauth/authorize"));
        var stateStore = new Mock<IOAuthStateStore>();
        OAuthLoginState? storedState = null;
        stateStore.Setup(x => x.StoreAsync(It.IsAny<OAuthLoginState>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .Callback<OAuthLoginState, TimeSpan, CancellationToken>((s, _, _) => storedState = s)
            .Returns(Task.CompletedTask);
        var dateTimeProvider = new Mock<IDateTimeProvider>();
        dateTimeProvider.Setup(x => x.UtcNow).Returns(new DateTimeOffset(2026, 7, 7, 12, 0, 0, TimeSpan.Zero));
        var currentUser = CreateCurrentUser();
        var stepUp = CreateStepUp(new DateTimeOffset(2026, 7, 7, 12, 0, 0, TimeSpan.Zero));
        var stepUpToken = await IssueStepUpProof(stepUp);

        var handler = new StartOAuthLinkCommandHandler(
            optionsProvider.Object, providerClient.Object, stateStore.Object,
            currentUser.Object, stepUp, dateTimeProvider.Object);

        var result = await handler.Handle(new StartOAuthLinkCommand
        {
            Provider = OAuthProvider.GitHub,
            StepUpToken = stepUpToken,
            ReturnUrl = null
        }, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        storedState.Should().NotBeNull();
        storedState!.Provider.Should().Be(OAuthProvider.GitHub);
        storedState.CodeVerifier.Should().BeNull();
    }
}
