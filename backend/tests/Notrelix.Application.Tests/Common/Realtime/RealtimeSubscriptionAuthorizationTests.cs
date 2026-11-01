using Notrelix.Application.Common.Diagnostics;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Common.Realtime.Gateway;
using Notrelix.Domain.Governance.Permissions;
using Notrelix.Application.Features.WorkManagement.Boards.Queries.AuthorizeBoardRealtimeSubscription;
using Notrelix.Application.Features.Workspaces.Workspaces.Queries.AuthorizeWorkspaceRealtimeSubscription;

namespace Notrelix.Application.Tests.Common.Realtime;

/// <summary>
/// The realtime subscription authorization queries run through the real
/// ExecutionContext and AccessControl behaviors and the real policy engine, so a
/// subscription is allowed or denied by exactly the gates the matching REST read uses.
/// </summary>
public sealed class RealtimeSubscriptionAuthorizationTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid AccountId = Guid.NewGuid();
    private static readonly Guid WorkspaceId = Guid.NewGuid();

    [Fact]
    public void Authorization_queries_use_the_same_permission_contract_as_the_rest_reads()
    {
        var workspace = new AuthorizeWorkspaceRealtimeSubscriptionQuery(WorkspaceId);
        var board = new AuthorizeBoardRealtimeSubscriptionQuery(Guid.NewGuid());

        ((IRequirePermission)workspace).Action.Should().Be(PermissionAction.ViewWorkspace);
        ((IRequirePermission)workspace).Resource!.Kind.Value.Should().Be("workspaces.workspace");
        ((IRequirePermission)board).Action.Should().Be(PermissionAction.ViewBoard);
        ((IRequirePermission)board).Resource!.Kind.Value.Should().Be("work-management.board");

        var workspaceDescriptor = RequestDescriptorValidator.Create(typeof(AuthorizeWorkspaceRealtimeSubscriptionQuery));
        workspaceDescriptor.Scope.Should().Be(ApplicationScopeKind.Workspace);
        workspaceDescriptor.Principal.Should().Be(ApplicationPrincipalKind.Authenticated);
        RequestDescriptorValidator.Create(typeof(AuthorizeBoardRealtimeSubscriptionQuery))
            .Scope.Should().Be(ApplicationScopeKind.Resource);
    }

    [Fact]
    public async Task Workspace_member_is_granted_with_tenant_binding_from_the_server()
    {
        var fixture = WorkspaceFixture(canAccess: true, role: "Member");

        var result = await fixture.Run(new AuthorizeWorkspaceRealtimeSubscriptionQuery(WorkspaceId));

        result.Succeeded.Should().BeTrue();
        result.Data.Should().Be(new RealtimeSubscriptionGrant(AccountId, WorkspaceId));
    }

    [Fact]
    public async Task Non_member_or_suspended_member_is_denied_before_the_handler_runs()
    {
        // The tenant bootstrap reports CanAccess=false for a user with no active
        // membership, including a suspended or removed member. The workspace is hidden from them:
        // the same NotFound as a workspace that does not exist.
        var fixture = WorkspaceFixture(canAccess: false, role: null);

        var act = () => fixture.Run(new AuthorizeWorkspaceRealtimeSubscriptionQuery(WorkspaceId));

        await act.Should().ThrowAsync<NotFoundException>();
        fixture.HandlerCalls.Should().Be(0);
    }

    [Fact]
    public async Task Cross_tenant_workspace_is_denied_even_if_the_client_names_it()
    {
        var foreignWorkspaceId = Guid.NewGuid();
        var fixture = WorkspaceFixture(canAccess: false, role: null, requestedWorkspaceId: foreignWorkspaceId);

        var act = () => fixture.Run(new AuthorizeWorkspaceRealtimeSubscriptionQuery(foreignWorkspaceId));

        await act.Should().ThrowAsync<NotFoundException>();
        fixture.HandlerCalls.Should().Be(0);
    }

    [Fact]
    public async Task Member_whose_role_fact_disappeared_after_bootstrap_is_denied_by_policy()
    {
        var fixture = WorkspaceFixture(canAccess: true, role: null);

        var act = () => fixture.Run(new AuthorizeWorkspaceRealtimeSubscriptionQuery(WorkspaceId));

        await act.Should().ThrowAsync<ForbiddenException>();
        fixture.HandlerCalls.Should().Be(0);
    }

    [Fact]
    public async Task Board_viewer_is_granted_with_tenant_binding_resolved_from_the_board()
    {
        var boardId = Guid.NewGuid();
        var fixture = BoardFixture(boardId, locatedIn: (AccountId, WorkspaceId), role: "Member", resourceExists: true);

        var result = await fixture.Run(new AuthorizeBoardRealtimeSubscriptionQuery(boardId));

        result.Data.Should().Be(new RealtimeSubscriptionGrant(AccountId, WorkspaceId));
    }

    [Fact]
    public async Task Board_in_another_account_is_hidden_as_not_found()
    {
        var boardId = Guid.NewGuid();
        var fixture = BoardFixture(
            boardId, locatedIn: (Guid.NewGuid(), Guid.NewGuid()), role: "Member", resourceExists: true,
            selectedAccountId: AccountId);

        var act = () => fixture.Run(new AuthorizeBoardRealtimeSubscriptionQuery(boardId));

        await act.Should().ThrowAsync<NotFoundException>();
        fixture.HandlerCalls.Should().Be(0);
    }

    [Fact]
    public async Task Unknown_board_is_not_found()
    {
        var boardId = Guid.NewGuid();
        var fixture = BoardFixture(boardId, locatedIn: null, role: "Member", resourceExists: true);

        var act = () => fixture.Run(new AuthorizeBoardRealtimeSubscriptionQuery(boardId));

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Board_without_workspace_membership_is_denied()
    {
        var boardId = Guid.NewGuid();
        var fixture = BoardFixture(boardId, locatedIn: (AccountId, WorkspaceId), role: null, resourceExists: true);

        var act = () => fixture.Run(new AuthorizeBoardRealtimeSubscriptionQuery(boardId));

        await act.Should().ThrowAsync<ForbiddenException>();
        fixture.HandlerCalls.Should().Be(0);
    }

    [Fact]
    public async Task Deleted_or_archived_board_is_not_found_even_for_a_member()
    {
        var boardId = Guid.NewGuid();
        var fixture = BoardFixture(boardId, locatedIn: (AccountId, WorkspaceId), role: "Member", resourceExists: false);

        var act = () => fixture.Run(new AuthorizeBoardRealtimeSubscriptionQuery(boardId));

        await act.Should().ThrowAsync<NotFoundException>();
        fixture.HandlerCalls.Should().Be(0);
    }

    [Fact]
    public async Task Board_with_a_first_priority_deny_rule_is_denied()
    {
        var boardId = Guid.NewGuid();
        var fixture = BoardFixture(
            boardId, locatedIn: (AccountId, WorkspaceId), role: "Member", resourceExists: true,
            rules: [new AccessPermissionRule(1, "Deny")]);

        var act = () => fixture.Run(new AuthorizeBoardRealtimeSubscriptionQuery(boardId));

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task Unauthenticated_principal_is_rejected()
    {
        var fixture = WorkspaceFixture(canAccess: true, role: "Member", authenticated: false);

        var act = () => fixture.Run(new AuthorizeWorkspaceRealtimeSubscriptionQuery(WorkspaceId));

        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    // ── Fixtures ────────────────────────────────────────────

    private static Fixture WorkspaceFixture(
        bool canAccess, string? role, Guid? requestedWorkspaceId = null, bool authenticated = true)
    {
        var workspaceId = requestedWorkspaceId ?? WorkspaceId;
        var bootstrap = new Mock<ITenantBootstrapStore>();
        bootstrap.Setup(store => store.ResolveWorkspaceAccessAsync(workspaceId, It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkspaceAccessSnapshot(AccountId, workspaceId, UserId, canAccess, canAccess));
        return Fixture.Create(bootstrap.Object, new Mock<IResourceLocator>().Object, Facts(role, resourceExists: true), null, authenticated);
    }

    private static Fixture BoardFixture(
        Guid boardId,
        (Guid AccountId, Guid WorkspaceId)? locatedIn,
        string? role,
        bool resourceExists,
        Guid? selectedAccountId = null,
        IReadOnlyList<AccessPermissionRule>? rules = null)
    {
        var locator = new Mock<IResourceLocator>();
        var kind = ResourceKind.Create("work-management.board");
        locator.Setup(l => l.LocateAsync(It.IsAny<ResourceRef>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(locatedIn is { } where
                ? new ResourceLocation(kind, boardId, where.AccountId, where.WorkspaceId)
                : null);
        return Fixture.Create(
            new Mock<ITenantBootstrapStore>().Object, locator.Object,
            Facts(role, resourceExists, rules), selectedAccountId, authenticated: true);
    }

    private static AccessFacts Facts(string? role, bool resourceExists, IReadOnlyList<AccessPermissionRule>? rules = null) => new(
        true, true, true, role is null ? null : "Member", true, role, resourceExists, "Workspace", null, false,
        rules ?? [], false, false, null, null);

    private sealed class Fixture
    {
        private ITenantBootstrapStore _bootstrap = null!;
        private IResourceLocator _locator = null!;
        private AccessFacts _facts = null!;
        private Guid? _selectedAccountId;
        private bool _authenticated;

        public int HandlerCalls { get; private set; }

        public static Fixture Create(
            ITenantBootstrapStore bootstrap,
            IResourceLocator locator,
            AccessFacts facts,
            Guid? selectedAccountId,
            bool authenticated)
            => new()
            {
                _bootstrap = bootstrap,
                _locator = locator,
                _facts = facts,
                _selectedAccountId = selectedAccountId,
                _authenticated = authenticated,
            };

        public Task<Result<RealtimeSubscriptionGrant>> Run(AuthorizeWorkspaceRealtimeSubscriptionQuery request)
            => Execute(request, execution => new AuthorizeWorkspaceRealtimeSubscriptionQueryHandler(execution)
                .Handle(request, CancellationToken.None));

        public Task<Result<RealtimeSubscriptionGrant>> Run(AuthorizeBoardRealtimeSubscriptionQuery request)
            => Execute(request, execution => new AuthorizeBoardRealtimeSubscriptionQueryHandler(execution)
                .Handle(request, CancellationToken.None));

        private async Task<Result<RealtimeSubscriptionGrant>> Execute<TRequest>(
            TRequest request,
            Func<IExecutionContextReader, Task<Result<RealtimeSubscriptionGrant>>> handler)
            where TRequest : IRequest<Result<RealtimeSubscriptionGrant>>
        {
            var execution = new Notrelix.Application.Common.Context.ExecutionContext();
            if (_authenticated)
            {
                execution.SetUser(UserId, "user@example.com", "User");
            }

            var tenant = new Mock<ICurrentTenantContext>();
            tenant.SetupGet(t => t.UserId).Returns(_authenticated ? UserId : null);
            tenant.SetupGet(t => t.AccountId).Returns(_selectedAccountId);
            var credential = new Mock<ICurrentCredentialContext>();
            credential.SetupGet(c => c.Kind).Returns(CredentialKind.UserSession);

            var descriptor = RequestDescriptorValidator.Create(typeof(TRequest));
            var descriptors = new Mock<IRequestDescriptorRegistry>();
            descriptors.Setup(registry => registry.GetRequired(typeof(TRequest))).Returns(descriptor);
            var provider = new Mock<IAccessFactsProvider>();
            provider.Setup(p => p.ResolveAsync(
                    It.IsAny<RequestDescriptor>(), It.IsAny<ExecutionContextSnapshot>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(_facts);

            var contextBehavior = new ExecutionContextBehavior<TRequest, Result<RealtimeSubscriptionGrant>>(
                descriptors.Object, execution, tenant.Object, credential.Object, _locator, _bootstrap);
            var accessBehavior = new AccessControlBehavior<TRequest, Result<RealtimeSubscriptionGrant>>(
                descriptors.Object, execution, provider.Object, new AccessPolicyEngine(), new PipelineMetrics(),
                Microsoft.Extensions.Logging.Abstractions.NullLogger<AccessControlBehavior<TRequest, Result<RealtimeSubscriptionGrant>>>.Instance);

            return await contextBehavior.Handle(
                request,
                ct => accessBehavior.Handle(request, _ =>
                {
                    HandlerCalls++;
                    return handler(execution);
                }, ct),
                CancellationToken.None);
        }
    }
}
