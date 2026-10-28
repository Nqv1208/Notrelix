using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Notrelix.Application.Common.Behaviors;
using Notrelix.Application.Common.Data;
using Notrelix.Application.Common.Diagnostics;
using Notrelix.Application.Common.Idempotency;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Common.Requests.Execution;
using Notrelix.Application.Features.Accounts.Abstractions;
using Notrelix.Application.Features.Automation.Abstractions;
using Notrelix.Application.Features.Collaboration.Abstractions;
using Notrelix.Application.Features.Documents.Abstractions;
using Notrelix.Application.Features.Documents.Public.PageAuthorization;
using Notrelix.Application.Features.Governance.Abstractions;
using Notrelix.Application.Features.Integrations.Abstractions;
using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Application.Features.Workspaces.Abstractions;
using Notrelix.Domain.Accounts.Accounts;
using Notrelix.Domain.Accounts.Members;
using Notrelix.Domain.Identity.Users;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Domain.Workspaces.Workspaces;
using Notrelix.Infrastructure;
using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Data.Authz;
using Notrelix.Infrastructure.Data.Rls;
using Notrelix.Infrastructure.Operations.Idempotency;
using Notrelix.Infrastructure.Services;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;
using AppForbidden = Notrelix.Application.Common.Exceptions.ForbiddenException;

using System.Text.Json;
using Notrelix.Application.Features.Collaboration.Comments.DTOs;
using Notrelix.Application.Features.Collaboration.Comments.Queries.GetComments;
using Notrelix.Application.Features.Collaboration.Reactions.Commands.AddReaction;
using Notrelix.Application.Features.Collaboration.Reactions.Commands.RemoveReaction;
using Notrelix.Application.Features.WorkManagement.Approvals.Commands.CancelApprovalRequest;
using Notrelix.Application.Features.WorkManagement.Labels.Commands.DeleteLabel;
using Notrelix.Domain.Collaboration.Comments;
using Notrelix.Domain.SharedKernel;
using Notrelix.Domain.SharedKernel.Ordering;
using Notrelix.Domain.WorkManagement.Approvals;
using Notrelix.Domain.WorkManagement.BoardGroups;
using Notrelix.Domain.WorkManagement.Boards;
using Notrelix.Domain.WorkManagement.Items;
using Notrelix.Domain.WorkManagement.Labels;
using Notrelix.Application.Features.WorkManagement.Common.DTOs;
using Notrelix.Application.Features.WorkManagement.Views.Commands.CreateSavedFilter;
using Notrelix.Application.Features.WorkManagement.Views.Commands.DeleteSavedFilter;
using Notrelix.Application.Features.WorkManagement.Views.Commands.RenameSavedFilter;
using Notrelix.Application.Features.WorkManagement.Views.Commands.RestoreSavedFilter;
using Notrelix.Application.Features.WorkManagement.Views.Commands.UpdateSavedFilterVisibility;
using Notrelix.Application.Features.WorkManagement.Views.Queries.ListSavedFilters;
using Notrelix.Domain.WorkManagement.Views;
using NotFound = Notrelix.Application.Common.Exceptions.NotFoundException;
namespace Notrelix.Integration.Tests.WorkManagement;

/// <summary>
/// Saved filters through the real pipeline on PostgreSQL: the resource locator and the Board-child relation resolve
/// them to their Board, a personal filter is its creator's alone (anyone else gets NotFound), a shared one follows
/// the Board, and only the creator changes who sees it.
/// </summary>
[Collection("Database")]
[Trait("Category", "Integration")]
public sealed class SavedFilterPipelineIntegrationTests : IAsyncLifetime
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;
    private const string Criteria = "{\"filter\":\"p.high\",\"groupBy\":\"status\"}";

    private readonly PostgresTestContainer _db;
    private Guid _accountId, _ownerId, _memberId, _workspaceId, _boardId;

    public SavedFilterPipelineIntegrationTests(PostgresTestContainer db) => _db = db;

    public async Task InitializeAsync()
    {
        await new DatabaseReset(_db.OwnerConnectionString).ResetAsync();
        (_accountId, _ownerId, _workspaceId, _memberId) = await SeedWorkspaceStackAsync();
        var board = Board.Create(_accountId, _workspaceId, _ownerId, "Filter Board", null, Now);
        _boardId = board.Id;
        await using var seed = _db.CreateOwnerContext(SystemTenant());
        seed.Boards.Add(board);
        await seed.SaveChangesAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task PersonalFilter_IsListedForItsCreatorOnly_AndKeepsItsCriteria()
    {
        var id = await CreateAsync(_ownerId, "Của tôi");

        var mine = await ListAsync(_ownerId);
        var theirs = await ListAsync(_memberId);

        mine.Should().ContainSingle(f => f.Id == id && f.Visibility == "Private" && f.IsMine && f.Criteria == Criteria);
        theirs.Should().BeEmpty();
    }

    [Fact]
    public async Task PersonalFilter_OfSomeoneElse_IsNotFound_ForEveryChange()
    {
        var id = await CreateAsync(_ownerId, "Riêng");
        var version = await VersionAsync(id);

        foreach (var command in new object[]
                 {
                     new RenameSavedFilterCommand(id, "Đổi tên", version),
                     new UpdateSavedFilterVisibilityCommand(id, SavedFilterVisibility.Public, version),
                     new DeleteSavedFilterCommand(id, version),
                     new RestoreSavedFilterCommand(id),
                 })
        {
            using var provider = CreateProvider(_accountId, _memberId);
            using var scope = provider.CreateScope();
            BindIdempotencyKey(scope);
            var act = () => Send<Result>(scope, command);
            await act.Should().ThrowAsync<NotFound>(command.GetType().Name);
        }

        (await VersionAsync(id)).Should().Be(version, "a refused change leaves the filter untouched");
    }

    [Fact]
    public async Task SharedFilter_IsVisibleToBoardViewers_AndEditableByEditors_ButOnlyTheCreatorUnshares()
    {
        var id = await CreateAsync(_ownerId, "Chung");
        await SendAsync<Result>(_ownerId, new UpdateSavedFilterVisibilityCommand(id, SavedFilterVisibility.Public, await VersionAsync(id)));

        (await ListAsync(_memberId)).Should().ContainSingle(f => f.Id == id && f.Visibility == "Public" && !f.IsMine);

        var renamed = await SendAsync<Result>(_memberId, new RenameSavedFilterCommand(id, "Đổi bởi thành viên", await VersionAsync(id)));
        renamed.Succeeded.Should().BeTrue();

        var version = await VersionAsync(id);
        var act = () => SendAsync<Result>(_memberId, new UpdateSavedFilterVisibilityCommand(id, SavedFilterVisibility.Private, version));
        await act.Should().ThrowAsync<AppForbidden>();
    }

    [Fact]
    public async Task Delete_ThenRestore_ByTheCreator_RoundTrips()
    {
        var id = await CreateAsync(_ownerId, "Xoá rồi khôi phục");

        (await SendAsync<Result>(_ownerId, new DeleteSavedFilterCommand(id, await VersionAsync(id)))).Succeeded.Should().BeTrue();
        (await ListAsync(_ownerId)).Should().BeEmpty();

        (await SendAsync<Result>(_ownerId, new RestoreSavedFilterCommand(id))).Succeeded.Should().BeTrue();
        (await ListAsync(_ownerId)).Should().ContainSingle(f => f.Id == id);
    }

    private async Task<Guid> CreateAsync(Guid userId, string name) =>
        (await SendAsync<Result<Guid>>(userId, new CreateSavedFilterCommand(_boardId, name, Criteria: Criteria))).Data;

    private async Task<List<SavedFilterDto>> ListAsync(Guid userId) =>
        (await SendAsync<Result<List<SavedFilterDto>>>(userId, new ListSavedFiltersQuery(_boardId))).Data!;

    private async Task<T> SendAsync<T>(Guid userId, object request) where T : class
    {
        using var provider = CreateProvider(_accountId, userId);
        using var scope = provider.CreateScope();
        BindIdempotencyKey(scope);
        return await Send<T>(scope, request);
    }

    private async Task<long> VersionAsync(Guid id)
    {
        await using var ctx = _db.CreateOwnerContext(SystemTenant());
        return await ctx.SavedFilters.IgnoreQueryFilters().Where(f => f.Id == id).Select(f => f.Version).SingleAsync();
    }

    private async Task<Guid> AddMemberAsync()
    {
        var now = DateTimeOffset.UtcNow;
        var user = User.Create($"wm-{Guid.NewGuid():N}@example.com", "Other", "hashed", now, true);
        user.ConfirmEmail(user.Id, now);
        await using var seed = _db.CreateOwnerContext(SystemTenant());
        seed.Users.Add(user);
        seed.WorkspaceMembers.Add(WorkspaceMember.Create(_accountId, _workspaceId, user.Id, WorkspaceRole.Member, _ownerId, now));
        await seed.SaveChangesAsync();
        await using var grant = _db.CreateOwnerContext(SystemTenant());
        await new AccessGrantProjectionService(grant).SyncWorkspaceMemberGrantAsync(_accountId, _workspaceId, user.Id, WorkspaceRole.Member, now, CancellationToken.None);
        await grant.SaveChangesAsync();
        return user.Id;
    }

    private static void BindIdempotencyKey(IServiceScope scope) =>
        scope.ServiceProvider.GetRequiredService<IIdempotencyExecutionContextWriter>()
            .Set($"live-{Guid.NewGuid():N}", IdempotencyExecutionSource.Internal);

    private static async Task<T> Send<T>(IServiceScope scope, object request) where T : class =>
        ((await scope.ServiceProvider.GetRequiredService<ISender>().Send(request)) as T)!;

    private ServiceProvider CreateProvider(Guid accountId, Guid userId)
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetAccount(accountId, userId);

        var requestContextMock = new Mock<ICurrentRequestContext>();
        requestContextMock.Setup(r => r.UserId).Returns(userId);
        requestContextMock.Setup(r => r.RequireAccountId()).Returns(accountId);
        requestContextMock.Setup(r => r.RequireWorkspaceId()).Returns(() => tenant.WorkspaceId ?? Guid.Empty);

        var currentUserMock = new Mock<ICurrentUser>();
        currentUserMock.Setup(u => u.UserId).Returns(userId);
        currentUserMock.Setup(u => u.IsAuthenticated).Returns(true);

        var credentialMock = new Mock<ICurrentCredentialContext>();
        credentialMock.Setup(c => c.Kind).Returns(CredentialKind.UserSession);

        var clockMock = new Mock<IDateTimeProvider>();
        clockMock.Setup(c => c.UtcNow).Returns(DateTimeOffset.UtcNow);

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddOptions();

        services.AddSingleton(new MediatRServiceConfiguration());
        services.AddScoped<IMediator, Mediator>();
        services.AddScoped<ISender>(sp => sp.GetRequiredService<IMediator>());

        services.AddSingleton(requestContextMock.Object);
        services.AddSingleton(currentUserMock.Object);
        services.AddSingleton<ICurrentTenantContext>(tenant);
        services.AddSingleton(credentialMock.Object);
        services.AddSingleton(clockMock.Object);
        services.AddSingleton(TimeProvider.System);

        services.AddSingleton<IRequestDescriptorRegistry>(
            RequestDescriptorRegistry.Create(typeof(CreateSavedFilterCommand).Assembly));

        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ExceptionMappingBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(RequestContractBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ExecutionContextBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(DataSessionBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(AccessControlBehavior<,>));

        services.AddScoped<ApplicationDbContext>(sp =>
            _db.CreateOwnerContext(sp.GetRequiredService<ICurrentTenantContext>()));
        services.AddScoped<IWorkspaceDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IAccountDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IGovernanceDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IWorkManagementDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IDocumentDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<ICollaborationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IAutomationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IIntegrationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());

        services.AddSingleton<IOptions<RlsOptions>>(Options.Create(new RlsOptions
        {
            Enabled = true,
            SetSessionContext = true,
        }));
        services.AddScoped<IRlsSessionContext, RlsSessionContext>();
        services.AddScoped<IRequestDataSession, EfRequestDataSession>();

        services.AddSingleton<PipelineMetrics>();
        services.AddSingleton<IAccessPolicyEvaluator, AccessPolicyEngine>();
        services.AddScoped<
            IPageAuthorizationFacts,
            PostgresPageAuthorizationFacts>();
        services.AddScoped<IAccessFactsProvider>(sp =>
            new PostgresAccessFactsProvider(
                sp.GetRequiredService<ApplicationDbContext>(),
                sp.GetRequiredService<TimeProvider>(),
                sp.GetRequiredService<IPageAuthorizationFacts>(),
                new FakeBillingSubscriptionFacts()));
        services.AddGovernanceInfrastructure(new ConfigurationBuilder().Build());

        services.AddScoped<IResourceLocator, ResourceLocator>();
        // ADR-009 placement: the create handlers resolve Space placement targets
        // and the target authorizer even when the request is not placed.
        services.AddScoped<global::Notrelix.Application.Features.Workspaces.Public.SpacePlacement.ISpacePlacementTargets,
            global::Notrelix.Infrastructure.Workspaces.Spaces.SpacePlacementTargets>();
        services.AddScoped<global::Notrelix.Application.Features.WorkManagement.Boards.Services.IBoardSpacePlacement,
            global::Notrelix.Application.Features.WorkManagement.Boards.Services.BoardSpacePlacement>();
        services.AddScoped<global::Notrelix.Application.Features.Documents.Pages.Services.IPageSpacePlacement,
            global::Notrelix.Application.Features.Documents.Pages.Services.PageSpacePlacement>();
        services.AddScoped<global::Notrelix.Application.Common.Tenancy.ITenantBootstrapStore, TenantBootstrapStore>();

        services.AddScoped<Notrelix.Application.Common.Context.ExecutionContext>();
        services.AddScoped<IExecutionContextAccessor>(sp =>
            sp.GetRequiredService<Notrelix.Application.Common.Context.ExecutionContext>());
        services.AddScoped<IExecutionContextReader>(sp =>
            sp.GetRequiredService<Notrelix.Application.Common.Context.ExecutionContext>());

        services.AddOptions<IdempotencyOptions>().Configure(_ => { });
        services.AddSingleton<IIdempotencyRequestFingerprint, JsonIdempotencyRequestFingerprint>();
        services.AddSingleton<IIdempotencyReplayPolicy, DefaultIdempotencyReplayPolicy>();
        services.AddScoped<IdempotencyPartitionFactory>();
        services.AddScoped<IIdempotencyStore>(sp =>
            new EfIdempotencyStore(
                sp.GetRequiredService<ApplicationDbContext>(),
                sp.GetRequiredService<TimeProvider>(),
                sp.GetRequiredService<IOptions<IdempotencyOptions>>()));
        services.AddScoped<IdempotencyExecutionContext>();
        services.AddScoped<IIdempotencyExecutionContext>(sp =>
            sp.GetRequiredService<IdempotencyExecutionContext>());
        services.AddScoped<IIdempotencyExecutionContextWriter>(sp =>
            sp.GetRequiredService<IdempotencyExecutionContext>());

        services.AddScoped<Notrelix.Application.Features.Governance.Public.Visibility.IResourceVisibility, PostgresResourceVisibility>();
        services.AddScoped<Notrelix.Application.Common.Tenancy.IActorLookupService, ActorLookupService>();
        services.AddScoped<IRequestHandler<CreateSavedFilterCommand, Result<Guid>>, CreateSavedFilterCommandHandler>();
        services.AddScoped<IRequestHandler<ListSavedFiltersQuery, Result<List<SavedFilterDto>>>, ListSavedFiltersQueryHandler>();
        services.AddScoped<IRequestHandler<RenameSavedFilterCommand, Result>, RenameSavedFilterCommandHandler>();
        services.AddScoped<IRequestHandler<UpdateSavedFilterVisibilityCommand, Result>, UpdateSavedFilterVisibilityCommandHandler>();
        services.AddScoped<IRequestHandler<DeleteSavedFilterCommand, Result>, DeleteSavedFilterCommandHandler>();
        services.AddScoped<IRequestHandler<RestoreSavedFilterCommand, Result>, RestoreSavedFilterCommandHandler>();
        services.AddProductionCrossContextAdapters();

        return services.BuildServiceProvider();
    }

    // ── seeding ---------------------------------------------------------------

    private async Task<(Guid AccountId, Guid OwnerId, Guid WorkspaceId, Guid MemberId)> SeedWorkspaceStackAsync()
    {
        var now = DateTimeOffset.UtcNow;
        var owner = User.Create($"wm-{Guid.NewGuid():N}@example.com", "WM Owner", "hashed", now, true);
        owner.ConfirmEmail(owner.Id, now);
        var member = User.Create($"wm-{Guid.NewGuid():N}@example.com", "WM Member", "hashed", now, true);
        member.ConfirmEmail(member.Id, now);
        var account = Account.Create("WM Account", $"wm-{Guid.NewGuid():N}", AccountType.Team, owner.Id, now);
        var workspace = Workspace.Create(account.Id, owner.Id, "WM Workspace", $"wm-ws-{Guid.NewGuid():N}", now);

        await using var seed = _db.CreateOwnerContext(SystemTenant());
        seed.Users.Add(owner);
        seed.Users.Add(member);
        seed.Accounts.Add(account);
        seed.AccountMembers.Add(AccountMember.Create(account.Id, owner.Id, AccountRole.Owner, owner.Id, now));
        seed.Workspaces.Add(workspace);
        seed.WorkspaceMembers.Add(WorkspaceMember.Create(account.Id, workspace.Id, owner.Id, WorkspaceRole.Owner, owner.Id, now));
        seed.WorkspaceMembers.Add(WorkspaceMember.Create(account.Id, workspace.Id, member.Id, WorkspaceRole.Member, owner.Id, now));
        await seed.SaveChangesAsync();

        await using var grant = _db.CreateOwnerContext(SystemTenant());
        var projection = new AccessGrantProjectionService(grant);
        await projection.SyncWorkspaceMemberGrantAsync(account.Id, workspace.Id, owner.Id, WorkspaceRole.Owner, now, CancellationToken.None);
        await projection.SyncWorkspaceMemberGrantAsync(account.Id, workspace.Id, member.Id, WorkspaceRole.Member, now, CancellationToken.None);
        await grant.SaveChangesAsync();

        return (account.Id, owner.Id, workspace.Id, member.Id);
    }

    private static FakeCurrentTenantContext SystemTenant()
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        return tenant;
    }
}