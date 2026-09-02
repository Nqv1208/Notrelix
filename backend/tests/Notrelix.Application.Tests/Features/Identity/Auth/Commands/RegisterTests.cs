using Notrelix.Application.Features.Identity.Registration.Commands.Register;
using Notrelix.Application.Common.Security.Auth;
using Notrelix.Application.Events.Identity;
using Notrelix.Application.Features.Accounts.Provisioning;
using Notrelix.Application.Features.Identity.Verification.Abstractions;
using Notrelix.Domain.Identity.Users;

namespace Notrelix.Application.Tests.Features.Identity.Auth.Commands;

public class RegisterTests : IdentityHandlerTestBase
{
    private readonly Mock<IAccountProvisioningService> _provisioningServiceMock = new();

    private RegisterCommandHandler CreateSut(IEmailVerificationTokenIssuer? tokenIssuer = null) => new(
        IdentityContextMock.Object,
        _provisioningServiceMock.Object,
        PasswordHasherMock.Object,
        SessionIssuerMock.Object,
        DateTimeProviderMock.Object,
        IntegrationEventCollectorMock.Object,
        tokenIssuer);

    private void SetupProvisioning(Guid accountId)
    {
        _provisioningServiceMock
            .Setup(s => s.ProvisionPersonalAccountAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PersonalAccountProvisioningResult(accountId));
    }

    [Fact]
    public async Task Handle_WhenNewEmail_ReturnsSuccess()
    {
        var accountId = Guid.NewGuid();
        SetupProvisioning(accountId);

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
        _provisioningServiceMock.Verify(
            s => s.ProvisionPersonalAccountAsync(It.IsAny<Guid>(), "Test User", TestNow, It.IsAny<CancellationToken>()),
            Times.Once);
        AccountContextMock.Verify(c => c.Accounts.Add(It.IsAny<Notrelix.Domain.Accounts.Accounts.Account>()), Times.Never);
        AccountContextMock.Verify(c => c.AccountMembers.Add(It.IsAny<Notrelix.Domain.Accounts.Members.AccountMember>()), Times.Never);
        IntegrationEventCollectorMock.Verify(c => c.Add(It.IsAny<IIntegrationEvent>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenNewEmail_RaisesEventWithProvisionedAccountId()
    {
        var accountId = Guid.NewGuid();
        SetupProvisioning(accountId);

        PasswordHasherMock.Setup(h => h.HashPassword(TestPassword)).Returns(TestHashedPassword);
        SessionIssuerMock.Setup(s => s.IssueAsync(It.IsAny<User>(), TestNow, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AuthResult
            {
                AccessToken = "access-token",
                RefreshToken = "refresh-token",
                ExpiresAt = TestNow.AddHours(1).UtcDateTime,
                User = new UserDto { Id = TestUserId, Email = TestEmail, Name = "Test User", EmailConfirmed = false }
            });

        IIntegrationEvent? captured = null;
        IntegrationEventCollectorMock
            .Setup(c => c.Add(It.IsAny<IIntegrationEvent>()))
            .Callback<IIntegrationEvent>(e => captured = e);

        var sut = CreateSut();
        var result = await sut.Handle(new RegisterCommand
        {
            Email = TestEmail,
            Password = TestPassword,
            Name = "Test User"
        }, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        var integrationEvent = captured.Should().BeOfType<IdentityRegistrationCompletedIntegrationEventV1>().Subject;
        integrationEvent.AccountId.Should().Be(accountId);
        integrationEvent.AccountId.Should().NotBe(Guid.Empty);
        integrationEvent.AccountName.Should().Be("Test User's Account");
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
        _provisioningServiceMock.Verify(
            s => s.ProvisionPersonalAccountAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenTokenIssuerAvailable_IssuesVerificationToken()
    {
        SetupProvisioning(Guid.NewGuid());

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

    [Fact]
    public async Task Handle_WhenProvisioningFails_DoesNotEmitRegistrationCompleted()
    {
        PasswordHasherMock.Setup(h => h.HashPassword(TestPassword)).Returns(TestHashedPassword);
        _provisioningServiceMock
            .Setup(s => s.ProvisionPersonalAccountAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("provisioning failed"));

        var sut = CreateSut();

        var act = () => sut.Handle(new RegisterCommand
        {
            Email = TestEmail,
            Password = TestPassword,
            Name = "Test User"
        }, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
        IntegrationEventCollectorMock.Verify(c => c.Add(It.IsAny<IIntegrationEvent>()), Times.Never);
    }
}
