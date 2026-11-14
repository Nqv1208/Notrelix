using Notrelix.Application.Features.Chat.Abstractions;
using Notrelix.Application.Features.Chat.Notifications.Services;
using Notrelix.Application.Tests.Features.Collaboration;
using Notrelix.Domain.Chat.Spaces;
using Space = Notrelix.Domain.Chat.Spaces.Space;

namespace Notrelix.Application.Tests.Features.Chat.Notifications;

/// <summary>The Inbox shows a chat notification only while its recipient is an active member of a live space.</summary>
public class ChatSpaceViewFactsTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 12, 9, 0, 0, TimeSpan.Zero);
    private readonly Guid _account = Guid.NewGuid();
    private readonly Guid _workspace = Guid.NewGuid();
    private readonly Guid _me = Guid.NewGuid();
    private readonly Guid _other = Guid.NewGuid();

    [Fact]
    public async Task Lists_the_spaces_the_person_is_an_active_member_of_and_names_channels_only()
    {
        var joined = Space.CreateChannel(_account, _workspace, SpaceKind.ChannelPrivate, "ops", null, null, 1, _me, Now);
        var left = Space.CreateChannel(_account, _workspace, SpaceKind.ChannelPublic, "old", null, null, 2, _other, Now);
        left.AddMember(_me, MembershipRole.Member, Now);
        left.RemoveMember(_me, Now.AddHours(1));
        var strangers = Space.CreateChannel(_account, _workspace, SpaceKind.ChannelPublic, "x", null, null, 3, _other, Now);
        var direct = Space.CreateDirect(_account, _workspace, _other, _me, Now);
        var gone = Space.CreateChannel(_account, _workspace, SpaceKind.ChannelPublic, "gone", null, null, 4, _me, Now);
        gone.Delete(_me, Now);
        var otherWorkspace = Space.CreateChannel(_account, Guid.NewGuid(), SpaceKind.ChannelPublic, "elsewhere", null, null, 5, _me, Now);
        var spaces = new List<Space> { joined, left, strangers, direct, gone, otherWorkspace };
        var db = new Mock<IChatDbContext>();
        db.Setup(d => d.ChatSpaces).Returns(() => TestDbSet.Create(spaces).Object);
        db.Setup(d => d.ChatMemberships).Returns(() => TestDbSet.Create(spaces.SelectMany(s => s.Memberships).ToList()).Object);

        var result = await new ChatSpaceViewFacts(db.Object).ViewableSpacesAsync(
            _workspace, _me, spaces.Select(s => s.Id).ToList(), CancellationToken.None);

        result.Should().BeEquivalentTo(new Dictionary<Guid, string?> { [joined.Id] = "ops", [direct.Id] = null });
    }
}
