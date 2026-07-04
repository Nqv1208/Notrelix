
namespace Notrelix.Infrastructure.Identity.Services;

public class CurrentAccount : ICurrentAccount
{
    private const string ItemsKey = "CurrentAccountId";

    private readonly IHttpContextAccessor _httpContextAccessor;
    private Guid? _explicitAccountId;

    public CurrentAccount(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? AccountId
    {
        get
        {
            if (_explicitAccountId.HasValue)
                return _explicitAccountId.Value;

            var context = _httpContextAccessor.HttpContext;
            if (context?.Items.TryGetValue(ItemsKey, out var value) == true && value is Guid guid)
                return guid;

            return null;
        }
    }

    public bool IsSet => _explicitAccountId.HasValue
        || _httpContextAccessor.HttpContext?.Items.ContainsKey(ItemsKey) == true;

    public void SetAccount(Guid accountId)
    {
        _explicitAccountId = accountId;
    }
}
