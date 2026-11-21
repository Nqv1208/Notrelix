using Microsoft.Extensions.DependencyInjection;
using Notrelix.Application.Features.Workspaces.Settings.Commands.UpdateWorkspaceSettings;
using Notrelix.Application.Features.Workspaces.Settings.Queries.GetWorkspaceSettings;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Integration.Tests.Workspaces.OrgTree;
using AppForbidden = Notrelix.Application.Common.Exceptions.ForbiddenException;
using AppNotFound = Notrelix.Application.Common.Exceptions.NotFoundException;
using AppValidation = Notrelix.Application.Common.Exceptions.ValidationException;
using DomainBusinessRuleException = Notrelix.Domain.Common.Exceptions.BusinessRuleException;
using PreconditionFailed = Notrelix.Application.Common.Exceptions.PreconditionFailedException;

namespace Notrelix.Integration.Tests.Workspaces;

/// <summary>
/// Reading and changing the workspace settings on PostgreSQL through the real pipeline (WSP-012, WGREQ010,
/// WX-SET-001): every active member reads the values and a person who is not a member is told the workspace does
/// not exist; only an Owner or Admin changes them; a write carries the version it read, a stale one is a 412 that
/// changes nothing, a successful one moves the version by one and the next write carries the new one, and an
/// unchanged write is a no-op.
/// </summary>
[Collection("Database")]
[Trait("Category", "Integration")]
public sealed class WorkspaceSettingsAccessIntegrationTests : IAsyncLifetime
{
    private readonly PostgresTestContainer _db;
    private readonly OrgTreeTestHost _host;

    public WorkspaceSettingsAccessIntegrationTests(PostgresTestContainer db)
    {
        _db = db;
        _host = new OrgTreeTestHost(db);
    }

    public async Task InitializeAsync() => await new DatabaseReset(_db.OwnerConnectionString).ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Read_EveryActiveRoleGetsTheStoredValues_AndANonMemberIsToldTheWorkspaceDoesNotExist()
    {
        var stack = await _host.SeedWorkspaceAsync();
        using var asOwner = _host.CreateProvider(stack.AccountId, stack.OwnerId);
        await UpdateAsync(asOwner, stack.WorkspaceId, allowGuestInvites: true, defaultRole: "Guest", invitationExpiryDays: 14, guestDefaultExpiryDays: 90, size: "11-50", language: "en");
        var admin = await _host.AddMemberAsync(stack, WorkspaceRole.Admin, "Admin");
        var member = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Member");
        var guest = await _host.AddMemberAsync(stack, WorkspaceRole.Guest, "Guest");
        var outsider = await _host.AddOutsiderAsync(stack);

        foreach (var userId in new[] { stack.OwnerId, admin, member, guest })
        {
            using var provider = _host.CreateProvider(stack.AccountId, userId);
            var settings = await OrgTreeTestHost.QueryAsync(provider, new GetWorkspaceSettingsQuery(stack.WorkspaceId));
            settings.Should().BeEquivalentTo(new WorkspaceSettingsDto(
                false, false, true, "Guest", 14, 90, "11-50", "en", "Asia/Ho_Chi_Minh", "monday"));
        }

        using var asOutsider = _host.CreateProvider(stack.AccountId, outsider);
        var read = () => OrgTreeTestHost.QueryAsync(asOutsider, new GetWorkspaceSettingsQuery(stack.WorkspaceId));
        await read.Should().ThrowAsync<AppNotFound>("a person who is not an active member gets the answer for a missing id");
    }

    [Fact]
    public async Task Read_AnotherTenantsOwner_IsToldTheWorkspaceDoesNotExist_AndTheMadeUpIdAnswersTheSame()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var other = await _host.SeedWorkspaceAsync();
        using var asOtherOwner = _host.CreateProvider(other.AccountId, other.OwnerId);

        var foreign = await Capture(() => OrgTreeTestHost.QueryAsync(asOtherOwner, new GetWorkspaceSettingsQuery(stack.WorkspaceId)));
        var madeUp = await Capture(() => OrgTreeTestHost.QueryAsync(asOtherOwner, new GetWorkspaceSettingsQuery(Guid.NewGuid())));

        foreign.Should().BeOfType<AppNotFound>("another tenant's workspace answers like a made-up id");
        madeUp.Should().BeOfType<AppNotFound>();
    }

    // Admin is not asserted: the write needs ManageWorkspace, which the role baseline gives the Owner only, while the
    // catalog row lists Owner and Admin. That is an open product question, so no test pins either answer for Admin.
    [Fact]
    public async Task Write_TheOwnerSavesWithTheVersionRead_VersionMovesByOne_AndTheNextWriteCarriesIt()
    {
        var stack = await _host.SeedWorkspaceAsync();
        using var provider = _host.CreateProvider(stack.AccountId, stack.OwnerId);
        var before = await VersionAsync(stack.WorkspaceId);

        await OrgTreeTestHost.SendAsync(provider, new UpdateWorkspaceSettingsCommand(stack.WorkspaceId, false, true, "Member", 14, before));
        var after = await VersionAsync(stack.WorkspaceId);
        after.Should().Be(before + 1, "a saved change moves the version by one");

        await OrgTreeTestHost.SendAsync(provider, new UpdateWorkspaceSettingsCommand(stack.WorkspaceId, false, true, "Member", 21, after));
        (await VersionAsync(stack.WorkspaceId)).Should().Be(after + 1, "a second save carries the version the first one produced");
        (await OrgTreeTestHost.QueryAsync(provider, new GetWorkspaceSettingsQuery(stack.WorkspaceId))).InvitationExpiryDays.Should().Be(21);
    }

    [Fact]
    public async Task Write_MemberAndGuest_AreForbidden_AnOutsiderIsNotFound_AndNothingChanges()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var member = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Member");
        var guest = await _host.AddMemberAsync(stack, WorkspaceRole.Guest, "Guest");
        var outsider = await _host.AddOutsiderAsync(stack);
        var version = await VersionAsync(stack.WorkspaceId);

        foreach (var userId in new[] { member, guest })
        {
            using var provider = _host.CreateProvider(stack.AccountId, userId);
            var write = () => OrgTreeTestHost.SendAsync(provider, new UpdateWorkspaceSettingsCommand(stack.WorkspaceId, true, true, "Member", 14, version));
            await write.Should().ThrowAsync<AppForbidden>("only an Owner or Admin changes the workspace settings");
        }

        using var asOutsider = _host.CreateProvider(stack.AccountId, outsider);
        var outsiderWrite = () => OrgTreeTestHost.SendAsync(asOutsider, new UpdateWorkspaceSettingsCommand(stack.WorkspaceId, true, true, "Member", 14, version));
        await outsiderWrite.Should().ThrowAsync<AppNotFound>();

        (await VersionAsync(stack.WorkspaceId)).Should().Be(version);
        using var asOwner = _host.CreateProvider(stack.AccountId, stack.OwnerId);
        (await OrgTreeTestHost.QueryAsync(asOwner, new GetWorkspaceSettingsQuery(stack.WorkspaceId))).AllowPublicSharing.Should().BeFalse();
    }

    [Fact]
    public async Task Write_AStaleVersionIs412_ChangesNothing_AndAMissingVersionIsNotAccepted()
    {
        var stack = await _host.SeedWorkspaceAsync();
        using var asOwner = _host.CreateProvider(stack.AccountId, stack.OwnerId);
        var read = await VersionAsync(stack.WorkspaceId);
        await OrgTreeTestHost.SendAsync(asOwner, new UpdateWorkspaceSettingsCommand(stack.WorkspaceId, false, true, "Member", 14, read));
        var current = await VersionAsync(stack.WorkspaceId);

        var stale = () => OrgTreeTestHost.SendAsync(asOwner, new UpdateWorkspaceSettingsCommand(stack.WorkspaceId, true, true, "Member", 3, read));
        (await stale.Should().ThrowAsync<PreconditionFailed>("another admin saved after this one read"))
            .Which.ErrorCode.Should().Be("common.precondition-failed", "the API maps this error to 412 Precondition Failed");

        (await VersionAsync(stack.WorkspaceId)).Should().Be(current, "a rejected write does not move the version");
        var settings = await OrgTreeTestHost.QueryAsync(asOwner, new GetWorkspaceSettingsQuery(stack.WorkspaceId));
        settings.InvitationExpiryDays.Should().Be(14);
        settings.AllowPublicSharing.Should().BeFalse("a rejected write changes nothing");

        var versionless = () => OrgTreeTestHost.SendAsync(asOwner, new UpdateWorkspaceSettingsCommand(stack.WorkspaceId, true, true, "Member", 3, 0));
        await versionless.Should().ThrowAsync<AppValidation>("a write without a version is not accepted");
    }

    [Fact]
    public async Task Write_AnUnchangedSave_IsANoOp_KeepsTheVersionAndWritesNoAuditFact()
    {
        var stack = await _host.SeedWorkspaceAsync();
        using var asOwner = _host.CreateProvider(stack.AccountId, stack.OwnerId);
        await UpdateAsync(asOwner, stack.WorkspaceId, allowGuestInvites: true, invitationExpiryDays: 14);
        var version = await VersionAsync(stack.WorkspaceId);
        var facts = await AuditFactsAsync(stack.WorkspaceId);

        await OrgTreeTestHost.SendAsync(asOwner, new UpdateWorkspaceSettingsCommand(stack.WorkspaceId, false, true, "Member", 14, version));

        (await VersionAsync(stack.WorkspaceId)).Should().Be(version, "saving the values already stored is a no-op");
        (await AuditFactsAsync(stack.WorkspaceId)).Should().Be(facts, "a no-op records nothing");
    }

    [Fact]
    public async Task Write_OpeningPublicSharing_RecordsOneWarningAuditFactInTheStore_AndStampsTheFlipInstant()
    {
        var stack = await _host.SeedWorkspaceAsync();
        using var asOwner = _host.CreateProvider(stack.AccountId, stack.OwnerId);
        _host.Now = OrgTreeTestHost.BaseTime.AddDays(1);

        await UpdateAsync(asOwner, stack.WorkspaceId, allowPublicSharing: true);

        var warnings = await _host.ReadAsync(c => c.EnterpriseAuditLogs
            .Where(a => a.WorkspaceId == stack.WorkspaceId && a.Action == "UpdateWorkspaceSettings")
            .Select(a => a.Severity).ToListAsync());
        warnings.Should().Equal("Warning");
        var stamp = await _host.ReadAsync(c => c.Workspaces.Where(w => w.Id == stack.WorkspaceId).Select(w => w.Settings.PublicSharingChangedAt).SingleAsync());
        stamp.Should().Be(OrgTreeTestHost.BaseTime.AddDays(1));
    }

    [Fact]
    public async Task Write_InvalidValues_AreRejectedAndChangeNothing()
    {
        var stack = await _host.SeedWorkspaceAsync();
        using var asOwner = _host.CreateProvider(stack.AccountId, stack.OwnerId);
        var version = await VersionAsync(stack.WorkspaceId);

        var cases = new (string Name, UpdateWorkspaceSettingsCommand Command)[]
        {
            ("expiry 0", new(stack.WorkspaceId, false, true, "Member", 0, version)),
            ("expiry 31", new(stack.WorkspaceId, false, true, "Member", 31, version)),
            ("owner as default role", new(stack.WorkspaceId, false, true, "Owner", 7, version)),
            ("size", new(stack.WorkspaceId, false, true, "Member", 7, version, null, "huge")),
            ("language", new(stack.WorkspaceId, false, true, "Member", 7, version, null, null, "xx")),
            ("guest expiry 45", new(stack.WorkspaceId, false, true, "Member", 7, version, 45)),
        };
        foreach (var (name, command) in cases)
        {
            var write = () => OrgTreeTestHost.SendAsync(asOwner, command);
            var failure = await write.Should().ThrowAsync<Exception>(name);
            failure.Which.Should().Match<Exception>(e => e is DomainBusinessRuleException || e is AppValidation, name);
        }

        (await VersionAsync(stack.WorkspaceId)).Should().Be(version, "no invalid write moves the version");
    }

    private static async Task<Exception?> Capture(Func<Task> action)
    {
        try { await action(); return null; }
        catch (Exception ex) { return ex; }
    }

    private async Task UpdateAsync(
        ServiceProvider provider,
        Guid workspaceId,
        bool allowPublicSharing = false,
        bool allowGuestInvites = false,
        string defaultRole = "Member",
        int invitationExpiryDays = 7,
        int? guestDefaultExpiryDays = null,
        string? size = null,
        string? language = null)
    {
        var version = await VersionAsync(workspaceId);
        await OrgTreeTestHost.SendAsync(provider, new UpdateWorkspaceSettingsCommand(
            workspaceId, allowPublicSharing, allowGuestInvites, defaultRole, invitationExpiryDays, version,
            guestDefaultExpiryDays, size, language));
    }

    private Task<long> VersionAsync(Guid workspaceId) =>
        _host.ReadAsync(c => c.Workspaces.Where(w => w.Id == workspaceId).Select(w => w.Version).SingleAsync());

    private Task<int> AuditFactsAsync(Guid workspaceId) =>
        _host.ReadAsync(c => c.EnterpriseAuditLogs.CountAsync(a => a.WorkspaceId == workspaceId && a.Action == "UpdateWorkspaceSettings"));
}
