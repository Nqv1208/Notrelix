namespace Notrelix.Application.Features.Integrations.N8n.Configuration;

/// <summary>
/// Integrations-owned strict allow-list for the n8n webhook path (the Automation Domain keeps its own
/// equal check at rule creation; this one guards dispatch and the provider adapter):
/// one or more segments of ASCII letters, digits, '_' or '-' (1..64 characters
/// each) joined by single '/'. Everything that could change where the request
/// lands relative to the internal n8n base address is outside the allow-list:
/// '.', '%', '?', '#', '\', ':', whitespace, a leading or trailing '/', and an
/// empty segment. Equivalent to
/// <c>^[A-Za-z0-9_-]{1,64}(/[A-Za-z0-9_-]{1,64})*$</c>, evaluated without
/// regex so the rule is exact and allocation free.
/// </summary>
public static class N8nWebhookPath
{
    public const int MaxSegmentLength = 64;
    public const int MaxLength = 512;

    public static bool IsValid(string? path)
    {
        if (string.IsNullOrEmpty(path) || path.Length > MaxLength)
            return false;

        var segmentLength = 0;
        foreach (var c in path)
        {
            if (c == '/')
            {
                if (segmentLength == 0)
                    return false;
                segmentLength = 0;
                continue;
            }

            if (!IsAllowed(c) || ++segmentLength > MaxSegmentLength)
                return false;
        }

        return segmentLength > 0;
    }

    private static bool IsAllowed(char c) =>
        c is >= 'a' and <= 'z'
          or >= 'A' and <= 'Z'
          or >= '0' and <= '9'
          or '_' or '-';
}
