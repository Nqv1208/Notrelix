using Notrelix.Application.Events.Documents;
using Notrelix.Application.Features.Documents.Versions.Services;
using Notrelix.Infrastructure.Data;

namespace Notrelix.Infrastructure.Messaging.Consumers.Documents;

/// <summary>
/// After the blocks of a page were saved and committed, adds an automatic history entry when the page has
/// changed since its last entry and that entry is at least ten minutes old (DCT-016/017). The save itself never
/// waits for this and never fails because of it. Retention: every entry is kept for now; no pruning runs.
/// </summary>
[OrderingNotApplicable("An entry is added only when the committed content differs from the newest entry and that entry is old enough, so a late, repeated or reordered delivery adds nothing the page's state does not warrant.")]
public sealed class PageBlocksSavedVersionConsumer : IConsumer<PageBlocksSavedIntegrationEvent>
{
    private readonly IPageVersionRecorder _recorder;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<PageBlocksSavedVersionConsumer> _logger;

    public PageBlocksSavedVersionConsumer(IPageVersionRecorder recorder, ApplicationDbContext context, ILogger<PageBlocksSavedVersionConsumer> logger)
    {
        _recorder = recorder;
        _context = context;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<PageBlocksSavedIntegrationEvent> context)
    {
        var msg = context.Message;
        if (msg.ActorUserId is not { } actor || actor == Guid.Empty) return;

        var version = await _recorder.CaptureAutomaticAsync(msg.PageId, actor, DateTimeOffset.UtcNow, context.CancellationToken);
        if (version is null) return;

        await _context.SaveChangesAsync(context.CancellationToken);
        _logger.LogInformation("[Documents] Automatic version {VersionNumber} of page {PageId}", version.VersionNumber, msg.PageId);
    }
}

public sealed class PageBlocksSavedVersionConsumerDefinition : ConsumerDefinition<PageBlocksSavedVersionConsumer>
{
    public PageBlocksSavedVersionConsumerDefinition()
    {
        EndpointName = "notrelix-doc-page-blocks-saved-version-v1";
        ConcurrentMessageLimit = 4;
    }

    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<PageBlocksSavedVersionConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
        endpointConfigurator.PrefetchCount = 8;
        endpointConfigurator.UseMessageRetry(r => r.Interval(3, TimeSpan.FromMilliseconds(200)));
    }
}
