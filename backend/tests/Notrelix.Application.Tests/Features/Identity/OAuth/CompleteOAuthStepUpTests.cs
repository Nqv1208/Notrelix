using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Identity.OAuth.Abstractions;
using Notrelix.Application.Features.Identity.OAuth.Commands.CompleteOAuthStepUp;
using Notrelix.Application.Features.Identity.OAuth.DTOs;
using Notrelix.Application.Features.Identity.Mfa.DTOs;
using Notrelix.Application.Features.Identity.Security.DTOs;
using Notrelix.Domain.Identity.OAuth;

namespace Notrelix.Application.Tests.Features.Identity.OAuth;

/// <summary>A fresh login with the person's own linked provider buys a proof bound to the user, session and purpose.</summary>
public class CompleteOAuthStepUpTests : IdentityHandlerTestBase
{
    private const string Subject = "linked-subject";
    private readonly Mock<IOAuthStateStore> _states = new();
    private readonly Mock<IOAuthProviderClient> _provider = new();
    private readonly Mock<IOAuthOptionsProvider> _options = new();

    public CompleteOAuthStepUpTests()
    {
        SetupUsers(CreateUser());
        _options.Setup(o => o.GetRedirectUri(It.IsAny<OAuthProvider>())).Returns("https://api.test/callback");
        StepUpServiceMock
            .Setup(s => s.GrantOAuthProofAsync(TestUserId, TestSessionId, It.IsAny<StepUpPurpose>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => Result<StepUpProofResult>.Success(new StepUpProofResult(Guid.NewGuid().ToString("N"), TestNow.AddMinutes(5))));
        LinkProvider(Subject);
        ProviderAnswers(Subject);
    }

    private CompleteOAuthStepUpCommandHandler CreateSut() => new(
        _states.Object, _provider.Object, _options.Object, IdentityContextMock.Object,
        StepUpServiceMock.Object, DateTimeProviderMock.Object, new Mock<ILogger<CompleteOAuthStepUpCommandHandler>>().Object);

    private void LinkProvider(string subject, Guid? userId = null) =>
        IdentityContextMock.Setup(c => c.OAuthAccounts).Returns(CreateAsyncDbSet(new List<OAuthAccount>
        {
            OAuthAccount.Create(userId ?? TestUserId, OAuthProvider.Google, subject,
                OAuthProfileSnapshot.Create(OAuthProvider.Google, 1, JsonValue.EmptyObject())),
        }));

    private void ProviderAnswers(string subject) =>
        _provider.Setup(p => p.RedeemCodeAsync(OAuthProvider.Google, It.IsAny<OAuthCodeRedemptionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExternalOAuthProfile(OAuthProvider.Google, subject, null, false, null, null, JsonValue.EmptyObject()));

    private void State(OAuthFlowKind flow = OAuthFlowKind.StepUp, StepUpPurpose purpose = StepUpPurpose.RevokeOtherSessions)
    {
        var challenge = purpose == StepUpPurpose.EnrollMfa
            ? MfaChallengePurpose.StepUpEnrollMfa
            : MfaChallengePurpose.StepUpRevokeOtherSessions;
        _states.Setup(s => s.ConsumeAsync("s", It.IsAny<CancellationToken>())).ReturnsAsync(new OAuthLoginState(
            "s", "n", "v", OAuthProvider.Google, null, TestNow.AddMinutes(10),
            flow, TestUserId, TestSessionId, challenge));
    }

    private static CompleteOAuthStepUpCommand Callback(string state = "s") =>
        new() { Provider = OAuthProvider.Google, Code = "c", State = state };

    [Fact]
    public async Task TheLinkedSubject_GrantsOneProof_BoundToTheStatesUserSessionAndPurpose()
    {
        State(purpose: StepUpPurpose.RevokeOtherSessions);

        var result = await CreateSut().Handle(Callback(), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data!.Proof.ProofToken.Should().NotBeNullOrEmpty();
        result.Data.SpareProof.Should().BeNull();
        StepUpServiceMock.Verify(s => s.GrantOAuthProofAsync(
            TestUserId, TestSessionId, StepUpPurpose.RevokeOtherSessions, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ForEnrollment_OneReauthenticationBuysTwoDifferentProofs()
    {
        State(purpose: StepUpPurpose.EnrollMfa);

        var result = await CreateSut().Handle(Callback(), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data!.SpareProof.Should().NotBeNull();
        result.Data.SpareProof!.ProofToken.Should().NotBe(result.Data.Proof.ProofToken);
        StepUpServiceMock.Verify(s => s.GrantOAuthProofAsync(
            TestUserId, TestSessionId, StepUpPurpose.EnrollMfa, It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task ADifferentProviderSubject_GrantsNothing()
    {
        State();
        ProviderAnswers("someone-else");

        var result = await CreateSut().Handle(Callback(), CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        StepUpServiceMock.Verify(s => s.GrantOAuthProofAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<StepUpPurpose>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ASubjectLinkedToAnotherPerson_GrantsNothing()
    {
        State();
        LinkProvider(Subject, userId: Guid.CreateVersion7());

        var result = await CreateSut().Handle(Callback(), CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        StepUpServiceMock.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AnUnknownSpentOrOtherFlowState_GrantsNothing(bool otherFlow)
    {
        if (otherFlow) State(flow: OAuthFlowKind.Login);

        var result = await CreateSut().Handle(Callback(), CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        _provider.Verify(p => p.RedeemCodeAsync(It.IsAny<OAuthProvider>(), It.IsAny<OAuthCodeRedemptionRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        StepUpServiceMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task AProviderError_GrantsNothing()
    {
        var result = await CreateSut().Handle(
            new CompleteOAuthStepUpCommand { Provider = OAuthProvider.Google, Code = "", State = "s", Error = "access_denied" },
            CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        StepUpServiceMock.VerifyNoOtherCalls();
    }
}
