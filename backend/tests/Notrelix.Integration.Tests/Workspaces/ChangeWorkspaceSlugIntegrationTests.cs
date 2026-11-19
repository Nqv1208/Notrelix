using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Notrelix.Application.Common.Realtime;
using Notrelix.Application.Features.Workspaces.Members.Services;
using Notrelix.Application.Features.Workspaces.Workspaces.Commands.ArchiveWorkspace;
using Notrelix.Application.Features.Workspaces.Workspaces.Commands.ChangeWorkspaceSlug;
using Notrelix.Application.Features.Workspaces.Workspaces.Commands.CreateWorkspace;
using Notrelix.Domain.Workspaces;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Domain.Workspaces.Workspaces;
using Notrelix.Infrastructure;
using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Data.Authz;
using Notrelix.Infrastructure.Data.Interceptors;
using Notrelix.Infrastructure.Workspaces.Members;
using Notrelix.Infrastructure.Workspaces.Workspaces;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Integration.Tests.Workspaces.OrgTree;
using AppConflict = Notrelix.Application.Common.Exceptions.ConflictException;
using AppForbidden = Notrelix.Application.Common.Exceptions.ForbiddenException;
using AppNotFound = Notrelix.Application.Common.Exceptions.NotFoundException;
using AppValidation = Notrelix.Application.Common.Exceptions.ValidationException;
using DomainBusinessRuleException = Notrelix.Domain.Common.Exceptions.BusinessRuleException;
using PreconditionFailed = Notrelix.Application.Common.Exceptions.PreconditionFailedException;

namespace Notrelix.Integration.Tests.Workspaces;

/// <summary>
/// Changing the workspace address on PostgreSQL through the real pipeline as the application role (row-level
/// security applies, the real outbox interceptor is on): only the Owner (<c>ManageWorkspace</c>) changes it; an
/// Admin, a Member and a Guest are forbidden and a person outside the workspace is told it does not exist, also
/// across tenants; a write carries the version it read (stale is a 412 that changes nothing); the same address is a
/// no-op; a taken address in the Account is a conflict while another Account's address does not collide; an archived
/// workspace refuses with its code.
/// </summary>
[Collection("Database")]
[Trait("Category", "Integration")]
public sealed class ChangeWorkspaceSlugIntegrationTests : IAsyncLifetime
{
    private readonly PostgresTestContainer _db;
    private readonly OrgTreeTestHost _host;

    public ChangeWorkspaceSlugIntegrationTests(PostgresTestContainer db)
    {
        _db = db;
        _host = new OrgTreeTestHost(db);
    }

    public async Task InitializeAsync() => await new DatabaseReset(_db.OwnerConnectionString).ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

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

    private Task<long> VersionAsync(Guid workspaceId) =>
        _host.ReadAsync(c => c.Workspaces.Where(w => w.Id == workspaceId).Select(w => w.Version).SingleAsync());

    private Task<string> SlugAsync(Guid workspaceId) =>
        _host.ReadAsync(c => c.Workspaces.Where(w => w.Id == workspaceId).Select(w => w.Slug).SingleAsync());

    private async Task<Guid> AddWorkspaceToAccountAsync(OrgStack stack, string slug)
    {
        var other = Workspace.Create(stack.AccountId, stack.OwnerId, "Neighbour", slug, OrgTreeTestHost.BaseTime);
        await _host.WithSystemContextAsync(seed =>
        {
            seed.Workspaces.Add(other);
            return Task.CompletedTask;
        });
        return other.Id;
    }

    [Fact]
    public async Task TheOwner_ChangesTheAddress_AndTheVersionMovesByOne()
    {
        var stack = await _host.SeedWorkspaceAsync();
        using var asOwner = As(stack, stack.OwnerId);
        var version = await VersionAsync(stack.WorkspaceId);

        await OrgTreeTestHost.SendAsync(asOwner, new ChangeWorkspaceSlugCommand(stack.WorkspaceId, "  Acme-Team ", version));

        (await SlugAsync(stack.WorkspaceId)).Should().Be("acme-team");
        (await VersionAsync(stack.WorkspaceId)).Should().Be(version + 1);
    }

    [Fact]
    public async Task AfterTheChange_TheNewAddressResolvesToTheWorkspace_AndTheOldOneNoLongerDoes()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var oldSlug = await SlugAsync(stack.WorkspaceId);
        using var asOwner = As(stack, stack.OwnerId);
        await OrgTreeTestHost.SendAsync(asOwner, new ChangeWorkspaceSlugCommand(stack.WorkspaceId, "acme-team", await VersionAsync(stack.WorkspaceId)));

        // The anonymous resolve reads through the Workspaces port, as the application role and with no tenant.
        await using var anonymous = _db.CreateAppContext(new Notrelix.Testing.Application.Fakes.FakeCurrentTenantContext());
        var resolver = new WorkspaceSlugResolver(anonymous);

        (await resolver.ResolveAsync(stack.AccountId, "acme-team", default))!.Id.Should().Be(stack.WorkspaceId);
        (await resolver.ResolveAsync(stack.AccountId, oldSlug, default)).Should().BeNull("the old address is released, with no redirect");
    }

    [Fact]
    public async Task AdminMemberAndGuest_AreForbidden_AnOutsiderIsNotFound_AndNothingChanges()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var admin = await _host.AddMemberAsync(stack, WorkspaceRole.Admin, "Admin");
        var member = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Member");
        var guest = await _host.AddMemberAsync(stack, WorkspaceRole.Guest, "Guest");
        var outsider = await _host.AddOutsiderAsync(stack);
        var version = await VersionAsync(stack.WorkspaceId);
        var slug = await SlugAsync(stack.WorkspaceId);

        foreach (var userId in new[] { admin, member, guest })
        {
            using var provider = As(stack, userId);
            var write = () => OrgTreeTestHost.SendAsync(provider, new ChangeWorkspaceSlugCommand(stack.WorkspaceId, "acme-team", version));
            await write.Should().ThrowAsync<AppForbidden>("only the Owner changes the workspace address");
        }

        using var asOutsider = As(stack, outsider);
        var outsiderWrite = () => OrgTreeTestHost.SendAsync(asOutsider, new ChangeWorkspaceSlugCommand(stack.WorkspaceId, "acme-team", version));
        await outsiderWrite.Should().ThrowAsync<AppNotFound>();

        (await VersionAsync(stack.WorkspaceId)).Should().Be(version);
        (await SlugAsync(stack.WorkspaceId)).Should().Be(slug);
    }

    [Fact]
    public async Task AnotherTenantsOwner_IsToldTheWorkspaceDoesNotExist_AndNothingChanges()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var other = await _host.SeedWorkspaceAsync();
        var version = await VersionAsync(stack.WorkspaceId);
        var slug = await SlugAsync(stack.WorkspaceId);
        using var asOtherOwner = _host.CreateProvider(other.AccountId, other.OwnerId, RegisterAppRole);

        var foreign = () => OrgTreeTestHost.SendAsync(asOtherOwner, new ChangeWorkspaceSlugCommand(stack.WorkspaceId, "acme-team", version));
        var madeUp = () => OrgTreeTestHost.SendAsync(asOtherOwner, new ChangeWorkspaceSlugCommand(Guid.NewGuid(), "acme-team", version));

        await foreign.Should().ThrowAsync<AppNotFound>("another tenant's workspace answers like a made-up id");
        await madeUp.Should().ThrowAsync<AppNotFound>();
        (await SlugAsync(stack.WorkspaceId)).Should().Be(slug);
        (await VersionAsync(stack.WorkspaceId)).Should().Be(version);
    }

    [Fact]
    public async Task AStaleVersion_Is412_ChangesNothing_AndAMissingVersionIsNotAccepted()
    {
        var stack = await _host.SeedWorkspaceAsync();
        using var asOwner = As(stack, stack.OwnerId);
        var read = await VersionAsync(stack.WorkspaceId);
        await OrgTreeTestHost.SendAsync(asOwner, new ChangeWorkspaceSlugCommand(stack.WorkspaceId, "acme-team", read));
        var current = await VersionAsync(stack.WorkspaceId);

        var stale = () => OrgTreeTestHost.SendAsync(asOwner, new ChangeWorkspaceSlugCommand(stack.WorkspaceId, "acme-other", read));
        (await stale.Should().ThrowAsync<PreconditionFailed>("someone else changed the workspace after this one read"))
            .Which.ErrorCode.Should().Be("common.precondition-failed");

        (await SlugAsync(stack.WorkspaceId)).Should().Be("acme-team");
        (await VersionAsync(stack.WorkspaceId)).Should().Be(current);

        var versionless = () => OrgTreeTestHost.SendAsync(asOwner, new ChangeWorkspaceSlugCommand(stack.WorkspaceId, "acme-other", 0));
        await versionless.Should().ThrowAsync<AppValidation>();
    }

    [Fact]
    public async Task TheSameAddress_IsANoOp_NoVersionBump()
    {
        var stack = await _host.SeedWorkspaceAsync();
        using var asOwner = As(stack, stack.OwnerId);
        var version = await VersionAsync(stack.WorkspaceId);
        var slug = await SlugAsync(stack.WorkspaceId);

        await OrgTreeTestHost.SendAsync(asOwner, new ChangeWorkspaceSlugCommand(stack.WorkspaceId, slug.ToUpperInvariant(), version));

        (await VersionAsync(stack.WorkspaceId)).Should().Be(version);
        (await SlugAsync(stack.WorkspaceId)).Should().Be(slug);
    }

    [Fact]
    public async Task AnAddressTakenInTheAccount_IsAConflictWithTheSlugTakenCode_ButAnotherAccountsAddressDoesNotCollide()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var elsewhere = await _host.SeedWorkspaceAsync();
        await AddWorkspaceToAccountAsync(stack, "taken-here");
        var elsewhereSlug = await SlugAsync(elsewhere.WorkspaceId);
        using var asOwner = As(stack, stack.OwnerId);
        var version = await VersionAsync(stack.WorkspaceId);

        var taken = () => OrgTreeTestHost.SendAsync(asOwner, new ChangeWorkspaceSlugCommand(stack.WorkspaceId, "taken-here", version));
        (await taken.Should().ThrowAsync<AppConflict>()).Which.ErrorCode.Should().Be(CreateWorkspaceCommandHandler.SlugTakenErrorCode);
        (await VersionAsync(stack.WorkspaceId)).Should().Be(version);

        await OrgTreeTestHost.SendAsync(asOwner, new ChangeWorkspaceSlugCommand(stack.WorkspaceId, elsewhereSlug, version));
        (await SlugAsync(stack.WorkspaceId)).Should().Be(elsewhereSlug, "addresses are unique per Account, not global");
    }

    [Fact]
    public async Task AnAddressBreakingTheRule_IsRefusedWithItsRuleCode_AndChangesNothing()
    {
        var stack = await _host.SeedWorkspaceAsync();
        using var asOwner = As(stack, stack.OwnerId);
        var version = await VersionAsync(stack.WorkspaceId);

        foreach (var (slug, code) in new[]
        {
            ("ab", WorkspaceRuleCodes.Workspaces_Workspace_SlugTooShort),
            ("settings", WorkspaceRuleCodes.Workspaces_Workspace_SlugReserved),
        })
        {
            var write = () => OrgTreeTestHost.SendAsync(asOwner, new ChangeWorkspaceSlugCommand(stack.WorkspaceId, slug, version));
            (await write.Should().ThrowAsync<DomainBusinessRuleException>(slug)).Which.RuleCode.Should().Be(code);
        }

        (await VersionAsync(stack.WorkspaceId)).Should().Be(version);
    }

    [Fact]
    public async Task AnArchivedWorkspace_RefusesWithTheArchivedCode_AndChangesNothing()
    {
        var stack = await _host.SeedWorkspaceAsync();
        using var asOwner = As(stack, stack.OwnerId);
        await OrgTreeTestHost.SendAsync(asOwner, new ArchiveWorkspaceCommand(stack.WorkspaceId, await VersionAsync(stack.WorkspaceId)));
        var version = await VersionAsync(stack.WorkspaceId);
        var slug = await SlugAsync(stack.WorkspaceId);

        var write = () => OrgTreeTestHost.SendAsync(asOwner, new ChangeWorkspaceSlugCommand(stack.WorkspaceId, "acme-team", version));

        (await write.Should().ThrowAsync<AppForbidden>()).Which.Code.Should().Be("workspace.archived");
        (await SlugAsync(stack.WorkspaceId)).Should().Be(slug);
        (await VersionAsync(stack.WorkspaceId)).Should().Be(version);
    }
}
