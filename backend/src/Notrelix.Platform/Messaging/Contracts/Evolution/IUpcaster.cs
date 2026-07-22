namespace Notrelix.Platform.Messaging.Contracts.Evolution;

public interface IUpcaster
{
    string EventName { get; }
    bool CanUpcast(int fromVersion, int toVersion);
    object Upcast(object @event, int fromVersion, int toVersion);
}
