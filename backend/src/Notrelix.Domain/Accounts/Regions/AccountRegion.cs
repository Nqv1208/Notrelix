namespace Notrelix.Domain.Accounts.Regions;

public class AccountRegion : AuditableEntity, IAccountScoped
{
    public Guid AccountId { get; private set; }
    public string RegionCode { get; private set; } = null!;
    public string DataResidencyMode { get; private set; } = "Default";
    public bool IsPrimary { get; private set; }
    public string? MigrationStatus { get; private set; }

    private AccountRegion() : base() { }

    public AccountRegion(Guid accountId, string regionCode, bool isPrimary = false) : base()
    {
        Guard.NotEmpty(accountId);
        Guard.NotNullOrWhiteSpace(regionCode);

        AccountId = accountId;
        RegionCode = regionCode.Trim();
        DataResidencyMode = "Default";
        IsPrimary = isPrimary;
    }

    public void SetAsPrimary()
    {
        IsPrimary = true;
    }

    public void UnsetPrimary()
    {
        IsPrimary = false;
    }

    public void StartMigration()
    {
        DataResidencyMode = "Migrating";
        MigrationStatus = "InProgress";
    }

    public void CompleteMigration()
    {
        DataResidencyMode = "Pinned";
        MigrationStatus = "Completed";
    }
}
