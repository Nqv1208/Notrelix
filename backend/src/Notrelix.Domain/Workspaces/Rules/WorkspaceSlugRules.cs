namespace Notrelix.Domain.Workspaces.Rules;

/// <summary>
/// The address rules of a Workspace slug (accepted 2026-10-24): 3 to 63 characters in the shared slug
/// format, and never one of the reserved words that name product routes. A slug is unique per Account,
/// which the data layer enforces, not this rule.
/// The rule applies when a slug is chosen (creation, or a future rename). A Workspace created before the
/// rule keeps its slug and keeps working: reading, resolving and every other operation never re-validate
/// a stored slug.
/// </summary>
public static class WorkspaceSlugRules
{
    public const int MinLength = 3;
    public const int MaxLength = 63;

    /// <summary>Addresses nobody may take, because the product uses them as routes or names.</summary>
    public static readonly IReadOnlySet<string> Reserved = new HashSet<string>(StringComparer.Ordinal)
    {
        "admin", "api", "app", "www", "settings", "login", "signup", "billing", "support", "help",
        "new", "invite", "join", "onboarding", "workspaces",
    };

    /// <summary>The slug as the rule reads it: trimmed and lower-cased.</summary>
    public static string Normalize(string? slug) => (slug ?? string.Empty).Trim().ToLowerInvariant();

    /// <summary>
    /// The rule code the slug breaks, or null when it is acceptable. Format first (shared slug format),
    /// then length, then the reserved words.
    /// </summary>
    public static string? FindViolation(string? slug)
    {
        var normalized = Normalize(slug);
        if (!Slug.IsWellFormed(normalized))
            return SharedKernelRuleCodes.SharedKernel_Slug_InvalidFormat;
        if (normalized.Length < MinLength)
            return WorkspaceRuleCodes.Workspaces_Workspace_SlugTooShort;
        if (normalized.Length > MaxLength)
            return WorkspaceRuleCodes.Workspaces_Workspace_SlugTooLong;
        if (Reserved.Contains(normalized))
            return WorkspaceRuleCodes.Workspaces_Workspace_SlugReserved;
        return null;
    }

    /// <summary>Throws the stable rule code for a slug that may not name a Workspace.</summary>
    public static void Validate(string slug)
    {
        switch (FindViolation(slug))
        {
            case null:
                return;
            case var code when code == WorkspaceRuleCodes.Workspaces_Workspace_SlugTooShort:
                throw new BusinessRuleException(code, $"A workspace address has at least {MinLength} characters.");
            case var code when code == WorkspaceRuleCodes.Workspaces_Workspace_SlugTooLong:
                throw new BusinessRuleException(code, $"A workspace address has at most {MaxLength} characters.");
            case var code when code == WorkspaceRuleCodes.Workspaces_Workspace_SlugReserved:
                throw new BusinessRuleException(code, $"'{Normalize(slug)}' is reserved and cannot be a workspace address.");
            default:
                throw new BusinessRuleException(
                    SharedKernelRuleCodes.SharedKernel_Slug_InvalidFormat,
                    "A workspace address uses only lowercase letters, numbers and single hyphens.");
        }
    }
}
