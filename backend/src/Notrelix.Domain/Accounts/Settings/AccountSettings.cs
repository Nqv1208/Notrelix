namespace Notrelix.Domain.Accounts.Settings;

public class AccountSettings : AuditableEntity, IAccountScoped
{
    public Guid AccountId { get; private set; }
    public string SettingKey { get; private set; } = null!;
    public string SettingValue { get; private set; } = null!;

    private AccountSettings() : base() { }

    public AccountSettings(Guid accountId, string key, string value) : base()
    {
        Guard.NotEmpty(accountId);
        Guard.NotNullOrWhiteSpace(key);
        Guard.MaxLength(key, 120);

        AccountId = accountId;
        SettingKey = key.Trim();
        SettingValue = value;
    }

    public void UpdateValue(string value)
    {
        SettingValue = value;
    }
}
