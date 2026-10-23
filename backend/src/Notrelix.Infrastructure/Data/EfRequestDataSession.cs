using Notrelix.Application.Features.WorkManagement.Ordering;
using Notrelix.Infrastructure.Data.WorkManagement;
using Notrelix.Application.Common.Diagnostics;
using Notrelix.Application.Common.Exceptions;
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

    public EfRequestDataSession(
        ApplicationDbContext dbContext,
        IRlsSessionContext rls,
        ILogger<EfRequestDataSession> logger,
        IOutboxWakeSignal? outboxWakeSignal = null,
        ExpectedVersionTargetMap? expectedVersionTargets = null,
        PipelineMetrics? metrics = null,
        Notrelix.Application.Features.WorkManagement.Ordering.WorkOrderingMetrics? orderingMetrics = null,
        IRequestRollbackCompensations? compensations = null)
    {
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

            var response = await action(cancellationToken);

            if (options.Access == RequestDataAccess.Transactional)
            {
                ApplyExpectedVersion(options.ExpectedVersion);
                _logger.LogTrace("Saving changes");
                using var saveChanges = PipelineActivitySource.Instance.StartActivity("data_session.save_changes");
                try
                {
                    await _dbContext.SaveChangesAsync(cancellationToken);
                }
                catch (DbUpdateConcurrencyException)
                {
                    // Normal optimistic-concurrency precondition failure: the
                    // declared version lost the race at the database.
                    _metrics.ExpectedVersionConflicts.Add(1);
                    throw new PreconditionFailedException(
                        "The resource was modified by another request. Reload and retry.", "common.precondition-failed");
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
