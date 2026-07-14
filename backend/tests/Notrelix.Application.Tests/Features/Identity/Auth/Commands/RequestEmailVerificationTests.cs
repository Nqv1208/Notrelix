using Notrelix.Application.Features.Identity.Verification.Commands.RequestEmailVerification;
using Notrelix.Application.Features.Identity.Verification.Abstractions;

namespace Notrelix.Application.Tests.Features.Identity.Auth.Commands;

public class RequestEmailVerificationTests : IdentityHandlerTestBase
{
    private RequestEmailVerificationCommandHandler CreateSut() => new(
        IdentityContextMock.Object,
        RequestContextMock.Object,
        TokenIssuerMock.Object,
        DateTimeProviderMock.Object);

    [Fact]
    public async Task Handle_WhenEmailNotConfirmed_IssuesToken()
    {
        var user = CreateUser(emailConfirmed: false);
        SetupUsers(user);
        TokenIssuerMock.Setup(t => t.IssueAsync(user, TestUserId, TestNow, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EmailVerificationTokenIssue(Guid.NewGuid(), TestUserId, TestEmail, "protected", 1, TestNow.AddDays(1)));

        var sut = CreateSut();
        var result = await sut.Handle(new RequestEmailVerificationCommand(), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        TokenIssuerMock.Verify(t => t.IssueAsync(user, TestUserId, TestNow, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenEmailAlreadyConfirmed_ReturnsFailure()
    {
        var user = CreateUser(emailConfirmed: true);
        SetupUsers(user);

        var sut = CreateSut();
        var result = await sut.Handle(new RequestEmailVerificationCommand(), CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("already confirmed"));
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ReturnsFailure()
    {
        SetupUsers();

        var sut = CreateSut();
        var result = await sut.Handle(new RequestEmailVerificationCommand(), CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("User not found"));
    }
}
