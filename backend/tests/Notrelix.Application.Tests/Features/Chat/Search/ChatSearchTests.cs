using Notrelix.Application.Features.Chat.Abstractions;
using Notrelix.Application.Features.Chat.Channels.Services;
using Notrelix.Application.Features.Chat.RateLimits;
using Notrelix.Application.Features.Chat.Search.Queries.SearchChatMessages;
using Notrelix.Application.Features.Chat.Search.Services;
using Notrelix.Application.Features.Workspaces.Public.Members;
using Notrelix.Domain.Chat.Messages;
using Notrelix.Domain.Chat.Retention;
using Notrelix.Domain.Chat.Spaces;
using Notrelix.Application.Tests.Features.Collaboration;
using Notrelix.Testing.Application.Fakes;
using AppNotFoundException = Notrelix.Application.Common.Exceptions.NotFoundException;
using Space = Notrelix.Domain.Chat.Spaces.Space;

namespace Notrelix.Application.Tests.Features.Chat.Search;

/// <summary>UC-CHT-H07: search messages. The permission matrix, the exclusions (tombstone, retention, system), the filters, the paging and the snippet.</summary>
public class ChatSearchTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 12, 9, 0, 0, TimeSpan.Zero);
    private readonly Guid _ws = Guid.NewGuid();
    private readonly Guid _account = Guid.NewGuid();
    private readonly Guid _member = Guid.NewGuid();
    private readonly Guid _other = Guid.NewGuid();
    private readonly Guid _admin = Guid.NewGuid();
    private readonly Guid _guest = Guid.NewGuid();
    private readonly Guid _stranger = Guid.NewGuid();
    private readonly List<Space> _spaces = [];
    private readonly List<Message> _messages = [];
    private readonly List<MessageMention> _mentions = [];
    private readonly List<RetentionPolicy> _policies = [];
    private readonly InMemoryChatMessageAttachments _files = new();
    private readonly Mock<IChatDbContext> _db = new();
    private readonly Mock<ICurrentRequestContext> _ctx = new();
    private readonly Mock<IWorkspaceMemberFacts> _members = new();
    private readonly Mock<IDateTimeProvider> _clock = new();
    private readonly Mock<IChatRateLimiter> _limits = new();
    private Guid _caller;
    private long _seq;

    public ChatSearchTests()
    {
        _caller = _member;
        _clock.Setup(c => c.UtcNow).Returns(Now);
        _ctx.Setup(c => c.RequireAccountId()).Returns(_account);
        _ctx.Setup(c => c.UserId).Returns(() => _caller);
        _db.Setup(d => d.ChatSpaces).Returns(() => TestDbSet.Create(_spaces.Where(s => s.DeletedAt == null).ToList()).Object);
        _db.Setup(d => d.ChatMemberships).Returns(() => TestDbSet.Create(_spaces.SelectMany(s => s.Memberships).ToList()).Object);
        _db.Setup(d => d.ChatMessages).Returns(() => TestDbSet.Create(_messages).Object);
        _db.Setup(d => d.ChatMessageMentions).Returns(() => TestDbSet.Create(_mentions).Object);
        _db.Setup(d => d.ChatRetentionPolicies).Returns(() => TestDbSet.Create(_policies).Object);
        foreach (var (id, role) in new[] { (_member, "Member"), (_other, "Member"), (_admin, "Admin"), (_guest, "Guest"), (_stranger, "Member") })
            _members.Setup(m => m.FindAsync(_ws, id, It.IsAny<CancellationToken>())).ReturnsAsync(new WorkspaceMemberFact(id, role, true));
        _limits.Setup(l => l.RequireAsync(It.IsAny<ChatRateLimitPolicy>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
    }

    private SearchChatMessagesQueryHandler Handler() => new(_db.Object, _ctx.Object, _members.Object, _clock.Object, _limits.Object, _files);

    private Space Channel(string name, SpaceKind kind, params Guid[] members)
    {
        var space = Space.CreateChannel(_account, _ws, kind, name, null, null, 1, members.Length > 0 ? members[0] : _other, Now.AddDays(-60));
        foreach (var m in members.Skip(1)) space.AddMember(m, MembershipRole.Member, Now.AddDays(-59));
        _spaces.Add(space);
        return space;
    }

    private Message Say(Space space, Guid author, string content, int ageMinutes = 10)
    {
        var m = Message.CreateUserMessage(_account, _ws, space.Id, ++_seq, author, content, $"c{_seq}", null, Now.AddMinutes(-ageMinutes));
        _messages.Add(m);
        return m;
    }

    private Task<IReadOnlyList<Guid>> Ids(string q, Action<SearchOptions>? configure = null) => Run(q, configure);

    private sealed class SearchOptions
    {
        public Guid? Space, Author;
        public DateTimeOffset? From, To;
        public bool? HasAttachment, MentionsMe;
        public string? Cursor;
        public int Limit = 20;
    }

    private async Task<IReadOnlyList<Guid>> Run(string q, Action<SearchOptions>? configure = null)
    {
        var o = new SearchOptions();
        configure?.Invoke(o);
        var result = await Handler().Handle(new SearchChatMessagesQuery(_ws, q, o.Space, o.Author, o.From, o.To, o.HasAttachment, o.MentionsMe, o.Cursor, o.Limit), default);
        return result.Data!.Items.Select(i => i.MessageId).ToList();
    }

    // ---- permission matrix ----------------------------------------------------------------------

    [Fact]
    public async Task AMemberFindsAPublicChannelMessage_AndAPrivateOneTheyBelongTo()
    {
        var pub = Channel("general", SpaceKind.ChannelPublic, _other);
        var priv = Channel("secret", SpaceKind.ChannelPrivate, _other, _member);
        var a = Say(pub, _other, "invoice approved");
        var b = Say(priv, _other, "invoice secret");

        (await Ids("invoice")).Should().BeEquivalentTo([a.Id, b.Id]);
    }

    [Fact]
    public async Task APrivateChannelTheCallerIsNotIn_IsNeverSearched_EvenForAWorkspaceAdmin()
    {
        var priv = Channel("secret", SpaceKind.ChannelPrivate, _other);
        Say(priv, _other, "invoice secret");

        _caller = _member;
        (await Ids("invoice")).Should().BeEmpty();
        _caller = _admin;
        (await Ids("invoice")).Should().BeEmpty();
    }

    [Fact]
    public async Task APersonRemovedFromAPrivateChannel_NoLongerFindsItsMessages()
    {
        var priv = Channel("secret", SpaceKind.ChannelPrivate, _other, _member);
        var m = Say(priv, _other, "invoice secret");
        (await Ids("invoice")).Should().Equal(m.Id);

        priv.RemoveMember(_member, Now);
        (await Ids("invoice")).Should().BeEmpty();
    }

    [Fact]
    public async Task AGuestFindsOnlySpacesTheyWereAddedTo_AnUnaddedPublicChannelIsNotSearched()
    {
        var pubNotAdded = Channel("general", SpaceKind.ChannelPublic, _other);
        var pubAdded = Channel("shared", SpaceKind.ChannelPublic, _other, _guest);
        Say(pubNotAdded, _other, "invoice general");
        var visible = Say(pubAdded, _other, "invoice shared");

        _caller = _guest;
        (await Ids("invoice")).Should().Equal(visible.Id);
    }

    [Fact]
    public async Task ADirectConversation_IsSearchedByItsParticipantsOnly()
    {
        var dm = Space.CreateDirect(_account, _ws, _member, _other, Now.AddDays(-5));
        _spaces.Add(dm);
        var m = Say(dm, _other, "invoice in private");

        _caller = _member;
        (await Ids("invoice")).Should().Equal(m.Id);
        _caller = _stranger;
        (await Ids("invoice")).Should().BeEmpty();
        _caller = _admin;
        (await Ids("invoice")).Should().BeEmpty();
    }

    [Fact]
    public async Task AnArchivedChannelStaysSearchable_AndADeletedOneDoesNot()
    {
        var archived = Channel("old", SpaceKind.ChannelPublic, _other);
        var gone = Channel("gone", SpaceKind.ChannelPublic, _other);
        var a = Say(archived, _other, "invoice archived");
        Say(gone, _other, "invoice gone");
        archived.SetArchived(true, _other, Now);
        gone.Delete(_other, Now);

        (await Ids("invoice")).Should().Equal(a.Id);
    }

    [Fact]
    public async Task ASuspendedCaller_IsNotFound()
    {
        _members.Setup(m => m.FindAsync(_ws, _member, It.IsAny<CancellationToken>())).ReturnsAsync(new WorkspaceMemberFact(_member, "Member", false));
        var act = () => Run("invoice");
        await act.Should().ThrowAsync<AppNotFoundException>();
    }

    [Fact]
    public async Task TheSearchUsesTheSameRuleAsTheHistory_OverTheWholeMatrix()
    {
        var pub = Channel("pub", SpaceKind.ChannelPublic, _other);
        var pubIn = Channel("pubin", SpaceKind.ChannelPublic, _other, _member, _guest);
        var priv = Channel("priv", SpaceKind.ChannelPrivate, _other);
        var privIn = Channel("privin", SpaceKind.ChannelPrivate, _other, _member, _guest);
        var left = Channel("left", SpaceKind.ChannelPublic, _other, _member, _guest);
        left.RemoveMember(_member, Now);
        left.RemoveMember(_guest, Now);
        var dm = Space.CreateDirect(_account, _ws, _member, _other, Now);
        _spaces.Add(dm);

        foreach (var (user, role) in new[] { (_member, "Member"), (_guest, "Guest"), (_admin, "Admin"), (_stranger, "Member") })
        {
            var caller = new WorkspaceMemberFact(user, role, true);
            var inSql = ChannelAccess.VisibleSpaces(_db.Object, _ws, user, ChannelAccess.IsGuest(caller)).Select(s => s.Id).ToHashSet();
            foreach (var space in _spaces)
            {
                var requireVisible = true;
                try { ChannelAccess.RequireVisible(space, caller, user); }
                catch (AppNotFoundException) { requireVisible = false; }
                inSql.Contains(space.Id).Should().Be(requireVisible, $"{role} on {space.Name}/{space.Kind}");
            }
        }
    }

    // ---- exclusions -----------------------------------------------------------------------------

    [Fact]
    public async Task ATombstoneASystemMessageAndAnExpiredMessage_NeverAppear()
    {
        var pub = Channel("general", SpaceKind.ChannelPublic, _other);
        var live = Say(pub, _other, "invoice live");
        var deleted = Say(pub, _other, "invoice deleted");
        deleted.Delete(_other, Now);
        _messages.Add(Message.CreateSystemMessage(_account, _ws, pub.Id, ++_seq, _other, "invoice system", Now.AddMinutes(-5)));

        (await Ids("invoice")).Should().Equal(live.Id);
    }

    [Fact]
    public async Task AMessagePastTheRetentionWindow_IsHiddenBeforeTheJobReachesIt()
    {
        var pub = Channel("general", SpaceKind.ChannelPublic, _other);
        var old = Say(pub, _other, "invoice old", ageMinutes: 40 * 24 * 60);
        var fresh = Say(pub, _other, "invoice fresh", ageMinutes: 10);
        var policy = RetentionPolicy.CreateDefault(_account, _ws, Now.AddDays(-90));
        policy.Change(30, RetentionMode.Purge, _admin, Now.AddDays(-80));
        _policies.Add(policy);

        (await Ids("invoice")).Should().Equal(fresh.Id);

        policy.Change(null, RetentionMode.Purge, _admin, Now);
        (await Ids("invoice")).Should().BeEquivalentTo([fresh.Id, old.Id]);
    }

    [Fact]
    public async Task ABlockedAuthorsPublicMessages_AreStillFound_BlockingOnlyStopsDirectConversationsAndNotices()
    {
        var pub = Channel("general", SpaceKind.ChannelPublic, _other);
        var m = Say(pub, _other, "invoice from the blocked person");
        (await Ids("invoice")).Should().Equal(m.Id);
    }

    // ---- matching -------------------------------------------------------------------------------

    [Theory]
    [InlineData("hoa don", "Hóa đơn tháng 11")]
    [InlineData("HÓA ĐƠN", "hoa don thang 11")]
    [InlineData("đơn", "don hang")]
    [InlineData("nguyễn", "Nguyen Van A")]
    public async Task TheMatchIgnoresCaseAndVietnameseMarks_InBothDirections(string typed, string stored)
    {
        var pub = Channel("general", SpaceKind.ChannelPublic, _other);
        var m = Say(pub, _other, stored);
        (await Ids(typed)).Should().Equal(m.Id);
    }

    [Fact]
    public async Task TheTypedTextIsNeverARegularExpression()
    {
        var pub = Channel("general", SpaceKind.ChannelPublic, _other);
        var m = Say(pub, _other, "cost is (a+b)* here");
        Say(pub, _other, "aab");
        (await Ids("(a+b)*")).Should().Equal(m.Id);
    }

    // ---- filters --------------------------------------------------------------------------------

    [Fact]
    public async Task TheFiltersNarrowTheHits_Space_Author_Range_File_Mention()
    {
        var one = Channel("one", SpaceKind.ChannelPublic, _other);
        var two = Channel("two", SpaceKind.ChannelPublic, _other);
        var a = Say(one, _other, "invoice a", ageMinutes: 60);
        var b = Say(two, _member, "invoice b", ageMinutes: 30);
        var c = Say(two, _other, "invoice c", ageMinutes: 5);
        _files.Seed(c.Id);
        _mentions.Add(MessageMention.Create(_account, _ws, two.Id, b.Id, new MentionSpan(MentionKind.User, 0, 2, _member)));
        _mentions.Add(MessageMention.Create(_account, _ws, two.Id, a.Id, new MentionSpan(MentionKind.BroadcastChannel, 0, 2, null)));

        (await Ids("invoice", o => o.Space = two.Id)).Should().BeEquivalentTo([b.Id, c.Id]);
        (await Ids("invoice", o => o.Author = _member)).Should().Equal(b.Id);
        (await Ids("invoice", o => { o.From = Now.AddMinutes(-40); o.To = Now.AddMinutes(-5); })).Should().Equal(b.Id);
        (await Ids("invoice", o => o.HasAttachment = true)).Should().Equal(c.Id);
        (await Ids("invoice", o => o.MentionsMe = true)).Should().Equal(b.Id);
        (await Ids("invoice", o => o.HasAttachment = false)).Should().HaveCount(3);
    }

    [Fact]
    public async Task ASpaceFilterOnASpaceTheCallerCannotSee_IsEmpty_NotAnError()
    {
        var priv = Channel("secret", SpaceKind.ChannelPrivate, _other);
        Say(priv, _other, "invoice secret");
        (await Ids("invoice", o => o.Space = priv.Id)).Should().BeEmpty();
        (await Ids("invoice", o => o.Space = Guid.NewGuid())).Should().BeEmpty();
    }

    // ---- paging ---------------------------------------------------------------------------------

    [Fact]
    public async Task PagesAreNewestFirst_StableAndComplete_EvenWithEqualTimes()
    {
        var pub = Channel("general", SpaceKind.ChannelPublic, _other);
        var all = new List<Message>();
        for (var i = 0; i < 7; i++) all.Add(Say(pub, _other, $"invoice {i}", ageMinutes: i < 4 ? 20 : 10 - i));

        var seen = new List<Guid>();
        string? cursor = null;
        do
        {
            var result = await Handler().Handle(new SearchChatMessagesQuery(_ws, "invoice", null, null, null, null, null, null, cursor, 3), default);
            seen.AddRange(result.Data!.Items.Select(i => i.MessageId));
            cursor = result.Data.NextCursor;
        }
        while (cursor is not null);

        seen.Should().OnlyHaveUniqueItems().And.HaveCount(7);
        seen.Should().BeEquivalentTo(all.Select(m => m.Id));
        var order = all.OrderByDescending(m => m.CreatedAt).ThenByDescending(m => m.Id).Select(m => m.Id);
        seen.Should().Equal(order);
    }

    [Fact]
    public async Task TheFileFilterPagesExactly_EvenWhenFewHitsHaveFiles()
    {
        var pub = Channel("general", SpaceKind.ChannelPublic, _other);
        var withFiles = new List<Message>();
        for (var i = 0; i < 450; i++)
        {
            var m = Say(pub, _other, $"invoice {i}", ageMinutes: 1 + i);
            if (i is 3 or 220 or 440) { _files.Seed(m.Id); withFiles.Add(m); }
        }

        var seen = new List<Guid>();
        string? cursor = null;
        var pages = 0;
        do
        {
            var result = await Handler().Handle(new SearchChatMessagesQuery(_ws, "invoice", null, null, null, null, true, null, cursor, 2), default);
            seen.AddRange(result.Data!.Items.Select(i => i.MessageId));
            cursor = result.Data.NextCursor;
            pages++;
        }
        while (cursor is not null && pages < 20);

        seen.Should().Equal(withFiles.Select(m => m.Id));
    }

    [Fact]
    public async Task ABadCursor_IsAValidationError()
    {
        Channel("general", SpaceKind.ChannelPublic, _other);
        var act = () => Run("invoice", o => o.Cursor = "garbage");
        await act.Should().ThrowAsync<Notrelix.Application.Common.Exceptions.ValidationException>();
    }

    // ---- request shape --------------------------------------------------------------------------

    [Fact]
    public void ATooShortOrTooLongTextOrBadLimitOrRange_IsRefused_ByTheValidator()
    {
        var v = new SearchChatMessagesQueryValidator();
        SearchChatMessagesQuery Q(string q, int limit = 20, DateTimeOffset? from = null, DateTimeOffset? to = null) =>
            new(_ws, q, null, null, from, to, null, null, null, limit);

        v.Validate(Q("")).IsValid.Should().BeFalse();
        v.Validate(Q("   ")).IsValid.Should().BeFalse();
        v.Validate(Q("a")).IsValid.Should().BeFalse();
        v.Validate(Q(new string('a', 101))).IsValid.Should().BeFalse();
        v.Validate(Q("ab", limit: 0)).IsValid.Should().BeFalse();
        v.Validate(Q("ab", limit: 51)).IsValid.Should().BeFalse();
        v.Validate(Q("ab", from: Now, to: Now.AddDays(-1))).IsValid.Should().BeFalse();
        v.Validate(Q("ab")).IsValid.Should().BeTrue();
        v.Validate(Q(new string('a', 100), limit: 50)).IsValid.Should().BeTrue();
    }

    [Fact]
    public void TheQueryNeedsTheChatSearchAction()
    {
        new SearchChatMessagesQuery(_ws, "ab", null, null, null, null, null, null, null, 20).Action
            .Should().Be(Notrelix.Domain.Governance.Permissions.PermissionAction.ChatSearch);
    }

    [Fact]
    public async Task EachSearchIsCountedAgainstTheSearchLimit_PerPersonAndWorkspace()
    {
        Channel("general", SpaceKind.ChannelPublic, _other);
        await Run("invoice");
        _limits.Verify(l => l.RequireAsync(ChatRateLimitPolicy.Search, _ws, _member, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AHitCarriesTheJumpTarget_TheSpace_AndTheThreadRootOfAReply()
    {
        var pub = Channel("general", SpaceKind.ChannelPublic, _other);
        var root = Say(pub, _other, "root");
        var reply = Message.CreateThreadReply(_account, _ws, pub.Id, ++_seq, root.Id, 1, _other, "invoice reply", "r1", false, Now.AddMinutes(-1));
        _messages.Add(reply);

        var result = await Handler().Handle(new SearchChatMessagesQuery(_ws, "invoice", null, null, null, null, null, null, null, 20), default);
        var hit = result.Data!.Items.Should().ContainSingle().Subject;
        hit.SpaceId.Should().Be(pub.Id);
        hit.SpaceName.Should().Be("general");
        hit.SpaceKind.Should().Be(SpaceWire.ChannelPublic);
        hit.ThreadRootId.Should().Be(root.Id);
        hit.Snippet.Text.Should().Be("invoice reply");
    }

    // ---- snippet --------------------------------------------------------------------------------

    [Fact]
    public void TheSnippetIsPlainText_WithRangesOfEveryMatchInTheOriginalLetters()
    {
        var snippet = ChatSearchSnippets.Build("Hóa đơn <b>số 1</b> và hoa don số 2", "hoa don");
        snippet.Text.Should().Be("Hóa đơn <b>số 1</b> và hoa don số 2");
        snippet.Ranges.Select(r => snippet.Text.Substring(r.Start, r.Length)).Should().Equal("Hóa đơn", "hoa don");
        snippet.CutBefore.Should().BeFalse();
        snippet.CutAfter.Should().BeFalse();
    }

    [Fact]
    public void ALongMessageIsCutAroundTheFirstMatch_AndSaysSo()
    {
        var text = new string('x', 300) + " needle " + new string('y', 300);
        var snippet = ChatSearchSnippets.Build(text, "needle");
        snippet.CutBefore.Should().BeTrue();
        snippet.CutAfter.Should().BeTrue();
        snippet.Text.Length.Should().BeLessThan(200);
        snippet.Ranges.Should().ContainSingle().Which.Should().Match<Notrelix.Application.Features.Chat.Search.DTOs.ChatSearchRangeDto>(
            r => snippet.Text.Substring(r.Start, r.Length) == "needle");
    }

    [Fact]
    public void ASnippetOfTextWithEmojiAndLoneSurrogates_DoesNotThrow_NorSplitAPair()
    {
        var text = string.Concat(Enumerable.Repeat("😀", 60)) + "needle" + "\ud83d" + string.Concat(Enumerable.Repeat("😀", 80));
        var snippet = ChatSearchSnippets.Build(text, "needle");
        snippet.Ranges.Should().ContainSingle();
        char.IsLowSurrogate(snippet.Text[0]).Should().BeFalse();
    }
}
