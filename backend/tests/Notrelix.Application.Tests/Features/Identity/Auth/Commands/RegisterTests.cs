using Notrelix.Application.Features.Identity.Registration.Commands.Register;
using Notrelix.Application.Events.Identity;
using Notrelix.Application.Features.Accounts.Public.PersonalAccountProvisioning;
using Notrelix.Application.Features.Identity.Registration.Ports;
using Notrelix.Application.Features.Identity.Verification.Abstractions;
using Notrelix.Domain.Identity.Users;

namespace Notrelix.Application.Tests.Features.Identity.Auth.Commands;

public class RegisterTests : IdentityHandlerTestBase
{
    private readonly Mock<IAccountProvisioningActions> _provisioningActionsMock = new();
    private readonly Mock<IEmailRegistrationProbe> _probeMock = new();

    private RegisterCommandHandler CreateSut(IEmailVerificationTokenIssuer? tokenIssuer = null) => new(
        IdentityContextMock.Object,
        _provisioningActionsMock.Object,
        PasswordHasherMock.Object,
        DateTimeProviderMock.Object,
        IntegrationEventCollectorMock.Object,
        RateLimitServiceMock.Object,
        tokenIssuer,
        _probeMock.Object);

    private RegisterCommand Command() => new()
    {
        Email = TestEmail,
        Password = TestPassword,
        Name = "Test User"
    };

    private void SetupProvisioning(Guid accountId)
    {
        _provisioningActionsMock
            .Setup(s => s.ProvisionPersonalAccountAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PersonalAccountProvisioningResult(accountId));
    }

    private void SetupExistingAddress()
    {
        _probeMock.Setup(p => p.IsRegisteredAsync(TestEmail, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        PasswordHasherMock.Setup(h => h.HashPassword(TestPassword)).Returns(TestHashedPassword);
    }

    [Fact]
    public async Task Handle_WhenNewEmail_ReturnsSuccess()
    {
        var accountId = Guid.NewGuid();
        SetupProvisioning(accountId);

        PasswordHasherMock.Setup(h => h.HashPassword(TestPassword)).Returns(TestHashedPassword);

        var sut = CreateSut();
        var result = await sut.Handle(new RegisterCommand
        {
            Email = TestEmail,
            Password = TestPassword,
            Name = "Test User"
        }, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        SessionIssuerMock.VerifyNoOtherCalls();
        _provisioningActionsMock.Verify(
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
    public async Task Handle_WhenEmailAlreadyExists_ReturnsTheSameSuccessAndRaisesOnlyTheOwnerNotice()
    {
        SetupExistingAddress();
        IIntegrationEvent? captured = null;
        IntegrationEventCollectorMock
            .Setup(c => c.Add(It.IsAny<IIntegrationEvent>()))
            .Callback<IIntegrationEvent>(e => captured = e);

        var sut = CreateSut();
        var result = await sut.Handle(Command(), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Errors.Should().BeEmpty();
        _provisioningActionsMock.Verify(
            s => s.ProvisionPersonalAccountAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()),
            Times.Never);
        IdentityContextMock.Verify(c => c.Users.Add(It.IsAny<User>()), Times.Never);
        SessionIssuerMock.VerifyNoOtherCalls();
        // Only the notice fact, addressed to the owner: no registration fact, no account, no session.
        IntegrationEventCollectorMock.Verify(
            c => c.Add(It.Is<IIntegrationEvent>(e => e is RegistrationAttemptedOnExistingAccountIntegrationEventV1)),
            Times.Once);
        IntegrationEventCollectorMock.Verify(c => c.Add(It.IsAny<IIntegrationEvent>()), Times.Once);
        captured.Should().BeOfType<RegistrationAttemptedOnExistingAccountIntegrationEventV1>()
            .Which.Email.Should().Be(TestEmail);
    }

    [Fact]
    public async Task Handle_WhenEmailAlreadyExists_HashesThePasswordLikeANewRegistration()
    {
        SetupExistingAddress();

        await CreateSut().Handle(Command(), CancellationToken.None);

        PasswordHasherMock.Verify(h => h.HashPassword(TestPassword), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenEmailAlreadyExistsAndTheNoticeIsLimited_StaysSilentWithTheSameSuccess()
    {
        SetupExistingAddress();
        RateLimitServiceMock
            .Setup(r => r.IsRateLimitedAsync("register-existing-notice", TestEmail, It.IsAny<int>(), It.IsAny<TimeSpan>()))
            .ReturnsAsync(true);

        var result = await CreateSut().Handle(Command(), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        IntegrationEventCollectorMock.Verify(c => c.Add(It.IsAny<IIntegrationEvent>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenEmailIsMixedCaseAndPadded_ProbesTheNormalizedAddress()
    {
        SetupExistingAddress();

        var command = Command() with { Email = "  Test@Example.COM " };
        await CreateSut().Handle(command, CancellationToken.None);

        _probeMock.Verify(p => p.IsRegisteredAsync(TestEmail, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenTokenIssuerAvailable_IssuesVerificationToken()
    {
        SetupProvisioning(Guid.NewGuid());

        PasswordHasherMock.Setup(h => h.HashPassword(TestPassword)).Returns(TestHashedPassword);

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
        _provisioningActionsMock
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
