using System.Reflection;
using System.Text.Json;
using Notrelix.Application.Common.Auditing;
using Notrelix.Application.Tests.Features.Collaboration;
using Notrelix.Application.Features.Governance.Abstractions;
using Notrelix.Application.Features.Governance.DTOs;
using Notrelix.Application.Features.Governance.ShareLinks.Commands.CreateShareLink;
using Notrelix.Domain.Common.Auditing;
using Notrelix.Domain.Governance.ShareLinks;

namespace Notrelix.Application.Tests.Features.Governance;

public class CreateShareLinkHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 9, 12, 0, 0, TimeSpan.Zero);
    private readonly Guid _workspaceId = Guid.CreateVersion7();
    private readonly Guid _actorId = Guid.CreateVersion7();
    private readonly Guid _boardId = Guid.CreateVersion7();
    private readonly List<ShareLink> _links = [];
    private readonly Mock<IAuditService> _audit = new();

    private CreateShareLinkCommandHandler CreateSut()
    {
        var links = TestDbSet.Create(_links);
        var context = new Mock<IGovernanceDbContext>();
        context.Setup(c => c.ShareLinks).Returns(links.Object);

        var requestContext = new Mock<ICurrentRequestContext>();
        requestContext.Setup(r => r.UserId).Returns(_actorId);
        requestContext.Setup(r => r.RequireWorkspaceId()).Returns(_workspaceId);
        requestContext.Setup(r => r.RequireAccountId()).Returns(Guid.CreateVersion7());

        var clock = new Mock<IDateTimeProvider>();
        clock.Setup(c => c.UtcNow).Returns(Now);

        var policy = new Mock<IWorkspaceSharingPolicy>();
        policy.Setup(p => p.AllowsShareLinksAsync(_workspaceId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        return new CreateShareLinkCommandHandler(context.Object, requestContext.Object, clock.Object, policy.Object, _audit.Object);
    }

    private CreateShareLinkCommand Command(string level, DateTime? expiresAt = null) =>
        new("work-management.board", _boardId, level, expiresAt ?? Now.AddDays(7).UtcDateTime);

    private CreateShareLinkCommand WithoutExpiry(string level) =>
        new("work-management.board", _boardId, level, null);

    [Fact]
    public void Dto_CarriesNoTokenOrHash_OnlyMetadata()
    {
        typeof(ShareLinkDto).GetProperty("TokenHash", BindingFlags.Public | BindingFlags.Instance).Should().BeNull(
            "the create response may not carry the token hash (Lead decision L6)");
        typeof(ShareLinkDto).GetProperties().Select(p => p.Name).Should().Contain("Level");
    }

    [Fact]
    public async Task Handle_HonoursTheRequestLevel_AndReturnsTheRawTokenOnceWithoutTheHash()
    {
        var result = await CreateSut().Handle(Command("Commenter"), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data!.ShareLink.Level.Should().Be("Commenter");
        result.Data.RawToken.Should().NotBeNullOrWhiteSpace();

        var stored = _links.Should().ContainSingle().Subject;
        stored.Level.Should().Be(Notrelix.Domain.Governance.Permissions.PermissionLevel.Commenter);
        stored.TokenHash.Hash.Should().NotBe(result.Data.RawToken, "only the hash is persisted");
    }

    [Theory]
    [InlineData("Editor")]
    [InlineData("Manager")]
    [InlineData("Owner")]
    [InlineData("None")]
    [InlineData("Nonsense")]
    public void Validator_RejectsAnyLevelAboveCommenter(string level)
    {
        var validation = new CreateShareLinkCommandValidator().Validate(Command(level));

        validation.IsValid.Should().BeFalse("a share link grants Viewer or Commenter only (Lead decision L5)");
        validation.Errors.Should().Contain(e => e.PropertyName == "Level");
    }

    [Theory]
    [InlineData("Viewer")]
    [InlineData("Commenter")]
    public void Validator_AcceptsViewerAndCommenter(string level)
    {
        new CreateShareLinkCommandValidator().Validate(Command(level)).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validator_RequiresAnExpiry()
    {
        var validation = new CreateShareLinkCommandValidator().Validate(WithoutExpiry("Viewer"));

        validation.IsValid.Should().BeFalse("creation requires an expiry (Lead decision L5)");
        validation.Errors.Should().Contain(e => e.PropertyName == "ExpiresAt");
    }

    [Fact]
    public async Task Handle_RefusesAnExpiryInThePast_AndStoresNothing()
    {
        var act = () => CreateSut().Handle(Command("Viewer", Now.AddMinutes(-1).UtcDateTime), CancellationToken.None);

        (await act.Should().ThrowAsync<Notrelix.Domain.Common.Exceptions.BusinessRuleException>()).Which.RuleCode
            .Should().Be("ShareLinkExpiryInPast");
        _links.Should().BeEmpty();
        _audit.Verify(a => a.RecordAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<ResourceRef>(),
            It.IsAny<AuditMetadata>(), It.IsAny<AuditSeverity>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<CancellationToken>(), It.IsAny<AuditChange?>()), Times.Never);
    }

    [Fact]
    public async Task Handle_RecordsOneAuditFact_WhosePayloadNeverCarriesTheRawToken()
    {
        AuditChange? recorded = null;
        _audit.Setup(a => a.RecordAsync(_workspaceId, _actorId, "CreateShareLink", It.IsAny<ResourceRef>(),
                It.IsAny<AuditMetadata>(), AuditSeverity.Info, It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>(), It.IsAny<AuditChange?>()))
            .Callback<Guid, Guid, string, ResourceRef, AuditMetadata, AuditSeverity, string, string, CancellationToken, AuditChange?>(
                (_, _, _, _, _, _, _, _, _, change) => recorded = change)
            .Returns(Task.CompletedTask);

        var result = await CreateSut().Handle(Command("Viewer"), CancellationToken.None);

        _audit.Verify(a => a.RecordAsync(_workspaceId, _actorId, "CreateShareLink", It.IsAny<ResourceRef>(),
            It.IsAny<AuditMetadata>(), AuditSeverity.Info, It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<CancellationToken>(), It.IsAny<AuditChange?>()), Times.Once);
        recorded.Should().NotBeNull();
        JsonSerializer.Serialize(recorded!.After).Should().Contain("Viewer").And.NotContain(result.Data!.RawToken);
    }
}
