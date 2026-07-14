using Notrelix.Application.Features.Identity.Registration.Commands.Register;
using Notrelix.Application.Common.Security.Auth;
using Notrelix.Application.Features.Identity.Verification.Abstractions;
using Notrelix.Domain.Identity.Users;

namespace Notrelix.Application.Tests.Features.Identity.Auth.Commands;

public class RegisterTests : IdentityHandlerTestBase
{
    private RegisterCommandHandler CreateSut(IEmailVerificationTokenIssuer? tokenIssuer = null) => new(
        IdentityContextMock.Object,
        AccountContextMock.Object,
        PasswordHasherMock.Object,
        SessionIssuerMock.Object,
        DateTimeProviderMock.Object,
        IntegrationEventCollectorMock.Object,
        tokenIssuer);

    [Fact]
    public async Task Handle_WhenNewEmail_ReturnsSuccess()
    {
        PasswordHasherMock.Setup(h => h.HashPassword(TestPassword)).Returns(TestHashedPassword);
        SessionIssuerMock.Setup(s => s.IssueAsync(It.IsAny<User>(), TestNow, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AuthResult
            {
                AccessToken = "access-token",
                RefreshToken = "refresh-token",
                ExpiresAt = TestNow.AddHours(1).UtcDateTime,
                User = new UserDto { Id = TestUserId, Email = TestEmail, Name = "Test User", EmailConfirmed = false }
            });

        var sut = CreateSut();
        var result = await sut.Handle(new RegisterCommand
        {
            Email = TestEmail,
            Password = TestPassword,
            Name = "Test User"
        }, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data!.WorkspaceProvisioning.Should().Be("pending");
        IntegrationEventCollectorMock.Verify(c => c.Add(It.IsAny<IIntegrationEvent>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenEmailAlreadyExists_ReturnsFailure()
    {
        var existingUser = CreateUser();
        SetupUsers(existingUser);

        var sut = CreateSut();
        var result = await sut.Handle(new RegisterCommand
        {
            Email = TestEmail,
            Password = TestPassword,
            Name = "Test User"
        }, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("already in use"));
    }

    [Fact]
    public async Task Handle_WhenTokenIssuerAvailable_IssuesVerificationToken()
    {
        PasswordHasherMock.Setup(h => h.HashPassword(TestPassword)).Returns(TestHashedPassword);
        SessionIssuerMock.Setup(s => s.IssueAsync(It.IsAny<User>(), TestNow, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AuthResult
            {
                AccessToken = "access-token",
                RefreshToken = "refresh-token",
                ExpiresAt = TestNow.AddHours(1).UtcDateTime,
                User = new UserDto { Id = TestUserId, Email = TestEmail, Name = "Test User", EmailConfirmed = false }
            });

        TokenIssuerMock.Setup(t => t.IssueAsync(It.IsAny<User>(), It.IsAny<Guid>(), TestNow, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EmailVerificationTokenIssue(Guid.NewGuid(), TestUserId, TestEmail, "protected-token", 1, TestNow.AddDays(1)));

        var sut = CreateSut(TokenIssuerMock.Object);
        var result = await sut.Handle(new RegisterCommand
        {
            Email = TestEmail,
            Password = TestPassword,
            Name = "Test User"
        }, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        TokenIssuerMock.Verify(t => t.IssueAsync(It.IsAny<User>(), It.IsAny<Guid>(), TestNow, It.IsAny<CancellationToken>()), Times.Once);
    }
}
