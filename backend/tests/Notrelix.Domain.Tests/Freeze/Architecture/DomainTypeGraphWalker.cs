using System.Reflection;

namespace Notrelix.Domain.Tests.Freeze.Architecture;

public static class DomainTypeGraphWalker
{
    private static readonly HashSet<Type> Visited = new();
    private static readonly List<Type> Result = new();

    public static IReadOnlyList<Type> GetReferencedTypes(Type root)
    {
        Visited.Clear();
        Result.Clear();
        Walk(root);
        return Result.AsReadOnly();
    }

    private static void Walk(Type type)
    {
        if (type == null || type == typeof(object) || type == typeof(ValueType) || !type.Assembly.GetName().Name!.StartsWith("Notrelix."))
            return;

        if (!Visited.Add(type))
            return;

        Result.Add(type);

        foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            Walk(prop.PropertyType);

        foreach (var iface in type.GetInterfaces())
            Walk(iface);

        if (type.IsGenericType)
        {
            foreach (var arg in type.GetGenericArguments())
                Walk(arg);
        }
    }
}
