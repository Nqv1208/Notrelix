using FluentAssertions;
using Notrelix.Application.Common.Context;
using Notrelix.Application.Common.Exceptions;
using Notrelix.Application.Common.Time;
using Notrelix.Application.Features.Chat.Abstractions;
using Notrelix.Application.Features.Chat.Channels.Commands.CreateChannel;
using Notrelix.Application.Features.Chat.Channels.Commands.UpdateChannel;
using Notrelix.Application.Features.Chat.Channels.Queries.ListChannels;
using Notrelix.Application.Features.Chat.Members.Commands.JoinChannel;
using Notrelix.Application.Features.Chat.Members.Commands.RemoveChannelMember;
using Notrelix.Application.Features.Workspaces.Public.Members;
using Notrelix.Application.Tests.Features.Collaboration;
using Notrelix.Domain.Chat.Rules;
using Notrelix.Domain.Chat.Spaces;
using Space = Notrelix.Domain.Chat.Spaces.Space;

namespace Notrelix.Application.Tests.Features.Chat;

/// <summary>Channel use cases UC-CHT-A01 to A07 at the handler level: visibility, manager rules, guests, last manager.</summary>
public class ChannelHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 10, 9, 0, 0, TimeSpan.Zero);

    private readonly Guid _workspaceId = Guid.NewGuid();
    private readonly Guid _accountId = Guid.NewGuid();
    private readonly Guid _owner = Guid.NewGuid();
    private readonly Guid _member = Guid.NewGuid();
    private readonly Guid _guest = Guid.NewGuid();
    private readonly Guid _outsider = Guid.NewGuid();

    private readonly Mock<ICurrentRequestContext> _context = new();
    private readonly Mock<IDateTimeProvider> _clock = new();
    private readonly Mock<IWorkspaceMemberFacts> _members = new();
    private readonly List<Space> _spaces = [];
    private readonly List<Membership> _memberships = [];
    private readonly List<ChannelGroup> _groups = [];
    private readonly Mock<IChatDbContext> _db = new();

    public ChannelHandlerTests()
    {
        _clock.Setup(c => c.UtcNow).Returns(Now);
        _context.Setup(c => c.RequireAccountId()).Returns(_accountId);
        _context.Setup(c => c.RequireWorkspaceId()).Returns(_workspaceId);
        _context.Setup(c => c.IsAuthenticated).Returns(true);

        _db.Setup(d => d.ChatSpaces).Returns(TestDbSet.Create(_spaces).Object);
        _db.Setup(d => d.ChatMemberships).Returns(TestDbSet.Create(_memberships).Object);
        _db.Setup(d => d.ChatChannelGroups).Returns(TestDbSet.Create(_groups).Object);

        SetCaller(_owner, "Owner");
        _members.Setup(m => m.FindAsync(_workspaceId, _member, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkspaceMemberFact(_member, "Member", true));
        _members.Setup(m => m.FindAsync(_workspaceId, _guest, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkspaceMemberFact(_guest, "Guest", true));
        _members.Setup(m => m.FindAsync(_workspaceId, _outsider, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkspaceMemberFact(_outsider, "Member", true));
    }

    private void SetCaller(Guid userId, string role)
    {
        _context.Setup(c => c.UserId).Returns(userId);
        _members.Setup(m => m.FindAsync(_workspaceId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkspaceMemberFact(userId, role, true));
    }

    private void AsCaller(Guid userId) => _context.Setup(c => c.UserId).Returns(userId);

    private Space PublicChannel(string name = "general")
    {
        var space = Space.CreateChannel(_accountId, _workspaceId, SpaceKind.ChannelPublic, name, null, null, 1, _owner, Now);
        _spaces.Add(space);
        return space;
    }

    private Space PrivateChannel(string name = "secret")
    {
        var space = Space.CreateChannel(_accountId, _workspaceId, SpaceKind.ChannelPrivate, name, null, null, 1, _owner, Now);
        _spaces.Add(space);
        return space;
    }

    private CreateChannelCommandHandler CreateHandler() => new(_db.Object, _context.Object, _clock.Object);
    private JoinChannelCommandHandler JoinHandler() => new(_db.Object, _context.Object, _members.Object, _clock.Object);
    private RemoveChannelMemberCommandHandler RemoveHandler() => new(_db.Object, _context.Object, _members.Object, _clock.Object);
    private UpdateChannelCommandHandler UpdateHandler() => new(_db.Object, _context.Object, _members.Object, _clock.Object);
    private ListChannelsQueryHandler ListHandler() => new(_db.Object, _context.Object, _members.Object);

    [Fact]
    public async Task CreateChannel_AddsAPublicChannelWithTheCallerAsManager()
    {
        var result = await CreateHandler().Handle(
            new CreateChannelCommand(_workspaceId, "  design ", "Ideas", false, null), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data!.Name.Should().Be("design");
        result.Data!.Kind.Should().Be("channel_public");
        result.Data!.IsMember.Should().BeTrue();
        result.Data!.Role.Should().Be("manager");
        _spaces.Should().ContainSingle(s => s.Name == "design" && s.IsPublic);
    }

    [Fact]
    public async Task CreateChannel_RefusesADuplicateNameIgnoringCase()
    {
        PublicChannel("General");

        var act = () => CreateHandler().Handle(
            new CreateChannelCommand(_workspaceId, "general", null, false, null), CancellationToken.None);

        var error = await act.Should().ThrowAsync<ConflictException>();
        error.Which.ErrorCode.Should().Be(ChatRuleCodes.NameConflict);
    }

    [Fact]
    public async Task CreateChannel_AllowsTheNameOfAnArchivedChannel()
    {
        var old = PublicChannel("launch");
        old.SetArchived(true, _owner, Now);

        var result = await CreateHandler().Handle(
            new CreateChannelCommand(_workspaceId, "launch", null, false, null), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task JoinChannel_OfAPrivateChannelIsNotFoundForANonMember()
    {
        var secret = PrivateChannel();
        AsCaller(_outsider);

        var act = () => JoinHandler().Handle(new JoinChannelCommand(_workspaceId, secret.Id), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        secret.IsActiveMember(_outsider).Should().BeFalse();
    }

    [Fact]
    public async Task JoinChannel_IsForbiddenForAGuest()
    {
        var general = PublicChannel();
        AsCaller(_guest);

        var act = () => JoinHandler().Handle(new JoinChannelCommand(_workspaceId, general.Id), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task JoinChannel_AddsAMemberToAPublicChannel()
    {
        var general = PublicChannel();
        AsCaller(_member);

        var result = await JoinHandler().Handle(new JoinChannelCommand(_workspaceId, general.Id), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data!.IsMember.Should().BeTrue();
        result.Data!.Role.Should().Be("member");
        general.IsActiveMember(_member).Should().BeTrue();
    }

    [Fact]
    public async Task ListChannels_BrowseIsEmptyForAGuest()
    {
        PublicChannel();
        AsCaller(_guest);

        var result = await ListHandler().Handle(new ListChannelsQuery(_workspaceId, Browse: true), CancellationToken.None);

        result.Data!.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateChannel_RefusesAPlainMemberOfAPublicChannel()
    {
        var general = PublicChannel();
        general.AddMember(_member, MembershipRole.Member, Now);
        AsCaller(_member);

        var act = () => UpdateHandler().Handle(
            new UpdateChannelCommand(_workspaceId, general.Id, "renamed", null, general.Version), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
        general.Name.Should().Be("general");
    }

    [Fact]
    public async Task UpdateChannel_OfAPrivateChannelIsNotFoundForAnOutsider()
    {
        var secret = PrivateChannel();
        AsCaller(_outsider);

        var act = () => UpdateHandler().Handle(
            new UpdateChannelCommand(_workspaceId, secret.Id, "renamed", null, secret.Version), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task UpdateChannel_LetsAWorkspaceAdminManageAPublicChannel()
    {
        var general = PublicChannel();
        var admin = Guid.NewGuid();
        SetCaller(admin, "Admin");
        AsCaller(admin);

        var result = await UpdateHandler().Handle(
            new UpdateChannelCommand(_workspaceId, general.Id, "renamed", "New topic", general.Version), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        general.Name.Should().Be("renamed");
    }

    [Fact]
    public async Task RemoveChannelMember_CannotLeaveAsTheLastManager()
    {
        var general = PublicChannel();
        general.AddMember(_member, MembershipRole.Member, Now);
        AsCaller(_owner);

        var act = () => RemoveHandler().Handle(
            new RemoveChannelMemberCommand(_workspaceId, general.Id, _owner), CancellationToken.None);

        var error = await act.Should().ThrowAsync<Notrelix.Domain.Common.Exceptions.BusinessRuleException>();
        error.Which.RuleCode.Should().Be(ChatRuleCodes.LastManager);
        general.IsActiveMember(_owner).Should().BeTrue();
    }

    [Fact]
    public async Task RemoveChannelMember_AMemberMayLeaveOnTheirOwn()
    {
        var general = PublicChannel();
        general.AddMember(_member, MembershipRole.Member, Now);
        AsCaller(_member);

        var result = await RemoveHandler().Handle(
            new RemoveChannelMemberCommand(_workspaceId, general.Id, _member), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        general.IsActiveMember(_member).Should().BeFalse();
    }

    [Fact]
    public async Task RemoveChannelMember_AMemberMayNotRemoveAnotherMember()
    {
        var general = PublicChannel();
        var third = Guid.NewGuid();
        general.AddMember(_member, MembershipRole.Member, Now);
        general.AddMember(third, MembershipRole.Member, Now);
        AsCaller(_member);

        var act = () => RemoveHandler().Handle(
            new RemoveChannelMemberCommand(_workspaceId, general.Id, third), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
        general.IsActiveMember(third).Should().BeTrue();
    }
}
