using System.Collections.ObjectModel;
using System.Text.Json;

namespace Notrelix.Domain.Identity.Tokens;

public sealed class ApiTokenScopes : ValueObject
{
    public const string WorkRead = "work:read";
    public const string WorkWrite = "work:write";
    public const string DocsRead = "docs:read";
    public const string DocsWrite = "docs:write";
    public const string ChatRead = "chat:read";
    public const string ChatWrite = "chat:write";
    public const string WorkspaceRead = "workspace:read";

    /// <summary>Every scope a token may carry. Write does not imply read; a scope only narrows what the issuer may do.</summary>
    public static readonly IReadOnlyList<string> Vocabulary =
        [WorkRead, WorkWrite, DocsRead, DocsWrite, ChatRead, ChatWrite, WorkspaceRead];

    private readonly IReadOnlySet<string> _scopes;

    public IReadOnlySet<string> Scopes => _scopes;

    private ApiTokenScopes()
    {
        _scopes = new HashSet<string>();
    }

    private ApiTokenScopes(HashSet<string> scopes)
    {
        _scopes = new ReadOnlySet<string>(scopes);
    }

    /// <summary>The scopes chosen at issue: at least one, each from <see cref="Vocabulary"/>.</summary>
    public static ApiTokenScopes Create(IEnumerable<string> scopes)
    {
        var chosen = scopes.ToHashSet(StringComparer.Ordinal);
        if (chosen.Count == 0)
            throw new BusinessRuleException(IdentityRuleCodes.Identity_ApiToken_ScopesRequired, "An API token needs at least one scope.");

        var unknown = chosen.Where(scope => !Vocabulary.Contains(scope)).Order().ToList();
        if (unknown.Count > 0)
            throw new BusinessRuleException(IdentityRuleCodes.Identity_ApiToken_UnknownScope, $"Unknown API token scope: {string.Join(", ", unknown)}.");

        return new ApiTokenScopes(chosen);
    }

    /// <summary>Every scope; what a token issued before scopes existed was migrated to.</summary>
    public static ApiTokenScopes All() => new(Vocabulary.ToHashSet(StringComparer.Ordinal));

    public static ApiTokenScopes FromJson(string json)
    {
        try
        {
            var scopes = JsonSerializer.Deserialize<HashSet<string>>(json)
                         ?? new HashSet<string>();

            return new ApiTokenScopes(scopes);
        }
        catch (JsonException)
        {
            throw new BusinessRuleException(IdentityRuleCodes.Identity_ApiToken_InvalidScopesFormat, "Invalid API token scopes JSON format.");
        }
    }

    public bool Allows(string scope)
    {
        return _scopes.Contains(scope);
    }

    public bool HasAny()
    {
        return _scopes.Count > 0;
    }

    public string ToJson()
    {
        return JsonSerializer.Serialize(_scopes.ToList());
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        foreach (var scope in _scopes.OrderBy(x => x))
            yield return scope;
    }
}
