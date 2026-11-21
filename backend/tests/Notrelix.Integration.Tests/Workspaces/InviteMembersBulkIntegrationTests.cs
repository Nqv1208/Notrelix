using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Notrelix.Application.Common.Auditing;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Common.Realtime;
using Notrelix.Application.Common.Requests;
using Notrelix.Application.Common.Tokens;
using Notrelix.Application.Features.Workspaces.Invitations.Commands.InviteMember;
using Notrelix.Application.Features.Workspaces.Invitations.Commands.InviteMembers;
using Notrelix.Application.Features.Workspaces.Invitations.Services;
using Notrelix.Application.Features.Workspaces.Members.Services;
using Notrelix.Application.Features.Workspaces.Teams.Commands.CreateTeam;
using Notrelix.Application.Features.Workspaces.Workspaces.Commands.ArchiveWorkspace;
using Notrelix.Domain.Governance.Permissions;
using Notrelix.Domain.Workspaces;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Domain.Workspaces.Spaces;
using Notrelix.Infrastructure;
using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Data.Authz;
using Notrelix.Infrastructure.Data.Interceptors;
using Notrelix.Infrastructure.Data.Messaging;
using Notrelix.Infrastructure.Workspaces.Members;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Integration.Tests.Workspaces.OrgTree;
using AppForbidden = Notrelix.Application.Common.Exceptions.ForbiddenException;
using AppNotFound = Notrelix.Application.Common.Exceptions.NotFoundException;
using AppValidation = Notrelix.Application.Common.Exceptions.ValidationException;

namespace Notrelix.Integration.Tests.Workspaces;

/// <summary>
/// Inviting many people at once on PostgreSQL through the real pipeline as the application role: each address gets its
/// own outcome and the batch is not all-or-nothing; while guest invitations are off the Guest entries are refused one by
/// one with <c>workspace.guest_invites_off</c> (decided by the same policy engine as a single invitation) and the Member
/// entries still go out; a Member caller is forbidden, an outsider is told the workspace does not exist, and an archived
/// workspace refuses the request.
/// </summary>
[Collection("Database")]
[Trait("Category", "Integration")]
public sealed class InviteMembersBulkIntegrationTests : IAsyncLifetime
{
    private const string OffCode = "workspace.guest_invites_off";

    private readonly PostgresTestContainer _db;
    private readonly OrgTreeTestHost _host;

    public InviteMembersBulkIntegrationTests(PostgresTestContainer db)
    {
        _db = db;
        _host = new OrgTreeTestHost(db);
    }

    public async Task InitializeAsync() => await new DatabaseReset(_db.OwnerConnectionString).ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    private void Register(IServiceCollection services)
    {
        var tokens = new Mock<IOneTimeTokenService>();
        tokens.Setup(t => t.Generate(TokenPurpose.WorkspaceInvitation)).Returns(() =>
            new IssuedOneTimeToken("raw", Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N"), 1));
        var encryptor = new Mock<ISecretEncryptor>();
        encryptor.Setup(e => e.Protect(It.IsAny<string>(), It.IsAny<string>())).Returns("protected");
        services.AddSingleton(tokens.Object);
        services.AddSingleton(encryptor.Object);
        services.AddSingleton(new Mock<IAuditService>().Object);
        services.AddScoped<InvitationInitialEntries>();
        // The production pipeline validates every request; the host registers no validators on its own.
        services.AddValidatorsFromAssembly(typeof(InviteMembersCommand).Assembly);

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

    private ServiceProvider As(OrgStack stack, Guid userId) => _host.CreateProvider(stack.AccountId, userId, Register);

    private static InviteMembersCommand Batch(OrgStack stack, params (string Email, string Role)[] entries) =>
        new(stack.WorkspaceId, entries.Select(e => new MemberInvitationEntry(e.Email, e.Role)).ToList());

    private static async Task<MemberInvitationBatchResultDto> SendAsync(ServiceProvider provider, InviteMembersCommand command) =>
        ((Result<MemberInvitationBatchResultDto>)(await OrgTreeTestHost.SendAsync(provider, command))!).Data!;

    private Task<List<string>> InvitedEmailsAsync(OrgStack stack) =>
        _host.ReadAsync(c => c.WorkspaceInvitations.Where(i => i.WorkspaceId == stack.WorkspaceId)
            .Select(i => i.Email).OrderBy(e => e).ToListAsync());

    // The delivery request (the email) is staged as an outbox message together with the invitation row.
    private Task<int> InvitationDeliveriesAsync(OrgStack stack) =>
        _host.ReadAsync(c => c.Set<MessagingOutboxMessage>().IgnoreQueryFilters()
            .CountAsync(m => m.WorkspaceId == stack.WorkspaceId && m.MessageName.Contains("invitation")));

    private static string Outcome(MemberInvitationBatchResultDto result, string email) =>
        result.Outcomes.Single(o => o.Email == email).Outcome;

    private static string Dump(MemberInvitationBatchResultDto result) =>
        string.Join("; ", result.Outcomes.Select(o => $"{o.Email}={o.Outcome}/{o.Code}"));

    [Fact]
    public async Task GuestInvitesOff_RefusesTheGuestEntriesOneByOne_AndTheMemberEntriesStillGoOut()
    {
        var stack = await _host.SeedWorkspaceAsync();
        await _host.SetGuestInvitesAsync(stack, false);
        using var asOwner = As(stack, stack.OwnerId);

        var result = await SendAsync(asOwner, Batch(stack,
            ("ana@example.com", "Member"), ("guest1@example.com", "Guest"),
            ("ben@example.com", "Member"), ("guest2@example.com", "Guest")));

        Outcome(result, "ana@example.com").Should().Be(MemberInvitationOutcomes.Sent);
        Outcome(result, "ben@example.com").Should().Be(MemberInvitationOutcomes.Sent);
        foreach (var guest in new[] { "guest1@example.com", "guest2@example.com" })
        {
            var refused = result.Outcomes.Single(o => o.Email == guest);
            refused.Outcome.Should().Be(MemberInvitationOutcomes.Refused);
            refused.Code.Should().Be(OffCode);
        }
        (await InvitedEmailsAsync(stack)).Should().Equal("ana@example.com", "ben@example.com");
    }

    [Fact]
    public async Task GuestInvitesOn_SendsTheGuestEntriesToo()
    {
        var stack = await _host.SeedWorkspaceAsync();
        await _host.SetGuestInvitesAsync(stack, true);
        using var asOwner = As(stack, stack.OwnerId);

        var result = await SendAsync(asOwner, Batch(stack, ("ana@example.com", "Member"), ("guest@example.com", "Guest")));

        result.Outcomes.Select(o => o.Outcome).Should().OnlyContain(o => o == MemberInvitationOutcomes.Sent);
        (await InvitedEmailsAsync(stack)).Should().Equal("ana@example.com", "guest@example.com");
    }

    [Fact]
    public async Task MoreThanFiftyEntries_AreRefusedByTheValidator_AndNothingIsSent()
    {
        var stack = await _host.SeedWorkspaceAsync();
        using var asOwner = As(stack, stack.OwnerId);
        var tooMany = Enumerable.Range(0, 51).Select(i => ($"p{i}@example.com", "Member")).ToArray();

        var fifty = await SendAsync(asOwner, Batch(stack, tooMany.Take(50).ToArray()));
        fifty.Outcomes.Should().HaveCount(50).And.OnlyContain(o => o.Outcome == MemberInvitationOutcomes.Sent);

        var send = () => OrgTreeTestHost.SendAsync(asOwner, Batch(stack, tooMany));
        await send.Should().ThrowAsync<AppValidation>();
        var none = () => OrgTreeTestHost.SendAsync(asOwner, Batch(stack));
        await none.Should().ThrowAsync<AppValidation>("an empty batch is refused too");
        (await InvitedEmailsAsync(stack)).Should().HaveCount(50);
    }

    [Fact]
    public async Task ADuplicateInTheBatch_CollapsesToOneInvitation()
    {
        var stack = await _host.SeedWorkspaceAsync();
        using var asOwner = As(stack, stack.OwnerId);

        var result = await SendAsync(asOwner, Batch(stack,
            ("Ana@Example.com", "Member"), (" ana@example.com ", "Member"), ("ben@example.com", "Member")));

        result.Outcomes.Select(o => o.Email).Should().Equal("ana@example.com", "ben@example.com");
        (await InvitedEmailsAsync(stack)).Should().Equal("ana@example.com", "ben@example.com");
    }

    [Fact]
    public async Task AlreadyPending_Invalid_AndARefusedRole_AreReportedPerAddress_WhileTheRestGoOut()
    {
        var stack = await _host.SeedWorkspaceAsync();
        using var asOwner = As(stack, stack.OwnerId);
        await OrgTreeTestHost.SendAsync(asOwner, new InviteMemberCommand(stack.WorkspaceId, "pending@example.com", WorkspaceRole.Member));

        var result = await SendAsync(asOwner, Batch(stack,
            ("pending@example.com", "Member"), ("not-an-address", "Member"),
            ("ana@example.com", "Owner"), ("fresh@example.com", "Member")));

        Outcome(result, "pending@example.com").Should().Be(MemberInvitationOutcomes.AlreadyPending, Dump(result));
        Outcome(result, "not-an-address").Should().Be(MemberInvitationOutcomes.Invalid);
        result.Outcomes.Single(o => o.Email == "ana@example.com").Outcome.Should().Be(MemberInvitationOutcomes.Refused, "Owner is not an invitable role");
        Outcome(result, "fresh@example.com").Should().Be(MemberInvitationOutcomes.Sent);
        (await InvitedEmailsAsync(stack)).Should().Equal("fresh@example.com", "pending@example.com");
    }

    [Fact]
    public async Task AMemberAndAGuest_AreForbiddenForTheWholeBatch_AnOutsiderIsNotFound_AndNothingIsStoredOrSent()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var member = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Member");
        var guest = await _host.AddMemberAsync(stack, WorkspaceRole.Guest, "Guest");
        var outsider = await _host.AddOutsiderAsync(stack);

        foreach (var caller in new[] { member, guest })
        {
            using var provider = As(stack, caller);
            var send = () => OrgTreeTestHost.SendAsync(provider, Batch(stack, ("ana@example.com", "Member"), ("ben@example.com", "Guest")));
            await send.Should().ThrowAsync<AppForbidden>();
        }

        using var asOutsider = As(stack, outsider);
        var asOutsiderSend = () => OrgTreeTestHost.SendAsync(asOutsider, Batch(stack, ("ana@example.com", "Member")));
        await asOutsiderSend.Should().ThrowAsync<AppNotFound>();

        (await InvitedEmailsAsync(stack)).Should().BeEmpty();
        (await InvitationDeliveriesAsync(stack)).Should().Be(0);
    }

    [Fact]
    public async Task OwnerAndAdminEntries_AreRefusedOneByOneByTheDomain_NothingIsStoredOrSentForThem_AndTheValidEntriesGoOut()
    {
        var stack = await _host.SeedWorkspaceAsync();
        await _host.SetGuestInvitesAsync(stack, true);
        using var asOwner = As(stack, stack.OwnerId);

        var result = await SendAsync(asOwner, Batch(stack,
            ("owner@example.com", "Owner"), ("ana@example.com", "Member"),
            ("admin@example.com", "Admin"), ("shouty@example.com", "ADMIN"), ("ben@example.com", "Guest")));

        result.Outcomes.Single(o => o.Email == "owner@example.com").Should().Match<MemberInvitationOutcomeDto>(
            o => o.Outcome == MemberInvitationOutcomes.Refused && o.Code == WorkspaceRuleCodes.Workspaces_Invitation_CannotInviteAsOwner && o.InvitationId == null, Dump(result));
        foreach (var admin in new[] { "admin@example.com", "shouty@example.com" })
            result.Outcomes.Single(o => o.Email == admin).Should().Match<MemberInvitationOutcomeDto>(
                o => o.Outcome == MemberInvitationOutcomes.Refused && o.Code == WorkspaceRuleCodes.Workspaces_Invitation_CannotInviteAsAdmin && o.InvitationId == null, Dump(result));
        Outcome(result, "ana@example.com").Should().Be(MemberInvitationOutcomes.Sent);
        Outcome(result, "ben@example.com").Should().Be(MemberInvitationOutcomes.Sent);

        (await InvitedEmailsAsync(stack)).Should().Equal("ana@example.com", "ben@example.com");
        (await InvitationDeliveriesAsync(stack)).Should().Be(2, "only the two valid entries stage an email");
    }

    [Theory]
    [InlineData("0")]
    [InlineData("1")]
    [InlineData("2")]
    [InlineData("3")]
    public async Task ARoleGivenAsANumber_IsInvalid_NothingIsStoredOrSent_AndTheValidEntryGoesOut(string numericRole)
    {
        var stack = await _host.SeedWorkspaceAsync();
        using var asOwner = As(stack, stack.OwnerId);

        var result = await SendAsync(asOwner, Batch(stack, ("num@example.com", numericRole), ("ana@example.com", "Member")));

        Outcome(result, "num@example.com").Should().Be(MemberInvitationOutcomes.Invalid);
        Outcome(result, "ana@example.com").Should().Be(MemberInvitationOutcomes.Sent);
        (await InvitedEmailsAsync(stack)).Should().Equal("ana@example.com");
        (await InvitationDeliveriesAsync(stack)).Should().Be(1);
    }

    [Fact]
    public async Task AnAdmin_MayInviteInBulkLikeASingleInvitation()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var admin = await _host.AddMemberAsync(stack, WorkspaceRole.Admin, "Admin");
        using var asAdmin = As(stack, admin);

        var result = await SendAsync(asAdmin, Batch(stack, ("ana@example.com", "Member")));

        Outcome(result, "ana@example.com").Should().Be(MemberInvitationOutcomes.Sent);
    }

    [Fact]
    public async Task AnArchivedWorkspace_RefusesTheBatch_WithTheArchivedCode()
    {
        var stack = await _host.SeedWorkspaceAsync();
        using var asOwner = As(stack, stack.OwnerId);
        var version = await _host.ReadAsync(c => c.Workspaces.Where(w => w.Id == stack.WorkspaceId).Select(w => w.Version).SingleAsync());
        await OrgTreeTestHost.SendAsync(asOwner, new ArchiveWorkspaceCommand(stack.WorkspaceId, version));

        var send = () => OrgTreeTestHost.SendAsync(asOwner, Batch(stack, ("ana@example.com", "Member")));

        (await send.Should().ThrowAsync<AppForbidden>()).Which.Code.Should().Be("workspace.archived");
        (await InvitedEmailsAsync(stack)).Should().BeEmpty();
    }

    private async Task<(Guid TeamId, Guid SpaceId)> SeedTeamAndSpaceAsync(OrgStack stack)
    {
        using var owner = _host.CreateProvider(stack.AccountId, stack.OwnerId);
        var teamId = ((Result<Guid>)(await OrgTreeTestHost.SendAsync(owner, new CreateTeamCommand(stack.WorkspaceId, "Platform", null)))!).Data;
        var spaceId = await _host.SeedSpaceAsync(stack, "Engineering", SpaceVisibility.Workspace, stack.OwnerId);
        return (teamId, spaceId);
    }

    [Fact]
    public async Task InitialEntries_AreStoredPerAddress_AGuestNamingATeamIsRefusedOnItsOwn_AndTheRestGoOut()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var (teamId, spaceId) = await SeedTeamAndSpaceAsync(stack);
        await _host.SetGuestInvitesAsync(stack, true);
        using var asOwner = As(stack, stack.OwnerId);
        InvitationInitialEntryInput[] both = [new("Team", teamId, "Member"), new("Space", spaceId, "Viewer")];
        InvitationInitialEntryInput[] team = [new("Team", teamId, "Member")];
        InvitationInitialEntryInput[] space = [new("Space", spaceId, "Viewer")];

        var result = await SendAsync(asOwner, new InviteMembersCommand(stack.WorkspaceId,
        [
            new("ana@example.com", "Member", both), new("guest-team@example.com", "Guest", team),
            new("guest-space@example.com", "Guest", space), new("plain@example.com", "Member"),
        ]));

        Outcome(result, "ana@example.com").Should().Be(MemberInvitationOutcomes.Sent, Dump(result));
        Outcome(result, "guest-team@example.com").Should().Be(MemberInvitationOutcomes.Refused, Dump(result));
        result.Outcomes.Single(o => o.Email == "guest-team@example.com").Code.Should().Be(WorkspaceRuleCodes.Workspaces_Invitation_GuestEntryRoleTooPrivileged);
        Outcome(result, "guest-space@example.com").Should().Be(MemberInvitationOutcomes.Sent, Dump(result));
        Outcome(result, "plain@example.com").Should().Be(MemberInvitationOutcomes.Sent, Dump(result));
        (await InvitedEmailsAsync(stack)).Should().Equal("ana@example.com", "guest-space@example.com", "plain@example.com");
        var stored = await _host.ReadAsync(c => c.WorkspaceInvitations.Where(i => i.WorkspaceId == stack.WorkspaceId)
            .ToDictionaryAsync(i => i.Email, i => i.InitialEntries.Count));
        stored["ana@example.com"].Should().Be(2);
        stored["guest-space@example.com"].Should().Be(1);
        stored["plain@example.com"].Should().Be(0);
    }

    [Fact]
    public async Task InitialEntriesInABatch_NeedTheEntryActions_LikeASingleInvitation_AndNothingIsStoredWhenDenied()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var (teamId, spaceId) = await SeedTeamAndSpaceAsync(stack);
        var member = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Member");
        await _host.SeedRuleAsync(stack, PermissionSubjectType.User, member, PermissionAction.ManageTeams, PermissionEffect.Allow);
        using var asMember = As(stack, member);

        var mixed = () => OrgTreeTestHost.SendAsync(asMember, new InviteMembersCommand(stack.WorkspaceId,
            [new("ana@example.com", "Member", [new("Team", teamId, "Member"), new("Space", spaceId, "Viewer")])]));

        await mixed.Should().ThrowAsync<AppForbidden>("the Space entry also needs ManageSpaces");
        (await InvitedEmailsAsync(stack)).Should().BeEmpty();
        (await InvitationDeliveriesAsync(stack)).Should().Be(0);
    }
}
