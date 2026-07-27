using Microsoft.Extensions.Logging;

namespace Notrelix.Platform.Messaging.Observability;

public sealed class NullDiagnosticEventPublisher : IDiagnosticEventPublisher
{
    private readonly ILogger<NullDiagnosticEventPublisher>? _logger;

    public NullDiagnosticEventPublisher(ILogger<NullDiagnosticEventPublisher>? logger = null)
    {
        _logger = logger;
    }

    public void Publish<T>(T diagnosticEvent) where T : DiagnosticEvent
    {
        _logger?.LogDebug("Diagnostic event: {EventType} at {Timestamp}",
            typeof(T).Name, diagnosticEvent.Timestamp);
    }
}
