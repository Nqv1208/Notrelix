using Notrelix.Application.Common.Auditing;
using Notrelix.Application.Tests.Features.Collaboration;
using Notrelix.Application.Features.Governance.Abstractions;
using Notrelix.Application.Features.Governance.ShareLinks.Commands.DisableShareLink;
using Notrelix.Domain.Common.Auditing;
using Notrelix.Domain.Governance.Permissions;
using Notrelix.Domain.Governance.ShareLinks;

namespace Notrelix.Application.Tests.Features.Governance;

public class DisableShareLinkHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 9, 12, 0, 0, TimeSpan.Zero);
    private readonly Guid _actorId = Guid.CreateVersion7();
    private readonly Guid _boardId = Guid.CreateVersion7();
    private readonly Mock<IAuditService> _audit = new();
    private readonly List<ShareLink> _links = [];

    private ShareLink SeedLink(ShareLinkStatus? status = null)
    {
        var link = ShareLink.Create(Guid.CreateVersion7(), Guid.CreateVersion7(), ResourceKind.Create("work-management.board"),
            _boardId, ShareLinkTokenHash.Create("hash-" + Guid.NewGuid().ToString("N")), ShareLinkAccessMode.WorkspaceOnly,
            _actorId, Now, Now.AddDays(7), PermissionLevel.Viewer);
        if (status == ShareLinkStatus.Expired) link.Expire(Now.AddMinutes(30));
        _links.Add(link);
        return link;
    }

    private DisableShareLinkCommandHandler CreateSut()
    {
        var context = new Mock<IGovernanceDbContext>();
        context.Setup(c => c.ShareLinks).Returns(TestDbSet.Create(_links).Object);
        var currentUser = new Mock<ICurrentUser>();
        currentUser.Setup(u => u.UserId).Returns(_actorId);
        var clock = new Mock<IDateTimeProvider>();
        clock.Setup(c => c.UtcNow).Returns(Now.AddHours(1));
        return new DisableShareLinkCommandHandler(context.Object, currentUser.Object, clock.Object, _audit.Object);
    }

    private void VerifyAudit(Times times) => _audit.Verify(a => a.RecordAsync(It.IsAny<Guid>(), _actorId, "DisableShareLink",
        It.IsAny<ResourceRef>(), It.IsAny<AuditMetadata>(), AuditSeverity.Info, It.IsAny<string>(), It.IsAny<string>(),
        It.IsAny<CancellationToken>(), It.IsAny<AuditChange?>()), times);

    [Fact]
    public async Task Handle_TurnsTheLinkOff_AndRecordsOneAuditFact()
    {
        var link = SeedLink();

        var result = await CreateSut().Handle(new DisableShareLinkCommand("work-management.board", _boardId, link.Id), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        link.Status.Should().Be(ShareLinkStatus.Disabled);
        VerifyAudit(Times.Once());
    }

    [Fact]
    public async Task Handle_OnAnAlreadyOffLink_IsANoOp_WithoutASecondAuditFact()
    {
        var link = SeedLink();
        link.Disable(_actorId, Now);
        var version = link.Version;

        var result = await CreateSut().Handle(new DisableShareLinkCommand("work-management.board", _boardId, link.Id), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        link.Version.Should().Be(version);
        VerifyAudit(Times.Never());
    }

    [Fact]
    public async Task Handle_OnAnExpiredLink_TurnsItOffAndAuditsTheExpiredBeforeState()
    {
        var link = SeedLink(ShareLinkStatus.Expired);
        AuditChange? recorded = null;
        _audit.Setup(a => a.RecordAsync(It.IsAny<Guid>(), _actorId, "DisableShareLink", It.IsAny<ResourceRef>(),
                It.IsAny<AuditMetadata>(), AuditSeverity.Info, It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>(), It.IsAny<AuditChange?>()))
            .Callback<Guid, Guid, string, ResourceRef, AuditMetadata, AuditSeverity, string, string, CancellationToken, AuditChange?>(
                (_, _, _, _, _, _, _, _, _, change) => recorded = change)
            .Returns(Task.CompletedTask);

        await CreateSut().Handle(new DisableShareLinkCommand("work-management.board", _boardId, link.Id), CancellationToken.None);

        link.Status.Should().Be(ShareLinkStatus.Disabled);
        recorded!.Before.ToString().Should().Contain("Expired");
        recorded.After.ToString().Should().Contain("Disabled");
    }

    [Fact]
    public async Task Handle_OfALinkOfAnotherBoard_IsNotFound_AndNothingIsAudited()
    {
        var link = SeedLink();

        var act = () => CreateSut().Handle(new DisableShareLinkCommand("work-management.board", Guid.CreateVersion7(), link.Id), CancellationToken.None);

        await act.Should().ThrowAsync<Notrelix.Application.Common.Exceptions.NotFoundException>();
        link.Status.Should().Be(ShareLinkStatus.Active);
        VerifyAudit(Times.Never());
    }
}
