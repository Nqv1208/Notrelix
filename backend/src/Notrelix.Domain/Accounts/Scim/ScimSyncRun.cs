namespace Notrelix.Domain.Accounts.Scim;

public class ScimSyncRun : AuditableEntity, IAccountScoped
{
    public Guid AccountId { get; private set; }
    public Guid DirectoryId { get; private set; }
    public string Status { get; private set; } = "Pending";
    public DateTimeOffset? StartedAt { get; private set; }
    public DateTimeOffset? FinishedAt { get; private set; }
    public int UsersCreated { get; private set; }
    public int UsersUpdated { get; private set; }
    public int UsersDisabled { get; private set; }
    public string? ErrorMessage { get; private set; }

    private ScimSyncRun() : base() { }

    public ScimSyncRun(Guid accountId, Guid directoryId) : base()
    {
        Guard.NotEmpty(accountId);
        Guard.NotEmpty(directoryId);

        AccountId = accountId;
        DirectoryId = directoryId;
    }

    public void Start(DateTimeOffset startedAt)
    {
        Status = "Running";
        StartedAt = startedAt;
    }

    public void Complete(int usersCreated, int usersUpdated, int usersDisabled, DateTimeOffset finishedAt)
    {
        Status = "Succeeded";
        UsersCreated = usersCreated;
        UsersUpdated = usersUpdated;
        UsersDisabled = usersDisabled;
        FinishedAt = finishedAt;
    }

    public void Fail(string errorMessage, DateTimeOffset finishedAt)
    {
        Status = "Failed";
        ErrorMessage = errorMessage;
        FinishedAt = finishedAt;
    }

    public void Cancel(DateTimeOffset finishedAt)
    {
        Status = "Cancelled";
        FinishedAt = finishedAt;
    }
}
