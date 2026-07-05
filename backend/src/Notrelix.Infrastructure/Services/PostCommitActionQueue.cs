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

        foreach (var action in _actions)
        {
            _logger.LogTrace("Flushing post-commit action: {ActionType}", action.GetType().Name);
            await action.ExecuteAsync(ct);
        }

        Clear();
    }

    public void Clear()
    {
        _actions.Clear();
    }

    public void EndScope() => _isInScope = false;
}
