using Notrelix.Application.Common.Email;
using Notrelix.Application.Features.Identity.Security.Abstractions;
using Notrelix.Application.Features.Identity.Security.Services;

namespace Notrelix.Application.Tests.Features.Identity.Security;

/// <summary>The plain mail a person gets after a security change: what happened, when, on which device, and no secret.</summary>
public class SecurityNoticeServiceTests : IdentityHandlerTestBase
{
    private readonly Mock<IEmailOutboxWriter> _outbox = new();
    private readonly Mock<ISecurityPageLink> _link = new();
    private readonly List<QueueRenderedEmailRequest> _queued = new();

    public SecurityNoticeServiceTests()
    {
        _link.Setup(l => l.Build()).Returns("https://app.example.test/");
        _outbox.Setup(o => o.QueueRenderedEmailAsync(It.IsAny<QueueRenderedEmailRequest>(), It.IsAny<CancellationToken>()))
            .Callback<QueueRenderedEmailRequest, CancellationToken>((r, _) => _queued.Add(r))
            .Returns(Task.CompletedTask);
        ClientMetadataMock.Setup(c => c.UserAgent).Returns("Mozilla/5.0 (Macintosh) Safari/605");
        ClientMetadataMock.Setup(c => c.IpAddress).Returns("203.0.113.9");
    }

    private SecurityNoticeService Sut() => new(IdentityContextMock.Object, ClientMetadataMock.Object, _outbox.Object, _link.Object);

    [Theory]
    [InlineData(SecurityNoticeKind.PasswordChanged)]
    [InlineData(SecurityNoticeKind.PasswordReset)]
    [InlineData(SecurityNoticeKind.MfaEnabled)]
    [InlineData(SecurityNoticeKind.MfaDisabled)]
    [InlineData(SecurityNoticeKind.ProviderLinked)]
    [InlineData(SecurityNoticeKind.ProviderUnlinked)]
    [InlineData(SecurityNoticeKind.SessionRevoked)]
    [InlineData(SecurityNoticeKind.OtherSessionsRevoked)]
    [InlineData(SecurityNoticeKind.ApiTokenIssued)]
    public async Task EveryKind_QueuesOneMailToTheSignInAddress_WithTheTimeTheDeviceAndALink(SecurityNoticeKind kind)
    {
        var user = CreateUser();
        SetupUsers(user);

        await Sut().QueueAsync(user.Id, kind, TestNow, null, CancellationToken.None);

        var mail = _queued.Should().ContainSingle().Subject;
        mail.RecipientEmail.Should().Be(user.Email.Value);
        mail.RecipientUserId.Should().Be(user.Id);
        mail.Subject.Should().EndWith("— Notrelix").And.NotBeNullOrWhiteSpace();
        mail.BodyHtml.Should().Contain("Mozilla/5.0 (Macintosh) Safari/605");
        mail.BodyHtml.Should().Contain("203.0.113.9");
        mail.BodyHtml.Should().Contain("2025-01-15 10:00 UTC");
        mail.BodyHtml.Should().Contain("https://app.example.test/");
    }

    [Fact]
    public async Task TheMailHoldsNoSecret()
    {
        var user = CreateUser();
        SetupUsers(user);

        await Sut().QueueAsync(user.Id, SecurityNoticeKind.PasswordChanged, TestNow, null, CancellationToken.None);

        var body = _queued.Single().BodyHtml!;
        body.Should().NotContain(user.PasswordHash);
        body.Should().NotContainEquivalentOf("token=").And.NotContainEquivalentOf("password:");
    }

    [Fact]
    public async Task TheDeviceTextIsEncoded_SoAnAgentStringCannotInjectMarkup()
    {
        var user = CreateUser();
        SetupUsers(user);
        ClientMetadataMock.Setup(c => c.UserAgent).Returns("<script>alert(1)</script>");

        await Sut().QueueAsync(user.Id, SecurityNoticeKind.MfaDisabled, TestNow, null, CancellationToken.None);

        _queued.Single().BodyHtml.Should().NotContain("<script>").And.Contain("&lt;script&gt;");
    }

    [Fact]
    public async Task WithoutClientDetails_TheMailSaysTheDeviceIsUnknown()
    {
        var user = CreateUser();
        SetupUsers(user);
        ClientMetadataMock.Setup(c => c.UserAgent).Returns((string?)null);
        ClientMetadataMock.Setup(c => c.IpAddress).Returns((string?)null);

        await Sut().QueueAsync(user.Id, SecurityNoticeKind.ProviderLinked, TestNow, null, CancellationToken.None);

        _queued.Single().BodyHtml.Should().Contain("Unknown device");
    }

    [Fact]
    public async Task TheSameEventQueuesTheSameKey_SoARetrySendsAtMostOneMail_AndADifferentEventGetsAnother()
    {
        var user = CreateUser();
        SetupUsers(user);
        var tokenA = Guid.CreateVersion7();
        var tokenB = Guid.CreateVersion7();

        await Sut().QueueAsync(user.Id, SecurityNoticeKind.ApiTokenIssued, TestNow, tokenA, CancellationToken.None);
        await Sut().QueueAsync(user.Id, SecurityNoticeKind.ApiTokenIssued, TestNow, tokenA, CancellationToken.None);
        await Sut().QueueAsync(user.Id, SecurityNoticeKind.ApiTokenIssued, TestNow, tokenB, CancellationToken.None);

        _queued[0].DeduplicationKey.Should().Be(_queued[1].DeduplicationKey);
        _queued[2].DeduplicationKey.Should().NotBe(_queued[0].DeduplicationKey);
    }

    [Fact]
    public async Task ForAnUnknownPerson_NothingIsQueuedAndNothingFails()
    {
        SetupUsers();

        var act = () => Sut().QueueAsync(Guid.CreateVersion7(), SecurityNoticeKind.PasswordChanged, TestNow, null, CancellationToken.None);

        await act.Should().NotThrowAsync();
        _queued.Should().BeEmpty();
    }

    [Fact]
    public async Task AFailureToQueue_NeverFailsTheEvent()
    {
        var user = CreateUser();
        SetupUsers(user);
        _outbox.Setup(o => o.QueueRenderedEmailAsync(It.IsAny<QueueRenderedEmailRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("outbox down"));

        var act = () => Sut().QueueAsync(user.Id, SecurityNoticeKind.PasswordChanged, TestNow, null, CancellationToken.None);

        await act.Should().NotThrowAsync();
    }
}
