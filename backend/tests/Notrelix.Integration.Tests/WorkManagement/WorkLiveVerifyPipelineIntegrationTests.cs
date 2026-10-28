using Notrelix.Domain.WorkManagement.Fields;
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
using Notrelix.Application.Features.WorkManagement.Boards.Commands.CreateBoardInWorkspace;
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
namespace Notrelix.Integration.Tests.WorkManagement;

/// <summary>
/// Live-verification regressions through the real pipeline on PostgreSQL: deleting a label that sits on Items,
/// cancelling an approval request (resource location, requester rule, stale version), reaction toggles that
/// repeat, and the UTC shape of serialized comment timestamps.
/// </summary>
[Collection("Database")]
[Trait("Category", "Integration")]
public sealed class WorkLiveVerifyPipelineIntegrationTests : IAsyncLifetime
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    private readonly PostgresTestContainer _db;
    private Guid _accountId, _ownerId, _memberId, _workspaceId, _boardId, _itemA, _itemB, _labelId, _approvalId, _commentId;

    public WorkLiveVerifyPipelineIntegrationTests(PostgresTestContainer db) => _db = db;

    public async Task InitializeAsync()
    {
        await new DatabaseReset(_db.OwnerConnectionString).ResetAsync();
        (_accountId, _ownerId, _workspaceId, _memberId) = await SeedWorkspaceStackAsync();

        var board = Board.Create(_accountId, _workspaceId, _ownerId, "Live Board", null, Now);
        var group = BoardGroup.Create(_accountId, _workspaceId, board.Id, "Todo", Color.Create("#808080"), FractionalIndex.Create("a0"), _ownerId, Now);
        var a = BoardItem.CreateRoot(_accountId, _workspaceId, board.Id, group.Id, "A", FractionalIndex.Create("a0"), _ownerId, Now);
        var b = BoardItem.CreateRoot(_accountId, _workspaceId, board.Id, group.Id, "B", FractionalIndex.Create("a1"), _ownerId, Now);
        var label = Label.Create(_accountId, _workspaceId, board.Id, "Bug", LabelColor.Create("#FF0000"), _ownerId, Now);
        var approval = ApprovalRequest.Create(_accountId, _workspaceId,
            ResourceRef.Create(ResourceKind.Create("work-management.board-item"), a.Id), "Please approve", _memberId, Now);
        var comment = Comment.Create(_accountId, _workspaceId,
            ResourceRef.Create(ResourceKind.Create("work-management.board-item"), a.Id, _workspaceId), "hello", _ownerId, Now);
        (_boardId, _itemA, _itemB, _labelId, _approvalId, _commentId) = (board.Id, a.Id, b.Id, label.Id, approval.Id, comment.Id);

        await using var seed = _db.CreateOwnerContext(SystemTenant());
        seed.Boards.Add(board);
        seed.BoardGroups.Add(group);
        seed.BoardItems.AddRange(a, b);
        seed.Labels.Add(label);
        seed.BoardItemLabels.Add(BoardItemLabel.Create(_accountId, _workspaceId, board.Id, a.Id, label.Id, _ownerId, Now));
        seed.BoardItemLabels.Add(BoardItemLabel.Create(_accountId, _workspaceId, board.Id, b.Id, label.Id, _ownerId, Now));
        seed.ApprovalRequests.Add(approval);
        seed.Comments.Add(comment);
        await seed.SaveChangesAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task DeleteLabel_HeldByTwoItems_Succeeds_RemovesLinks_KeepsItems()
    {
        using var provider = CreateProvider(_accountId, _ownerId);
        using var scope = provider.CreateScope();
        BindIdempotencyKey(scope);

        var result = await Send<Result>(scope, new DeleteLabelCommand(_labelId));

        result.Succeeded.Should().BeTrue();
        await using var verify = _db.CreateOwnerContext(SystemTenant());
        (await verify.BoardItemLabels.CountAsync(l => l.LabelId == _labelId)).Should().Be(0);
        (await verify.Labels.CountAsync(l => l.Id == _labelId)).Should().Be(0);
        (await verify.BoardItems.CountAsync(i => i.Id == _itemA || i.Id == _itemB)).Should().Be(2);
    }

    [Fact]
    public async Task CancelApproval_ByRequester_Succeeds()
    {
        using var provider = CreateProvider(_accountId, _memberId);
        using var scope = provider.CreateScope();
        BindIdempotencyKey(scope);

        var result = await Send<Result>(scope, new CancelApprovalRequestCommand(_approvalId, await ApprovalVersionAsync()));

        result.Succeeded.Should().BeTrue();
        await using var verify = _db.CreateOwnerContext(SystemTenant());
        (await verify.ApprovalRequests.SingleAsync(r => r.Id == _approvalId)).Status.Should().Be(ApprovalStatus.Cancelled);
    }

    [Fact]
    public async Task CancelApproval_ByAnotherMemberWithoutManageBoard_IsForbidden()
    {
        var other = await AddMemberAsync();
        using var provider = CreateProvider(_accountId, other);
        using var scope = provider.CreateScope();
        BindIdempotencyKey(scope);

        var version = await ApprovalVersionAsync();
        var act = () => Send<Result>(scope, new CancelApprovalRequestCommand(_approvalId, version));

        await act.Should().ThrowAsync<AppForbidden>();
    }

    [Fact]
    public async Task CancelApproval_WithStaleVersion_IsRejectedAsPreconditionFailed()
    {
        using var provider = CreateProvider(_accountId, _memberId);
        using var scope = provider.CreateScope();
        BindIdempotencyKey(scope);

        var stale = (await ApprovalVersionAsync()) + 5;
        var act = () => Send<Result>(scope, new CancelApprovalRequestCommand(_approvalId, stale));

        await act.Should().ThrowAsync<Notrelix.Application.Common.Exceptions.PreconditionFailedException>();
    }

    [Fact]
    public async Task Reactions_RepeatedPutAndDelete_AreNoOps()
    {
        var emoji = Notrelix.Application.Features.Collaboration.Reactions.ReactionPalette.Emojis[0];
        foreach (var step in new[] { "put", "put", "delete", "delete" })
        {
            using var provider = CreateProvider(_accountId, _memberId);
            using var scope = provider.CreateScope();
            object command = step == "put"
                ? new AddReactionCommand(_itemA, _commentId, emoji)
                : new RemoveReactionCommand(_itemA, _commentId, emoji);

            var result = await Send<Result>(scope, command);

            result.Succeeded.Should().BeTrue(step);
        }

        await using var verify = _db.CreateOwnerContext(SystemTenant());
        (await verify.Reactions.CountAsync(r => r.UserId == _memberId)).Should().Be(0);
    }

    [Fact]
    public async Task CommentTimestamps_SerializeAsUtcWithZ()
    {
        using var provider = CreateProvider(_accountId, _ownerId);
        using var scope = provider.CreateScope();

        var result = await Send<Result<List<CommentDto>>>(scope, GetCommentsQuery.ForBoardItem(_itemA));

        var json = JsonSerializer.Serialize(result.Data, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        var created = JsonDocument.Parse(json).RootElement[0].GetProperty("createdAt").GetString()!;
        created.Should().EndWith("Z");
    }

    private async Task<long> ApprovalVersionAsync()
    {
        await using var ctx = _db.CreateOwnerContext(SystemTenant());
        return await ctx.ApprovalRequests.Where(r => r.Id == _approvalId).Select(r => r.Version).SingleAsync();
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
            RequestDescriptorRegistry.Create(typeof(DeleteLabelCommand).Assembly));

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
        services.AddScoped<IRequestHandler<DeleteLabelCommand, Result>, DeleteLabelCommandHandler>();
        services.AddScoped<IRequestHandler<CancelApprovalRequestCommand, Result>, CancelApprovalRequestCommandHandler>();
        services.AddScoped<IRequestHandler<AddReactionCommand, Result>, AddReactionCommandHandler>();
        services.AddScoped<IRequestHandler<RemoveReactionCommand, Result>, RemoveReactionCommandHandler>();
        services.AddScoped<IRequestHandler<GetCommentsQuery, Result<List<CommentDto>>>, GetCommentsQueryHandler>();
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