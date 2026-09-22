namespace Notrelix.Infrastructure.Events.Replay;

public sealed class ReplayPipeline : IReplayPipeline
{
    private readonly ILogger<ReplayPipeline> _logger;

    public ReplayPipeline(ILogger<ReplayPipeline> logger)
    {
        _logger = logger;
    }

    public Task<ReplayResult> ExecuteAsync(ReplayRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!request.Authorized)
        {
            return Task.FromResult(new ReplayResult
            {
                Errors = ["Replay not authorized."],
            });
        }

        _logger.LogInformation(
            "Replay pipeline unavailable for {EventName} v{EventVersion} requested by {RequestedBy}: no retained event source is configured",
            request.EventName, request.EventVersion, request.RequestedBy);

        return Task.FromResult(new ReplayResult
        {
            Errors = ["Replay is unavailable: no retained event source is configured."],
        });
    }
}
