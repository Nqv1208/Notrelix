using MediatR;
using Notrelix.Application.Common.Models;
using Microsoft.Extensions.DependencyInjection;
using Notrelix.Application.Features.Identity.Security;
using Notrelix.Application.Features.Identity.Security.DTOs;
using Notrelix.Application.Features.Workspaces.Members.Commands.LeaveWorkspace;
using Notrelix.Application.Features.Workspaces.Members.Commands.RemoveMember;
using Notrelix.Application.Features.Workspaces.Members.Services;
using Notrelix.Application.Features.Workspaces.Workspaces.Commands.TransferOwnership;
using Notrelix.Domain.Workspaces;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Domain.Workspaces.Spaces;
using Notrelix.Domain.Workspaces.Teams;
using Notrelix.Infrastructure.Data.Authz;
using Notrelix.Infrastructure.Workspaces.Members;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Integration.Tests.Workspaces.OrgTree;
using DomainBusinessRuleException = Notrelix.Domain.Common.Exceptions.BusinessRuleException;

namespace Notrelix.Integration.Tests.Workspaces;

/// <summary>
/// Leave, handover and cleanup through the real pipeline on PostgreSQL with the real policy engine and the
/// narrowed database roles: the sole Owner cannot leave until another active member holds Owner; only an Owner hands
/// over, only to an active non-Guest member, never to oneself, never across workspaces; a stale version is refused;
/// a person who leaves or is removed loses their Space and Team entries in the same transaction.
/// </summary>
[Collection("Database")]
[Trait("Category", "Integration")]
public sealed class WorkspaceLeaveAndHandoverIntegrationTests : IAsyncLifetime
{
    private readonly PostgresTestContainer _db;
    private readonly OrgTreeTestHost _host;
    private readonly HandoverStepUp _stepUp;
    private readonly Dictionary<Guid, Guid> _sessions = new();

    public WorkspaceLeaveAndHandoverIntegrationTests(PostgresTestContainer db)
    {
        _db = db;
        _host = new OrgTreeTestHost(db);
        var clock = new Mock<IDateTimeProvider>();
        clock.Setup(c => c.UtcNow).Returns(() => _host.Now);
        _stepUp = new HandoverStepUp(clock.Object);
    }

    public async Task InitializeAsync() => await new DatabaseReset(_db.OwnerConnectionString).ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    private void RegisterServices(IServiceCollection services)
    {
        services.AddSingleton(_stepUp.Service);
        services.AddSingleton(new Moq.Mock<Notrelix.Application.Common.Realtime.IAccessChangeSignal>().Object);
        services.AddScoped<AccessGrantProjectionService>();
        services.AddScoped<IWorkspaceGrantProjectionService, WorkspaceGrantProjectionServiceAdapter>();
        services.AddScoped<IWorkspaceOwnerUpdateLocker, WorkspaceOwnerUpdateLocker>();
        services.AddScoped<Notrelix.Application.Common.Messaging.IIntegrationEventCollector, Notrelix.Application.Common.Messaging.IntegrationEventCollector>();
    }

    private Guid SessionOf(Guid userId) => _sessions.TryGetValue(userId, out var id) ? id : _sessions[userId] = Guid.CreateVersion7();

    private ServiceProvider As(OrgStack stack, Guid userId) =>
        _host.CreateProvider(stack.AccountId, userId, RegisterServices, SessionOf(userId));

    private Task<string> ProofOf(Guid userId, StepUpPurpose purpose = StepUpPurpose.TransferWorkspaceOwnership) =>
        _stepUp.ProofAsync(userId, SessionOf(userId), purpose);

    /// <summary>Sends through the real pipeline and hands back the failed Result instead of failing the test.</summary>
    private static async Task<Result> TrySendAsync(ServiceProvider provider, TransferOwnershipCommand command)
    {
        using var scope = provider.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<ISender>().Send(command);
    }

    private async Task<(OrgStack Stack, Guid Admin)> SeedOwnerAndAdminAsync()
    {
        var (stack, admin, _, _) = await SeedAsync();
        return (stack, admin);
    }

    private async Task AssertOwnershipUnchangedAsync(OrgStack stack, Guid admin, long version)
    {
        (await MemberRowAsync(stack, stack.OwnerId)).Role.Should().Be(WorkspaceRole.Owner);
        (await MemberRowAsync(stack, admin)).Role.Should().Be(WorkspaceRole.Admin);
        (await WorkspaceVersionAsync(stack)).Should().Be(version);
    }

    private Task<WorkspaceMember> MemberRowAsync(OrgStack stack, Guid userId) =>
        _host.ReadAsync(c => c.WorkspaceMembers.SingleAsync(m => m.WorkspaceId == stack.WorkspaceId && m.UserId == userId));

    private Task<long> WorkspaceVersionAsync(OrgStack stack) =>
        _host.ReadAsync(c => c.Workspaces.Where(w => w.Id == stack.WorkspaceId).Select(w => w.Version).SingleAsync());

    private async Task<(OrgStack Stack, Guid Admin, Guid Member, Guid Guest)> SeedAsync()
    {
        var stack = await _host.SeedWorkspaceAsync();
        return (stack,
            await _host.AddMemberAsync(stack, WorkspaceRole.Admin, "Admin"),
            await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Member"),
            await _host.AddMemberAsync(stack, WorkspaceRole.Guest, "Guest"));
    }

    /// <summary>A handover command from <paramref name="caller"/> carrying a fresh proof of that caller, unless another is given.</summary>
    private async Task<TransferOwnershipCommand> TransferAsync(OrgStack stack, Guid to, Guid caller, string? proof = null) =>
        new(stack.WorkspaceId, to, await WorkspaceVersionAsync(stack), proof ?? await ProofOf(caller));

    // ── The handover ──────────────────────────────────────────────────────

    [Theory]
    [InlineData(WorkspaceRole.Admin)]
    [InlineData(WorkspaceRole.Member)]
    public async Task TheOwner_HandsOverToAnActiveNonGuestMember_WhoBecomesOwner_AndTheOwnerBecomesAdmin(WorkspaceRole targetRole)
    {
        var (stack, admin, member, _) = await SeedAsync();
        var target = targetRole == WorkspaceRole.Admin ? admin : member;
        using var asOwner = As(stack, stack.OwnerId);

        await OrgTreeTestHost.SendAsync(asOwner, await TransferAsync(stack, target, stack.OwnerId));

        (await MemberRowAsync(stack, target)).Role.Should().Be(WorkspaceRole.Owner);
        (await MemberRowAsync(stack, stack.OwnerId)).Role.Should().Be(WorkspaceRole.Admin);
        // The new Owner really holds the Owner grant: they can hand the workspace back through the real policy engine.
        using var asNewOwner = As(stack, target);
        await OrgTreeTestHost.SendAsync(asNewOwner, await TransferAsync(stack, stack.OwnerId, target));
        (await MemberRowAsync(stack, stack.OwnerId)).Role.Should().Be(WorkspaceRole.Owner);
    }

    [Theory]
    [InlineData(WorkspaceRole.Admin)]
    [InlineData(WorkspaceRole.Member)]
    [InlineData(WorkspaceRole.Guest)]
    public async Task OnlyAnOwnerHandsOver_AnyoneElse_IsRefused_AndNobodyIsPromoted(WorkspaceRole callerRole)
    {
        var (stack, admin, member, guest) = await SeedAsync();
        var caller = callerRole switch { WorkspaceRole.Admin => admin, WorkspaceRole.Member => member, _ => guest };
        var victim = callerRole == WorkspaceRole.Admin ? member : admin;
        using var asCaller = As(stack, caller);
        var command = await TransferAsync(stack, caller == admin ? member : admin, caller);

        var act = () => OrgTreeTestHost.SendAsync(asCaller, command);

        await act.Should().ThrowAsync<Exception>("a non-Owner cannot promote anyone, including themselves");
        (await MemberRowAsync(stack, command.NewOwnerId)).Role.Should().NotBe(WorkspaceRole.Owner);
        (await MemberRowAsync(stack, caller)).Role.Should().Be(callerRole);
        (await MemberRowAsync(stack, stack.OwnerId)).Role.Should().Be(WorkspaceRole.Owner);
        _ = victim;
    }

    [Theory]
    [InlineData(WorkspaceRole.Admin)]
    [InlineData(WorkspaceRole.Member)]
    public async Task AMember_CannotPromoteThemselves(WorkspaceRole role)
    {
        var (stack, admin, member, _) = await SeedAsync();
        var self = role == WorkspaceRole.Admin ? admin : member;
        using var asSelf = As(stack, self);

        var act = async () => await OrgTreeTestHost.SendAsync(asSelf, new TransferOwnershipCommand(stack.WorkspaceId, self, 1, await ProofOf(self)));

        await act.Should().ThrowAsync<Exception>();
        (await MemberRowAsync(stack, self)).Role.Should().Be(role);
    }

    [Fact]
    public async Task AGuest_CannotBeMadeOwner_AndNothingChanges()
    {
        var (stack, _, _, guest) = await SeedAsync();
        using var asOwner = As(stack, stack.OwnerId);

        var act = async () => await OrgTreeTestHost.SendAsync(asOwner, await TransferAsync(stack, guest, stack.OwnerId));

        (await act.Should().ThrowAsync<DomainBusinessRuleException>())
            .Which.RuleCode.Should().Be(WorkspaceRuleCodes.Workspaces_Member_CannotPromoteGuestToOwner);
        (await MemberRowAsync(stack, guest)).Role.Should().Be(WorkspaceRole.Guest);
        (await MemberRowAsync(stack, stack.OwnerId)).Role.Should().Be(WorkspaceRole.Owner);
    }

    [Fact]
    public async Task ASuspendedMember_CannotBeMadeOwner_AndAnOutsider_IsNotFound()
    {
        var (stack, admin, _, _) = await SeedAsync();
        using var asOwner = As(stack, stack.OwnerId);
        await OrgTreeTestHost.SendAsync(asOwner, new Notrelix.Application.Features.Workspaces.Members.Commands.SuspendMember.SuspendMemberCommand(
            stack.WorkspaceId, admin, (await MemberRowAsync(stack, admin)).Version));
        var outsider = await _host.AddOutsiderAsync(stack);

        foreach (var target in new[] { admin, outsider })
        {
            var act = async () => await OrgTreeTestHost.SendAsync(asOwner, await TransferAsync(stack, target, stack.OwnerId));
            await act.Should().ThrowAsync<NotFoundException>();
        }

        (await MemberRowAsync(stack, stack.OwnerId)).Role.Should().Be(WorkspaceRole.Owner);
    }

    [Fact]
    public async Task AStaleWorkspaceVersion_IsRefused_AndNothingChanges()
    {
        var (stack, admin, _, _) = await SeedAsync();
        using var asOwner = As(stack, stack.OwnerId);
        var version = await WorkspaceVersionAsync(stack);

        var act = async () => await OrgTreeTestHost.SendAsync(asOwner, new TransferOwnershipCommand(stack.WorkspaceId, admin, version + 7, await ProofOf(stack.OwnerId)));

        await act.Should().ThrowAsync<PreconditionFailedException>();
        (await MemberRowAsync(stack, admin)).Role.Should().Be(WorkspaceRole.Admin);
    }

    [Fact]
    public async Task AnOwnerOfAnotherWorkspace_CannotHandOverThisOne()
    {
        var (stack, admin, _, _) = await SeedAsync();
        var other = await _host.SeedWorkspaceAsync();
        using var asForeignOwner = As(other, other.OwnerId);

        var act = async () => await OrgTreeTestHost.SendAsync(asForeignOwner, new TransferOwnershipCommand(stack.WorkspaceId, admin, 1, await ProofOf(other.OwnerId)));

        await act.Should().ThrowAsync<Exception>("another workspace's id answers as if it did not exist");
        (await MemberRowAsync(stack, admin)).Role.Should().Be(WorkspaceRole.Admin);
        (await MemberRowAsync(stack, stack.OwnerId)).Role.Should().Be(WorkspaceRole.Owner);
    }

    // ── The step-up proof ─────────────────────────────────────────────────

    [Theory]
    [InlineData("")]
    [InlineData("not-a-proof")]
    public async Task AHandoverWithoutAFreshProof_IsRefused_AndOwnershipDoesNotChange(string token)
    {
        var (stack, admin) = await SeedOwnerAndAdminAsync();
        var version = await WorkspaceVersionAsync(stack);
        using var asOwner = As(stack, stack.OwnerId);

        var result = await TrySendAsync(asOwner, new TransferOwnershipCommand(stack.WorkspaceId, admin, version, token));

        result.Succeeded.Should().BeFalse();
        result.TypedErrors.Should().ContainSingle().Which.Code.Should().Be(
            token.Length == 0 ? "identity.security.step-up-required" : "identity.security.step-up-invalid");
        await AssertOwnershipUnchangedAsync(stack, admin, version);
    }

    [Fact]
    public async Task AFreshProof_HandsOver_AndAProofOlderThanItsWindow_IsRefused()
    {
        var (stack, admin) = await SeedOwnerAndAdminAsync();
        var version = await WorkspaceVersionAsync(stack);
        using var asOwner = As(stack, stack.OwnerId);
        var stale = await ProofOf(stack.OwnerId);
        _host.Now = _host.Now.Add(SecurityStepUpPolicy.ProofTtl).AddSeconds(1);

        var refused = await TrySendAsync(asOwner, new TransferOwnershipCommand(stack.WorkspaceId, admin, version, stale));

        refused.TypedErrors.Should().ContainSingle().Which.Code.Should().Be("identity.security.step-up-invalid");
        await AssertOwnershipUnchangedAsync(stack, admin, version);

        var accepted = await TrySendAsync(asOwner, new TransferOwnershipCommand(stack.WorkspaceId, admin, version, await ProofOf(stack.OwnerId)));

        accepted.Succeeded.Should().BeTrue();
        (await MemberRowAsync(stack, admin)).Role.Should().Be(WorkspaceRole.Owner);
    }

    [Fact]
    public async Task AProofOfAnotherPurpose_IsRefused_AndOwnershipDoesNotChange()
    {
        var (stack, admin) = await SeedOwnerAndAdminAsync();
        var version = await WorkspaceVersionAsync(stack);
        using var asOwner = As(stack, stack.OwnerId);

        var result = await TrySendAsync(asOwner, new TransferOwnershipCommand(
            stack.WorkspaceId, admin, version, await ProofOf(stack.OwnerId, StepUpPurpose.RevokeOtherSessions)));

        result.TypedErrors.Should().ContainSingle().Which.Code.Should().Be("identity.security.step-up-invalid");
        await AssertOwnershipUnchangedAsync(stack, admin, version);
    }

    [Fact]
    public async Task AProofOfAnotherUserOrAnotherSession_CannotBeReplayed()
    {
        var (stack, admin) = await SeedOwnerAndAdminAsync();
        var version = await WorkspaceVersionAsync(stack);
        using var asOwner = As(stack, stack.OwnerId);
        var adminsProof = await ProofOf(admin);
        var ownersOtherSession = await _stepUp.ProofAsync(stack.OwnerId, Guid.CreateVersion7());

        foreach (var foreign in new[] { adminsProof, ownersOtherSession })
        {
            var result = await TrySendAsync(asOwner, new TransferOwnershipCommand(stack.WorkspaceId, admin, version, foreign));
            result.TypedErrors.Should().ContainSingle().Which.Code.Should().Be("identity.security.step-up-invalid");
        }

        await AssertOwnershipUnchangedAsync(stack, admin, version);
    }

    [Fact]
    public async Task ASpentProof_CannotBeUsedAgain()
    {
        var (stack, admin) = await SeedOwnerAndAdminAsync();
        using var asOwner = As(stack, stack.OwnerId);
        var proof = await ProofOf(stack.OwnerId);
        var first = await TrySendAsync(asOwner, new TransferOwnershipCommand(stack.WorkspaceId, admin, await WorkspaceVersionAsync(stack), proof));
        first.Succeeded.Should().BeTrue();
        var version = await WorkspaceVersionAsync(stack);
        using var asNewOwner = As(stack, admin);

        var replay = await TrySendAsync(asNewOwner, new TransferOwnershipCommand(stack.WorkspaceId, stack.OwnerId, version, proof));

        replay.TypedErrors.Should().ContainSingle().Which.Code.Should().Be("identity.security.step-up-invalid");
        (await MemberRowAsync(stack, admin)).Role.Should().Be(WorkspaceRole.Owner);
        (await MemberRowAsync(stack, stack.OwnerId)).Role.Should().Be(WorkspaceRole.Admin);
    }

    [Fact]
    public async Task AnAdminWithoutAProof_StillGetsTheNotFoundAnOutsiderGets()
    {
        var (stack, admin) = await SeedOwnerAndAdminAsync();
        var version = await WorkspaceVersionAsync(stack);
        using var asAdmin = As(stack, admin);

        var act = async () => await TrySendAsync(asAdmin, new TransferOwnershipCommand(stack.WorkspaceId, admin, version, ""));

        await act.Should().ThrowAsync<Exception>("a non-Owner never learns that a proof is wanted");
        await AssertOwnershipUnchangedAsync(stack, admin, version);
    }

    // ── Leave ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task TheSoleOwner_CannotLeave_UntilAnotherActiveMemberHoldsOwner()
    {
        var (stack, admin, _, _) = await SeedAsync();
        using var asOwner = As(stack, stack.OwnerId);

        var refused = () => OrgTreeTestHost.SendAsync(asOwner, new LeaveWorkspaceCommand(stack.WorkspaceId));
        (await refused.Should().ThrowAsync<ConflictException>())
            .Which.ErrorCode.Should().Be("workspaces.member.last-owner-cannot-leave");
        (await MemberRowAsync(stack, stack.OwnerId)).Status.Should().Be(WorkspaceMemberStatus.Active);

        await OrgTreeTestHost.SendAsync(asOwner, await TransferAsync(stack, admin, stack.OwnerId));
        await OrgTreeTestHost.SendAsync(asOwner, new LeaveWorkspaceCommand(stack.WorkspaceId));

        (await MemberRowAsync(stack, stack.OwnerId)).Status.Should().Be(WorkspaceMemberStatus.Removed);
        (await MemberRowAsync(stack, admin)).Role.Should().Be(WorkspaceRole.Owner);
    }

    [Theory]
    [InlineData(WorkspaceRole.Member)]
    [InlineData(WorkspaceRole.Guest)]
    public async Task AMemberOrGuest_Leaves_LosesTheEntries_AndALaterLeaveIsNotFound(WorkspaceRole role)
    {
        var (stack, _, member, guest) = await SeedAsync();
        var person = role == WorkspaceRole.Member ? member : guest;
        var (soleAdminSpace, sharedSpace, teamWithLead, otherPeoplesSpace) = await SeedEntriesAsync(stack, person);
        using var asPerson = As(stack, person);

        await OrgTreeTestHost.SendAsync(asPerson, new LeaveWorkspaceCommand(stack.WorkspaceId));

        (await MemberRowAsync(stack, person)).Status.Should().Be(WorkspaceMemberStatus.Removed);
        await AssertNoEntriesAsync(stack, person, soleAdminSpace, sharedSpace, teamWithLead);
        (await EntryCountAsync(otherPeoplesSpace)).Should().BeGreaterThan(0, "other people's entries stay");

        var again = () => OrgTreeTestHost.SendAsync(asPerson, new LeaveWorkspaceCommand(stack.WorkspaceId));
        await again.Should().ThrowAsync<Exception>("a second leave finds no membership");
    }

    [Fact]
    public async Task ARemoval_LosesTheEntries_InTheSameTransaction()
    {
        var (stack, _, member, _) = await SeedAsync();
        var (soleAdminSpace, sharedSpace, teamWithLead, _) = await SeedEntriesAsync(stack, member);
        using var asOwner = As(stack, stack.OwnerId);

        await OrgTreeTestHost.SendAsync(asOwner, new RemoveMemberCommand(stack.WorkspaceId, member, (await MemberRowAsync(stack, member)).Version));

        await AssertNoEntriesAsync(stack, member, soleAdminSpace, sharedSpace, teamWithLead);
        var team = await _host.ReadAsync(c => c.Teams.Include(t => t.Members).SingleAsync(t => t.Id == teamWithLead));
        team.Members.Should().NotContain(m => m.Status == TeamMemberStatus.Active && m.Role == TeamMemberRole.Lead, "the team is left needing a lead");
    }

    [Fact]
    public async Task ALeaveInAnotherWorkspace_NeverTouchesTheEntriesHere()
    {
        var (stack, _, member, _) = await SeedAsync();
        var (_, sharedSpace, teamWithLead, _) = await SeedEntriesAsync(stack, member);
        var other = await _host.SeedWorkspaceAsync();
        using var asForeignOwner = As(other, other.OwnerId);

        var act = () => OrgTreeTestHost.SendAsync(asForeignOwner, new RemoveMemberCommand(stack.WorkspaceId, member, 1));

        await act.Should().ThrowAsync<Exception>();
        (await EntryCountAsync(sharedSpace)).Should().BeGreaterThan(1);
        (await _host.ReadAsync(c => c.Teams.Include(t => t.Members).SingleAsync(t => t.Id == teamWithLead)))
            .Members.Should().Contain(m => m.UserId == member && m.Status == TeamMemberStatus.Active);
    }

    // ── Seeds and reads ───────────────────────────────────────────────────

    /// <summary>The person is the only Admin of one Space, a plain entry of another, and the only Lead of a Team.</summary>
    private async Task<(Guid SoleAdminSpace, Guid SharedSpace, Guid Team, Guid OthersSpace)> SeedEntriesAsync(OrgStack stack, Guid person)
    {
        var soleAdminSpace = await _host.SeedSpaceAsync(stack, "Mine", SpaceVisibility.Workspace, person);
        var sharedSpace = await _host.SeedSpaceAsync(stack, "Shared", SpaceVisibility.Workspace, stack.OwnerId);
        var othersSpace = await _host.SeedSpaceAsync(stack, "Others", SpaceVisibility.Workspace, stack.OwnerId);
        var teamId = Guid.Empty;
        await _host.WithSystemContextAsync(c =>
        {
            var shared = c.Spaces.Include(s => s.Members).Single(s => s.Id == sharedSpace);
            shared.AddMember(SpaceMemberSubjectType.User, person, SpaceRole.Member, stack.OwnerId, OrgTreeTestHost.BaseTime);
            var team = Team.Create(stack.AccountId, stack.WorkspaceId, "ERP", stack.OwnerId, OrgTreeTestHost.BaseTime);
            team.AddMember(person, TeamMemberRole.Lead, stack.OwnerId, OrgTreeTestHost.BaseTime);
            c.Teams.Add(team);
            teamId = team.Id;
            return Task.CompletedTask;
        });
        return (soleAdminSpace, sharedSpace, teamId, othersSpace);
    }

    private Task<int> EntryCountAsync(Guid spaceId) =>
        _host.ReadAsync(c => c.Spaces.Where(s => s.Id == spaceId).SelectMany(s => s.Members).CountAsync());

    private async Task AssertNoEntriesAsync(OrgStack stack, Guid person, Guid soleAdminSpace, Guid sharedSpace, Guid team)
    {
        var spaceEntries = await _host.ReadAsync(c => c.Spaces.Where(s => s.WorkspaceId == stack.WorkspaceId)
            .SelectMany(s => s.Members).CountAsync(m => m.SubjectType == SpaceMemberSubjectType.User && m.SubjectId == person));
        spaceEntries.Should().Be(0, "no Space entry of the person survives");
        (await _host.ReadAsync(c => c.TeamMembers.CountAsync(m => m.WorkspaceId == stack.WorkspaceId && m.UserId == person && m.Status == TeamMemberStatus.Active)))
            .Should().Be(0, "no Team membership of the person survives");
        _ = (soleAdminSpace, sharedSpace, team);
    }
}
