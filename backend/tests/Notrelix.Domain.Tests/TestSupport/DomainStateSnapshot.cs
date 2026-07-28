using System.Reflection;
using System.Text;

namespace Notrelix.Domain.Tests.TestSupport;

/// <summary>
/// Reflection-based capture of all instance fields on a Domain object.
/// Uses private fields (not properties) to avoid computed getters.
/// Walks owned Domain types recursively with reference-cycle protection.
/// </summary>
internal static class DomainStateSnapshot
{
    private static readonly HashSet<Type> VisitedTypes = new();
    private static readonly HashSet<int> VisitedObjectHashes = new();

    public static string Capture(object root)
    {
        VisitedTypes.Clear();
        VisitedObjectHashes.Clear();

        var sb = new StringBuilder();
        CaptureObject(root, sb, depth: 0, path: "");
        return sb.ToString();
    }

    private static void CaptureObject(object obj, StringBuilder sb, int depth, string path)
    {
        if (obj is null)
        {
            sb.AppendLine($"{path} = <null>");
            return;
        }

        var type = obj.GetType();
        var hash = obj.GetHashCode();

        if (!VisitedObjectHashes.Add(hash))
        {
            sb.AppendLine($"{path} = <ref cycle ({type.Name} #{hash})>");
            return;
        }

        if (!VisitedTypes.Add(type))
        {
            sb.AppendLine($"{path} = <type already visited ({type.Name})>");
            return;
        }

        if (depth > 10)
        {
            sb.AppendLine($"{path} = <max depth exceeded ({type.Name})>");
            return;
        }

        var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
        var ordered = fields.OrderBy(f => f.Name, StringComparer.Ordinal).ToList();

        foreach (var field in ordered)
        {
            var fieldPath = string.IsNullOrEmpty(path) ? field.Name : $"{path}.{field.Name}";
            var value = field.GetValue(obj);

            if (value is null)
            {
                sb.AppendLine($"{fieldPath} = <null>");
                continue;
            }

            var valueType = value.GetType();

            if (IsPrimitive(valueType))
            {
                sb.AppendLine($"{fieldPath} = {FormatValue(value, valueType)}");
            }
            else if (value is Array array)
            {
                sb.AppendLine($"{fieldPath} = [{array.Length} items]");
                for (var i = 0; i < array.Length; i++)
                {
                    var item = array.GetValue(i);
                    if (item is not null && !IsPrimitive(item.GetType()))
                    {
                        CaptureObject(item, sb, depth + 1, $"{fieldPath}[{i}]");
                    }
                    else
                    {
                        sb.AppendLine($"{fieldPath}[{i}] = {FormatValue(item, item?.GetType())}");
                    }
                }
            }
            else if (value is System.Collections.IEnumerable enumerable and not string)
            {
                var items = enumerable.Cast<object>().ToList();
                sb.AppendLine($"{fieldPath} = [{items.Count} items]");
                for (var i = 0; i < items.Count; i++)
                {
                    var item = items[i];
                    if (item is not null && !IsPrimitive(item.GetType()))
                    {
                        CaptureObject(item, sb, depth + 1, $"{fieldPath}[{i}]");
                    }
                    else
                    {
                        sb.AppendLine($"{fieldPath}[{i}] = {FormatValue(item, item?.GetType())}");
                    }
                }
            }
            else if (valueType.Namespace?.StartsWith("Notrelix.Domain") == true)
            {
                CaptureObject(value, sb, depth + 1, fieldPath);
            }
            else
            {
                sb.AppendLine($"{fieldPath} = {FormatValue(value, valueType)}");
            }
        }

        // Walk base type fields
        var baseType = type.BaseType;
        if (baseType is not null && baseType != typeof(object) && baseType.Namespace?.StartsWith("Notrelix.Domain") == true)
        {
            var baseFields = baseType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            var baseOrdered = baseFields.OrderBy(f => f.Name, StringComparer.Ordinal).ToList();

            foreach (var field in baseOrdered)
            {
                var fieldPath = string.IsNullOrEmpty(path) ? field.Name : $"{path}.{field.Name}";
                var value = field.GetValue(obj);

                if (value is null)
                {
                    sb.AppendLine($"{fieldPath} = <null>");
                    continue;
                }

                var valueType = value.GetType();

                if (IsPrimitive(valueType))
                {
                    sb.AppendLine($"{fieldPath} = {FormatValue(value, valueType)}");
                }
                else if (valueType.Namespace?.StartsWith("Notrelix.Domain") == true)
                {
                    CaptureObject(value, sb, depth + 1, fieldPath);
                }
                else
                {
                    sb.AppendLine($"{fieldPath} = {FormatValue(value, valueType)}");
                }
            }
        }
    }

    private static bool IsPrimitive(Type type) =>
        type.IsPrimitive ||
        type == typeof(string) ||
        type == typeof(decimal) ||
        type == typeof(DateTime) ||
        type == typeof(DateTimeOffset) ||
        type == typeof(Guid) ||
        type.IsEnum ||
        (type.IsValueType && type.Namespace?.StartsWith("System") == true);

    private static string? FormatValue(object? value, Type? type)
    {
        if (value is null) return "<null>";
        if (type is null) return value.ToString();

        if (type == typeof(DateTimeOffset))
        {
            if ((DateTimeOffset)value == default) return "<default>";
        }
        else if (type == typeof(DateTime))
        {
            if ((DateTime)value == default) return "<default>";
        }
        else if (type == typeof(Guid))
        {
            if ((Guid)value == Guid.Empty) return "<empty>";
        }
        else if (type == typeof(string))
        {
            if (string.IsNullOrEmpty((string)value)) return "<empty>";
        }

        return value.ToString();
    }
}
