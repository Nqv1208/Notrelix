namespace Notrelix.Application.Common.Data;

/// <summary>
/// Per-request registry of compensating actions for external side effects (for example an
/// object written to storage) that cannot take part in the database transaction.
///
/// A handler registers the compensation BEFORE performing the effect. The data session runs
/// the registered compensations only when the request failed before the commit started, i.e.
/// when the database state is definitely rolled back. If the commit itself fails, the outcome
/// is unknown and the compensations are NOT run: leaving an unreachable object behind is the
/// safe direction, deleting an object a committed row points at is not.
///
/// Compensations are best effort and must be idempotent; a failing compensation is logged
/// and never masks the original failure.
/// </summary>
public interface IRequestRollbackCompensations
{
    void Register(string description, Func<CancellationToken, Task> compensation);

    /// <summary>Runs the registered compensations in reverse order and clears the registry.</summary>
    Task RunAsync(CancellationToken cancellationToken);
}

public sealed class RequestRollbackCompensations : IRequestRollbackCompensations
{
    private readonly List<(string Description, Func<CancellationToken, Task> Action)> _items = [];
    private readonly ILogger<RequestRollbackCompensations> _logger;

    public RequestRollbackCompensations(ILogger<RequestRollbackCompensations> logger)
    {
        _logger = logger;
    }

    public void Register(string description, Func<CancellationToken, Task> compensation)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        ArgumentNullException.ThrowIfNull(compensation);
        _items.Add((description, compensation));
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var pending = _items.ToArray();
        _items.Clear();

        for (var i = pending.Length - 1; i >= 0; i--)
        {
            try
            {
                await pending[i].Action(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Rollback compensation failed: {Compensation}", pending[i].Description);
            }
        }
    }
}
