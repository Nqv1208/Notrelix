using Microsoft.Extensions.Logging.Abstractions;
using Notrelix.Application.Features.Identity.Profiles.Commands.UpdateEmail;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Identity.Security.DTOs;
using Notrelix.Domain.Identity.Users;

namespace Notrelix.Application.Tests.Features.Identity.Profiles.Commands;

public class UpdateEmailTests : IdentityHandlerTestBase
{
    private UpdateEmailCommandHandler CreateSut() => new(
        IdentityContextMock.Object,
        RequestContextMock.Object,
        TokenIssuerMock.Object,
        DateTimeProviderMock.Object,
        StepUpServiceMock.Object,
        EmailServiceMock.Object,
        NullLogger<UpdateEmailCommandHandler>.Instance);

    private const string ValidProof = "valid-proof";

    private void GrantProof()
    {
        RequestContextMock.Setup(c => c.SessionId).Returns(TestSessionId);
        StepUpServiceMock
            .Setup(s => s.ConsumeAsync(ValidProof, TestUserId, TestSessionId, StepUpPurpose.ChangeSecurityIdentity, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());
    }

    private void VerifyNoLinkIssued() => TokenIssuerMock.Verify(
        t => t.IssueForPendingEmailAsync(It.IsAny<User>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()),
        Times.Never);

    [Fact]
    public async Task Handle_WhenNewEmailAvailable_ParksItMailsTheLinkAndKeepsTheSignInEmail()
    {
        var user = CreateUser();
        var oldNormalized = user.NormalizedEmail;
        SetupUsers(user);
        GrantProof();

        var result = await CreateSut().Handle(new UpdateEmailCommand("New@Example.com", ValidProof), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data!.PendingEmail.Should().Be("new@example.com");
        user.NormalizedEmail.Should().Be(oldNormalized);
        user.PendingEmail.Should().Be("new@example.com");
        TokenIssuerMock.Verify(
            t => t.IssueForPendingEmailAsync(user, "new@example.com", TestUserId, TestNow, It.IsAny<CancellationToken>()),
            Times.Once);
        EmailServiceMock.Verify(e => e.SendAsync(user.Email.Value, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenEmailAlreadyTaken_AnswersLikeAFreeAddressWithoutParkingOrMailingIt()
    {
        var user = CreateUser();
        var otherUser = CreateUser(email: "taken@example.com", id: Guid.CreateVersion7());
        SetupUsers(user, otherUser);
        GrantProof();

        var result = await CreateSut().Handle(new UpdateEmailCommand("taken@example.com", ValidProof), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data!.PendingEmail.Should().Be("taken@example.com");
        user.PendingEmail.Should().BeNull();
        VerifyNoLinkIssued();
        EmailServiceMock.Verify(e => e.SendAsync(user.Email.Value, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenNoStepUpProof_RefusesBeforeLearningWhetherTheAddressIsTaken()
    {
        var user = CreateUser();
        var otherUser = CreateUser(email: "taken@example.com", id: Guid.CreateVersion7());
        SetupUsers(user, otherUser);
        RequestContextMock.Setup(c => c.SessionId).Returns(TestSessionId);

        var result = await CreateSut().Handle(new UpdateEmailCommand("taken@example.com"), CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.TypedErrors.Should().ContainSingle().Which.Code.Should().Be("identity.security.step-up-required");
        user.PendingEmail.Should().BeNull();
        VerifyNoLinkIssued();
    }

    [Fact]
    public async Task Handle_WhenTheAddressIsAlreadyTheSignInEmail_IsANoOp()
    {
        var user = CreateUser();
        SetupUsers(user);

        var result = await CreateSut().Handle(new UpdateEmailCommand(user.Email.Value), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data!.PendingEmail.Should().BeNull();
        VerifyNoLinkIssued();
    }
}
