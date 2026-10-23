using Notrelix.Application.Features.Workspaces.Members.Commands.UpdateMemberJobTitle;
using Notrelix.Application.Features.Workspaces.Members.Commands.UpdateMyJobTitle;
using Notrelix.Application.Features.Workspaces.Members.Queries.GetWorkspaceMembers;
using Notrelix.Domain.Identity.Profiles;
using Notrelix.Domain.Workspaces;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Integration.Tests.Workspaces.OrgTree;
using AppForbidden = Notrelix.Application.Common.Exceptions.ForbiddenException;
using AppValidation = Notrelix.Application.Common.Exceptions.ValidationException;
using DomainBusinessRuleException = Notrelix.Domain.Common.Exceptions.BusinessRuleException;
using PreconditionFailed = Notrelix.Application.Common.Exceptions.PreconditionFailedException;

namespace Notrelix.Integration.Tests.Workspaces;

/// <summary>
/// D15 Workspace member profile on PostgreSQL through the real pipeline (NRX-003, NRX-004,
/// GOV-028): the job title is a per-Workspace fact. A member edits their own; Owner and Admin
/// edit anyone's; a plain Member cannot edit another's; a write carries the member version and a
/// stale one is 412 with nothing changed; nothing crosses Workspaces. Phone visibility on the
/// member list is proven here too: only an opted-in phone, only to a non-guest member of the same
/// Workspace, never to a guest, an outsider or another Workspace.
/// </summary>
[Collection("Database")]
[Trait("Category", "Integration")]
public sealed class MemberProfileIntegrationTests : IAsyncLifetime
{
    private const string Phone = "+84901234567";

    private readonly PostgresTestContainer _db;
    private readonly OrgTreeTestHost _host;

    public MemberProfileIntegrationTests(PostgresTestContainer db)
    {
        _db = db;
        _host = new OrgTreeTestHost(db);
    }

    public async Task InitializeAsync() => await new DatabaseReset(_db.ConnectionString).ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    // ── Job title ─────────────────────────────────────────────────────────

    [Fact]
    public async Task AdminAndOwner_CanSetAnotherMembersTitle_AndTheListShowsItWithTheNewVersion()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var member = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Teammate");
        var admin = await _host.AddMemberAsync(stack, WorkspaceRole.Admin, "Admin");
        using var asAdmin = _host.CreateProvider(stack.AccountId, admin);
        using var asOwner = _host.CreateProvider(stack.AccountId, stack.OwnerId);
        var v0 = (await PersistedAsync(stack.WorkspaceId, member)).Version;

        await OrgTreeTestHost.SendAsync(asAdmin, new UpdateMemberJobTitleCommand(stack.WorkspaceId, member, "  Product   designer ", v0));

        var afterAdmin = await PersistedAsync(stack.WorkspaceId, member);
        afterAdmin.JobTitle.Should().Be("Product designer");
        afterAdmin.Version.Should().BeGreaterThan(v0);

        await OrgTreeTestHost.SendAsync(asOwner, new UpdateMemberJobTitleCommand(stack.WorkspaceId, member, "Design lead", afterAdmin.Version));

        var row = (await OrgTreeTestHost.QueryAsync(asOwner, new GetWorkspaceMembersQuery(stack.WorkspaceId))).Single(m => m.UserId == member);
        row.JobTitle.Should().Be("Design lead");
        row.Version.Should().Be((await PersistedAsync(stack.WorkspaceId, member)).Version, "the list reports the version the next write must carry");
    }

    [Fact]
    public async Task PlainMemberAndGuest_CannotSetAnotherMembersTitle_ButEachCanSetTheirOwn()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var member = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Plain member");
        var other = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Other member");
        var guest = await _host.AddMemberAsync(stack, WorkspaceRole.Guest, "Guest");
        using var asMember = _host.CreateProvider(stack.AccountId, member);
        using var asGuest = _host.CreateProvider(stack.AccountId, guest);
        var otherBefore = await PersistedAsync(stack.WorkspaceId, other);

        var byMember = () => OrgTreeTestHost.SendAsync(asMember, new UpdateMemberJobTitleCommand(stack.WorkspaceId, other, "Hijack", otherBefore.Version));
        await byMember.Should().ThrowAsync<AppForbidden>("editing another member's profile is an Owner/Admin capability");
        var byGuest = () => OrgTreeTestHost.SendAsync(asGuest, new UpdateMemberJobTitleCommand(stack.WorkspaceId, other, "Hijack", otherBefore.Version));
        await byGuest.Should().ThrowAsync<AppForbidden>();
        (await PersistedAsync(stack.WorkspaceId, other)).Should().Be(otherBefore, "a denied write changes nothing");

        var mine = await PersistedAsync(stack.WorkspaceId, member);
        await OrgTreeTestHost.SendAsync(asMember, new UpdateMyJobTitleCommand(stack.WorkspaceId, "Engineer", mine.Version));
        (await PersistedAsync(stack.WorkspaceId, member)).JobTitle.Should().Be("Engineer");
        (await PersistedAsync(stack.WorkspaceId, other)).JobTitle.Should().BeNull("the own-title write never touches another member");

        var guestRow = await PersistedAsync(stack.WorkspaceId, guest);
        await OrgTreeTestHost.SendAsync(asGuest, new UpdateMyJobTitleCommand(stack.WorkspaceId, "External auditor", guestRow.Version));
        (await PersistedAsync(stack.WorkspaceId, guest)).JobTitle.Should().Be("External auditor");
    }

    [Fact]
    public async Task StaleVersion_Is412_AndLeavesTheTitleUnchanged_AndAVersionlessWriteIsRejected()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var member = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Teammate");
        using var asOwner = _host.CreateProvider(stack.AccountId, stack.OwnerId);
        using var asMember = _host.CreateProvider(stack.AccountId, member);
        var v0 = (await PersistedAsync(stack.WorkspaceId, member)).Version;
        await OrgTreeTestHost.SendAsync(asOwner, new UpdateMemberJobTitleCommand(stack.WorkspaceId, member, "Designer", v0));
        var current = await PersistedAsync(stack.WorkspaceId, member);

        var stale = () => OrgTreeTestHost.SendAsync(asOwner, new UpdateMemberJobTitleCommand(stack.WorkspaceId, member, "Stale", v0));
        (await stale.Should().ThrowAsync<PreconditionFailed>()).Which.ErrorCode.Should().Be("common.precondition-failed");
        var staleOwn = () => OrgTreeTestHost.SendAsync(asMember, new UpdateMyJobTitleCommand(stack.WorkspaceId, "Stale own", v0));
        (await staleOwn.Should().ThrowAsync<PreconditionFailed>()).Which.ErrorCode.Should().Be("common.precondition-failed");
        var versionless = () => OrgTreeTestHost.SendAsync(asMember, new UpdateMyJobTitleCommand(stack.WorkspaceId, "No version", 0));
        await versionless.Should().ThrowAsync<AppValidation>();

        (await PersistedAsync(stack.WorkspaceId, member)).Should().Be(current, "a rejected write leaves title and version unchanged");
    }

    [Fact]
    public async Task SemanticNoOp_KeepsTheVersion_AndBlankClears()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var member = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Teammate");
        using var asOwner = _host.CreateProvider(stack.AccountId, stack.OwnerId);
        await OrgTreeTestHost.SendAsync(asOwner, new UpdateMemberJobTitleCommand(stack.WorkspaceId, member, "Designer", (await PersistedAsync(stack.WorkspaceId, member)).Version));
        var set = await PersistedAsync(stack.WorkspaceId, member);

        await OrgTreeTestHost.SendAsync(asOwner, new UpdateMemberJobTitleCommand(stack.WorkspaceId, member, " Designer ", set.Version));
        (await PersistedAsync(stack.WorkspaceId, member)).Version.Should().Be(set.Version, "an unchanged title writes no new version");

        await OrgTreeTestHost.SendAsync(asOwner, new UpdateMemberJobTitleCommand(stack.WorkspaceId, member, "   ", set.Version));
        (await PersistedAsync(stack.WorkspaceId, member)).JobTitle.Should().BeNull();
    }

    [Fact]
    public async Task TooLongOrMultilineTitle_IsRejected_AndNothingChanges()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var member = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Teammate");
        using var asOwner = _host.CreateProvider(stack.AccountId, stack.OwnerId);
        var before = await PersistedAsync(stack.WorkspaceId, member);

        var tooLong = () => OrgTreeTestHost.SendAsync(asOwner, new UpdateMemberJobTitleCommand(stack.WorkspaceId, member, new string('x', 101), before.Version));
        (await tooLong.Should().ThrowAsync<DomainBusinessRuleException>()).Which.RuleCode.Should().Be(WorkspaceRuleCodes.Workspaces_Member_JobTitleTooLong);
        var multiline = () => OrgTreeTestHost.SendAsync(asOwner, new UpdateMemberJobTitleCommand(stack.WorkspaceId, member, "a\nb", before.Version));
        (await multiline.Should().ThrowAsync<DomainBusinessRuleException>()).Which.RuleCode.Should().Be(WorkspaceRuleCodes.Workspaces_Member_InvalidJobTitle);

        (await PersistedAsync(stack.WorkspaceId, member)).Should().Be(before);
    }

    [Fact]
    public async Task RemovedMember_IsNotEditable()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var member = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Leaver");
        using var asOwner = _host.CreateProvider(stack.AccountId, stack.OwnerId);
        var version = (await PersistedAsync(stack.WorkspaceId, member)).Version;
        await _host.WithSystemContextAsync(async c =>
        {
            var row = await c.WorkspaceMembers.SingleAsync(m => m.WorkspaceId == stack.WorkspaceId && m.UserId == member);
            row.Remove(activeOwnerCount: 1, stack.OwnerId, OrgTreeTestHost.BaseTime.AddMinutes(5));
        });
        var removedVersion = (await PersistedAsync(stack.WorkspaceId, member)).Version;

        var act = () => OrgTreeTestHost.SendAsync(asOwner, new UpdateMemberJobTitleCommand(stack.WorkspaceId, member, "Ghost", removedVersion));

        await act.Should().ThrowAsync<Notrelix.Application.Common.Exceptions.NotFoundException>();
        (await PersistedAsync(stack.WorkspaceId, member)).JobTitle.Should().BeNull();
        version.Should().BeLessThan(removedVersion);
    }

    [Fact]
    public async Task CrossWorkspace_AnAdminOfAnotherWorkspaceCannotEditOrReadAMember_AndTitlesDoNotLeakAcrossWorkspaces()
    {
        var a = await _host.SeedWorkspaceAsync();
        var b = await _host.SeedWorkspaceAsync();
        var memberOfA = await _host.AddMemberAsync(a, WorkspaceRole.Member, "A member");
        using var asOwnerA = _host.CreateProvider(a.AccountId, a.OwnerId);
        using var asOwnerB = _host.CreateProvider(b.AccountId, b.OwnerId);
        var before = await PersistedAsync(a.WorkspaceId, memberOfA);
        await OrgTreeTestHost.SendAsync(asOwnerA, new UpdateMemberJobTitleCommand(a.WorkspaceId, memberOfA, "Secret role", before.Version));
        var titled = await PersistedAsync(a.WorkspaceId, memberOfA);

        // B's owner addresses A's workspace id, then their own workspace with A's member id.
        var intoA = () => OrgTreeTestHost.SendAsync(asOwnerB, new UpdateMemberJobTitleCommand(a.WorkspaceId, memberOfA, "Hijack", titled.Version));
        await intoA.Should().ThrowAsync<Exception>("another workspace's owner has no access to workspace A");
        var viaB = () => OrgTreeTestHost.SendAsync(asOwnerB, new UpdateMemberJobTitleCommand(b.WorkspaceId, memberOfA, "Hijack", titled.Version));
        await viaB.Should().ThrowAsync<Notrelix.Application.Common.Exceptions.NotFoundException>("A's member is not a member of workspace B");
        var readIntoA = () => OrgTreeTestHost.SendAsync(asOwnerB, new GetWorkspaceMembersQuery(a.WorkspaceId));
        await readIntoA.Should().ThrowAsync<Exception>("the member list of workspace A is not readable from workspace B");

        (await PersistedAsync(a.WorkspaceId, memberOfA)).Should().Be(titled);
        var bList = await OrgTreeTestHost.QueryAsync(asOwnerB, new GetWorkspaceMembersQuery(b.WorkspaceId));
        bList.Should().NotContain(m => m.UserId == memberOfA);
        bList.Should().OnlyContain(m => m.JobTitle == null, "a title set in A is not part of B");
    }

    // ── Phone visibility on the member list ───────────────────────────────

    [Fact]
    public async Task MemberList_ShowsOnlyOptedInPhones_ToNonGuestMembers()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var sharer = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Sharer");
        var keeper = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Keeper");
        var reader = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Reader");
        await SeedProfileAsync(sharer, Phone, visible: true);
        await SeedProfileAsync(keeper, "+84907654321", visible: false);
        using var asReader = _host.CreateProvider(stack.AccountId, reader);

        var list = await OrgTreeTestHost.QueryAsync(asReader, new GetWorkspaceMembersQuery(stack.WorkspaceId));

        list.Single(m => m.UserId == sharer).Phone.Should().Be(Phone);
        list.Single(m => m.UserId == keeper).Phone.Should().BeNull("the default is self-only");
        list.Single(m => m.UserId == reader).Phone.Should().BeNull("no profile, no phone");
    }

    [Fact]
    public async Task MemberList_NeverShowsAPhone_ToAGuest_OrForAGuest()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var sharer = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Sharer");
        var guest = await _host.AddMemberAsync(stack, WorkspaceRole.Guest, "Guest");
        var guestSharer = await _host.AddMemberAsync(stack, WorkspaceRole.Guest, "Opted-in guest");
        await SeedProfileAsync(sharer, Phone, visible: true);
        await SeedProfileAsync(guestSharer, "+84900000001", visible: true);
        using var asGuest = _host.CreateProvider(stack.AccountId, guest);
        using var asOwner = _host.CreateProvider(stack.AccountId, stack.OwnerId);

        var seenByGuest = await OrgTreeTestHost.QueryAsync(asGuest, new GetWorkspaceMembersQuery(stack.WorkspaceId));
        var seenByOwner = await OrgTreeTestHost.QueryAsync(asOwner, new GetWorkspaceMembersQuery(stack.WorkspaceId));

        seenByGuest.Should().OnlyContain(m => m.Phone == null, "a guest never sees a colleague's phone");
        seenByOwner.Single(m => m.UserId == sharer).Phone.Should().Be(Phone);
        seenByOwner.Single(m => m.UserId == guestSharer).Phone.Should().BeNull("a guest's phone is not shared with the workspace");
    }

    [Fact]
    public async Task Phone_IsNotVisibleThroughAnotherWorkspace_OrToAnOutsider()
    {
        var a = await _host.AddMemberWorkspaceWithSharerAsync(Phone);
        using var asOwnerB = _host.CreateProvider(a.Other.AccountId, a.Other.OwnerId);
        var outsider = await _host.AddOutsiderAsync(a.Stack);
        using var asOutsider = _host.CreateProvider(a.Stack.AccountId, outsider);

        var intoA = () => OrgTreeTestHost.SendAsync(asOwnerB, new GetWorkspaceMembersQuery(a.Stack.WorkspaceId));
        await intoA.Should().ThrowAsync<Exception>();
        var byOutsider = () => OrgTreeTestHost.SendAsync(asOutsider, new GetWorkspaceMembersQuery(a.Stack.WorkspaceId));
        await byOutsider.Should().ThrowAsync<Exception>("a person who is not a member of the workspace cannot list it, so no phone is returned");

        var ownList = await OrgTreeTestHost.QueryAsync(asOwnerB, new GetWorkspaceMembersQuery(a.Other.WorkspaceId));
        ownList.Should().NotContain(m => m.UserId == a.Sharer);
        ownList.Should().OnlyContain(m => m.Phone == null);
    }

    private async Task SeedProfileAsync(Guid userId, string phone, bool visible)
    {
        await _host.WithSystemContextAsync(c =>
        {
            var profile = UserProfile.Create(userId, OrgTreeTestHost.BaseTime);
            profile.ApplyAccountPreferences(null, null, null, OrgTreeTestHost.BaseTime.AddMinutes(1), phone, visible);
            c.UserProfiles.Add(profile);
            return Task.CompletedTask;
        });
    }

    private Task<(string? JobTitle, long Version)> PersistedAsync(Guid workspaceId, Guid userId) =>
        _host.ReadAsync(async c =>
        {
            var row = await c.WorkspaceMembers
                .Where(m => m.WorkspaceId == workspaceId && m.UserId == userId)
                .Select(m => new { m.JobTitle, m.Version })
                .SingleAsync();
            return (row.JobTitle, row.Version);
        });
}

internal static class MemberProfileTestHostExtensions
{
    internal sealed record SharerWorkspaces(OrgStack Stack, OrgStack Other, Guid Sharer);

    /// <summary>Workspace A with an opted-in member, plus an unrelated workspace B.</summary>
    internal static async Task<SharerWorkspaces> AddMemberWorkspaceWithSharerAsync(this OrgTreeTestHost host, string phone)
    {
        var stack = await host.SeedWorkspaceAsync();
        var other = await host.SeedWorkspaceAsync();
        var sharer = await host.AddMemberAsync(stack, WorkspaceRole.Member, "Sharer");
        await host.WithSystemContextAsync(c =>
        {
            var profile = UserProfile.Create(sharer, OrgTreeTestHost.BaseTime);
            profile.ApplyAccountPreferences(null, null, null, OrgTreeTestHost.BaseTime.AddMinutes(1), phone, true);
            c.UserProfiles.Add(profile);
            return Task.CompletedTask;
        });
        return new SharerWorkspaces(stack, other, sharer);
    }
}
