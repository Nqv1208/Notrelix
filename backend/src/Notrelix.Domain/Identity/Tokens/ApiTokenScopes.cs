using System.Collections.ObjectModel;
using System.Text.Json;

namespace Notrelix.Domain.Identity.Tokens;

public sealed class ApiTokenScopes : ValueObject
{
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
            throw new BusinessRuleException("Invalid API token scopes JSON format.");
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
