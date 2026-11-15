namespace Notrelix.API.RateLimiting;

public enum PartitionKey
{
    Ip,
    /// <summary>The caller address together with the <c>formId</c> route value.</summary>
    IpAndForm,
    UserId,
    AccountId,
    WorkspaceId,
    ApiKey,
    Global,
}
