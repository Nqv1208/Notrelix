namespace Notrelix.Application.Features.Identity.OAuth;

/// <summary>
/// The only shape of "where to go after sign-in" the product accepts: an internal, root-relative
/// path. Anything that a browser could resolve to another origin (scheme, protocol-relative,
/// backslash tricks, encoded variants, control characters) is rejected.
/// </summary>
public static class SafeReturnPath
{
    public const int MaxLength = 512;

    public static bool IsSafe(string? path)
    {
        if (string.IsNullOrEmpty(path) || path.Length > MaxLength)
            return false;

        // A single percent-decode pass catches %2F%2F, %5C and encoded control characters; a second
        // pass catches double-encoded variants. The value is never decoded for use, only inspected.
        var current = path;
        for (var pass = 0; pass < 3; pass++)
        {
            if (!IsSafeLiteral(current))
                return false;

            string decoded;
            try
            {
                decoded = Uri.UnescapeDataString(current);
            }
            catch (UriFormatException)
            {
                return false;
            }

            if (decoded == current)
                return true;

            current = decoded;
        }

        // Still changing after three decode passes: refuse rather than guess.
        return false;
    }

    /// <summary>The path when it is safe, otherwise <c>null</c>.</summary>
    public static string? OrNull(string? path) => IsSafe(path) ? path : null;

    private static bool IsSafeLiteral(string value)
    {
        if (value.Length == 0 || value[0] != '/')
            return false;

        // "//host" and "/\host" are protocol-relative for browsers.
        if (value.Length > 1 && (value[1] == '/' || value[1] == '\\'))
            return false;

        for (var index = 0; index < value.Length; index++)
        {
            var character = value[index];
            if (character == '\\' || char.IsControl(character))
                return false;

            // A '%' must start a complete escape; a broken one is refused rather than interpreted.
            if (character == '%'
                && (index + 2 >= value.Length || !Uri.IsHexDigit(value[index + 1]) || !Uri.IsHexDigit(value[index + 2])))
            {
                return false;
            }
        }

        return true;
    }
}
