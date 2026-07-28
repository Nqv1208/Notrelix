using System.Text;
using System.Reflection;

namespace Notrelix.Domain.Tests.Freeze;

/// <summary>
/// Produces canonical method signatures for mutation coverage matching.
/// Format: MethodName(System.String,System.Guid,System.DateTimeOffset)
/// Uses fully qualified generic/nullable type names.
/// </summary>
public static class MutationSignatureFormatter
{
    public static string Format(MethodInfo method)
    {
        var sb = new StringBuilder();
        sb.Append(method.Name);
        sb.Append('(');

        var parameters = method.GetParameters();
        for (var i = 0; i < parameters.Length; i++)
        {
            if (i > 0) sb.Append(',');
            sb.Append(FormatType(parameters[i].ParameterType));
        }

        sb.Append(')');
        return sb.ToString();
    }

    public static string FormatType(Type type)
    {
        if (type.IsByRef)
            return FormatType(type.GetElementType()!) + '&';

        if (type.IsArray)
            return FormatType(type.GetElementType()!) + "[]";

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            return FormatType(type.GetGenericArguments()[0]) + '?';

        if (type.IsGenericType)
        {
            var name = type.Name;
            var backtick = name.IndexOf('`');
            if (backtick > 0) name = name[..backtick];

            var args = type.GetGenericArguments();
            var formatted = string.Join(",", args.Select(FormatType));
            return $"{type.Namespace}.{name}<{formatted}>";
        }

        if (type.IsPointer)
            return FormatType(type.GetElementType()!) + '*';

        // Non-generic type: use full name
        return type.FullName ?? type.Name;
    }
}
