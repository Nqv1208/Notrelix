using Notrelix.Application.Features.Accounts.Public.PersonalAccountProvisioning;
using Notrelix.Application.Features.Identity.OAuth;
using Notrelix.Application.Features.Identity.OAuth.Abstractions;
using Notrelix.Application.Features.Identity.OAuth.Commands.CompleteOAuthLogin;
using Notrelix.Application.Features.Identity.OAuth.DTOs;
using Notrelix.Domain.Identity.OAuth;

namespace Notrelix.Application.Tests.Features.Identity.OAuth;

/// <summary>The provider's error text comes from the query string; it must not forge log lines.</summary>
public class CompleteOAuthLoginErrorLogTests : IdentityHandlerTestBase
{
    [Fact]
    public async Task ACallbackError_IsLoggedWithoutControlCharactersAndCutShort()
    {
        var state = new Mock<IOAuthStateStore>();
        state.Setup(s => s.ConsumeAsync("state", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OAuthLoginState("state", "nonce", "verifier", OAuthProvider.Google, null, TestNow.AddMinutes(10)));
        var logger = new CapturingLogger<CompleteOAuthLoginCommandHandler>();
        var sut = new CompleteOAuthLoginCommandHandler(
            state.Object, new Mock<IOAuthProviderClient>().Object, new Mock<IOAuthOptionsProvider>().Object,
            IdentityContextMock.Object, new Mock<IAccountProvisioningActions>().Object, SessionIssuerMock.Object,
            ChallengeStoreMock.Object, PasswordHasherMock.Object, DateTimeProviderMock.Object,
            IntegrationEventCollectorMock.Object, logger);

        var result = await sut.Handle(new CompleteOAuthLoginCommand
        {
            Provider = OAuthProvider.Google,
            Code = "code",
            State = "state",
            Error = "access_denied",
            ErrorDescription = "x\r\n2026-01-01 FAKE Critical: admin signed in" + new string('y', 500),
        }, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        var logged = logger.Messages.Should().ContainSingle(m => m.Contains("OAuth callback error")).Subject;
        logged.Should().NotContain("\r").And.NotContain("\n");
        logged.Length.Should().BeLessThan(300);
    }

    [Theory]
    [InlineData("plain", "plain")]
    [InlineData("a\r\nb\tc\u0000d", "a  b c d")]
    [InlineData(null, "")]
    public void Sanitize_StripsControlCharacters(string? input, string expected) =>
        OAuthCallbackLog.Sanitize(input).Should().Be(expected);

    [Fact]
    public void Sanitize_CapsTheLength() =>
        OAuthCallbackLog.Sanitize(new string('z', 1000)).Should().HaveLength(100);

    private sealed class CapturingLogger<T> : ILogger<T>
    {
        public List<string> Messages { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            Messages.Add(formatter(state, exception));
    }
}
