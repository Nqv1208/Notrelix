using Notrelix.Application.Common.Security;
using Notrelix.Application.Common.Tokens;
using Notrelix.Application.Events.Identity;
using Notrelix.Application.Features.Identity.Verification.Abstractions;
using Notrelix.Application.Features.Identity.Verification.Services;
using Notrelix.Domain.Identity.Tokens;

namespace Notrelix.Application.Tests.Features.Identity.Verification;

public class EmailVerificationTokenIssuerTests : IdentityHandlerTestBase
{
    private readonly Mock<IActiveVerificationTokenLocker> _locker = new();
    private readonly Mock<ISecretEncryptor> _encryptor = new();

    private EmailVerificationTokenIssuer CreateSut()
    {
        OneTimeTokenServiceMock.Setup(s => s.Generate(TokenPurpose.EmailVerification))
            .Returns(new IssuedOneTimeToken("raw-new", "hash-new", 1));
        _encryptor.Setup(e => e.Protect(It.IsAny<string>(), It.IsAny<string>())).Returns("protected");
        return new EmailVerificationTokenIssuer(
            IdentityContextMock.Object,
            _locker.Object,
            OneTimeTokenServiceMock.Object,
            _encryptor.Object,
            IntegrationEventCollectorMock.Object);
    }

    [Fact]
    public async Task IssueAsync_WhenAnEarlierLinkIsStillActive_ReplacesItSoOnlyTheNewestWorks()
    {
        var user = CreateUser(emailConfirmed: false);
        var earlier = EmailVerificationToken.Create(
            user.Id, TokenHash.Create("hash-old"), 1, TestEmail, TestNow.AddHours(1), TestNow.AddMinutes(-5));
        _locker.Setup(l => l.LockActiveTokensAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { earlier });

        var issue = await CreateSut().IssueAsync(user, user.Id, TestNow, CancellationToken.None);

        earlier.Status.Should().Be(UserTokenStatus.Revoked);
        issue.UserId.Should().Be(user.Id);
        IdentityContextMock.Verify(c => c.EmailVerificationTokens.Add(It.Is<EmailVerificationToken>(t => t.Status == UserTokenStatus.Active)), Times.Once);
        IntegrationEventCollectorMock.Verify(c => c.Add(It.IsAny<EmailVerificationDeliveryRequestedIntegrationEventV1>()), Times.Once);
    }
}
