using System.Reflection;

namespace Notrelix.Domain.Tests.Contracts.Snapshots;

public static class CanonicalTypeNameFormatter
{
    private static readonly Dictionary<Type, string> BuiltInAliases = new()
    {
        [typeof(void)] = "System.Void",
        [typeof(bool)] = "System.Boolean",
        [typeof(byte)] = "System.Byte",
        [typeof(sbyte)] = "System.SByte",
        [typeof(short)] = "System.Int16",
        [typeof(ushort)] = "System.UInt16",
        [typeof(int)] = "System.Int32",
        [typeof(uint)] = "System.UInt32",
        [typeof(long)] = "System.Int64",
        [typeof(ulong)] = "System.UInt64",
        [typeof(nint)] = "System.IntPtr",
        [typeof(nuint)] = "System.UIntPtr",
        [typeof(float)] = "System.Single",
        [typeof(double)] = "System.Double",
        [typeof(decimal)] = "System.Decimal",
        [typeof(char)] = "System.Char",
        [typeof(string)] = "System.String",
        [typeof(object)] = "System.Object",
        [typeof(Guid)] = "System.Guid",
        [typeof(DateTime)] = "System.DateTime",
        [typeof(DateTimeOffset)] = "System.DateTimeOffset",
        [typeof(TimeSpan)] = "System.TimeSpan",
        [typeof(Uri)] = "System.Uri",
        [typeof(Version)] = "System.Version",
        [typeof(IntPtr)] = "System.IntPtr",
        [typeof(UIntPtr)] = "System.UIntPtr",
    };

    public static string Format(Type type)
    {
        if (type.IsByRef)
            return Format(type.GetElementType()!) + '&';

        if (type.IsArray)
        {
            if (type.IsSZArray)
                return Format(type.GetElementType()!) + "[]";
            return Format(type.GetElementType()!) + "[,]";
        }

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            return Format(type.GetGenericArguments()[0]) + '?';

        if (type.IsGenericParameter)
            return type.Name;

        if (BuiltInAliases.TryGetValue(type, out var alias))
            return alias;

        if (!type.IsGenericType)
            return type.FullName ?? type.Name;

        var name = type.Name;
        var backtick = name.IndexOf('`');
        if (backtick > 0) name = name[..backtick];

        var args = type.GetGenericArguments();
        var formattedArgs = string.Join(",", args.Select(Format));

        return $"{type.Namespace}.{name}<{formattedArgs}>";
    }

    public static string FormatMethod(MethodInfo method)
    {
        var returnType = Format(method.ReturnType);
        var parameters = string.Join(",", method.GetParameters().Select(p => $"{Format(p.ParameterType)} {p.Name}"));
        return $"{returnType} {method.Name}({parameters})";
    }

    public static string FormatParameterList(MethodInfo method)
    {
        return string.Join(",", method.GetParameters().Select(p => $"{Format(p.ParameterType)} {p.Name}"));
    }

    public static string FormatSignature(MethodInfo method)
    {
        var parameters = string.Join(",", method.GetParameters().Select(p => Format(p.ParameterType)));
        return $"{method.Name}({parameters})";
    }
}
