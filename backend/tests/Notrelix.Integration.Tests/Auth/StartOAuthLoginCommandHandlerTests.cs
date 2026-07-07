using Notrelix.Application.Features.Identity.OAuth.Abstractions;
using Notrelix.Application.Features.Identity.OAuth.Commands.StartOAuthLogin;
using Notrelix.Application.Features.Identity.OAuth.DTOs;
using Notrelix.Domain.Identity.OAuth;

namespace Notrelix.Integration.Tests.Auth;

public class StartOAuthLoginCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenProviderDisabled_ShouldReturnFailure()
    {
        var optionsProvider = new Mock<IOAuthOptionsProvider>();
        optionsProvider.Setup(x => x.IsProviderEnabled(OAuthProvider.Google)).Returns(false);
        var providerClient = new Mock<IOAuthProviderClient>();
        var stateStore = new Mock<IOAuthStateStore>();
        var dateTimeProvider = new Mock<IDateTimeProvider>();

        var handler = new StartOAuthLoginCommandHandler(
            optionsProvider.Object, providerClient.Object, stateStore.Object, dateTimeProvider.Object);

        var result = await handler.Handle(new StartOAuthLoginCommand
        {
            Provider = OAuthProvider.Google,
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

        var handler = new StartOAuthLoginCommandHandler(
            optionsProvider.Object, providerClient.Object, stateStore.Object, dateTimeProvider.Object);

        var result = await handler.Handle(new StartOAuthLoginCommand
        {
            Provider = OAuthProvider.Google,
            ReturnUrl = null
        }, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data!.AuthorizationUrl.Should().Be("https://accounts.google.com/o/oauth2/auth?state=xyz");
    }

    [Fact]
    public async Task Handle_WhenValid_ShouldStoreStateWithExpiration()
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
        TimeSpan storedTtl = default;
        stateStore.Setup(x => x.StoreAsync(It.IsAny<OAuthLoginState>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .Callback<OAuthLoginState, TimeSpan, CancellationToken>((s, ttl, _) =>
            {
                storedState = s;
                storedTtl = ttl;
            })
            .Returns(Task.CompletedTask);
        var now = new DateTimeOffset(2026, 7, 7, 12, 0, 0, TimeSpan.Zero);
        var dateTimeProvider = new Mock<IDateTimeProvider>();
        dateTimeProvider.Setup(x => x.UtcNow).Returns(now);

        var handler = new StartOAuthLoginCommandHandler(
            optionsProvider.Object, providerClient.Object, stateStore.Object, dateTimeProvider.Object);

        await handler.Handle(new StartOAuthLoginCommand
        {
            Provider = OAuthProvider.Google,
            ReturnUrl = null
        }, CancellationToken.None);

        storedState.Should().NotBeNull();
        storedState!.Provider.Should().Be(OAuthProvider.Google);
        storedState.ExpiresAt.Should().Be(now.AddMinutes(10));
        storedTtl.Should().Be(TimeSpan.FromMinutes(10));
        storedState.State.Should().NotBeNullOrWhiteSpace();
        storedState.Nonce.Should().NotBeNullOrWhiteSpace();
        storedState.CodeVerifier.Should().NotBeNullOrWhiteSpace();
        storedState.ReturnUrl.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WhenReturnUrlSpecified_ShouldPreserveReturnUrl()
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
        var dateTimeProvider = new Mock<IDateTimeProvider>();
        dateTimeProvider.Setup(x => x.UtcNow).Returns(new DateTimeOffset(2026, 7, 7, 12, 0, 0, TimeSpan.Zero));

        var handler = new StartOAuthLoginCommandHandler(
            optionsProvider.Object, providerClient.Object, stateStore.Object, dateTimeProvider.Object);

        await handler.Handle(new StartOAuthLoginCommand
        {
            Provider = OAuthProvider.Google,
            ReturnUrl = "/dashboard"
        }, CancellationToken.None);

        storedState!.ReturnUrl.Should().Be("/dashboard");
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
            .ReturnsAsync(new OAuthAuthorizationUrlResult("https://github.com/login/oauth/authorize?state=xyz"));
        var stateStore = new Mock<IOAuthStateStore>();
        OAuthLoginState? storedState = null;
        stateStore.Setup(x => x.StoreAsync(It.IsAny<OAuthLoginState>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .Callback<OAuthLoginState, TimeSpan, CancellationToken>((s, _, _) => storedState = s)
            .Returns(Task.CompletedTask);
        var dateTimeProvider = new Mock<IDateTimeProvider>();
        dateTimeProvider.Setup(x => x.UtcNow).Returns(new DateTimeOffset(2026, 7, 7, 12, 0, 0, TimeSpan.Zero));

        var handler = new StartOAuthLoginCommandHandler(
            optionsProvider.Object, providerClient.Object, stateStore.Object, dateTimeProvider.Object);

        var result = await handler.Handle(new StartOAuthLoginCommand
        {
            Provider = OAuthProvider.GitHub,
            ReturnUrl = null
        }, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        storedState.Should().NotBeNull();
        storedState!.Provider.Should().Be(OAuthProvider.GitHub);
        storedState.CodeVerifier.Should().BeNull();
        storedState.State.Should().NotBeNullOrWhiteSpace();
        storedState.Nonce.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Handle_GitHubProvider_ShouldPassNullCodeChallengeToProviderClient()
    {
        var optionsProvider = new Mock<IOAuthOptionsProvider>();
        optionsProvider.Setup(x => x.IsProviderEnabled(OAuthProvider.GitHub)).Returns(true);
        optionsProvider.Setup(x => x.GetRedirectUri(OAuthProvider.GitHub)).Returns("https://localhost/github/callback");
        var providerClient = new Mock<IOAuthProviderClient>();
        OAuthAuthorizationRequest? capturedRequest = null;
        providerClient.Setup(x => x.BuildAuthorizationUrlAsync(
                OAuthProvider.GitHub, It.IsAny<OAuthAuthorizationRequest>(), It.IsAny<CancellationToken>()))
            .Callback<OAuthProvider, OAuthAuthorizationRequest, CancellationToken>((_, req, _) => capturedRequest = req)
            .ReturnsAsync(new OAuthAuthorizationUrlResult("https://github.com/login/oauth/authorize"));
        var stateStore = new Mock<IOAuthStateStore>();
        stateStore.Setup(x => x.StoreAsync(It.IsAny<OAuthLoginState>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var dateTimeProvider = new Mock<IDateTimeProvider>();
        dateTimeProvider.Setup(x => x.UtcNow).Returns(new DateTimeOffset(2026, 7, 7, 12, 0, 0, TimeSpan.Zero));

        var handler = new StartOAuthLoginCommandHandler(
            optionsProvider.Object, providerClient.Object, stateStore.Object, dateTimeProvider.Object);

        await handler.Handle(new StartOAuthLoginCommand
        {
            Provider = OAuthProvider.GitHub,
            ReturnUrl = null
        }, CancellationToken.None);

        capturedRequest.Should().NotBeNull();
        capturedRequest!.CodeChallenge.Should().BeNull();
    }
}
