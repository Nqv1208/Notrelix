using System.Reflection;

namespace Notrelix.Architecture.Tests;

/// <summary>
/// Stateless, thread-safe walker that returns all types reachable from a root type
/// through the CLR type graph: base types, interfaces, fields, properties,
/// constructors, method parameters/return types, generic arguments, arrays,
/// and nested generic arguments.
/// </summary>
public static class DomainTypeGraphWalker
{
    private static readonly Type[] EmptyTypes = [];

    public static IReadOnlyList<Type> GetReferencedTypes(Type root)
    {
        return GetReferencedTypes(root, IsDomainType);
    }

    public static IReadOnlyList<Type> GetReferencedTypes(IEnumerable<Type> roots)
    {
        return GetReferencedTypes(roots, IsDomainType);
    }

    /// <summary>
    /// Returns all types reachable from the root, collecting only types that match
    /// the supplied predicate. Defaults to Domain types; tests can supply a broader
    /// predicate to walk over their own helper types.
    /// </summary>
    public static IReadOnlyList<Type> GetReferencedTypes(Type root, Func<Type, bool> collect)
    {
        var visited = new HashSet<Type>();
        var result = new List<Type>();
        Walk(root, collect, visited, result);
        return result;
    }

    public static IReadOnlyList<Type> GetReferencedTypes(IEnumerable<Type> roots, Func<Type, bool> collect)
    {
        var visited = new HashSet<Type>();
        var result = new List<Type>();
        foreach (var root in roots)
            Walk(root, collect, visited, result);
        return result;
    }

    public static IReadOnlyList<Type> GetReferencedTypesFiltered(Type root, Func<Type, bool> include)
    {
        var all = GetReferencedTypes(root);
        return all.Where(include).ToList();
    }

    public static IReadOnlyList<Type> GetReferencedTypesFiltered(IEnumerable<Type> roots, Func<Type, bool> include)
    {
        var all = GetReferencedTypes(roots);
        return all.Where(include).ToList();
    }

    private static void Walk(Type? type, Func<Type, bool> collect, HashSet<Type> visited, List<Type> result)
    {
        if (type is null) return;

        // Unwrap Nullable<T>, ReadOnlySpan<T>, etc.
        try
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                Walk(type.GetGenericArguments()[0], collect, visited, result);
                return;
            }
        }
        catch (System.BadImageFormatException)
        {
            return;
        }

        // Unwrap arrays: T[] -> T
        if (type.IsArray)
        {
            Walk(type.GetElementType(), collect, visited, result);
            return;
        }

        // Skip system/object/value primitives early
        if (type == typeof(object) || type == typeof(ValueType) || type == typeof(string)
            || type == typeof(Guid) || type == typeof(DateTime) || type == typeof(DateTimeOffset)
            || type == typeof(decimal) || type == typeof(byte[]) || type == typeof(void)
            || type.IsPrimitive)
        {
            return;
        }

        if (!visited.Add(type))
            return;

        if (collect(type))
            result.Add(type);

        // Base type chain
        Type? baseType = null;
        try
        {
            baseType = type.BaseType;
        }
        catch (System.BadImageFormatException)
        {
            // skip
        }

        if (baseType is not null && baseType != typeof(object) && baseType != typeof(ValueType))
            Walk(baseType, collect, visited, result);

        // Interfaces
        try
        {
            foreach (var iface in type.GetInterfaces())
                Walk(iface, collect, visited, result);
        }
        catch (System.BadImageFormatException)
        {
            // skip
        }

        // Generic type definition arguments (when type is constructed generic)
        try
        {
            if (type.IsGenericType && !type.IsGenericTypeDefinition)
            {
                foreach (var arg in type.GetGenericArguments())
                    Walk(arg, collect, visited, result);
            }
        }
        catch (System.BadImageFormatException)
        {
            // skip
        }

        // Fields (instance + declared only to avoid inherited noise)
        try
        {
            foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                Walk(field.FieldType, collect, visited, result);
        }
        catch (System.BadImageFormatException)
        {
            // skip
        }

        // Properties (declared only)
        try
        {
            foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                Walk(prop.PropertyType, collect, visited, result);
        }
        catch (System.BadImageFormatException)
        {
            // skip
        }

        // Constructors
        try
        {
            foreach (var ctor in type.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                foreach (var param in ctor.GetParameters())
                    Walk(param.ParameterType, collect, visited, result);
            }
        }
        catch (System.BadImageFormatException)
        {
            // skip
        }

        // Methods (declared only) — return type + parameters
        try
        {
            foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
            {
                if (method.IsSpecialName) continue; // skip property/event accessors

                Walk(method.ReturnType, collect, visited, result);
                foreach (var param in method.GetParameters())
                    Walk(param.ParameterType, collect, visited, result);
            }
        }
        catch (System.BadImageFormatException)
        {
            // skip
        }

        // Events (delegate signatures)
        try
        {
            foreach (var evt in type.GetEvents(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                if (evt.EventHandlerType is not null)
                    Walk(evt.EventHandlerType, collect, visited, result);
            }
        }
        catch (System.BadImageFormatException)
        {
            // skip
        }
    }

    private static bool IsDomainType(Type type)
    {
        try
        {
            return type.Namespace?.StartsWith("Notrelix.Domain.", StringComparison.Ordinal) == true
                && type.Assembly.GetName().Name?.StartsWith("Notrelix.", StringComparison.Ordinal) == true;
        }
        catch (System.BadImageFormatException)
        {
            return false;
        }
    }
}
