namespace Notrelix.Infrastructure.Services;

public sealed class PostCommitActionQueue : IPostCommitActionQueue
{
    private readonly ILogger<PostCommitActionQueue> _logger;
    private readonly List<IPostCommitAction> _actions = new();
    private bool _isInScope;

    public PostCommitActionQueue(
        ILogger<PostCommitActionQueue> logger)
    {
        _logger = logger;
    }

    public IReadOnlyList<IPostCommitAction> Actions => _actions;

    public void BeginScope() => _isInScope = true;

    public void Enqueue(IPostCommitAction action)
    {
        _actions.Add(action);
    }

    public async Task FlushAsync(CancellationToken ct)
    {
        if (!_isInScope) return;

        var failedActions = new List<(string Type, Exception Exception)>();

        foreach (var action in _actions)
        {
            try
            {
                _logger.LogTrace("Flushing post-commit action: {ActionType}", action.GetType().Name);
                await action.ExecuteAsync(ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Post-commit action failed: {ActionType}", action.GetType().Name);
                failedActions.Add((action.GetType().Name, ex));
            }
        }

        Clear();

        if (failedActions.Count > 0)
        {
            _logger.LogWarning("Post-commit actions completed with {Count} failure(s): {Failed}",
                failedActions.Count, string.Join(", ", failedActions.Select(a => a.Type)));
        }
    }

    public void Clear()
    {
        _actions.Clear();
    }

    public void EndScope() => _isInScope = false;
}
