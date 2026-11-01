using Notrelix.Application.Features.Identity.Security.DTOs;
using Notrelix.Application.Features.Identity.Security.Services;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Application.Tests.Features.Identity.Security;

/// <summary>A verified proof opens one action of one purpose in one session of one person, once.</summary>
public class SecurityStepUpServiceConsumeTests : IdentityHandlerTestBase
{
    private const string Token = "proof-token";
    private readonly InMemoryStepUpProofStore _proofs;

    public SecurityStepUpServiceConsumeTests()
    {
        _proofs = new InMemoryStepUpProofStore(DateTimeProviderMock.Object);
    }

    private SecurityStepUpService CreateSut() => new(
        IdentityContextMock.Object,
        ChallengeStoreMock.Object,
        _proofs,
        new Mock<Notrelix.Application.Features.Identity.Mfa.Abstractions.IMfaCodeVerifier>().Object,
        PasswordHasherMock.Object,
        DateTimeProviderMock.Object,
        ThrottleMock.Object);

    private Task Issue(Guid? userId = null, Guid? sessionId = null, StepUpPurpose purpose = StepUpPurpose.EnrollMfa, TimeSpan? ttl = null) =>
        _proofs.StoreAsync(
            Token,
            new StepUpProofPayload(userId ?? TestUserId, sessionId ?? TestSessionId, purpose, TestNow, TestNow.Add(ttl ?? TimeSpan.FromMinutes(5))),
            TimeSpan.FromMinutes(5));

    [Fact]
    public async Task AMatchingProof_IsAccepted()
    {
        await Issue();

        var result = await CreateSut().ConsumeAsync(Token, TestUserId, TestSessionId, StepUpPurpose.EnrollMfa, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task AProofBoundToAnotherSession_IsRefused()
    {
        await Issue(sessionId: Guid.NewGuid());

        var result = await CreateSut().ConsumeAsync(Token, TestUserId, TestSessionId, StepUpPurpose.EnrollMfa, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.TypedErrors.Should().ContainSingle(e => e.Code == "identity.security.step-up-invalid");
    }

    [Fact]
    public async Task AProofBoundToAnotherUser_IsRefused()
    {
        await Issue(userId: Guid.NewGuid());

        var result = await CreateSut().ConsumeAsync(Token, TestUserId, TestSessionId, StepUpPurpose.EnrollMfa, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task AProofOfAnotherPurpose_IsRefused()
    {
        await Issue(purpose: StepUpPurpose.DisableMfa);

        var result = await CreateSut().ConsumeAsync(Token, TestUserId, TestSessionId, StepUpPurpose.EnrollMfa, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task AReplayedProof_IsRefused()
    {
        await Issue();
        var sut = CreateSut();

        (await sut.ConsumeAsync(Token, TestUserId, TestSessionId, StepUpPurpose.EnrollMfa, CancellationToken.None)).Succeeded.Should().BeTrue();
        (await sut.ConsumeAsync(Token, TestUserId, TestSessionId, StepUpPurpose.EnrollMfa, CancellationToken.None)).Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task AMismatchedConsume_BurnsTheProof()
    {
        await Issue();
        var sut = CreateSut();

        (await sut.ConsumeAsync(Token, TestUserId, Guid.NewGuid(), StepUpPurpose.EnrollMfa, CancellationToken.None)).Succeeded.Should().BeFalse();
        (await sut.ConsumeAsync(Token, TestUserId, TestSessionId, StepUpPurpose.EnrollMfa, CancellationToken.None)).Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task AnExpiredProof_IsRefused()
    {
        await Issue(ttl: TimeSpan.FromSeconds(-1));

        var result = await CreateSut().ConsumeAsync(Token, TestUserId, TestSessionId, StepUpPurpose.EnrollMfa, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
    }
}
