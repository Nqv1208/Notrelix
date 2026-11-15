using Notrelix.Application.Features.Collaboration.Preferences.Commands.UpdateNotificationPreferences;
using Notrelix.Application.Features.Collaboration.Preferences.DTOs;
using Notrelix.Application.Features.Collaboration.Preferences.Queries.GetNotificationPreferences;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Integration.Tests.Workspaces.OrgTree;
using PreconditionFailed = Notrelix.Application.Common.Exceptions.PreconditionFailedException;

namespace Notrelix.Integration.Tests.Collaboration;

/// <summary>
/// The notification preference matrix owned by Collaboration on PostgreSQL through the real pipeline
/// (NRX-003, NRX-004, NRX-012): a person reads and changes only their own matrix, one for every workspace,
/// defaults fill unchanged cells, stale versions are refused and unknown types are rejected atomically.
/// </summary>
[Collection("Database")]
[Trait("Category", "Integration")]
public sealed class NotificationPreferencesIntegrationTests : IAsyncLifetime
{
    private readonly PostgresTestContainer _db;
    private readonly OrgTreeTestHost _host;

    public NotificationPreferencesIntegrationTests(PostgresTestContainer db)
    {
        _db = db;
        _host = new OrgTreeTestHost(db);
    }

    public async Task InitializeAsync() => await new DatabaseReset(_db.OwnerConnectionString).ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Read_WithoutAChange_ReturnsTheAcceptedDefaultsAtVersionZero()
    {
        var stack = await _host.SeedWorkspaceAsync();

        var dto = await ReadAsync(stack, stack.OwnerId);

        dto.Version.Should().Be(0);
        dto.Rows.Select(r => r.Type).Should().Equal("assigned", "mention", "approval", "overdue", "watching", "invitation", "digest");
        dto.Rows.Should().OnlyContain(r => r.InApp);
        dto.Rows.Where(r => r.Email).Select(r => r.Type).Should().BeEquivalentTo("assigned", "mention", "approval", "invitation");
        (await _host.ReadAsync(c => c.NotificationPreferenceSets.CountAsync())).Should().Be(0, "no row exists until the person changes something");
    }

    [Fact]
    public async Task Update_PersistsOwnChoices_AndReadsThemBack_WithVersioning()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var member = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Member");

        var first = await UpdateAsync(stack, member, 0, new NotificationPreferenceRowChange("mention", false, null), new NotificationPreferenceRowChange("digest", null, true));
        first.Version.Should().Be(1);

        var second = await UpdateAsync(stack, member, 1, new NotificationPreferenceRowChange("overdue", true, true));
        second.Version.Should().Be(2);

        var read = await ReadAsync(stack, member);
        read.Version.Should().Be(2);
        read.Rows.Single(r => r.Type == "mention").Should().Be(new NotificationPreferenceRowDto("mention", false, true));
        read.Rows.Single(r => r.Type == "digest").Should().Be(new NotificationPreferenceRowDto("digest", true, true));
        read.Rows.Single(r => r.Type == "overdue").Should().Be(new NotificationPreferenceRowDto("overdue", true, true));
        read.Rows.Single(r => r.Type == "assigned").Should().Be(new NotificationPreferenceRowDto("assigned", true, true));

        (await _host.ReadAsync(c => c.NotificationPreferenceSets.CountAsync())).Should().Be(1);
    }

    [Fact]
    public async Task Update_ValuesThatAlreadyHold_AreANoOp_AndKeepTheVersion()
    {
        var stack = await _host.SeedWorkspaceAsync();
        await UpdateAsync(stack, stack.OwnerId, 0, new NotificationPreferenceRowChange("mention", false, null));

        var noOp = await UpdateAsync(stack, stack.OwnerId, 1, new NotificationPreferenceRowChange("mention", false, null), new NotificationPreferenceRowChange("assigned", true, true));

        noOp.Version.Should().Be(1);
        (await _host.ReadAsync(c => c.NotificationPreferenceSets.Select(s => s.Version).SingleAsync())).Should().Be(1);

        var firstNoOp = await UpdateAsync(stack, await _host.AddMemberAsync(stack, WorkspaceRole.Member), 0, new NotificationPreferenceRowChange("mention", true, true));
        firstNoOp.Version.Should().Be(0);
        (await _host.ReadAsync(c => c.NotificationPreferenceSets.CountAsync())).Should().Be(1, "a no-op first save creates no row");
    }

    [Fact]
    public async Task Update_AStaleExpectedVersion_IsRefused_AndChangesNothing()
    {
        var stack = await _host.SeedWorkspaceAsync();
        await UpdateAsync(stack, stack.OwnerId, 0, new NotificationPreferenceRowChange("mention", false, null));

        var stale = () => UpdateAsync(stack, stack.OwnerId, 0, new NotificationPreferenceRowChange("overdue", true, true));
        var ahead = () => UpdateAsync(stack, stack.OwnerId, 9, new NotificationPreferenceRowChange("overdue", true, true));

        (await stale.Should().ThrowAsync<PreconditionFailed>()).Which.CurrentVersion.Should().Be(1);
        await ahead.Should().ThrowAsync<PreconditionFailed>();
        (await ReadAsync(stack, stack.OwnerId)).Rows.Single(r => r.Type == "overdue").Email.Should().BeFalse();
    }

    [Fact]
    public async Task Update_AnUnknownType_IsRejected_WithoutPartialChange()
    {
        var stack = await _host.SeedWorkspaceAsync();

        var act = () => UpdateAsync(stack, stack.OwnerId, 0, new NotificationPreferenceRowChange("digest", true, true), new NotificationPreferenceRowChange("nonsense", true, null));

        await act.Should().ThrowAsync<Exception>();
        (await _host.ReadAsync(c => c.NotificationPreferenceSets.CountAsync())).Should().Be(0);
    }

    [Fact]
    public async Task Preferences_ArePerPerson_AndTheSameInEveryWorkspace()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var member = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Member");
        await UpdateAsync(stack, member, 0, new NotificationPreferenceRowChange("mention", false, false));

        (await ReadAsync(stack, member)).Rows.Single(r => r.Type == "mention").Should().Be(new NotificationPreferenceRowDto("mention", false, false));
        (await ReadAsync(stack, stack.OwnerId)).Rows.Single(r => r.Type == "mention").Should().Be(new NotificationPreferenceRowDto("mention", true, true),
            "another member's matrix is never the caller's, a workspace admin's included");

        // The same person reads the one matrix from any other workspace's context.
        var second = await _host.SeedWorkspaceAsync();
        var fromSecond = await ReadAsync(second, member);
        fromSecond.Version.Should().Be(1);
        fromSecond.Rows.Single(r => r.Type == "mention").Should().Be(new NotificationPreferenceRowDto("mention", false, false));
        (await _host.ReadAsync(c => c.NotificationPreferenceSets.CountAsync())).Should().Be(1);
    }

    [Fact]
    public async Task AGuestOfTheWorkspace_ManagesOnlyTheirOwnMatrix()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var guest = await _host.AddMemberAsync(stack, WorkspaceRole.Guest, "Guest");

        var saved = await UpdateAsync(stack, guest, 0, new NotificationPreferenceRowChange("watching", false, null));

        saved.Version.Should().Be(1);
        (await ReadAsync(stack, stack.OwnerId)).Version.Should().Be(0);
    }

    [Fact]
    public async Task APersonWithoutAnyWorkspaceMembership_StillManagesTheirOwnMatrix()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var outsider = await _host.AddOutsiderAsync(stack);

        (await ReadAsync(stack, outsider)).Version.Should().Be(0);
        var saved = await UpdateAsync(stack, outsider, 0, new NotificationPreferenceRowChange("mention", false, null));

        saved.Version.Should().Be(1);
        (await ReadAsync(stack, stack.OwnerId)).Version.Should().Be(0);
    }

    private async Task<NotificationPreferencesDto> ReadAsync(OrgStack stack, Guid userId)
    {
        using var provider = _host.CreateProvider(stack.AccountId, userId);
        return await OrgTreeTestHost.QueryAsync(provider, new GetNotificationPreferencesQuery());
    }

    private async Task<NotificationPreferencesDto> UpdateAsync(
        OrgStack stack, Guid userId, long expectedVersion, params NotificationPreferenceRowChange[] rows)
    {
        using var provider = _host.CreateProvider(stack.AccountId, userId);
        var result = await OrgTreeTestHost.SendAsync(provider, new UpdateNotificationPreferencesCommand(expectedVersion, rows));
        return ((Application.Common.Models.Result<NotificationPreferencesDto>)result!).Data!;
    }
}
