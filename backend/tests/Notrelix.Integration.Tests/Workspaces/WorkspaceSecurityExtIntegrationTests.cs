using Microsoft.Extensions.DependencyInjection;
using Notrelix.Application.Features.Workspaces.Settings.Commands.UpdateWorkspaceSettings;
using Notrelix.Application.Features.Workspaces.Settings.Queries.GetWorkspaceSecurityExt;
using Notrelix.Application.Features.Workspaces.Settings.Queries.GetWorkspaceSettings;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Integration.Tests.Workspaces.OrgTree;
using AppForbidden = Notrelix.Application.Common.Exceptions.ForbiddenException;
using DomainBusinessRuleException = Notrelix.Domain.Common.Exceptions.BusinessRuleException;

namespace Notrelix.Integration.Tests.Workspaces;

/// <summary>
/// CTR-GAP-WS-SECURITY-EXT on PostgreSQL through the real pipeline (NRX-003, NRX-004, GOV-031,
/// GOV-032): the Guest count counts active Guest memberships of this Workspace only, the
/// "links off since" instant moves only when the sharing policy flips, the default Guest expiry
/// is persisted and validated, and a Guest or an outsider never reads the page data.
/// </summary>
[Collection("Database")]
[Trait("Category", "Integration")]
public sealed class WorkspaceSecurityExtIntegrationTests : IAsyncLifetime
{
    private readonly PostgresTestContainer _db;
    private readonly OrgTreeTestHost _host;

    public WorkspaceSecurityExtIntegrationTests(PostgresTestContainer db)
    {
        _db = db;
        _host = new OrgTreeTestHost(db);
    }

    public async Task InitializeAsync() => await new DatabaseReset(_db.ConnectionString).ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GuestCount_CountsActiveGuestsOfThisWorkspaceOnly()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var other = await _host.SeedWorkspaceAsync();
        await _host.AddMemberAsync(stack, WorkspaceRole.Guest, "Guest A");
        await _host.AddMemberAsync(stack, WorkspaceRole.Guest, "Guest B");
        await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Member");
        await _host.AddMemberAsync(other, WorkspaceRole.Guest, "Other workspace guest");
        var suspended = await _host.AddMemberAsync(stack, WorkspaceRole.Guest, "Suspended guest");
        await _host.WithSystemContextAsync(async c =>
        {
            var row = c.WorkspaceMembers.Single(m => m.WorkspaceId == stack.WorkspaceId && m.UserId == suspended);
            row.Suspend(stack.OwnerId, OrgTreeTestHost.BaseTime, activeOwnerCount: 1);
            await c.SaveChangesAsync();
        });
        using var asOwner = _host.CreateProvider(stack.AccountId, stack.OwnerId);

        var ext = await OrgTreeTestHost.QueryAsync(asOwner, new GetWorkspaceSecurityExtQuery(stack.WorkspaceId));

        ext.GuestCount.Should().Be(2, "only active Guest memberships of this Workspace count");
        ext.CompanyDomain.Should().BeNull();
        ext.GuestExpiryDays.Should().BeNull();
    }

    [Fact]
    public async Task LinksOffSince_IsCreationUntilTheFirstFlip_ThenTracksTheFlipsAndIsNullWhileLinksAreOn()
    {
        var stack = await _host.SeedWorkspaceAsync();
        using var asOwner = _host.CreateProvider(stack.AccountId, stack.OwnerId);

        var initial = await OrgTreeTestHost.QueryAsync(asOwner, new GetWorkspaceSecurityExtQuery(stack.WorkspaceId));
        initial.LinksOffSince.Should().Be(OrgTreeTestHost.BaseTime, "links are off from creation, so the creation instant is the answer");

        _host.Now = OrgTreeTestHost.BaseTime.AddDays(2);
        await UpdateAsync(asOwner, stack.WorkspaceId, allowPublicSharing: true);
        (await OrgTreeTestHost.QueryAsync(asOwner, new GetWorkspaceSecurityExtQuery(stack.WorkspaceId)))
            .LinksOffSince.Should().BeNull("links are on");

        _host.Now = OrgTreeTestHost.BaseTime.AddDays(3);
        await UpdateAsync(asOwner, stack.WorkspaceId, allowPublicSharing: true, enforceMfa: true);
        _host.Now = OrgTreeTestHost.BaseTime.AddDays(5);
        await UpdateAsync(asOwner, stack.WorkspaceId, allowPublicSharing: false, enforceMfa: true);

        (await OrgTreeTestHost.QueryAsync(asOwner, new GetWorkspaceSecurityExtQuery(stack.WorkspaceId)))
            .LinksOffSince.Should().Be(OrgTreeTestHost.BaseTime.AddDays(5), "the instant of the flip that turned links off");

        _host.Now = OrgTreeTestHost.BaseTime.AddDays(9);
        await UpdateAsync(asOwner, stack.WorkspaceId, allowPublicSharing: false, enforceMfa: false);
        (await OrgTreeTestHost.QueryAsync(asOwner, new GetWorkspaceSecurityExtQuery(stack.WorkspaceId)))
            .LinksOffSince.Should().Be(OrgTreeTestHost.BaseTime.AddDays(5), "a write that leaves the policy alone does not move the instant");
    }

    [Fact]
    public async Task GuestDefaultExpiry_IsPersistedWithEverySetting_AndOnlyThirtyNinetyOrOneEightyIsAccepted()
    {
        var stack = await _host.SeedWorkspaceAsync();
        using var asOwner = _host.CreateProvider(stack.AccountId, stack.OwnerId);

        await UpdateAsync(asOwner, stack.WorkspaceId, guestDefaultExpiryDays: 90, allowGuestInvites: true, defaultRole: "Guest", invitationExpiryDays: 14);

        var settings = await OrgTreeTestHost.QueryAsync(asOwner, new GetWorkspaceSettingsQuery(stack.WorkspaceId));
        settings.GuestDefaultExpiryDays.Should().Be(90);
        settings.AllowGuestInvites.Should().BeTrue("the guest-invite flag is persisted");
        settings.DefaultMemberRole.Should().Be("Guest");
        settings.InvitationExpiryDays.Should().Be(14);
        (await OrgTreeTestHost.QueryAsync(asOwner, new GetWorkspaceSecurityExtQuery(stack.WorkspaceId))).GuestExpiryDays.Should().Be(90);

        var version = await VersionAsync(stack.WorkspaceId);
        var invalid = () => OrgTreeTestHost.SendAsync(asOwner, new UpdateWorkspaceSettingsCommand(
            stack.WorkspaceId, false, false, true, "Guest", 14, version, 45));
        await invalid.Should().ThrowAsync<DomainBusinessRuleException>();
        (await OrgTreeTestHost.QueryAsync(asOwner, new GetWorkspaceSettingsQuery(stack.WorkspaceId)))
            .GuestDefaultExpiryDays.Should().Be(90, "a rejected value changes nothing");

        await UpdateAsync(asOwner, stack.WorkspaceId, guestDefaultExpiryDays: null, allowGuestInvites: true, defaultRole: "Guest", invitationExpiryDays: 14);
        (await OrgTreeTestHost.QueryAsync(asOwner, new GetWorkspaceSecurityExtQuery(stack.WorkspaceId))).GuestExpiryDays.Should().BeNull("none clears the default");
    }

    [Fact]
    public async Task GuestAndOutsider_CannotReadTheSecurityExtras_ButMemberAndAdminCan()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var guest = await _host.AddMemberAsync(stack, WorkspaceRole.Guest, "Guest");
        var member = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Member");
        var admin = await _host.AddMemberAsync(stack, WorkspaceRole.Admin, "Admin");
        var outsider = await _host.AddOutsiderAsync(stack);
        var other = await _host.SeedWorkspaceAsync();
        using var asOwnerOfOther = _host.CreateProvider(other.AccountId, other.OwnerId);

        foreach (var userId in new[] { member, admin })
        {
            using var provider = _host.CreateProvider(stack.AccountId, userId);
            (await OrgTreeTestHost.QueryAsync(provider, new GetWorkspaceSecurityExtQuery(stack.WorkspaceId))).GuestCount.Should().Be(1);
        }

        foreach (var userId in new[] { guest, outsider })
        {
            using var provider = _host.CreateProvider(stack.AccountId, userId);
            var read = () => OrgTreeTestHost.QueryAsync(provider, new GetWorkspaceSecurityExtQuery(stack.WorkspaceId));
            await read.Should().ThrowAsync<AppForbidden>();
        }

        var cross = () => OrgTreeTestHost.QueryAsync(asOwnerOfOther, new GetWorkspaceSecurityExtQuery(stack.WorkspaceId));
        await cross.Should().ThrowAsync<Exception>("another workspace's owner has no access to this workspace's extras");
    }

    private async Task UpdateAsync(
        ServiceProvider provider,
        Guid workspaceId,
        bool allowPublicSharing = false,
        bool enforceMfa = false,
        bool allowGuestInvites = false,
        string defaultRole = "Member",
        int invitationExpiryDays = 7,
        int? guestDefaultExpiryDays = null)
    {
        var version = await VersionAsync(workspaceId);
        await OrgTreeTestHost.SendAsync(provider, new UpdateWorkspaceSettingsCommand(
            workspaceId, allowPublicSharing, enforceMfa, allowGuestInvites, defaultRole, invitationExpiryDays, version, guestDefaultExpiryDays));
    }

    private Task<long> VersionAsync(Guid workspaceId) =>
        _host.ReadAsync(c => c.Workspaces.Where(w => w.Id == workspaceId).Select(w => w.Version).SingleAsync());
}
