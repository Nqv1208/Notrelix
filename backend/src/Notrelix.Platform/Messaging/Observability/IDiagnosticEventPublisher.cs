namespace Notrelix.Platform.Messaging.Observability;

public interface IDiagnosticEventPublisher
{
    void Publish<T>(T diagnosticEvent) where T : DiagnosticEvent;
}
