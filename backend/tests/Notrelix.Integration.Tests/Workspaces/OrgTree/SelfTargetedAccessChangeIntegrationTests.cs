using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Notrelix.Application.Common.Realtime;
using Notrelix.Application.Features.Governance.ResourcePermissions.Commands.RevokeResourcePermission;
using Notrelix.Application.Features.WorkManagement.Boards.Commands.RemoveBoardMember;
using Notrelix.Application.Features.WorkManagement.Boards.Commands.UpdateBoardMemberRole;
using Notrelix.Application.Features.Workspaces.Members.Commands.RemoveMember;
using Notrelix.Application.Features.Workspaces.Members.Commands.SuspendMember;
using Notrelix.Application.Features.Workspaces.Members.Commands.UpdateMemberRole;
using Notrelix.Application.Features.Workspaces.Members.Services;
using Notrelix.Application.Features.Workspaces.Spaces.Commands.AddSpaceMember;
using Notrelix.Application.Features.Workspaces.Spaces.Commands.ChangeSpaceMemberRole;
using Notrelix.Application.Features.Workspaces.Spaces.Commands.RemoveSpaceMember;
using Notrelix.Application.Features.Workspaces.Teams.Commands.RemoveTeamMember;
using Notrelix.Domain.Governance.Permissions;
using Notrelix.Domain.Workspaces;
using Notrelix.Domain.WorkManagement.Boards;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Domain.Workspaces.Spaces;
using Notrelix.Domain.Workspaces.Teams;
using Notrelix.Infrastructure;
using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Data.Authz;
using Notrelix.Infrastructure.Data.Interceptors;
using Notrelix.Infrastructure.Data.Messaging;
using Notrelix.Infrastructure.Workspaces.Members;
using Notrelix.Integration.Tests.Containers;

using DomainBusinessRuleException = Notrelix.Domain.Common.Exceptions.BusinessRuleException;

namespace Notrelix.Integration.Tests.Workspaces.OrgTree;

/// <summary>
/// A person who removes or lowers their own access (NRX-003, NRX-009) on PostgreSQL through the real pipeline
/// as the application role, with the real access signal and the real outbox interceptor. The signal row is
/// written under the outbox and event-log insert policies, which admit a row only while the writer still holds
/// an active workspace grant. Removing or suspending yourself would end that grant before those rows are
/// written, so it is refused (leaving the workspace is the self path); every other self-targeted change keeps
/// its grant and succeeds.
/// </summary>
[Collection("Database")]
[Trait("Category", "Integration")]
public sealed class SelfTargetedAccessChangeIntegrationTests : IAsyncLifetime
{
    private readonly PostgresTestContainer _db;
    private readonly OrgTreeTestHost _host;

    public SelfTargetedAccessChangeIntegrationTests(PostgresTestContainer db)
    {
        _db = db;
        _host = new OrgTreeTestHost(db);
    }

    public async Task InitializeAsync() => await new DatabaseReset(_db.OwnerConnectionString).ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    /// <summary>The application-role context with the production outbox interceptor and the real signal.</summary>
    private void RegisterAppRole(IServiceCollection services)
    {
        services.AddScoped<IIntegrationEventCollector, IntegrationEventCollector>();
        services.AddScoped<IAccessChangeSignal, AccessChangeSignal>();
        services.AddMessaging(new ConfigurationBuilder().Build());
        services.AddSingleton<IEventTypeRegistry, Notrelix.Infrastructure.Messaging.EventTypeRegistry>();
        services.AddSingleton<IClassificationPolicy>(_ => Notrelix.Infrastructure.Events.ClassificationPolicy.CreateBuilder().Build());
        services.AddSingleton<IDeliveryPolicy>(_ => Notrelix.Infrastructure.Events.DeliveryPolicy.CreateBuilder().Build());
        services.AddScoped<DomainEventInterceptor>();
        services.AddScoped<ApplicationDbContext>(sp => _db.CreateAppContext(
            sp.GetRequiredService<ICurrentTenantContext>(), sp.GetRequiredService<DomainEventInterceptor>()));

        services.AddScoped<AccessGrantProjectionService>();
        services.AddScoped<IWorkspaceGrantProjectionService, WorkspaceGrantProjectionServiceAdapter>();
        services.AddScoped<IWorkspaceOwnerUpdateLocker, WorkspaceOwnerUpdateLocker>();
    }

    private ServiceProvider As(OrgStack stack, Guid userId) => _host.CreateProvider(stack.AccountId, userId, RegisterAppRole);

    private Task<long> BoardVersionAsync(Guid board) =>
        _host.ReadAsync(c => c.Boards.Where(x => x.Id == board).Select(x => x.Version).SingleAsync());

    private Task<long> SpaceVersionAsync(Guid space) => _host.SpaceVersionAsync(space);

    private Task<long> MemberVersionAsync(OrgStack stack, Guid userId) =>
        _host.ReadAsync(c => c.WorkspaceMembers
            .Where(m => m.WorkspaceId == stack.WorkspaceId && m.UserId == userId).Select(m => m.Version).SingleAsync());

    /// <summary>Access-signal rows the request left in the outbox for this workspace.</summary>
    private Task<int> SignalRowsAsync(OrgStack stack) =>
        _host.ReadAsync(c => c.Set<MessagingOutboxMessage>().IgnoreQueryFilters()
            .CountAsync(m => m.WorkspaceId == stack.WorkspaceId && m.MessageName.Contains("realtime")));

    private async Task<Guid> BoardWithTwoOwnersAsync(OrgStack stack, Guid actor)
    {
        var board = await _host.SeedBoardAsync(stack, "Ledger", BoardVisibility.Private);
        await _host.SeedBoardMemberAsync(board, actor, BoardRole.Owner);
        await _host.SeedBoardMemberAsync(board, stack.OwnerId, BoardRole.Owner);
        return board;
    }

    private async Task<Guid> SpaceWithTwoAdminsAsync(OrgStack stack, Guid actor)
    {
        var space = await _host.SeedSpaceAsync(stack, "Accounting", SpaceVisibility.Workspace, actor);
        using var asOwner = As(stack, stack.OwnerId);
        await OrgTreeTestHost.SendAsync(asOwner,
            new AddSpaceMemberCommand(stack.WorkspaceId, space, "User", stack.OwnerId, "Admin", await SpaceVersionAsync(space)));
        return space;
    }

    [Fact]
    public async Task BoardOwner_RemovesThemself_AndTheBoardKeepsItsOtherOwner()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var actor = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Lead");
        var board = await BoardWithTwoOwnersAsync(stack, actor);
        using var asActor = As(stack, actor);

        await OrgTreeTestHost.SendIdempotentAsync(asActor, new RemoveBoardMemberCommand(board, actor, await BoardVersionAsync(board)));

        (await _host.ReadAsync(c => c.BoardMembers.AnyAsync(m => m.BoardId == board && m.UserId == actor))).Should().BeFalse();
        (await SignalRowsAsync(stack)).Should().BeGreaterThan(0, "a board removal still tells the person's client");
    }

    [Fact]
    public async Task BoardOwner_LowersTheirOwnRole()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var actor = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Lead");
        var board = await BoardWithTwoOwnersAsync(stack, actor);
        using var asActor = As(stack, actor);

        await OrgTreeTestHost.SendIdempotentAsync(asActor,
            new UpdateBoardMemberRoleCommand(board, actor, BoardRole.Observer, await BoardVersionAsync(board)));

        (await _host.ReadAsync(c => c.BoardMembers.Where(m => m.BoardId == board && m.UserId == actor).Select(m => m.Role).SingleAsync()))
            .Should().Be(BoardRole.Observer);
    }

    [Fact]
    public async Task BoardManager_RevokesTheirOwnExplicitPermission()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var actor = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Lead");
        var board = await _host.SeedBoardAsync(stack, "Ledger");
        await _host.SeedBoardMemberAsync(board, stack.OwnerId, BoardRole.Owner);
        await _host.SeedResourcePermissionAsync(stack, "work-management.board", board, PermissionSubjectType.User, actor, PermissionLevel.Manager);
        var permission = await _host.ReadAsync(c => c.ResourcePermissions.Where(p => p.SubjectId == actor).Select(p => p.Id).SingleAsync());
        using var asActor = As(stack, actor);

        await OrgTreeTestHost.SendAsync(asActor, new RevokeResourcePermissionCommand("work-management.board", board, permission));

        (await _host.ReadAsync(c => c.ResourcePermissions.AnyAsync(p => p.SubjectId == actor))).Should().BeFalse();
    }

    [Fact]
    public async Task SpaceAdmin_RemovesThemselfFromTheSpace()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var actor = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Lead");
        var space = await SpaceWithTwoAdminsAsync(stack, actor);
        using var asActor = As(stack, actor);

        await OrgTreeTestHost.SendAsync(asActor,
            new RemoveSpaceMemberCommand(stack.WorkspaceId, space, "User", actor, await SpaceVersionAsync(space)));

        (await _host.ReadAsync(c => c.Spaces.IgnoreQueryFilters().Where(s => s.Id == space)
            .SelectMany(s => s.Members).AnyAsync(m => m.SubjectId == actor))).Should().BeFalse();
    }

    [Fact]
    public async Task SpaceAdmin_LowersTheirOwnRole()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var actor = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Lead");
        var space = await SpaceWithTwoAdminsAsync(stack, actor);
        using var asActor = As(stack, actor);

        await OrgTreeTestHost.SendAsync(asActor,
            new ChangeSpaceMemberRoleCommand(stack.WorkspaceId, space, "User", actor, "Member", await SpaceVersionAsync(space)));

        (await _host.ReadAsync(c => c.Spaces.IgnoreQueryFilters().Where(s => s.Id == space)
            .SelectMany(s => s.Members).Where(m => m.SubjectId == actor).Select(m => m.Role).SingleAsync()))
            .Should().Be(SpaceRole.Member);
    }

    [Fact]
    public async Task WorkspaceAdmin_RemovesThemselfFromATeam()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var actor = await _host.AddMemberAsync(stack, WorkspaceRole.Admin, "Admin");
        var team = Team.Create(stack.AccountId, stack.WorkspaceId, "Accounting", stack.OwnerId, OrgTreeTestHost.BaseTime);
        team.AddMember(actor, TeamMemberRole.Member, stack.OwnerId, OrgTreeTestHost.BaseTime);
        await _host.WithSystemContextAsync(c => { c.Teams.Add(team); return Task.CompletedTask; });
        using var asActor = As(stack, actor);
        var version = await _host.ReadAsync(c => c.Teams.Where(t => t.Id == team.Id).Select(t => t.Version).SingleAsync());

        await OrgTreeTestHost.SendAsync(asActor, new RemoveTeamMemberCommand(stack.WorkspaceId, team.Id, actor, version));

        (await _host.ReadAsync(c => c.Teams.Where(t => t.Id == team.Id).SelectMany(t => t.Members).AnyAsync(m => m.UserId == actor && m.Status == TeamMemberStatus.Active)))
            .Should().BeFalse();
    }

    [Fact]
    public async Task WorkspaceOwner_LowersTheirOwnRole_WhenAnotherOwnerRemains()
    {
        var stack = await _host.SeedWorkspaceAsync();
        await _host.AddMemberAsync(stack, WorkspaceRole.Owner, "Second owner");
        using var asOwner = As(stack, stack.OwnerId);

        await OrgTreeTestHost.SendAsync(asOwner,
            new UpdateMemberRoleCommand(stack.WorkspaceId, stack.OwnerId, WorkspaceRole.Admin, await MemberVersionAsync(stack, stack.OwnerId)));

        (await _host.ReadAsync(c => c.WorkspaceMembers
            .Where(m => m.WorkspaceId == stack.WorkspaceId && m.UserId == stack.OwnerId).Select(m => m.Role).SingleAsync()))
            .Should().Be(WorkspaceRole.Admin);
    }

    [Fact]
    public async Task WorkspaceOwner_RemovingThemself_IsRefusedAndChangesNothing()
    {
        var stack = await _host.SeedWorkspaceAsync();
        await _host.AddMemberAsync(stack, WorkspaceRole.Owner, "Second owner");
        using var asOwner = As(stack, stack.OwnerId);
        var version = await MemberVersionAsync(stack, stack.OwnerId);

        var act = () => OrgTreeTestHost.SendAsync(asOwner, new RemoveMemberCommand(stack.WorkspaceId, stack.OwnerId, version));

        (await act.Should().ThrowAsync<DomainBusinessRuleException>()).Which.RuleCode
            .Should().Be(WorkspaceRuleCodes.Workspaces_Member_CannotActOnSelf);
        (await MemberVersionAsync(stack, stack.OwnerId)).Should().Be(version);
        (await GrantIsActiveAsync(stack, stack.OwnerId)).Should().BeTrue();
    }

    [Fact]
    public async Task WorkspaceOwner_SuspendingThemself_IsRefusedAndChangesNothing()
    {
        var stack = await _host.SeedWorkspaceAsync();
        await _host.AddMemberAsync(stack, WorkspaceRole.Owner, "Second owner");
        using var asOwner = As(stack, stack.OwnerId);
        var version = await MemberVersionAsync(stack, stack.OwnerId);

        var act = () => OrgTreeTestHost.SendAsync(asOwner, new SuspendMemberCommand(stack.WorkspaceId, stack.OwnerId, version));

        (await act.Should().ThrowAsync<DomainBusinessRuleException>()).Which.RuleCode
            .Should().Be(WorkspaceRuleCodes.Workspaces_Member_CannotActOnSelf);
        (await MemberVersionAsync(stack, stack.OwnerId)).Should().Be(version);
        (await GrantIsActiveAsync(stack, stack.OwnerId)).Should().BeTrue();
    }

    private Task<bool> GrantIsActiveAsync(OrgStack stack, Guid userId) =>
        _host.ReadAsync(c => c.AccessGrants.AnyAsync(g =>
            g.WorkspaceId == stack.WorkspaceId && g.UserId == userId && g.RevokedAt == null));
}
