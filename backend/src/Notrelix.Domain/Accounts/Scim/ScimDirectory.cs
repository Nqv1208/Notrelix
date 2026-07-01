namespace Notrelix.Domain.Accounts.Scim;

public class ScimDirectory : AggregateRoot, IAccountScoped
{
    public Guid AccountId { get; private set; }
    public Guid? IdentityProviderId { get; private set; }
    public string Name { get; private set; } = null!;
    public string? BaseUrl { get; private set; }
    public string? BearerTokenHash { get; private set; }
    public string Status { get; private set; } = "Active";
    public DateTimeOffset? LastSyncAt { get; private set; }

    private ScimDirectory() : base() { }

    public static ScimDirectory Create(Guid accountId, string name, Guid? identityProviderId = null, string? baseUrl = null)
    {
        Guard.NotEmpty(accountId);
        Guard.NotNullOrWhiteSpace(name);
        Guard.MaxLength(name, 120);

        return new ScimDirectory
        {
            AccountId = accountId,
            IdentityProviderId = identityProviderId,
            Name = name.Trim(),
            BaseUrl = baseUrl?.Trim()
        };
    }

    public void Enable()
    {
        if (Status == "Active") return;
        Status = "Active";
    }

    public void Disable()
    {
        if (Status == "Disabled") return;
        Status = "Disabled";
    }

    public void MarkError()
    {
        Status = "Error";
    }

    public void RecordSync(DateTimeOffset syncedAt)
    {
        LastSyncAt = syncedAt;
    }

    public void UpdateCredentials(string? bearerTokenHash)
    {
        BearerTokenHash = bearerTokenHash;
    }
}
