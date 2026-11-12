using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Notrelix.Application.Common.Time;
using Notrelix.Application.Features.Chat.RateLimits;
using Notrelix.Application.Features.Chat.Search.Queries.SearchChatMessages;
using Notrelix.Application.Features.Workspaces.Public.Members;
using Notrelix.Domain.Chat.Messages;
using Notrelix.Domain.Chat.Retention;
using Notrelix.Domain.Chat.Spaces;
using Notrelix.Domain.Identity.Users;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Infrastructure.Data.Rls;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;
using Space = Notrelix.Domain.Chat.Spaces.Space;

namespace Notrelix.Integration.Tests.Rls;

/// <summary>
/// UC-CHT-H07 against PostgreSQL through the application role (row-level security on): the accent-insensitive match runs in the database on
/// Vietnamese text with and without marks, the typed text is never a pattern, another Workspace's messages are never found, a private
/// channel and a conversation are found by their members only, and a removal, a delete and a retention window take effect at the next
/// search because there is no index to lag. Needs Docker locally.
/// </summary>
[Collection("DatabaseAppRole")]
[Trait("Category", "RlsAppRole")]
public sealed class ChatSearchRlsTests : IAsyncLifetime
{
    private static readonly DateTimeOffset Now = new(2026, 11, 12, 9, 0, 0, TimeSpan.Zero);

    private readonly AppRoleTestContainer _fixture;
    private AppRoleTenant _a = null!;
    private AppRoleTenant _b = null!;
    private User _member = null!;
    private User _guest = null!;
    private Guid _publicId;
    private Guid _privateId;
    private Guid _bPublicId;

    public ChatSearchRlsTests(AppRoleTestContainer fixture) => _fixture = fixture;

    public async Task InitializeAsync()
    {
        await _fixture.ResetAsync();
        _a = await AppRoleTenants.SeedAsync(_fixture, "chat-search-a@example.com", "SearchA", "hash");
        _b = await AppRoleTenants.SeedAsync(_fixture, "chat-search-b@example.com", "SearchB", "hash");
        _member = await AppRoleMembers.AddAsync(_fixture, _a, "SearchMember", WorkspaceRole.Member, WorkspaceMemberStatus.Active, "hash");
        _guest = await AppRoleMembers.AddAsync(_fixture, _a, "SearchGuest", WorkspaceRole.Guest, WorkspaceMemberStatus.Active, "hash");

        var owner = _a.User.Id;
        var pub = Space.CreateChannel(_a.AccountId, _a.WorkspaceId, SpaceKind.ChannelPublic, "general", null, null, 1, owner, Now.AddDays(-30));
        var priv = Space.CreateChannel(_a.AccountId, _a.WorkspaceId, SpaceKind.ChannelPrivate, "secret", null, null, 2, owner, Now.AddDays(-30));
        priv.AddMember(_member.Id, MembershipRole.Member, Now.AddDays(-29));
        var bPub = Space.CreateChannel(_b.AccountId, _b.WorkspaceId, SpaceKind.ChannelPublic, "general", null, null, 1, _b.User.Id, Now.AddDays(-30));
        _publicId = pub.Id;
        _privateId = priv.Id;
        _bPublicId = bPub.Id;

        var system = new FakeCurrentTenantContext();
        system.SetSystem();
        await using var ctx = _fixture.CreateOwnerContext(system);
        ctx.ChatSpaces.AddRange(pub, priv, bPub);
        ctx.ChatMessages.AddRange(
            Say(_a, pub, 1, owner, "Hóa đơn tháng 11 đã duyệt", 50),
            Say(_a, pub, 2, owner, "hoa don thang 12 chua duyet", 40),
            Say(_a, pub, 3, owner, "HOÁ ĐƠN gấp (a+b)* [x]", 30),
            Say(_a, pub, 4, owner, "Nguyễn Văn An gửi báo cáo", 20),
            Say(_a, priv, 5, owner, "hóa đơn bí mật", 10),
            Say(_b, bPub, 1, _b.User.Id, "hóa đơn của workspace khác", 10));
        await ctx.SaveChangesAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private static Message Say(AppRoleTenant tenant, Space space, long sequence, Guid author, string content, int ageMinutes) =>
        Message.CreateUserMessage(tenant.AccountId, tenant.WorkspaceId, space.Id, sequence, author, content, $"search-{sequence}-{space.Id:N}", null, Now.AddMinutes(-ageMinutes));

    private async Task<IReadOnlyList<string>> SearchAsync(Guid userId, string role, string q, Guid? space = null, AppRoleTenant? tenantOverride = null)
    {
        var tenantOf = tenantOverride ?? _a;
        var tenant = new FakeCurrentTenantContext();
        tenant.SetWorkspace(tenantOf.AccountId, tenantOf.WorkspaceId, userId);
        var request = new Mock<ICurrentRequestContext>();
        request.Setup(c => c.UserId).Returns(userId);
        request.Setup(c => c.IsAuthenticated).Returns(true);
        var members = new Mock<IWorkspaceMemberFacts>();
        members.Setup(m => m.FindAsync(_a.WorkspaceId, userId, It.IsAny<CancellationToken>())).ReturnsAsync(new WorkspaceMemberFact(userId, role, true));
        var limits = new Mock<IChatRateLimiter>();
        var clock = new Mock<IDateTimeProvider>();
        clock.Setup(c => c.UtcNow).Returns(Now);

        await using var db = _fixture.CreateAppContext(tenant);
        await using var tx = await db.Database.BeginTransactionAsync();
        var rls = new RlsSessionContext(db, Options.Create(new RlsOptions { Enabled = true, SetSessionContext = true }), tenant, NullLogger<RlsSessionContext>.Instance);
        await rls.ApplyAsync(CancellationToken.None);
        var handler = new SearchChatMessagesQueryHandler(db, request.Object, members.Object, clock.Object, limits.Object, new InMemoryChatMessageAttachments());
        var result = await handler.Handle(new SearchChatMessagesQuery(_a.WorkspaceId, q, space, null, null, null, null, null, null, 50), CancellationToken.None);
        return result.Data!.Items.Select(i => i.Snippet.Text).ToList();
    }

    [Theory]
    [InlineData("hoa don")]
    [InlineData("hóa đơn")]
    [InlineData("HÓA ĐƠN")]
    [InlineData("hoá đơn")]
    public async Task TheMatchIgnoresCaseAndMarks_OnVietnameseTextInTheDatabase(string typed)
    {
        var hits = await SearchAsync(_member.Id, "Member", typed, _publicId);
        hits.Should().BeEquivalentTo("Hóa đơn tháng 11 đã duyệt", "hoa don thang 12 chua duyet", "HOÁ ĐƠN gấp (a+b)* [x]");
    }

    [Theory]
    [InlineData("nguyen van an", 1)]
    [InlineData("NGUYỄN", 1)]
    [InlineData("đơn", 3)]
    [InlineData("don", 3)]
    [InlineData("bao cao", 1)]
    public async Task ASingleWordOrAnUnmarkedQueryFindsTheMarkedText(string typed, int expected)
    {
        (await SearchAsync(_member.Id, "Member", typed, _publicId)).Should().HaveCount(expected);
    }

    [Fact]
    public async Task TheTypedTextIsALiteralPhrase_NotAPattern()
    {
        (await SearchAsync(_member.Id, "Member", "(a+b)*", _publicId)).Should().ContainSingle();
        (await SearchAsync(_member.Id, "Member", "[x]", _publicId)).Should().ContainSingle();
        (await SearchAsync(_member.Id, "Member", ".*", _publicId)).Should().BeEmpty();
        (await SearchAsync(_member.Id, "Member", "a|b", _publicId)).Should().BeEmpty();
    }

    [Fact]
    public async Task AnotherWorkspacesMessagesAreNeverFound_AndTheirWorkspaceFindsNothingOfThisOne()
    {
        var hits = await SearchAsync(_member.Id, "Member", "workspace khac");
        hits.Should().BeEmpty();
        (await SearchAsync(_member.Id, "Member", "hoa don")).Should().HaveCount(4, "three public and the private channel the member is in");

        // The row policy alone: a context scoped to B reading with A's workspace id in the query still finds nothing of A.
        var asB = await SearchAsync(_member.Id, "Member", "hoa don", tenantOverride: _b);
        asB.Should().NotContain(h => h.Contains("bí mật") || h.Contains("Hóa đơn tháng"));
    }

    [Fact]
    public async Task APrivateChannelIsFoundByItsMembersOnly_NotByTheGuestNorTheOwnerWhoIsNotInIt()
    {
        (await SearchAsync(_member.Id, "Member", "bi mat")).Should().ContainSingle();
        (await SearchAsync(_guest.Id, "Guest", "bi mat")).Should().BeEmpty();
        (await SearchAsync(_guest.Id, "Guest", "hoa don")).Should().BeEmpty("a Guest added to nothing finds nothing, public channels included");
    }

    [Fact]
    public async Task ARemovalADeleteAndARetentionWindow_TakeEffectAtTheNextSearch()
    {
        var system = new FakeCurrentTenantContext();
        system.SetSystem();
        await using (var ctx = _fixture.CreateOwnerContext(system))
        {
            var priv = await ctx.ChatSpaces.Include(s => s.Memberships).SingleAsync(s => s.Id == _privateId);
            priv.RemoveMember(_member.Id, Now);
            var one = await ctx.ChatMessages.SingleAsync(m => m.SpaceId == _publicId && m.Sequence == 1);
            one.Delete(_a.User.Id, Now);
            await ctx.SaveChangesAsync();
        }

        (await SearchAsync(_member.Id, "Member", "bi mat")).Should().BeEmpty("a removed member no longer finds the private channel");
        (await SearchAsync(_member.Id, "Member", "hoa don", _publicId)).Should().HaveCount(2, "the deleted message is a tombstone with no body");

        await using (var ctx = _fixture.CreateOwnerContext(system))
        {
            var policy = RetentionPolicy.CreateDefault(_a.AccountId, _a.WorkspaceId, Now);
            policy.Change(7, RetentionMode.Purge, _a.User.Id, Now);
            ctx.ChatRetentionPolicies.Add(policy);
            var old = await ctx.ChatMessages.SingleAsync(m => m.SpaceId == _publicId && m.Sequence == 2);
            ctx.Entry(old).Property(m => m.CreatedAt).CurrentValue = Now.AddDays(-20);
            await ctx.SaveChangesAsync();
        }

        (await SearchAsync(_member.Id, "Member", "hoa don", _publicId)).Should().ContainSingle("the message past the window is hidden before the job runs");
    }
}
