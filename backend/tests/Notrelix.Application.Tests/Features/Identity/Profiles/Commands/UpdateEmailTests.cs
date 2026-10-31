using Notrelix.Application.Features.Identity.Profiles.Commands.UpdateEmail;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Identity.Security.DTOs;
using Notrelix.Application.Features.Identity.Verification.Abstractions;
using Notrelix.Domain.Identity.Users;

namespace Notrelix.Application.Tests.Features.Identity.Profiles.Commands;

public class UpdateEmailTests : IdentityHandlerTestBase
{
    private UpdateEmailCommandHandler CreateSut() => new(
        IdentityContextMock.Object,
        RequestContextMock.Object,
        TokenIssuerMock.Object,
        DateTimeProviderMock.Object,
        StepUpServiceMock.Object);

    private const string ValidProof = "valid-proof";

    private void GrantProof()
    {
        RequestContextMock.Setup(c => c.SessionId).Returns(TestSessionId);
        StepUpServiceMock
            .Setup(s => s.ConsumeAsync(ValidProof, TestUserId, TestSessionId, StepUpPurpose.ChangeSecurityIdentity, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());
    }

    [Fact]
    public async Task Handle_WhenNewEmailAvailable_UpdatesEmailAndIssuesToken()
    {
        var user = CreateUser();
        SetupUsers(user);
        GrantProof();
        TokenIssuerMock.Setup(t => t.IssueAsync(user, TestUserId, TestNow, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EmailVerificationTokenIssue(Guid.NewGuid(), TestUserId, "new@example.com", "protected", 1, TestNow.AddDays(1)));

        var sut = CreateSut();
        var result = await sut.Handle(new UpdateEmailCommand("new@example.com", ValidProof), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data!.EmailConfirmed.Should().BeFalse();
        result.Data!.SessionRefreshRequired.Should().BeTrue();
        TokenIssuerMock.Verify(t => t.IssueAsync(user, TestUserId, TestNow, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenEmailAlreadyTaken_ReturnsFailure()
    {
        var user = CreateUser();
        var otherUser = CreateUser(email: "taken@example.com", id: Guid.CreateVersion7());
        SetupUsers(user, otherUser);
        GrantProof();

        var sut = CreateSut();
        var result = await sut.Handle(new UpdateEmailCommand("taken@example.com", ValidProof), CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("already in use"));
    }

    [Fact]
    public async Task Handle_WhenNoStepUpProof_RefusesBeforeLearningWhetherTheAddressIsTaken()
    {
        var user = CreateUser();
        var otherUser = CreateUser(email: "taken@example.com", id: Guid.CreateVersion7());
        SetupUsers(user, otherUser);
        RequestContextMock.Setup(c => c.SessionId).Returns(TestSessionId);

        var sut = CreateSut();
        var result = await sut.Handle(new UpdateEmailCommand("taken@example.com"), CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().NotContain(e => e.Contains("already in use"));
        TokenIssuerMock.Verify(t => t.IssueAsync(It.IsAny<User>(), It.IsAny<Guid>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
