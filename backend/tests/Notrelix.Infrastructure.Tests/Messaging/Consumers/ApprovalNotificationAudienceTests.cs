using MassTransit;
using Notrelix.Application.Events.WorkManagement;
using Notrelix.Application.Features.Governance.Public.Visibility;
using Notrelix.Application.Features.Workspaces.Public.Teams;
using Notrelix.Application.Features.WorkManagement.Public.ItemNotifications;
using Notrelix.Infrastructure.Messaging.Consumers.Collaboration;

namespace Notrelix.Infrastructure.Tests.Messaging.Consumers;

/// <summary>A team approval request reaches only the members who can see the Item's Board, never the requester.</summary>
public sealed class ApprovalNotificationAudienceTests
{
    private static readonly Guid Workspace = Guid.NewGuid();
    private static readonly Guid Board = Guid.NewGuid();
    private static readonly Guid Item = Guid.NewGuid();
    private static readonly Guid Seer = Guid.NewGuid();
    private static readonly Guid Blind = Guid.NewGuid();
    private static readonly Guid Requester = Guid.NewGuid();

    private static ItemAudience Audience(bool itemExists = true)
    {
        var work = new Mock<IWorkItemNotificationFacts>();
        work.Setup(w => w.FindAsync(Workspace, Item, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(itemExists ? new WorkItemNotificationFacts(Board, "Item", false, null) : null);
        var visibility = new Mock<IResourceVisibility>();
        visibility.Setup(v => v.VisibleBoardIdsAsync(Workspace, It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { Guid.NewGuid() });
        visibility.Setup(v => v.VisibleBoardIdsAsync(Workspace, Seer, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { Board });
        return new ItemAudience(work.Object, visibility.Object);
    }

    [Fact]
    public async Task MemberWhoCanSeeTheBoard_IsKept_AndMemberWhoCannot_IsDropped()
    {
        var kept = await Audience().WhoCanSeeAsync(Workspace, Item, [Seer, Blind], CancellationToken.None);

        kept.Should().Equal(Seer);
    }

    [Fact]
    public async Task ItemThatIsGone_NotifiesNobody()
    {
        var kept = await Audience(itemExists: false).WhoCanSeeAsync(Workspace, Item, [Seer], CancellationToken.None);

        kept.Should().BeEmpty();
    }

    [Fact]
    public async Task TeamConsumer_NeverHandsTheRequesterToTheNotifier()
    {
        var teams = new Mock<ITeamMembershipFacts>();
        var team = Guid.NewGuid();
        teams.Setup(t => t.CurrentMemberIdsAsync(Workspace, team, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { Seer, Requester });
        var notifier = new Mock<ItemFollowerNotifier>(null!, null!, null!, null!);
        IReadOnlyCollection<Guid>? sent = null;
        notifier.Setup(n => n.NotifyAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<Guid>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<IReadOnlyCollection<Guid>>(),
                It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .Callback<Guid, Guid, Guid, Guid?, Guid, string, string, string?, IReadOnlyCollection<Guid>, DateTimeOffset, CancellationToken>(
                (_, _, _, _, _, _, _, _, recipients, _, _) => sent = recipients)
            .Returns(Task.CompletedTask);
        var message = new ApprovalTeamRequestedIntegrationEvent(
            Guid.NewGuid(), Guid.NewGuid(), Workspace, Guid.NewGuid(), ItemFollowerNotifier.BoardItemKind, Item,
            team, Requester, "Item", "note", Guid.NewGuid());
        var context = new Mock<ConsumeContext<ApprovalTeamRequestedIntegrationEvent>>();
        context.SetupGet(c => c.Message).Returns(message);
        context.SetupGet(c => c.CancellationToken).Returns(CancellationToken.None);

        await new ApprovalTeamRequestedNotificationConsumer(notifier.Object, teams.Object).Consume(context.Object);

        sent.Should().Equal(Seer);
    }
}
