using System.Text.Json;
using Notrelix.Application.Common.Auditing;
using Notrelix.Application.Common.Storage;
using Notrelix.Application.Features.Chat.Abstractions;
using Notrelix.Application.Features.Chat.Audit.Services;
using Notrelix.Application.Features.Chat.Exports;
using Notrelix.Application.Features.Chat.Exports.Commands.CreateChatExport;
using Notrelix.Application.Features.Chat.Exports.Queries.GetChatExport;
using Notrelix.Application.Features.Chat.Exports.Queries.GetChatExportContent;
using Notrelix.Application.Features.Chat.RateLimits;
using Notrelix.Application.Features.Workspaces.Public.Members;
using Notrelix.Domain.Chat.Exports;
using Notrelix.Domain.Chat.Messages;
using Notrelix.Domain.Chat.Rules;
using Notrelix.Domain.Chat.Spaces;
using Notrelix.Domain.Common.Auditing;
using Notrelix.Testing.Application.Fakes;
using Notrelix.Application.Tests.Features.Collaboration;
using AppNotFoundException = Notrelix.Application.Common.Exceptions.NotFoundException;
using Space = Notrelix.Domain.Chat.Spaces.Space;

namespace Notrelix.Application.Tests.Features.Chat.Exports;

/// <summary>UC-CHT-H06: the export of a person's chat data: request, scope and exclusions, size bound, status, download and audit.</summary>
public class ChatExportTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 12, 9, 0, 0, TimeSpan.Zero);
    private readonly Guid _ws = Guid.NewGuid();
    private readonly Guid _account = Guid.NewGuid();
    private readonly Guid _admin = Guid.NewGuid();
    private readonly Guid _otherAdmin = Guid.NewGuid();
    private readonly Guid _subject = Guid.NewGuid();
    private readonly Guid _other = Guid.NewGuid();
    private readonly List<Space> _spaces = [];
    private readonly List<Message> _messages = [];
    private readonly List<ChatExportJob> _jobs = [];
    private readonly List<(string Action, AuditChange? Change)> _facts = [];
    private readonly Mock<IChatDbContext> _db = new();
    private readonly Mock<ICurrentRequestContext> _ctx = new();
    private readonly Mock<IWorkspaceMemberFacts> _members = new();
    private readonly Mock<IDateTimeProvider> _clock = new();
    private readonly Mock<IAuditService> _audit = new();
    private readonly Mock<IChatRateLimiter> _limits = new();
    private readonly Mock<IStorageService> _storage = new();
    private long _seq;

    public ChatExportTests()
    {
        _clock.Setup(c => c.UtcNow).Returns(Now);
        _ctx.Setup(c => c.RequireAccountId()).Returns(_account);
        _ctx.Setup(c => c.UserId).Returns(_admin);
        _db.Setup(d => d.ChatSpaces).Returns(() => TestDbSet.Create(_spaces).Object);
        _db.Setup(d => d.ChatMemberships).Returns(() => TestDbSet.Create(_spaces.SelectMany(s => s.Memberships).ToList()).Object);
        _db.Setup(d => d.ChatMessages).Returns(() => TestDbSet.Create(_messages).Object);
        _db.Setup(d => d.ChatExportJobs).Returns(() => TestDbSet.Create(_jobs).Object);
        foreach (var (id, role) in new[] { (_admin, "Admin"), (_otherAdmin, "Owner"), (_subject, "Member"), (_other, "Member") })
            _members.Setup(m => m.FindAsync(_ws, id, It.IsAny<CancellationToken>())).ReturnsAsync(new WorkspaceMemberFact(id, role, true));
        _audit.Setup(a => a.RecordAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<ResourceRef>(), It.IsAny<AuditMetadata>(),
                It.IsAny<AuditSeverity>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<AuditChange?>()))
            .Callback((Guid _, Guid _, string action, ResourceRef _, AuditMetadata _, AuditSeverity _, string _, string _, CancellationToken _, AuditChange? change) =>
                _facts.Add((action, change)))
            .Returns(Task.CompletedTask);
        _limits.Setup(l => l.RequireAsync(It.IsAny<ChatRateLimitPolicy>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
    }

    private CreateChatExportCommandHandler Create() =>
        new(_db.Object, _ctx.Object, _members.Object, _clock.Object, _limits.Object, new ChatAuditFacts(_audit.Object, _ctx.Object));

    private Space Channel(string name, SpaceKind kind, params Guid[] members)
    {
        var space = Space.CreateChannel(_account, _ws, kind, name, null, null, 1, members.Length > 0 ? members[0] : _other, Now.AddDays(-60));
        foreach (var m in members.Skip(1)) space.AddMember(m, MembershipRole.Member, Now.AddDays(-59));
        _spaces.Add(space);
        return space;
    }

    private Message Say(Space space, Guid author, string content, int ageDays = 10)
    {
        var m = Message.CreateUserMessage(_account, _ws, space.Id, ++_seq, author, content, $"c{_seq}", null, Now.AddDays(-ageDays));
        _messages.Add(m);
        return m;
    }

    private ChatExportJob Job(DateTimeOffset? from = null, DateTimeOffset? to = null, Guid? requester = null)
    {
        var job = ChatExportJob.Request(_account, _ws, requester ?? _admin, _subject, from, to, Now);
        job.Start(Now);
        _jobs.Add(job);
        return job;
    }

    private async Task<(JsonElement Doc, ChatExportBuild Build)> Build(ChatExportJob job)
    {
        using var stream = new MemoryStream();
        var build = await new ChatExportBuilder(_db.Object).BuildAsync(job, Now, stream, default);
        return (JsonDocument.Parse(stream.ToArray()).RootElement.Clone(), build);
    }

    // ---- request --------------------------------------------------------------------------------

    [Fact]
    public async Task AnAdminRequestsAnExport_AQueuedJobAndOneAuditFactWithWhoWhoseAndRange()
    {
        var from = Now.AddDays(-30);
        var result = await Create().Handle(new CreateChatExportCommand(_ws, _subject, from, Now), default);

        result.Data!.Status.Should().Be("queued");
        result.Data.SubjectUserId.Should().Be(_subject);
        result.Data.DownloadPath.Should().BeNull();
        _jobs.Should().ContainSingle().Which.RequestedBy.Should().Be(_admin);
        _facts.Should().ContainSingle().Which.Action.Should().Be(ChatAuditFacts.ExportRequested);
        _limits.Verify(l => l.RequireAsync(ChatRateLimitPolicy.Export, _ws, _admin, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task APersonOutsideTheWorkspaceIsNotFound_AndNothingIsStoredOrAudited()
    {
        var act = () => Create().Handle(new CreateChatExportCommand(_ws, Guid.NewGuid(), null, null), default);

        await act.Should().ThrowAsync<AppNotFoundException>();
        _jobs.Should().BeEmpty();
        _facts.Should().BeEmpty();
    }

    [Fact]
    public async Task OverTheExportLimitTheRequestIs429AndStoresNothing()
    {
        _limits.Setup(l => l.RequireAsync(ChatRateLimitPolicy.Export, It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Notrelix.Application.Common.Exceptions.TooManyRequestsException("slow", ChatRuleCodes.RateLimited, TimeSpan.FromMinutes(5)));

        var act = () => Create().Handle(new CreateChatExportCommand(_ws, _subject, null, null), default);

        await act.Should().ThrowAsync<Notrelix.Application.Common.Exceptions.TooManyRequestsException>();
        _jobs.Should().BeEmpty();
    }

    [Fact]
    public void TheCommandNeedsTheSettingsAction_AndARangeThatEndsFirstIsInvalid()
    {
        new CreateChatExportCommand(_ws, _subject, null, null).Action.Should().Be(Notrelix.Domain.Governance.Permissions.PermissionAction.ManageWorkspaceSettings);
        var v = new CreateChatExportCommandValidator();
        v.Validate(new CreateChatExportCommand(_ws, _subject, Now, Now.AddDays(-1))).IsValid.Should().BeFalse();
        v.Validate(new CreateChatExportCommand(_ws, Guid.Empty, null, null)).IsValid.Should().BeFalse();
        v.Validate(new CreateChatExportCommand(_ws, _subject, Now.AddDays(-1), Now)).IsValid.Should().BeTrue();
    }

    // ---- scope ----------------------------------------------------------------------------------

    [Fact]
    public async Task TheFileHoldsOnlyThePersonsOwnMessages_NotOthersNorSystemOnes_AndNoRepliesToThem()
    {
        var general = Channel("general", SpaceKind.ChannelPublic, _other, _subject);
        Say(general, _subject, "mine one");
        Say(general, _other, "someone else's words");
        _messages.Add(Message.CreateSystemMessage(_account, _ws, general.Id, 90, _subject, "subject joined", Now.AddDays(-5)));

        var (doc, build) = await Build(Job());

        build.Messages.Should().Be(1);
        doc.GetProperty("messages").EnumerateArray().Select(m => m.GetProperty("content").GetString()).Should().Equal("mine one");
        doc.GetRawText().Should().NotContain("someone else's words").And.NotContain("subject joined");
        doc.GetProperty("format").GetString().Should().Be(ChatExportFiles.FormatName);
        doc.GetProperty("memberships").GetArrayLength().Should().Be(1);
    }

    [Fact]
    public async Task APrivateChannelAndAConversationTheRequesterIsNotInAreLeftOutAndOnlyCounted()
    {
        var general = Channel("general", SpaceKind.ChannelPublic, _other, _subject);
        var secret = Channel("secret-plans", SpaceKind.ChannelPrivate, _other, _subject);
        var shared = Channel("shared-private", SpaceKind.ChannelPrivate, _admin, _subject);
        Say(general, _subject, "public words");
        Say(secret, _subject, "secret words");
        Say(shared, _subject, "shared words");

        var (doc, build) = await Build(Job());

        build.Spaces.Should().Be(2);
        build.ExcludedSpaces.Should().Be(1);
        build.Messages.Should().Be(2);
        var text = doc.GetRawText();
        text.Should().NotContain("secret words").And.NotContain("secret-plans", "not even the name of a space the requester cannot see");
        text.Should().Contain("public words").And.Contain("shared words");
        doc.GetProperty("summary").GetProperty("spacesNotVisibleToRequester").GetInt32().Should().Be(1);
    }

    [Fact]
    public async Task TombstonesAreCountedNotListed_AndADeletedSpaceIsNotPartOfTheExport()
    {
        var general = Channel("general", SpaceKind.ChannelPublic, _other, _subject);
        var gone = Channel("gone", SpaceKind.ChannelPublic, _other, _subject);
        Say(general, _subject, "kept");
        Say(general, _subject, "deleted words").Delete(_subject, Now.AddDays(-1));
        Say(gone, _subject, "in a deleted space");
        gone.Delete(_other, Now.AddDays(-1));

        var (doc, build) = await Build(Job());

        build.Messages.Should().Be(1);
        build.Tombstones.Should().Be(1);
        doc.GetRawText().Should().NotContain("deleted words").And.NotContain("in a deleted space");
    }

    [Fact]
    public async Task TheRangeKeepsTheFromAndDropsTheTo()
    {
        var general = Channel("general", SpaceKind.ChannelPublic, _other, _subject);
        Say(general, _subject, "too old", ageDays: 40);
        Say(general, _subject, "at the start", ageDays: 30);
        Say(general, _subject, "inside", ageDays: 15);
        Say(general, _subject, "at the end", ageDays: 5);

        var (doc, _) = await Build(Job(Now.AddDays(-30), Now.AddDays(-5)));

        doc.GetProperty("messages").EnumerateArray().Select(m => m.GetProperty("content").GetString())
            .Should().Equal("at the start", "inside");
    }

    [Fact]
    public async Task AFileOverTheMessageLimitFailsWithExportTooLarge()
    {
        var general = Channel("general", SpaceKind.ChannelPublic, _other, _subject);
        for (var i = 0; i < ChatLimits.ExportMaxMessages + 1; i++)
            Say(general, _subject, "x", ageDays: 1);

        using var stream = new MemoryStream();
        var build = await new ChatExportBuilder(_db.Object).BuildAsync(Job(), Now, stream, default);

        build.IsTooLarge.Should().BeTrue();
        build.FailureCode.Should().Be(ChatRuleCodes.ExportTooLarge);
    }

    // ---- status and download --------------------------------------------------------------------

    private ChatExportJob Ready(Guid requester)
    {
        var job = Job(requester: requester);
        job.Complete("acc/ws/chat-exports/k", 5, 1, 1, 0, 0, Now, TimeSpan.FromHours(24));
        return job;
    }

    [Fact]
    public async Task OnlyTheRequesterReadsTheStatus_AnotherAdminGetsNotFound()
    {
        var job = Ready(_admin);

        var own = await new GetChatExportQueryHandler(_db.Object, _ctx.Object, _members.Object, _clock.Object).Handle(new GetChatExportQuery(_ws, job.Id), default);
        own.Data!.Status.Should().Be("ready");
        own.Data.DownloadPath.Should().Be($"/api/v1/workspaces/{_ws}/chat/exports/{job.Id}/content");

        _ctx.Setup(c => c.UserId).Returns(_otherAdmin);
        var act = () => new GetChatExportQueryHandler(_db.Object, _ctx.Object, _members.Object, _clock.Object).Handle(new GetChatExportQuery(_ws, job.Id), default);
        await act.Should().ThrowAsync<AppNotFoundException>();
    }

    private GetChatExportContentQueryHandler Content() => new(_db.Object, _ctx.Object, _members.Object, _storage.Object, _clock.Object);

    [Fact]
    public async Task TheRequesterDownloadsAReadyFile_AndNotAfterItsExpiry_OrAnotherAdmin()
    {
        var job = Ready(_admin);
        _storage.Setup(s => s.OpenReadAsync(job.StorageKey!, It.IsAny<CancellationToken>())).ReturnsAsync(() => new MemoryStream([1, 2, 3]));

        var file = await Content().Handle(new GetChatExportContentQuery(_ws, job.Id), default);
        file.ContentType.Should().StartWith("application/json");
        file.FileName.Should().EndWith(".json");

        _clock.Setup(c => c.UtcNow).Returns(Now.AddHours(25));
        var expired = () => Content().Handle(new GetChatExportContentQuery(_ws, job.Id), default);
        await expired.Should().ThrowAsync<AppNotFoundException>("the link dies with the file");

        _clock.Setup(c => c.UtcNow).Returns(Now);
        _ctx.Setup(c => c.UserId).Returns(_otherAdmin);
        var other = () => Content().Handle(new GetChatExportContentQuery(_ws, job.Id), default);
        await other.Should().ThrowAsync<AppNotFoundException>();
    }

    [Fact]
    public async Task AQueuedOrFailedExportHasNoFile()
    {
        var queued = Job();
        var act = () => Content().Handle(new GetChatExportContentQuery(_ws, queued.Id), default);
        await act.Should().ThrowAsync<AppNotFoundException>();
    }
}
