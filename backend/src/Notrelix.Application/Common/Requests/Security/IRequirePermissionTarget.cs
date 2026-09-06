namespace Notrelix.Application.Common.Requests.Security;

/// <summary>
/// Resolves which Governance permission the actor intends to affect. Used by the
/// canonical AccessFacts query so the actor's authority and the target's existing
/// level are computed in one facts row. Not a policy input for the authorization
/// engine itself; network/UI circumstances must not override the engine decision.
/// </summary>
public interface IRequirePermissionTarget
{
    /// <summary>Subject kind of the affected permission (grant) or null (revoke).</summary>
    string? TargetSubjectType { get; }

    /// <summary>Subject id of the affected permission (grant) or null (revoke).</summary>
    Guid? TargetSubjectId { get; }

    /// <summary>Existing permission id being revoked, or null for a grant.</summary>
    Guid? TargetPermissionId { get; }
}