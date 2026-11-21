using Notrelix.Application.Features.Identity.OAuth.Abstractions;
using Notrelix.Application.Features.Identity.OAuth.Commands.StartOAuthStepUp;
using Notrelix.Application.Features.Identity.OAuth.DTOs;
using Notrelix.Application.Features.Identity.Security.DTOs;
using Notrelix.Domain.Identity.OAuth;

namespace Notrelix.Application.Tests.Features.Identity.OAuth;

/// <summary>The provider round trip starts bound to the person, the session, the purpose and what the proof is for.</summary>
public class StartOAuthStepUpTests : IdentityHandlerTestBase
{
    private readonly Mock<IOAuthStateStore> _states = new();
    private readonly Mock<IOAuthProviderClient> _provider = new();
    private readonly Mock<IOAuthOptionsProvider> _options = new();

    public StartOAuthStepUpTests()
    {
        RequestContextMock.Setup(c => c.SessionId).Returns(TestSessionId);
        _options.Setup(o => o.IsProviderEnabled(OAuthProvider.Google)).Returns(true);
        _options.Setup(o => o.GetRedirectUri(OAuthProvider.Google)).Returns("https://api.test/callback");
        _provider
            .Setup(p => p.BuildAuthorizationUrlAsync(OAuthProvider.Google, It.IsAny<OAuthAuthorizationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OAuthAuthorizationUrlResult("https://provider.test/authorize"));
        IdentityContextMock.Setup(c => c.OAuthAccounts).Returns(CreateAsyncDbSet(new List<OAuthAccount>
        {
            OAuthAccount.Create(TestUserId, OAuthProvider.Google, "subject",
                OAuthProfileSnapshot.Create(OAuthProvider.Google, 1, JsonValue.EmptyObject())),
        }));
    }

    private StartOAuthStepUpCommandHandler CreateSut() => new(
        _options.Object, _provider.Object, _states.Object, IdentityContextMock.Object,
        RequestContextMock.Object, DateTimeProviderMock.Object);

    [Fact]
    public async Task TheTargetOfTheProof_IsKeptWithTheStateTheCallbackConsumes()
    {
        var target = StepUpTarget.OwnershipTransfer(Guid.CreateVersion7(), Guid.CreateVersion7());

        var result = await CreateSut().Handle(new StartOAuthStepUpCommand
        {
            Provider = OAuthProvider.Google,
            Purpose = StepUpPurpose.TransferWorkspaceOwnership,
            ReturnUrl = "/workspaces/w/settings",
            Target = target,
        }, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        _states.Verify(s => s.StoreAsync(
            It.Is<OAuthLoginState>(st => st.StepUpTarget == target && st.BoundUserId == TestUserId && st.BoundSessionId == TestSessionId),
            It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task TheProviderIsAskedForAFreshSignIn()
    {
        await CreateSut().Handle(new StartOAuthStepUpCommand
        {
            Provider = OAuthProvider.Google,
            Purpose = StepUpPurpose.RevokeOtherSessions,
        }, CancellationToken.None);

        _provider.Verify(p => p.BuildAuthorizationUrlAsync(
            OAuthProvider.Google,
            It.Is<OAuthAuthorizationRequest>(r => r.FreshAuthentication),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
