using Notrelix.Application.Features.Accounts.Public.PersonalAccountProvisioning;
using Notrelix.Application.Features.Identity.OAuth;
using Notrelix.Application.Features.Identity.OAuth.Abstractions;
using Notrelix.Application.Features.Identity.OAuth.Commands.CompleteOAuthLogin;
using Notrelix.Application.Features.Identity.OAuth.Commands.StartOAuthLogin;
using Notrelix.Application.Features.Identity.OAuth.DTOs;
using Notrelix.Domain.Identity.OAuth;

namespace Notrelix.Application.Tests.Features.Identity.OAuth;

/// <summary>
/// A provider sign-in state only works for the browser that started it (login CSRF defence): the start keeps the
/// hash of a random value the browser holds in a cookie, and the callback must present that value.
/// </summary>
public class OAuthStateBindingTests : IdentityHandlerTestBase
{
    private const string Binding = "browser-binding";

    private readonly Mock<IOAuthStateStore> _state = new();
    private readonly Mock<IOAuthProviderClient> _provider = new();

    private CompleteOAuthLoginCommandHandler CreateSut() => new(
        _state.Object, _provider.Object, new Mock<IOAuthOptionsProvider>().Object,
        IdentityContextMock.Object, new Mock<IAccountProvisioningActions>().Object, SessionIssuerMock.Object,
        ChallengeStoreMock.Object, Mock.Of<Notrelix.Application.Features.Accounts.Public.Security.IAccountSecurityPolicyFacts>(),
        PasswordHasherMock.Object, DateTimeProviderMock.Object, IntegrationEventCollectorMock.Object,
        new Mock<ILogger<CompleteOAuthLoginCommandHandler>>().Object,
        new Notrelix.Testing.Application.Fakes.FakeAccountSsoFacts());

    private void StateIs(string? bindingHash) =>
        _state.Setup(s => s.ConsumeAsync("state", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OAuthLoginState(
                "state", "nonce", "verifier", OAuthProvider.Google, null, TestNow.AddMinutes(10), BindingHash: bindingHash));

    private static CompleteOAuthLoginCommand Callback(string? binding, string? error = "access_denied") => new()
    {
        Provider = OAuthProvider.Google,
        Code = "code",
        State = "state",
        Error = error,
        BrowserBinding = binding,
    };

    [Fact]
    public async Task ACallback_WithNoBindingCookie_IsRefusedAsAnInvalidState()
    {
        StateIs(StateBinding.Hash(Binding));

        var result = await CreateSut().Handle(Callback(binding: null), CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.TypedErrors.Should().ContainSingle().Which.Code.Should().Be(OAuthLoginErrorCodes.StateInvalid);
        _provider.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ACallback_WithTheBindingOfAnotherState_IsRefusedAsAnInvalidState()
    {
        StateIs(StateBinding.Hash(Binding));

        var result = await CreateSut().Handle(Callback("a-binding-from-another-start"), CancellationToken.None);

        result.TypedErrors.Should().ContainSingle().Which.Code.Should().Be(OAuthLoginErrorCodes.StateInvalid);
        _provider.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task AStateThatWasStartedWithoutABinding_IsRefusedEvenWithACookie()
    {
        StateIs(bindingHash: null);

        var result = await CreateSut().Handle(Callback(Binding), CancellationToken.None);

        result.TypedErrors.Should().ContainSingle().Which.Code.Should().Be(OAuthLoginErrorCodes.StateInvalid);
    }

    [Fact]
    public async Task AReplayedState_IsRefusedAsAnInvalidState()
    {
        // The store hands a state out once; the second callback finds nothing, whatever cookie it carries.
        _state.SetupSequence(s => s.ConsumeAsync("state", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OAuthLoginState(
                "state", "nonce", "verifier", OAuthProvider.Google, null, TestNow.AddMinutes(10), BindingHash: StateBinding.Hash(Binding)))
            .ReturnsAsync((OAuthLoginState?)null);
        var sut = CreateSut();

        var first = await sut.Handle(Callback(Binding), CancellationToken.None);
        var second = await sut.Handle(Callback(Binding), CancellationToken.None);

        first.TypedErrors.Should().ContainSingle().Which.Code.Should().Be(OAuthLoginErrorCodes.Denied);
        second.TypedErrors.Should().ContainSingle().Which.Code.Should().Be(OAuthLoginErrorCodes.StateInvalid);
    }

    [Fact]
    public async Task ACallback_WithTheMatchingBinding_GoesOnToTheProvidersAnswer()
    {
        StateIs(StateBinding.Hash(Binding));

        var result = await CreateSut().Handle(Callback(Binding), CancellationToken.None);

        result.TypedErrors.Should().ContainSingle().Which.Code.Should().Be(OAuthLoginErrorCodes.Denied);
    }

    [Fact]
    public async Task AProviderError_WithNoBindingCookie_DoesNotRevealWhichCheckFailed()
    {
        StateIs(StateBinding.Hash(Binding));

        var result = await CreateSut().Handle(Callback(binding: null, error: "access_denied"), CancellationToken.None);

        result.TypedErrors.Should().ContainSingle().Which.Code.Should().Be(OAuthLoginErrorCodes.StateInvalid);
    }

    [Fact]
    public async Task TheStart_KeepsOnlyTheHashOfTheBinding()
    {
        var options = new Mock<IOAuthOptionsProvider>();
        options.Setup(o => o.IsProviderEnabled(OAuthProvider.Google)).Returns(true);
        options.Setup(o => o.GetRedirectUri(OAuthProvider.Google)).Returns("https://localhost/callback");
        var client = new Mock<IOAuthProviderClient>();
        client.Setup(c => c.BuildAuthorizationUrlAsync(
                OAuthProvider.Google, It.IsAny<OAuthAuthorizationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OAuthAuthorizationUrlResult("https://accounts.google.com/o/oauth2/auth"));
        OAuthLoginState? stored = null;
        _state.Setup(s => s.StoreAsync(It.IsAny<OAuthLoginState>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .Callback<OAuthLoginState, TimeSpan, CancellationToken>((s, _, _) => stored = s)
            .Returns(Task.CompletedTask);
        DateTimeProviderMock.Setup(d => d.UtcNow).Returns(TestNow);
        var sut = new StartOAuthLoginCommandHandler(options.Object, client.Object, _state.Object, DateTimeProviderMock.Object);

        var result = await sut.Handle(
            new StartOAuthLoginCommand { Provider = OAuthProvider.Google, BrowserBinding = Binding }, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        stored!.BindingHash.Should().Be(StateBinding.Hash(Binding)).And.NotBe(Binding);
        stored.State.Should().NotBe(Binding);
    }

    [Theory]
    [InlineData(null, "hash")]
    [InlineData("", "hash")]
    [InlineData("value", null)]
    [InlineData("value", "")]
    public void Matches_NeverAcceptsAMissingBindingOrHash(string? presented, string? stored) =>
        StateBinding.Matches(presented, stored).Should().BeFalse();

    [Fact]
    public void Matches_AcceptsOnlyTheSameValue()
    {
        var value = StateBinding.NewValue();

        StateBinding.Matches(value, StateBinding.Hash(value)).Should().BeTrue();
        StateBinding.Matches(value + "x", StateBinding.Hash(value)).Should().BeFalse();
        StateBinding.NewValue().Should().NotBe(value);
    }
}
