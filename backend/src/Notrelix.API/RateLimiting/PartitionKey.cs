namespace Notrelix.API.RateLimiting;

public enum PartitionKey
{
    Ip,
    /// <summary>The caller address together with the <c>formId</c> (or public form <c>publicKey</c>) route value.</summary>
    IpAndForm,
    /// <summary>The form alone, whoever calls: the <c>formId</c> or <c>publicKey</c> route value. Caps a distributed flood of one form.</summary>
    Form,
    UserId,
    AccountId,
    WorkspaceId,
    ApiKey,
    Global,
}
