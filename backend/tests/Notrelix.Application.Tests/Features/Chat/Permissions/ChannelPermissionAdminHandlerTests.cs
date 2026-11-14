using Notrelix.Application.Common.Auditing;
using Notrelix.Application.Features.Chat.Abstractions;
using Notrelix.Application.Features.Chat.Audit.Services;
using Notrelix.Application.Features.Chat.Messages.Commands.ModerateDeleteMessage;
using Notrelix.Application.Features.Chat.Permissions.Commands.ClearChannelPermissions;
using Notrelix.Application.Features.Chat.Permissions.Commands.SetChannelPermissions;
using Notrelix.Application.Features.Chat.Permissions.DTOs;
using Notrelix.Application.Features.Chat.Permissions.Queries.GetChannelPermissions;
using Notrelix.Application.Features.Workspaces.Public.Members;
using Notrelix.Domain.Chat.Messages;
using Notrelix.Domain.Chat.Rules;
using Notrelix.Domain.Chat.Spaces;
using Notrelix.Domain.Common.Auditing;
using Notrelix.Testing.Application.Fakes;
using Notrelix.Application.Tests.Features.Collaboration;
using AppConflictException = Notrelix.Application.Common.Exceptions.ConflictException;
using AppForbiddenException = Notrelix.Application.Common.Exceptions.ForbiddenException;
using AppNotFoundException = Notrelix.Application.Common.Exceptions.NotFoundException;
using BusinessRuleException = Notrelix.Domain.Common.Exceptions.BusinessRuleException;
using Space = Notrelix.Domain.Chat.Spaces.Space;

namespace Notrelix.Application.Tests.Features.Chat.Permissions;

/// <summary>
/// UC-CHT-H01 (set, clear, read overrides), UC-CHT-H03 (moderator delete) and UC-CHT-H04 (Audit facts) at handler level. The stale
/// expectedVersion (412) is decided by the pipeline and proved in the architecture and integration tests.
/// </summary>
public class ChannelPermissionAdminHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 12, 9, 0, 0, TimeSpan.Zero);

    private readonly Guid _workspaceId = Guid.NewGuid();
    private readonly Guid _accountId = Guid.NewGuid();
    private readonly Guid _owner = Guid.NewGuid();
    private readonly Guid _manager = Guid.NewGuid();
    private readonly Guid _member = Guid.NewGuid();
    private readonly Guid _outsider = Guid.NewGuid();

    private readonly Mock<ICurrentRequestContext> _context = new();
    private readonly Mock<IDateTimeProvider> _clock = new();
    private readonly Mock<IWorkspaceMemberFacts> _members = new();
    private readonly Mock<IAuditService> _audit = new();
    private readonly List<(string Action, ResourceRef Target, AuditChange? Change, Guid Actor)> _facts = [];
    private readonly List<Space> _spaces = [];
    private readonly List<Message> _messages = [];
    private readonly Mock<IChatDbContext> _db = new();
    private readonly Mock<ISpaceSequenceAllocator> _sequences = new();
    private long _lastSequence;

    public ChannelPermissionAdminHandlerTests()
    {
        _clock.Setup(c => c.UtcNow).Returns(Now);
        _context.Setup(c => c.RequireAccountId()).Returns(_accountId);
        _context.Setup(c => c.RequireWorkspaceId()).Returns(_workspaceId);
        _context.Setup(c => c.IsAuthenticated).Returns(true);
        _context.Setup(c => c.UserId).Returns(_manager);

        _db.Setup(d => d.ChatSpaces).Returns(() => TestDbSet.Create(_spaces).Object);
        _db.Setup(d => d.ChatMemberships).Returns(() => TestDbSet.Create(_spaces.SelectMany(s => s.Memberships).ToList()).Object);
        _db.Setup(d => d.ChatPermissionOverrides).Returns(() => TestDbSet.Create(_spaces.SelectMany(s => s.PermissionOverrides).ToList()).Object);
        _db.Setup(d => d.ChatMessages).Returns(() => TestDbSet.Create(_messages).Object);
        _db.Setup(d => d.ChatPins).Returns(TestDbSet.Create(new List<Pin>()).Object);
        _db.Setup(d => d.ChatReactions).Returns(TestDbSet.Create(new List<Reaction>()).Object);
        _db.Setup(d => d.ChatMessageMentions).Returns(TestDbSet.Create(new List<MessageMention>()).Object);
        _sequences.Setup(s => s.LockAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        _audit.Setup(a => a.RecordAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<ResourceRef>(), It.IsAny<AuditMetadata>(),
                It.IsAny<AuditSeverity>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<AuditChange?>()))
            .Callback((Guid _, Guid actor, string action, ResourceRef target, AuditMetadata _, AuditSeverity _, string _, string _, CancellationToken _, AuditChange? change) =>
                _facts.Add((action, target, change, actor)))
            .Returns(Task.CompletedTask);

        Fact(_owner, "Owner");
        Fact(_manager, "Member");
        Fact(_member, "Member");
        Fact(_outsider, "Admin");
    }

    private void Fact(Guid userId, string role) =>
        _members.Setup(m => m.FindAsync(_workspaceId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkspaceMemberFact(userId, role, true));

    private void AsUser(Guid userId) => _context.Setup(c => c.UserId).Returns(userId);

    private Space Channel(SpaceKind kind = SpaceKind.ChannelPublic)
    {
        var space = Space.CreateChannel(_accountId, _workspaceId, kind, "announcements", null, null, 1, _owner, Now);
        space.AddMember(_manager, MembershipRole.Manager, Now);
        space.AddMember(_member, MembershipRole.Member, Now);
        _spaces.Add(space);
        return space;
    }

    private Message Text(Space space, Guid author, string content = "private words")
    {
        var message = Message.CreateUserMessage(_accountId, _workspaceId, space.Id, ++_lastSequence, author, content, $"c-{Guid.NewGuid():N}", null, Now);
        _messages.Add(message);
        return message;
    }

    private ChatAuditFacts Audit() => new(_audit.Object, _context.Object);

    private SetChannelPermissionsCommandHandler SetHandler() => new(_db.Object, _context.Object, _members.Object, _clock.Object, Audit());
    private ClearChannelPermissionsCommandHandler ClearHandler() => new(_db.Object, _context.Object, _members.Object, _clock.Object, Audit());
    private GetChannelPermissionsQueryHandler GetHandler() => new(_db.Object, _context.Object, _members.Object);
    private ModerateDeleteMessageCommandHandler ModerateHandler() =>
        new(_db.Object, _context.Object, _members.Object, _sequences.Object, _clock.Object, new InMemoryChatMessageAttachments(), Audit());

    private static ChannelPermissionEntryDto E(string subject, string action, string effect) => new(subject, action, effect);

    private Task<Notrelix.Application.Common.Models.Result<ChannelPermissionsDto>> Set(Space space, params ChannelPermissionEntryDto[] entries) =>
        SetHandler().Handle(new SetChannelPermissionsCommand(_workspaceId, space.Id, entries, space.Version), CancellationToken.None);

    // ---- Set (UC-CHT-H01) ---------------------------------------------------------------------

    [Fact]
    public async Task A_channel_manager_makes_the_channel_read_only_and_one_audit_fact_is_written()
    {
        var space = Channel();
        var before = space.Version;

        var result = await Set(space, E("member", "post", "deny"), E("guest", "post", "deny"));

        result.Succeeded.Should().BeTrue();
        result.Data!.Entries.Should().HaveCount(2);
        result.Data.Version.Should().Be(before + 1);
        space.PermissionOverrides.Should().HaveCount(2);
        _facts.Should().ContainSingle().Which.Should().Match<(string Action, ResourceRef Target, AuditChange? Change, Guid Actor)>(
            f => f.Action == ChatAuditFacts.PermissionsSet && f.Target.ResourceId == space.Id && f.Actor == _manager);
    }

    [Fact]
    public async Task Setting_the_stored_set_again_changes_nothing_and_writes_no_second_fact()
    {
        var space = Channel();
        await Set(space, E("member", "post", "deny"));

        var again = await Set(space, E("member", "post", "deny"));

        again.Succeeded.Should().BeTrue();
        _facts.Should().ContainSingle();
    }

    [Fact]
    public async Task A_deny_beats_an_allow_for_the_same_role_and_action()
    {
        var space = Channel();

        var result = await Set(space, E("member", "post", "allow"), E("member", "post", "deny"));

        result.Data!.Entries.Should().ContainSingle().Which.Effect.Should().Be("deny");
    }

    [Fact]
    public async Task A_plain_member_cannot_set_overrides()
    {
        var space = Channel();
        AsUser(_member);

        var act = () => Set(space, E("member", "post", "deny"));

        await act.Should().ThrowAsync<AppForbiddenException>();
        space.PermissionOverrides.Should().BeEmpty();
        _facts.Should().BeEmpty();
    }

    [Fact]
    public async Task A_workspace_admin_who_is_not_in_a_private_channel_does_not_find_it()
    {
        var space = Channel(SpaceKind.ChannelPrivate);
        AsUser(_outsider);

        var act = () => Set(space, E("member", "post", "deny"));

        await act.Should().ThrowAsync<AppNotFoundException>();
    }

    [Fact]
    public async Task An_allow_above_what_the_role_holds_is_refused()
    {
        var space = Channel();

        // A Guest holds no pin action by default (the Workspace baseline); an override never lifts it.
        var act = () => Set(space, E("guest", "pin", "allow"));

        var error = await act.Should().ThrowAsync<BusinessRuleException>();
        error.Which.RuleCode.Should().Be(ChatRuleCodes.PermissionExceedsRole);
        _facts.Should().BeEmpty();
    }

    [Fact]
    public async Task A_manager_cannot_be_denied_inviting()
    {
        var space = Channel();

        var act = () => Set(space, E("manager", "invite", "deny"));

        var error = await act.Should().ThrowAsync<BusinessRuleException>();
        error.Which.RuleCode.Should().Be(ChatRuleCodes.PermissionLockout);
    }

    [Fact]
    public async Task An_archived_channel_refuses_a_change_of_its_overrides()
    {
        var space = Channel();
        space.SetArchived(true, _owner, Now);

        var act = () => Set(space, E("member", "post", "deny"));

        var error = await act.Should().ThrowAsync<BusinessRuleException>();
        error.Which.RuleCode.Should().Be(ChatRuleCodes.ChannelArchived);
    }

    [Fact]
    public void The_validator_refuses_an_unknown_role_action_or_effect_and_an_admin_subject()
    {
        var validator = new SetChannelPermissionsCommandValidator();
        SetChannelPermissionsCommand Cmd(ChannelPermissionEntryDto e) => new(_workspaceId, Guid.NewGuid(), [e], 1);

        validator.Validate(Cmd(E("member", "post", "deny"))).IsValid.Should().BeTrue();
        validator.Validate(Cmd(E("admin", "post", "deny"))).IsValid.Should().BeFalse("a Workspace admin is never a subject");
        validator.Validate(Cmd(E("member", "manage", "deny"))).IsValid.Should().BeFalse();
        validator.Validate(Cmd(E("member", "post", "maybe"))).IsValid.Should().BeFalse();
        validator.Validate(new SetChannelPermissionsCommand(_workspaceId, Guid.NewGuid(), [], 0)).IsValid.Should().BeFalse("a version is required");
    }

    // ---- Clear and Get ------------------------------------------------------------------------

    [Fact]
    public async Task Clearing_removes_every_override_and_writes_a_cleared_fact_once()
    {
        var space = Channel();
        await Set(space, E("member", "post", "deny"));
        _facts.Clear();

        var result = await ClearHandler().Handle(new ClearChannelPermissionsCommand(_workspaceId, space.Id, space.Version), CancellationToken.None);

        result.Data!.Entries.Should().BeEmpty();
        _facts.Should().ContainSingle().Which.Action.Should().Be(ChatAuditFacts.PermissionsCleared);

        _facts.Clear();
        await ClearHandler().Handle(new ClearChannelPermissionsCommand(_workspaceId, space.Id, space.Version), CancellationToken.None);
        _facts.Should().BeEmpty("a channel with no overrides changes nothing");
    }

    [Fact]
    public async Task A_member_reads_the_overrides_and_the_version()
    {
        var space = Channel();
        await Set(space, E("member", "post", "deny"));
        AsUser(_member);

        var result = await GetHandler().Handle(new GetChannelPermissionsQuery(_workspaceId, space.Id), CancellationToken.None);

        result.Data!.Entries.Should().ContainSingle().Which.Should().Be(E("member", "post", "deny"));
        result.Data.Version.Should().Be(space.Version);
    }

    [Fact]
    public async Task A_private_channel_the_caller_is_not_in_is_not_found_on_read()
    {
        var space = Channel(SpaceKind.ChannelPrivate);
        AsUser(_outsider);

        var act = () => GetHandler().Handle(new GetChannelPermissionsQuery(_workspaceId, space.Id), CancellationToken.None);

        await act.Should().ThrowAsync<AppNotFoundException>();
    }

    // ---- Moderator delete (UC-CHT-H03, H04) ---------------------------------------------------

    [Fact]
    public async Task A_moderator_removes_a_members_message_with_a_reason_and_audit_records_actor_message_and_reason_once()
    {
        var space = Channel();
        var message = Text(space, _member, "private words");
        AsUser(_owner);

        var result = await ModerateHandler().Handle(
            new ModerateDeleteMessageCommand(_workspaceId, message.Id, "  spam  ", message.Version), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        message.IsDeleted.Should().BeTrue();
        message.Content.Should().BeNull();
        message.IsRemovedByModerator.Should().BeTrue();
        message.DeletedBy.Should().Be(_owner);
        var fact = _facts.Should().ContainSingle().Subject;
        fact.Action.Should().Be(ChatAuditFacts.MessageModerated);
        fact.Actor.Should().Be(_owner);
        fact.Target.ResourceId.Should().Be(message.Id);
        fact.Change!.SubjectId.Should().Be(_member);
        System.Text.Json.JsonSerializer.Serialize(fact.Change.After).Should().Contain("spam").And.NotContain("private words");
    }

    [Fact]
    public async Task Moderating_a_message_that_is_already_a_tombstone_changes_nothing_and_writes_no_second_fact()
    {
        var space = Channel();
        var message = Text(space, _member);
        AsUser(_owner);
        await ModerateHandler().Handle(new ModerateDeleteMessageCommand(_workspaceId, message.Id, "spam", message.Version), CancellationToken.None);

        var again = await ModerateHandler().Handle(new ModerateDeleteMessageCommand(_workspaceId, message.Id, "spam", message.Version), CancellationToken.None);

        again.Succeeded.Should().BeTrue();
        _facts.Should().ContainSingle();
    }

    [Fact]
    public async Task A_moderator_does_not_reach_a_private_channel_by_role()
    {
        var space = Channel(SpaceKind.ChannelPrivate);
        var message = Text(space, _member);
        AsUser(_outsider);

        var act = () => ModerateHandler().Handle(new ModerateDeleteMessageCommand(_workspaceId, message.Id, "spam", message.Version), CancellationToken.None);

        await act.Should().ThrowAsync<AppNotFoundException>();
        message.IsDeleted.Should().BeFalse();
        _facts.Should().BeEmpty();
    }

    [Fact]
    public async Task A_moderator_cannot_remove_their_own_message_or_a_system_message_or_in_an_archived_channel()
    {
        var space = Channel();
        var own = Text(space, _owner);
        var system = Message.CreateSystemMessage(_accountId, _workspaceId, space.Id, ++_lastSequence, _owner, "joined the channel", Now);
        _messages.Add(system);
        AsUser(_owner);

        var onOwn = () => ModerateHandler().Handle(new ModerateDeleteMessageCommand(_workspaceId, own.Id, "x", own.Version), CancellationToken.None);
        var onSystem = () => ModerateHandler().Handle(new ModerateDeleteMessageCommand(_workspaceId, system.Id, "x", system.Version), CancellationToken.None);
        (await onOwn.Should().ThrowAsync<BusinessRuleException>()).Which.RuleCode.Should().Be(ChatRuleCodes.SelfTarget);
        (await onSystem.Should().ThrowAsync<BusinessRuleException>()).Which.RuleCode.Should().Be(ChatRuleCodes.SystemMessageImmutable);

        var other = Text(space, _member);
        space.SetArchived(true, _owner, Now);
        var onArchived = () => ModerateHandler().Handle(new ModerateDeleteMessageCommand(_workspaceId, other.Id, "x", other.Version), CancellationToken.None);
        (await onArchived.Should().ThrowAsync<AppConflictException>()).Which.ErrorCode.Should().Be(ChatRuleCodes.ChannelArchived);
        _facts.Should().BeEmpty();
    }

    [Fact]
    public void The_moderation_validator_needs_a_reason_within_the_limit_and_a_version()
    {
        var validator = new ModerateDeleteMessageCommandValidator();

        validator.Validate(new ModerateDeleteMessageCommand(_workspaceId, Guid.NewGuid(), "spam", 1)).IsValid.Should().BeTrue();
        validator.Validate(new ModerateDeleteMessageCommand(_workspaceId, Guid.NewGuid(), "   ", 1)).IsValid.Should().BeFalse();
        validator.Validate(new ModerateDeleteMessageCommand(_workspaceId, Guid.NewGuid(), new string('x', ChatLimits.ModerationReasonMaxLength + 1), 1)).IsValid.Should().BeFalse();
        validator.Validate(new ModerateDeleteMessageCommand(_workspaceId, Guid.NewGuid(), "spam", 0)).IsValid.Should().BeFalse();
    }

    // ---- Audit of channel administration (UC-CHT-H04) -----------------------------------------

    [Fact]
    public async Task The_audit_facts_name_the_actor_the_target_and_never_the_message_content()
    {
        var space = Channel();
        var facts = Audit();

        await facts.ChannelCreatedAsync(space, CancellationToken.None);
        await facts.ChannelArchivedAsync(space, true, CancellationToken.None);
        await facts.ChannelArchivedAsync(space, false, CancellationToken.None);
        await facts.ChannelDeletedAsync(space, CancellationToken.None);
        await facts.MemberAddedAsync(space, _member, CancellationToken.None);
        await facts.MemberRemovedAsync(space, _member, CancellationToken.None);

        _facts.Select(f => f.Action).Should().Equal(
            ChatAuditFacts.ChannelCreated, ChatAuditFacts.ChannelArchived, ChatAuditFacts.ChannelUnarchived,
            ChatAuditFacts.ChannelDeleted, ChatAuditFacts.MemberAdded, ChatAuditFacts.MemberRemoved);
        _facts.Should().OnlyContain(f => f.Actor == _manager && f.Target.ResourceId == space.Id);
    }
}
