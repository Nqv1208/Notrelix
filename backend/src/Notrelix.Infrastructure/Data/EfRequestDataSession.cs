using Notrelix.Application.Features.WorkManagement.Ordering;
using Notrelix.Infrastructure.Data.WorkManagement;
using Notrelix.Application.Common.Diagnostics;
using Notrelix.Application.Common.Exceptions;
using Notrelix.Application.Common.Idempotency;
using Notrelix.Infrastructure.Data.Concurrency;

namespace Notrelix.Infrastructure.Data;

/// <summary>
/// EF Core implementation of the provider-independent data session port.
/// Owns transaction, RLS, read-only, and SaveChanges mechanics.
/// </summary>
public sealed class EfRequestDataSession : IRequestDataSession
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IRlsSessionContext _rls;
    private readonly ILogger<EfRequestDataSession> _logger;
    private readonly IOutboxWakeSignal? _outboxWakeSignal;
    private readonly ExpectedVersionTargetMap _expectedVersionTargets;
    private readonly PipelineMetrics _metrics;

    private readonly Notrelix.Application.Features.WorkManagement.Ordering.WorkOrderingMetrics? _orderingMetrics;
    private readonly IRequestRollbackCompensations? _compensations;
    private readonly IIdempotencyExecutionContext? _idempotency;
    private readonly IActorLookupService? _actors;

    public EfRequestDataSession(
        ApplicationDbContext dbContext,
        IRlsSessionContext rls,
        ILogger<EfRequestDataSession> logger,
        IOutboxWakeSignal? outboxWakeSignal = null,
        ExpectedVersionTargetMap? expectedVersionTargets = null,
        PipelineMetrics? metrics = null,
        Notrelix.Application.Features.WorkManagement.Ordering.WorkOrderingMetrics? orderingMetrics = null,
        IRequestRollbackCompensations? compensations = null,
        IIdempotencyExecutionContext? idempotency = null,
        IActorLookupService? actors = null)
    {
        _actors = actors;
        _idempotency = idempotency;
        _compensations = compensations;
        _orderingMetrics = orderingMetrics;
        _dbContext = dbContext;
        _rls = rls;
        _logger = logger;
        _outboxWakeSignal = outboxWakeSignal;
        _expectedVersionTargets = expectedVersionTargets ?? ExpectedVersionTargetMap.Default;
        _metrics = metrics ?? new PipelineMetrics();
    }

    public async Task<TResponse> ExecuteAsync<TResponse>(
        RequestDataSessionOptions options,
        Func<CancellationToken, Task<TResponse>> action,
        CancellationToken cancellationToken)
    {
        if (options.Access == RequestDataAccess.None)
            return await action(cancellationToken);

        if (options.ApplyTenantScope && options.UserScopeUserId is not null)
            throw new ArgumentException(
                "A data session applies either the tenant scope or the user scope, never both.", nameof(options));

        using var sessionOpen = PipelineActivitySource.Instance.StartActivity("data_session.open");
        await using var transaction =
            await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        var commitStarted = false;
        try
        {
            if (options.Access == RequestDataAccess.ReadOnly)
            {
                _logger.LogTrace("Setting READ ONLY transaction");
                await _dbContext.Database.ExecuteSqlRawAsync(
                    "SET TRANSACTION READ ONLY", cancellationToken);
            }

            if (options.ApplyTenantScope)
            {
                _logger.LogTrace("Applying RLS session context");
                using var sessionRls = PipelineActivitySource.Instance.StartActivity("data_session.rls");
                await _rls.ApplyAsync(cancellationToken);
            }

            if (options.UserScopeUserId is { } userScopeUserId)
            {
                _logger.LogTrace("Applying RLS user scope");
                using var userRls = PipelineActivitySource.Instance.StartActivity("data_session.rls");
                await _rls.ApplyUserScopeAsync(userScopeUserId, cancellationToken);
            }

            var response = await action(cancellationToken);

            if (options.Access == RequestDataAccess.Transactional)
            {
                // A replayed response ran no handler, so no aggregate is tracked and nothing changes:
                // the version the first run carried no longer describes the resource.
                if (_idempotency?.IsReplay != true)
                    ApplyExpectedVersion(options.ExpectedVersion);
                _logger.LogTrace("Saving changes");
                using var saveChanges = PipelineActivitySource.Instance.StartActivity("data_session.save_changes");
                try
                {
                    await _dbContext.SaveChangesAsync(cancellationToken);
                }
                catch (DbUpdateConcurrencyException concurrency)
                {
                    // Normal optimistic-concurrency precondition failure: the
                    // declared version lost the race at the database.
                    _logger.LogDebug(
                        "Concurrency conflict on {Entries}",
                        string.Join(", ", concurrency.Entries.Select(entry => $"{entry.Metadata.Name}:{entry.State}")));
                    _metrics.ExpectedVersionConflicts.Add(1);
                    throw new PreconditionFailedException(
                        "The resource was modified by another request. Reload and retry.", "common.precondition-failed")
                    {
                        Extensions = await LastWriterAsync(concurrency, cancellationToken),
                    };
                }
                catch (DbUpdateException exception) when (WorkOrderingConstraints.IsOrderingViolation(exception))
                {
                    // A named ordering uniqueness guard caught a placement collision.
                    _orderingMetrics?.Conflict();
                    // SaveChanges runs after the handler returns, so this is never
                    // retried here; the caller reloads and rebases (WM-V3-ORDER-003).
                    throw new ConflictException(
                        "The position was taken by a concurrent change. Reload and retry.", WorkOrderingErrorCodes.Conflict);
                }
            }

            using var transactionCommit = PipelineActivitySource.Instance.StartActivity("data_session.commit");
            commitStarted = true;
            await transaction.CommitAsync(cancellationToken);
            if (options.Access == RequestDataAccess.Transactional)
                _outboxWakeSignal?.TrySignal();
            _logger.LogTrace("Committed data session");

            return response;
        }
        catch
        {
            _logger.LogWarning("Rolling back data session");
            await RunCompensationsAsync(commitStarted);
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    /// <summary>
    /// Who saved the resource that won the race, and when: the last writer of the stale aggregate as the
    /// database holds it now. The name comes through the co-member profile lookup, so a person the caller may
    /// not see stays absent and the client says "someone else". Best effort: any failure leaves the answer plain.
    /// </summary>
    private async Task<IReadOnlyDictionary<string, object?>> LastWriterAsync(
        DbUpdateConcurrencyException concurrency, CancellationToken ct)
    {
        try
        {
            var entry = concurrency.Entries.FirstOrDefault(e => e.Entity is AuditableEntity);
            if (entry is null) return new Dictionary<string, object?>();
            var current = await entry.GetDatabaseValuesAsync(ct);
            if (current is null) return new Dictionary<string, object?>();
            var by = current.GetValue<Guid?>(nameof(AuditableEntity.UpdatedBy));
            var at = current.GetValue<DateTimeOffset?>(nameof(AuditableEntity.UpdatedAt));
            string? name = null;
            if (by is { } actorId && _actors is not null)
                name = (await _actors.FindManyAsync([actorId], ct)).FirstOrDefault()?.Name;
            return new Dictionary<string, object?>
            {
                ["changedByName"] = name,
                ["changedAt"] = at,
                ["currentVersion"] = current.GetValue<long>(nameof(AggregateRoot.Version)),
            };
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _logger.LogDebug(exception, "Could not read the last writer of a stale resource");
            return new Dictionary<string, object?>();
        }
    }

    /// <summary>
    /// Runs the registered external-effect compensations (for example deleting an uploaded
    /// object) when the failure happened before the commit started, i.e. the database state is
    /// definitely rolled back. When the commit itself failed the outcome is unknown, so nothing
    /// is compensated: an unreachable object is safe, a deleted object behind a committed row is not.
    /// The caller's cancellation token is deliberately not used: a client abort is the most
    /// common reason for the failure and must not prevent the cleanup.
    /// </summary>
    private async Task RunCompensationsAsync(bool commitStarted)
    {
        if (_compensations is null)
            return;

        if (commitStarted)
        {
            _logger.LogWarning("Commit outcome unknown; rollback compensations skipped");
            return;
        }

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        await _compensations.RunAsync(timeout.Token);
    }

    private void ApplyExpectedVersion(ExpectedVersionConstraint? constraint)
    {
        if (constraint is null)
            return;

        // Fail closed: a declared expected-version constraint MUST bind to exactly
        // one tracked aggregate of the mapped target type. Anything else is a
        // server-side execution misconfiguration, never a silent skip and never a
        // client-facing concurrency conflict.
        ExpectedVersionTargetMap.TargetDefinition target;
        try
        {
            target = _expectedVersionTargets.Resolve(constraint.RequestType);
        }
        catch (InvalidOperationException ex)
        {
            _metrics.ExpectedVersionBindingMisconfigurations.Add(1);
            throw new SecurityMisconfigurationException(
                $"Expected-version binding failed for {constraint.RequestType.Name}: {ex.Message}", ex);
        }

        if (!_expectedVersionTargets.MatchesKind(constraint.RequestType, constraint.Resource))
        {
            _metrics.ExpectedVersionBindingMisconfigurations.Add(1);
            throw new SecurityMisconfigurationException(
                $"{constraint.RequestType.Name} declares resource kind '{constraint.Resource.Kind.Value}' " +
                $"but its expected-version target requires '{target.ExpectedResourceKind}'.");
        }

        var tracked = _dbContext.ChangeTracker.Entries()
            .Where(candidate => target.AggregateType.IsInstanceOfType(candidate.Entity))
            .Where(candidate => target.Matches((AggregateRoot)candidate.Entity, constraint.Resource.ResourceId))
            .ToArray();

        if (tracked.Length == 0)
        {
            _metrics.ExpectedVersionBindingMisconfigurations.Add(1);
            throw new SecurityMisconfigurationException(
                $"{constraint.RequestType.Name} declared an expected version for " +
                $"'{constraint.Resource.Kind.Value}:{constraint.Resource.ResourceId}' but no matching aggregate " +
                "of type " + $"{target.AggregateType.Name} is tracked by the data session.");
        }

        if (tracked.Length > 1)
        {
            _metrics.ExpectedVersionBindingMisconfigurations.Add(1);
            throw new SecurityMisconfigurationException(
                $"{constraint.RequestType.Name} matched {tracked.Length} tracked " +
                $"{target.AggregateType.Name} instances for '{constraint.Resource.ResourceId}'; the version " +
                "guard must bind to exactly one aggregate.");
        }

        var entry = tracked[0];
        var version = entry.Property(nameof(AggregateRoot.Version));
        version.OriginalValue = constraint.Value;

        // Preserve precondition semantics for domain no-ops: issue a version-guarded
        // update even when the aggregate did not otherwise change.
        if (entry.State == EntityState.Unchanged)
            version.IsModified = true;
    }
}
