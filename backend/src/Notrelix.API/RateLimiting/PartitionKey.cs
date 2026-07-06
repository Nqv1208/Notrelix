namespace Notrelix.API.RateLimiting;

public enum PartitionKey
{
    Ip,
    UserId,
    AccountId,
    WorkspaceId,
    Global,
}
