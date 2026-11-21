using Microsoft.Extensions.DependencyInjection;
using Notrelix.Application.Features.Accounts.Public.Membership;
using Notrelix.Application.Features.Analytics.Abstractions;
using Notrelix.Application.Features.Billing.Public.Seats;
using Notrelix.Application.Features.WorkManagement.Public.ItemPlacement;
using Notrelix.Application.Features.Workspaces.Members.Commands.AddMember;
using Notrelix.Application.Features.Workspaces.Members.Commands.RemoveMember;
using Notrelix.Application.Features.Workspaces.Members.Commands.SuspendMember;
using Notrelix.Application.Features.Workspaces.Members.Services;
using Notrelix.Application.Features.Workspaces.Workspaces.Commands.ArchiveWorkspace;
using Notrelix.Domain.Workspaces;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Data.Authz;
using Notrelix.Infrastructure.Workspaces.Members;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Integration.Tests.Workspaces.OrgTree;
using AppForbidden = Notrelix.Application.Common.Exceptions.ForbiddenException;
using AppNotFound = Notrelix.Application.Common.Exceptions.NotFoundException;
using DomainBusinessRuleException = Notrelix.Domain.Common.Exceptions.BusinessRuleException;

namespace Notrelix.Integration.Tests.Workspaces;

/// <summary>
/// Adding an existing user to a workspace directly, through the real pipeline and the real policy engine on
/// PostgreSQL: the target must be an active member of the same Account and Billing must have a seat for the role
/// (a refused add leaves no row, no grant change and no status change); an Admin adds only a Member or a Guest;
/// an active member is not added twice; a Removed or Suspended person returns active with the new role (and the
/// seat gate applies to them too); a Member, a Guest and a stranger cannot add; an archived workspace refuses the
/// write with <c>workspace.archived</c>.
/// </summary>
[Collection("Database")]
[Trait("Category", "Integration")]
public sealed class AddMemberDirectIntegrationTests : IAsyncLifetime
{
    private readonly PostgresTestContainer _db;
    private readonly OrgTreeTestHost _host;

    public AddMemberDirectIntegrationTests(PostgresTestContainer db)
    {
        _db = db;
        _host = new OrgTreeTestHost(db);
    }

    public async Task InitializeAsync() => await new DatabaseReset(_db.OwnerConnectionString).ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    private sealed class SeatGate(bool admit) : IBillingSeatGate
    {
        public List<bool> AskedAsGuest { get; } = [];

        public Task<bool> CanAdmitAsync(Guid accountId, Guid workspaceId, Guid userId, bool asGuest, CancellationToken cancellationToken)
        {
            AskedAsGuest.Add(asGuest);
            return Task.FromResult(admit);
        }

        public Task<bool> HasFreeSeatAsync(Guid accountId, Guid workspaceId, bool asGuest, CancellationToken cancellationToken) =>
            Task.FromResult(admit);
    }

    private static void RegisterServices(IServiceCollection services, IBillingSeatGate seatGate)
    {
        services.AddSingleton(new Moq.Mock<Notrelix.Application.Common.Realtime.IAccessChangeSignal>().Object);
        services.AddScoped<AccessGrantProjectionService>();
        services.AddScoped<IWorkspaceGrantProjectionService, WorkspaceGrantProjectionServiceAdapter>();
        services.AddScoped<IWorkspaceOwnerUpdateLocker, WorkspaceOwnerUpdateLocker>();
        services.AddScoped<IReportingDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());

        var source = new Mock<IWorkItemProjectionSourceAdapter>();
        source.Setup(s => s.GetWorkspacePlacementsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<WorkItemPlacementSnapshot>());
        services.AddSingleton(source.Object);
        services.AddScoped<Notrelix.Application.Features.Analytics.Placements.Services.WorkspaceWorkItemPlacementService>();

        services.AddScoped<IIntegrationEventCollector, IntegrationEventCollector>();
        services.AddSingleton(seatGate);
        services.AddScoped<IAccountMembershipFacts, Notrelix.Application.Features.Accounts.Members.AccountMembershipFactsProvider>();
    }

    private ServiceProvider As(OrgStack stack, Guid userId, IBillingSeatGate? seatGate = null) =>
        _host.CreateProvider(stack.AccountId, userId, s => RegisterServices(s, seatGate ?? new SeatGate(true)));

    private async Task<(OrgStack Stack, Guid Admin, Guid Member, Guid Guest, Guid Target)> SeedAsync()
    {
        var stack = await _host.SeedWorkspaceAsync();
        return (
            stack,
            await _host.AddMemberAsync(stack, WorkspaceRole.Admin, "Admin"),
            await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Member"),
            await _host.AddMemberAsync(stack, WorkspaceRole.Guest, "Guest"),
            await _host.AddOutsiderAsync(stack));
    }

    private Task<WorkspaceMember?> RowAsync(OrgStack stack, Guid userId) =>
        _host.ReadAsync(c => c.WorkspaceMembers.SingleOrDefaultAsync(m => m.WorkspaceId == stack.WorkspaceId && m.UserId == userId));

    private Task<string?> ActiveGrantRoleAsync(OrgStack stack, Guid userId) =>
        _host.ReadAsync(async c => (await c.AccessGrants
            .Where(g => g.WorkspaceId == stack.WorkspaceId && g.UserId == userId && g.RevokedAt == null)
            .Select(g => g.RoleCodes)
            .SingleOrDefaultAsync())?.SingleOrDefault());

    private Task<int> GrantRowsAsync(OrgStack stack, Guid userId) =>
        _host.ReadAsync(c => c.AccessGrants.CountAsync(g => g.WorkspaceId == stack.WorkspaceId && g.UserId == userId));

    private async Task MoveToAsync(OrgStack stack, Guid userId, WorkspaceMemberStatus status)
    {
        using var asOwner = As(stack, stack.OwnerId);
        var version = (await RowAsync(stack, userId))!.Version;
        if (status == WorkspaceMemberStatus.Suspended)
            await OrgTreeTestHost.SendAsync(asOwner, new SuspendMemberCommand(stack.WorkspaceId, userId, version));
        else
            await OrgTreeTestHost.SendAsync(asOwner, new RemoveMemberCommand(stack.WorkspaceId, userId, version));
    }

    [Theory]
    [InlineData("Owner", WorkspaceRole.Admin)]
    [InlineData("Owner", WorkspaceRole.Member)]
    [InlineData("Owner", WorkspaceRole.Guest)]
    [InlineData("Admin", WorkspaceRole.Member)]
    [InlineData("Admin", WorkspaceRole.Guest)]
    public async Task AnOwnerOrAdminAddsAnAccountMember_ActiveRowAndGrantTheRoleCaps_AndTheSeatGateWasAsked(string actor, WorkspaceRole role)
    {
        var (stack, admin, _, _, target) = await SeedAsync();
        var seats = new SeatGate(true);
        using var asActor = As(stack, actor == "Owner" ? stack.OwnerId : admin, seats);

        await OrgTreeTestHost.SendAsync(asActor, new AddMemberCommand(stack.WorkspaceId, target, role));

        var row = (await RowAsync(stack, target))!;
        row.Status.Should().Be(WorkspaceMemberStatus.Active);
        row.Role.Should().Be(role);
        (await ActiveGrantRoleAsync(stack, target)).Should().Be(role.ToString(), "the access grant is synced in the same write");
        seats.AskedAsGuest.Should().Equal(role == WorkspaceRole.Guest);
    }

    [Theory]
    [InlineData(WorkspaceRole.Member)]
    [InlineData(WorkspaceRole.Guest)]
    public async Task AnAccountMemberOfAnotherAccountOrAnUnknownUser_IsRefusedWithNotInAccount_AndNothingChanges(WorkspaceRole role)
    {
        var (stack, admin, _, _, _) = await SeedAsync();
        var otherAccountUser = (await _host.SeedWorkspaceAsync()).OwnerId;
        var seats = new SeatGate(true);
        using var asAdmin = As(stack, admin, seats);

        foreach (var target in new[] { otherAccountUser, Guid.CreateVersion7() })
        {
            var act = () => OrgTreeTestHost.SendAsync(asAdmin, new AddMemberCommand(stack.WorkspaceId, target, role));

            (await act.Should().ThrowAsync<DomainBusinessRuleException>())
                .Which.RuleCode.Should().Be(WorkspaceRuleCodes.Workspaces_Member_NotInAccount);
            (await RowAsync(stack, target)).Should().BeNull();
            (await GrantRowsAsync(stack, target)).Should().Be(0);
        }

        seats.AskedAsGuest.Should().BeEmpty("the Account check comes before Billing is asked");
    }

    [Theory]
    [InlineData(WorkspaceRole.Member)]
    [InlineData(WorkspaceRole.Guest)]
    public async Task WhenBillingHasNoSeat_TheAddIsRefusedWithSeatUnavailable_AndNothingChanges(WorkspaceRole role)
    {
        var (stack, admin, _, _, target) = await SeedAsync();
        var seats = new SeatGate(false);
        using var asAdmin = As(stack, admin, seats);

        var act = () => OrgTreeTestHost.SendAsync(asAdmin, new AddMemberCommand(stack.WorkspaceId, target, role));

        (await act.Should().ThrowAsync<DomainBusinessRuleException>())
            .Which.RuleCode.Should().Be(WorkspaceRuleCodes.Workspaces_Member_SeatUnavailable);
        (await RowAsync(stack, target)).Should().BeNull();
        (await GrantRowsAsync(stack, target)).Should().Be(0);
        seats.AskedAsGuest.Should().Equal(role == WorkspaceRole.Guest);
    }

    [Theory]
    [InlineData(WorkspaceRole.Owner)]
    [InlineData(WorkspaceRole.Admin)]
    public async Task AnAdminCannotAddAnOwnerOrAnAdmin_AndNothingChanges(WorkspaceRole role)
    {
        var (stack, admin, _, _, target) = await SeedAsync();
        using var asAdmin = As(stack, admin);

        var act = () => OrgTreeTestHost.SendAsync(asAdmin, new AddMemberCommand(stack.WorkspaceId, target, role));

        (await act.Should().ThrowAsync<DomainBusinessRuleException>())
            .Which.RuleCode.Should().Be(WorkspaceRuleCodes.Workspaces_Member_ActorCannotAssignRole);
        (await RowAsync(stack, target)).Should().BeNull();
        (await GrantRowsAsync(stack, target)).Should().Be(0);
    }

    [Fact]
    public async Task AnOwnerCannotAddAnOwnerDirectly_OwnershipMovesOnlyThroughTheTransferWorkflow()
    {
        var (stack, _, _, _, target) = await SeedAsync();
        using var asOwner = As(stack, stack.OwnerId);

        var act = () => OrgTreeTestHost.SendAsync(asOwner, new AddMemberCommand(stack.WorkspaceId, target, WorkspaceRole.Owner));

        (await act.Should().ThrowAsync<DomainBusinessRuleException>())
            .Which.RuleCode.Should().Be(WorkspaceRuleCodes.Workspaces_Member_CannotDirectlyAssignOwner);
        (await RowAsync(stack, target)).Should().BeNull();
        (await GrantRowsAsync(stack, target)).Should().Be(0);
    }

    [Fact]
    public async Task AnActiveMemberIsNotAddedTwice_TheRoleAndVersionStay()
    {
        var (stack, _, member, _, _) = await SeedAsync();
        var before = (await RowAsync(stack, member))!;
        using var asOwner = As(stack, stack.OwnerId);

        var act = () => OrgTreeTestHost.SendAsync(asOwner, new AddMemberCommand(stack.WorkspaceId, member, WorkspaceRole.Guest));

        await act.Should().ThrowAsync<Notrelix.Application.Common.Exceptions.BusinessRuleException>();
        var after = (await RowAsync(stack, member))!;
        after.Role.Should().Be(before.Role);
        after.Version.Should().Be(before.Version);
    }

    [Theory]
    [InlineData(WorkspaceMemberStatus.Removed, WorkspaceRole.Guest)]
    [InlineData(WorkspaceMemberStatus.Removed, WorkspaceRole.Admin)]
    [InlineData(WorkspaceMemberStatus.Suspended, WorkspaceRole.Guest)]
    [InlineData(WorkspaceMemberStatus.Suspended, WorkspaceRole.Admin)]
    public async Task ARemovedOrSuspendedPersonIsActiveAgainWithTheNewRole_OnTheSameRow(WorkspaceMemberStatus status, WorkspaceRole role)
    {
        var (stack, _, member, _, _) = await SeedAsync();
        await MoveToAsync(stack, member, status);
        var rowId = (await RowAsync(stack, member))!.Id;
        var seats = new SeatGate(true);
        using var asOwner = As(stack, stack.OwnerId, seats);

        await OrgTreeTestHost.SendAsync(asOwner, new AddMemberCommand(stack.WorkspaceId, member, role));

        var row = (await RowAsync(stack, member))!;
        row.Id.Should().Be(rowId, "the same membership row returns");
        row.Status.Should().Be(WorkspaceMemberStatus.Active);
        row.Role.Should().Be(role);
        (await ActiveGrantRoleAsync(stack, member)).Should().Be(role.ToString());
        seats.AskedAsGuest.Should().Equal(role == WorkspaceRole.Guest);
    }

    [Theory]
    [InlineData(WorkspaceMemberStatus.Removed, WorkspaceRole.Member)]
    [InlineData(WorkspaceMemberStatus.Removed, WorkspaceRole.Guest)]
    [InlineData(WorkspaceMemberStatus.Suspended, WorkspaceRole.Member)]
    [InlineData(WorkspaceMemberStatus.Suspended, WorkspaceRole.Guest)]
    public async Task AReturningPersonWhoNeedsASeatWithNoneFree_IsRefusedAndStaysAsTheyWere(WorkspaceMemberStatus status, WorkspaceRole role)
    {
        var (stack, _, member, _, _) = await SeedAsync();
        await MoveToAsync(stack, member, status);
        var before = (await RowAsync(stack, member))!;
        using var asOwner = As(stack, stack.OwnerId, new SeatGate(false));

        var act = () => OrgTreeTestHost.SendAsync(asOwner, new AddMemberCommand(stack.WorkspaceId, member, role));

        (await act.Should().ThrowAsync<DomainBusinessRuleException>())
            .Which.RuleCode.Should().Be(WorkspaceRuleCodes.Workspaces_Member_SeatUnavailable);
        var after = (await RowAsync(stack, member))!;
        after.Status.Should().Be(status);
        after.Version.Should().Be(before.Version);
        (await ActiveGrantRoleAsync(stack, member)).Should().BeNull("a refused return does not bring the grant back");
    }

    [Theory]
    [InlineData("Member")]
    [InlineData("Guest")]
    public async Task AMemberOrAGuestCannotAdd_AndNothingChanges(string actor)
    {
        var (stack, _, member, guest, target) = await SeedAsync();
        using var asActor = As(stack, actor == "Member" ? member : guest);

        var act = () => OrgTreeTestHost.SendAsync(asActor, new AddMemberCommand(stack.WorkspaceId, target, WorkspaceRole.Member));

        await act.Should().ThrowAsync<AppForbidden>();
        (await RowAsync(stack, target)).Should().BeNull();
        (await GrantRowsAsync(stack, target)).Should().Be(0);
    }

    [Fact]
    public async Task AStrangerToTheWorkspace_GetsNotFound_AndNothingChanges()
    {
        var (stack, _, _, _, target) = await SeedAsync();
        var stranger = await _host.AddOutsiderAsync(stack);
        using var asStranger = As(stack, stranger);

        var act = () => OrgTreeTestHost.SendAsync(asStranger, new AddMemberCommand(stack.WorkspaceId, target, WorkspaceRole.Member));

        await act.Should().ThrowAsync<AppNotFound>();
        (await RowAsync(stack, target)).Should().BeNull();
        (await GrantRowsAsync(stack, target)).Should().Be(0);
    }

    [Theory]
    [InlineData("Owner")]
    [InlineData("Admin")]
    public async Task AnAddInAnArchivedWorkspace_IsRefusedWithTheArchivedCode_AndNothingChanges(string actor)
    {
        var (stack, admin, _, _, target) = await SeedAsync();
        using (var asOwner = As(stack, stack.OwnerId))
        {
            var version = await _host.ReadAsync(c => c.Workspaces.Where(w => w.Id == stack.WorkspaceId).Select(w => w.Version).SingleAsync());
            await OrgTreeTestHost.SendAsync(asOwner, new ArchiveWorkspaceCommand(stack.WorkspaceId, version));
        }

        using var asActor = As(stack, actor == "Owner" ? stack.OwnerId : admin);
        var act = () => OrgTreeTestHost.SendAsync(asActor, new AddMemberCommand(stack.WorkspaceId, target, WorkspaceRole.Member));

        (await act.Should().ThrowAsync<AppForbidden>()).Which.Code.Should().Be("workspace.archived");
        (await RowAsync(stack, target)).Should().BeNull();
        (await GrantRowsAsync(stack, target)).Should().Be(0);
    }
}
