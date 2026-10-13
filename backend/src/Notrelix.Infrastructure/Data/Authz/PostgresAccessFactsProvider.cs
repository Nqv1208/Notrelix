using System.Data;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Storage;
using Notrelix.Application.Common.Exceptions;
using Notrelix.Application.Common.Requests.Execution;
using Notrelix.Application.Common.Requests.Gates;
using Notrelix.Application.Common.Requests.Security;
using Notrelix.Application.Features.Billing.Public.Subscription;
using Notrelix.Application.Features.Documents.Public.PageAuthorization;
using Notrelix.Application.Features.WorkManagement.Public.Authorization;
using Notrelix.Infrastructure.CrossContext.Governance.WorkManagement;

namespace Notrelix.Infrastructure.Data.Authz;

public sealed class PostgresAccessFactsProvider : IAccessFactsProvider
{
    private const string Sql = AccessFactsQuery.Sql;
    private const string BoardKind = "work-management.board";

    private readonly ApplicationDbContext _dbContext;
    private readonly TimeProvider _timeProvider;
    private readonly IPageAuthorizationFacts _pageAuthorizationFacts;
    private readonly IBillingSubscriptionFacts _billingSubscriptionFacts;
    private readonly IBoardChildAuthorizationFacts _boardChildFacts;

    /// <param name="boardChildFacts">
    /// The Work Management-published Board-child relation; when not supplied, the
    /// Work Management adapter over the same request context is used.
    /// </param>
    public PostgresAccessFactsProvider(
        ApplicationDbContext dbContext,
        TimeProvider timeProvider,
        IPageAuthorizationFacts pageAuthorizationFacts,
        IBillingSubscriptionFacts billingSubscriptionFacts,
        IBoardChildAuthorizationFacts? boardChildFacts = null)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
        _pageAuthorizationFacts = pageAuthorizationFacts;
        _billingSubscriptionFacts = billingSubscriptionFacts;
        _boardChildFacts = boardChildFacts ?? new BoardChildAuthorizationFactsAdapter(dbContext);
    }

    public async Task<AccessFacts> ResolveAsync(
        RequestDescriptor descriptor,
        ExecutionContextSnapshot context,
        object request,
        CancellationToken cancellationToken)
    {
        if (!descriptor.Access.RequiresDatastoreFacts)
        {
            throw new SecurityMisconfigurationException(
                $"Access facts were requested for unprotected request {descriptor.RequestType.Name}.");
        }

        var permission = request as IRequirePermission;
        var target = request as IRequirePermissionTarget;
        var resource = permission?.Resource ?? context.Resource;
        if (descriptor.Scope == ApplicationScopeKind.Account && resource is null && context.AccountId.HasValue)
        {
            resource = ResourceRef.Create(ResourceKind.Create("accounts.account"), context.AccountId.Value);
        }

        var feature = request as IRequireFeature;

        // Page lifecycle/visibility/placement are Documents-owned facts, read
        // through the producer's published contract (never Documents persistence
        // in the shared SQL). The page's Space feeds the Space facts below.
        PageAuthorizationFact? page = null;
        if (resource?.Kind.Value == "documents.page")
        {
            page = await _pageAuthorizationFacts.ResolveAsync(resource.ResourceId, cancellationToken);
        }

        // GOV-033: a Board child resource is decided on its parent Board, read
        // through the Work Management-published relation (never Work child
        // tables in the shared SQL).
        var boardChild = resource is not null && resource.Kind.Value != BoardKind
            ? await _boardChildFacts.ResolveAsync(resource.Kind.Value, resource.ResourceId, cancellationToken)
            : BoardChildScope.NotBoardChild;
        var boardId = resource?.Kind.Value == BoardKind ? resource.ResourceId : boardChild.BoardId;

        var connection = _dbContext.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            throw new SecurityMisconfigurationException("Access facts require the active data-session connection.");
        }

        AccessFacts facts;
        await using (var command = connection.CreateCommand())
        {
            command.CommandText = Sql;
            command.Transaction = _dbContext.Database.CurrentTransaction?.GetDbTransaction()
                ?? throw new SecurityMisconfigurationException("Access facts require the active data-session transaction.");
            Add(command, "user_id", context.UserId, NpgsqlDbType.Uuid);
            Add(command, "account_id", context.AccountId, NpgsqlDbType.Uuid);
            Add(command, "workspace_id", context.WorkspaceId, NpgsqlDbType.Uuid);
            Add(command, "resource_type", resource?.Kind.Value, NpgsqlDbType.Text);
            Add(command, "resource_id", resource?.ResourceId, NpgsqlDbType.Uuid);
            Add(command, "resource_was_located", context.Resource is not null, NpgsqlDbType.Boolean);
            Add(command, "action", permission?.Action.ToString(), NpgsqlDbType.Text);
            Add(command, "target_subject_type", target?.TargetSubjectType, NpgsqlDbType.Text);
            Add(command, "target_subject_id", target?.TargetSubjectId, NpgsqlDbType.Uuid);
            Add(command, "target_permission_id", target?.TargetPermissionId, NpgsqlDbType.Uuid);
            Add(command, "feature_code", feature?.FeatureCode, NpgsqlDbType.Text);
            Add(command, "feature_amount", feature?.Amount ?? 0, NpgsqlDbType.Integer);
            Add(command, "now", _timeProvider.GetUtcNow(), NpgsqlDbType.TimestampTz);
            Add(command, "board_id", boardId, NpgsqlDbType.Uuid);
            Add(command, "board_child", boardChild.IsBoardChild, NpgsqlDbType.Boolean);
            Add(command, "page_space_id",
                page is not null && page.WorkspaceId == context.WorkspaceId ? page.SpaceId : null,
                NpgsqlDbType.Uuid);

            await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                throw new SecurityMisconfigurationException("Access facts query returned no row.");
            }

            var rules = JsonSerializer.Deserialize<AccessPermissionRule[]>(reader.GetString(10), JsonOptions) ?? [];
            facts = new AccessFacts(
                reader.GetBoolean(0),
                reader.GetBoolean(1),
                reader.GetBoolean(2),
                NullableString(reader, 3),
                reader.GetBoolean(4),
                NullableString(reader, 5),
                reader.GetBoolean(6),
                NullableString(reader, 7),
                NullableString(reader, 8),
                reader.GetBoolean(9),
                rules,
                true,
                reader.GetBoolean(11),
                NullableInt(reader, 12),
                NullableInt(reader, 13))
            {
                ResourceSpaceId = reader.IsDBNull(14) ? null : reader.GetGuid(14),
                SpaceExists = reader.GetBoolean(15),
                SpaceRole = NullableString(reader, 16),
                SpaceIsOpen = reader.GetBoolean(17),
                ParentBoardId = boardChild.IsBoardChild ? boardChild.BoardId : null,
            };
        }

        // The subscription requirement is a Billing-owned commercial decision,
        // composed here through the producer's published subscription contract
        // instead of reading billing.subscriptions (or re-deriving a tier
        // ladder) inside the shared authorization SQL. It is only evaluated
        // when the request actually carries a subscription gate.
        if (descriptor.Access.RequiresSubscription)
        {
            var requirement = request as IRequireSubscription
                ?? throw new SecurityMisconfigurationException(
                    $"{descriptor.RequestType.Name} declares RequiresSubscription but does not implement {nameof(IRequireSubscription)}.");
            var accountId = context.AccountId
                ?? throw new SecurityMisconfigurationException(
                    $"{descriptor.RequestType.Name} requires account context to evaluate the subscription gate.");
            var satisfied = await _billingSubscriptionFacts.SatisfiesRequirementAsync(
                accountId, requirement.MinimumTier, cancellationToken);
            facts = facts with { SubscriptionRequirementSatisfied = satisfied };
        }

        if (resource?.Kind.Value == "documents.page")
        {
            facts = ComposePageFacts(facts, page, context.WorkspaceId);
        }

        return facts;
    }

    private static AccessFacts ComposePageFacts(
        AccessFacts facts,
        PageAuthorizationFact? page,
        Guid? workspaceId)
    {
        var pageInScope = page is not null
            && workspaceId.HasValue
            && page.WorkspaceId == workspaceId.Value;

        return page is not null && pageInScope && page.Exists
            ? facts with
            {
                ResourceExists = page.IsActive,
                ResourceAudience = page.IsActive ? page.Visibility : null,
            }
            : facts with { ResourceExists = false, ResourceAudience = null };
    }

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private static string? NullableString(System.Data.Common.DbDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);

    private static int? NullableInt(System.Data.Common.DbDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? null : reader.GetInt32(ordinal);

    private static void Add(
        System.Data.Common.DbCommand command,
        string name,
        object? value,
        NpgsqlDbType type)
    {
        command.Parameters.Add(new NpgsqlParameter(name, type) { Value = value ?? DBNull.Value });
    }
}
