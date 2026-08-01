using System.Runtime.CompilerServices;

namespace Notrelix.Architecture.Tests;

public static class ArchitectureExclusions
{
    private static readonly HashSet<string> ExcludedTypes = new()
    {
        typeof(object).FullName!,
        typeof(ValueType).FullName!,
        typeof(IEquatable<>).FullName!,
        typeof(IComparable<>).FullName!,
        typeof(IComparable).FullName!,
    };

    private static readonly HashSet<string> ExcludedAttributes = new()
    {
        typeof(ObsoleteAttribute).FullName!,
        typeof(SerializableAttribute).FullName!,
        typeof(CompilerGeneratedAttribute).FullName!,
    };

    public static bool IsExcludedType(Type type)
    {
        if (type == null)
            return false;

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEquatable<>))
            return true;

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IComparable<>))
            return true;

        if (type.GetInterfaces().Any(i => i == typeof(IComparable)))
            return true;

        if (ExcludedTypes.Contains(type.FullName!))
            return true;

        if (typeof(Attribute).IsAssignableFrom(type) && ExcludedAttributes.Contains(type.FullName!))
            return true;

        return false;
    }

    public static bool IsExcludedAttribute(Type type)
    {
        if (type == null)
            return false;

        if (typeof(Attribute).IsAssignableFrom(type))
            return true;

        return ExcludedAttributes.Contains(type.FullName!);
    }
}
