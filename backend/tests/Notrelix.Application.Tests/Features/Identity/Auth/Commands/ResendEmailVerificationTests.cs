using Notrelix.Application.Features.Identity.Verification.Commands.ResendEmailVerification;
using Notrelix.Application.Features.Identity.Verification.Abstractions;
using Notrelix.Domain.Identity.Users;

namespace Notrelix.Application.Tests.Features.Identity.Auth.Commands;

public class ResendEmailVerificationTests : IdentityHandlerTestBase
{
    private ResendEmailVerificationCommandHandler CreateSut() => new(
        IdentityContextMock.Object,
        TokenIssuerMock.Object,
        RateLimitServiceMock.Object,
        DateTimeProviderMock.Object);

    [Fact]
    public async Task Handle_WhenUserExistsAndNotConfirmed_IssuesToken()
    {
        var user = CreateUser(emailConfirmed: false);
        SetupUsers(user);
        RateLimitServiceMock.Setup(r => r.IsRateLimitedAsync("email-verification-resend", TestEmail.ToLowerInvariant(), 5, TimeSpan.FromMinutes(15)))
            .ReturnsAsync(false);
        TokenIssuerMock.Setup(t => t.IssueAsync(user, TestUserId, TestNow, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EmailVerificationTokenIssue(Guid.NewGuid(), TestUserId, TestEmail, "protected", 1, TestNow.AddDays(1)));

        var sut = CreateSut();
        var result = await sut.Handle(new ResendEmailVerificationCommand(TestEmail), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        TokenIssuerMock.Verify(t => t.IssueAsync(user, TestUserId, TestNow, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenRateLimited_ReturnsSuccessSilently()
    {
        RateLimitServiceMock.Setup(r => r.IsRateLimitedAsync("email-verification-resend", TestEmail.ToLowerInvariant(), 5, TimeSpan.FromMinutes(15)))
            .ReturnsAsync(true);

        var sut = CreateSut();
        var result = await sut.Handle(new ResendEmailVerificationCommand(TestEmail), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        TokenIssuerMock.Verify(t => t.IssueAsync(It.IsAny<User>(), It.IsAny<Guid>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
