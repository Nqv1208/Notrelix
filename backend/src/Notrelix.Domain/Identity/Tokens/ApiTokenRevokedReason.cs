namespace Notrelix.Domain.Identity.Tokens;

/// <summary>Why a token stopped working, shown beside its Revoked status.</summary>
public enum ApiTokenRevokedReason
{
    /// <summary>An owner or admin revoked it from the API tokens page.</summary>
    Manual,

    /// <summary>The person who issued it was removed from the workspace.</summary>
    MemberRemoved,

    /// <summary>The person who issued it was suspended in the workspace.</summary>
    MemberSuspended,

    /// <summary>The person who issued it left the workspace.</summary>
    MemberLeft
}
