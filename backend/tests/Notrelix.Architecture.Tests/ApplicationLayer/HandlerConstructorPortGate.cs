using System.Reflection;

namespace Notrelix.Architecture.Tests.ApplicationLayer;

/// <summary>
/// FZ-APP-AUTHZ-GATE-01: finds feature handlers that inject a forbidden
/// decision port through a public instance constructor. Permission decisions
/// are owned by the authorization pipeline; handlers express requirements
/// through request markers instead.
/// </summary>
internal static class HandlerConstructorPortGate
{
    internal static IReadOnlyList<string> FindForbiddenPorts(
        IEnumerable<Type> handlerTypes,
        IReadOnlySet<string> forbiddenPortNames)
    {
        return handlerTypes
            .SelectMany(handler => handler.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
                .SelectMany(c => c.GetParameters())
                .Where(p => forbiddenPortNames.Contains(p.ParameterType.FullName ?? p.ParameterType.Name))
                .Select(p => $"{handler.FullName}:{p.ParameterType.FullName}"))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToList();
    }
}
