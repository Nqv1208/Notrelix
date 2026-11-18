using Notrelix.Application.Common.Messaging;
using Notrelix.Application.Events.Identity;
using Notrelix.Application.Features.Identity.Security.Abstractions;
using Notrelix.Application.Features.Identity.Security.Services;

namespace Notrelix.Application.Tests.Features.Identity.Security;

/// <summary>The fact a security change stages in its own transaction: who, what, when, from which device, and no secret.</summary>
public class SecurityNoticeServiceTests : IdentityHandlerTestBase
{
    private readonly Mock<IIntegrationEventCollector> _events = new();
    private readonly List<IIntegrationEvent> _staged = new();

    public SecurityNoticeServiceTests()
    {
        _events.Setup(e => e.Add(It.IsAny<IIntegrationEvent>())).Callback<IIntegrationEvent>(_staged.Add);
        ClientMetadataMock.Setup(c => c.UserAgent).Returns("Mozilla/5.0 (Macintosh) Safari/605");
        ClientMetadataMock.Setup(c => c.IpAddress).Returns("203.0.113.9");
    }

    private SecurityNoticeService Sut() => new(IdentityContextMock.Object, ClientMetadataMock.Object, _events.Object);

    private SecurityNoticeRequestedIntegrationEventV1 OnlyEvent() =>
        _staged.Should().ContainSingle().Subject.Should().BeOfType<SecurityNoticeRequestedIntegrationEventV1>().Subject;

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
    public async Task EveryKind_StagesOneEvent_ForTheSignInAddress_WithTheTimeAndTheDevice(SecurityNoticeKind kind)
    {
        var user = CreateUser();
        SetupUsers(user);

        await Sut().QueueAsync(user.Id, kind, TestNow, null, CancellationToken.None);

        var staged = OnlyEvent();
        staged.UserId.Should().Be(user.Id);
        staged.Email.Should().Be(user.Email.Value);
        staged.UserName.Should().Be(user.Name);
        staged.Kind.Should().Be(kind.ToString());
        staged.OccurredAt.Should().Be(TestNow);
        staged.Device.Should().Be("Mozilla/5.0 (Macintosh) Safari/605 · 203.0.113.9");
    }

    [Fact]
    public async Task TheEventHoldsNoSecret()
    {
        var user = CreateUser();
        SetupUsers(user);

        await Sut().QueueAsync(user.Id, SecurityNoticeKind.PasswordChanged, TestNow, null, CancellationToken.None);

        var json = System.Text.Json.JsonSerializer.Serialize(OnlyEvent());
        json.Should().NotContain(user.PasswordHash);
        json.Should().NotContainEquivalentOf("token=").And.NotContainEquivalentOf("password:");
    }

    [Fact]
    public async Task WithoutClientDetails_TheDeviceIsUnknown()
    {
        var user = CreateUser();
        SetupUsers(user);
        ClientMetadataMock.Setup(c => c.UserAgent).Returns((string?)null);
        ClientMetadataMock.Setup(c => c.IpAddress).Returns((string?)null);

        await Sut().QueueAsync(user.Id, SecurityNoticeKind.ProviderLinked, TestNow, null, CancellationToken.None);

        OnlyEvent().Device.Should().Be("Unknown device");
    }

    [Fact]
    public async Task EachCallStagesItsOwnEventId_AndTheSubjectTravelsWithIt()
    {
        var user = CreateUser();
        SetupUsers(user);
        var token = Guid.CreateVersion7();

        await Sut().QueueAsync(user.Id, SecurityNoticeKind.ApiTokenIssued, TestNow, token, CancellationToken.None);
        await Sut().QueueAsync(user.Id, SecurityNoticeKind.ApiTokenIssued, TestNow, token, CancellationToken.None);

        var events = _staged.Cast<SecurityNoticeRequestedIntegrationEventV1>().ToList();
        events.Should().HaveCount(2);
        events[0].EventId.Should().NotBe(events[1].EventId);
        events.Should().OnlyContain(e => e.SubjectId == token);
    }

    [Fact]
    public async Task ForAnUnknownPerson_NothingIsStagedAndNothingFails()
    {
        SetupUsers();

        var act = () => Sut().QueueAsync(Guid.CreateVersion7(), SecurityNoticeKind.PasswordChanged, TestNow, null, CancellationToken.None);

        await act.Should().NotThrowAsync();
        _staged.Should().BeEmpty();
    }
}
