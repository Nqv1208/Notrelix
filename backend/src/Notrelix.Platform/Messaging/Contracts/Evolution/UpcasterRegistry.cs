using System.Collections.Concurrent;

namespace Notrelix.Platform.Messaging.Contracts.Evolution;

public sealed class UpcasterRegistry
{
    private readonly ConcurrentDictionary<string, List<IUpcaster>> _upcasters = new();

    public void Register(IUpcaster upcaster)
    {
        var list = _upcasters.GetOrAdd(upcaster.EventName, _ => []);
        lock (list)
        {
            list.Add(upcaster);
        }
    }

    public IReadOnlyList<IUpcaster> GetUpcasters(string eventName)
    {
        return _upcasters.TryGetValue(eventName, out var list)
            ? list.ToArray()
            : [];
    }

    public bool CanUpcast(string eventName, int fromVersion, int toVersion)
    {
        if (fromVersion == toVersion)
            return true;

        var upcasters = GetUpcasters(eventName);
        if (upcasters.Count == 0)
            return false;

        return upcasters.Any(u => u.CanUpcast(fromVersion, toVersion));
    }

    public object? Upcast(object @event, string eventName, int fromVersion, int toVersion)
    {
        if (fromVersion == toVersion)
            return @event;

        var upcaster = GetUpcasters(eventName)
            .FirstOrDefault(u => u.CanUpcast(fromVersion, toVersion));

        return upcaster?.Upcast(@event, fromVersion, toVersion);
    }

    public object? UpcastChain(object @event, string eventName, int fromVersion, int toVersion)
    {
        if (fromVersion == toVersion)
            return @event;

        var upcasters = GetUpcasters(eventName)
            .OrderBy(u => u switch
            {
                _ when u.CanUpcast(fromVersion, fromVersion + 1) => fromVersion,
                _ => int.MaxValue,
            })
            .ToList();

        var current = @event;
        var currentVersion = fromVersion;

        while (currentVersion < toVersion)
        {
            var nextVersion = currentVersion + 1;
            var upcaster = upcasters.FirstOrDefault(u => u.CanUpcast(currentVersion, nextVersion));

            if (upcaster is null)
                return null;

            current = upcaster.Upcast(current!, currentVersion, nextVersion);
            currentVersion = nextVersion;
        }

        return current;
    }
}
