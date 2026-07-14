using Notrelix.Application.Features.Identity.Profiles.Commands.UpdateEmail;
using Notrelix.Application.Features.Identity.Verification.Abstractions;

namespace Notrelix.Application.Tests.Features.Identity.Profiles.Commands;

public class UpdateEmailTests : IdentityHandlerTestBase
{
    private UpdateEmailCommandHandler CreateSut() => new(
        IdentityContextMock.Object,
        RequestContextMock.Object,
        TokenIssuerMock.Object,
        DateTimeProviderMock.Object);

    [Fact]
    public async Task Handle_WhenNewEmailAvailable_UpdatesEmailAndIssuesToken()
    {
        var user = CreateUser();
        SetupUsers(user);
        TokenIssuerMock.Setup(t => t.IssueAsync(user, TestUserId, TestNow, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EmailVerificationTokenIssue(Guid.NewGuid(), TestUserId, "new@example.com", "protected", 1, TestNow.AddDays(1)));

        var sut = CreateSut();
        var result = await sut.Handle(new UpdateEmailCommand("new@example.com"), CancellationToken.None);

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

        var sut = CreateSut();
        var result = await sut.Handle(new UpdateEmailCommand("taken@example.com"), CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("already in use"));
    }
}
