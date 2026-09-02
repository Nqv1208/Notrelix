namespace Notrelix.Application.Common.Security;

/// <summary>
/// Transport-neutral resource-owner authorization facts SPI (WG-WM-004, phase 11 P3-B
/// "resource-owner facts provider boundary"). Implemented by the bounded context that owns
/// a resource's private persistence; consumed by Governance facts provisioning and by
/// resource scope resolution. It must carry NO project/type dependency on EF, HTTP, gRPC,
/// messaging/broker, or any policy decision — it returns source-owner facts only.
/// </summary>
public interface IResourceAuthorizationFactsProvider
{
    /// <summary>
    /// Resolves the resource-owner authorization facts for <paramref name="resource"/> from
    /// the owning context's authoritative persistence. Returns <c>null</c> when the resource
    /// cannot be identified/located so callers fail closed.
    /// </summary>
    Task<ResourceAuthorizationFacts?> ResolveAsync(
        ResourceRef resource,
        Guid actorUserId,
        CancellationToken cancellationToken);
}

/// <summary>
/// Source-owner facts a resource context exposes to Governance. It deliberately excludes any
/// policy decision (deny/allow): decisions remain with <see cref="AccessPolicyEngine"/>.
/// </summary>
public sealed record ResourceAuthorizationFacts(
    Guid ResourceId,
    Guid AccountId,
    Guid WorkspaceId,
    bool Exists,
    string? Audience,
    string? MemberRole);
