namespace Notrelix.Application.Features.Workspaces.Public.Invitations;

/// <summary>
/// Workspaces-published marker for a write that introduces a Guest by invitation (a resource invitation, its resend, a
/// workspace invitation with the Guest role). Governance refuses it with <c>workspace.guest_invites_off</c> once every
/// other rule allowed it and the workspace setting <c>AllowGuestInvites</c> is off. Revoking and cancelling never carry
/// it: closing access is never blocked.
/// </summary>
public interface IGuestInvitationRequest
{
    /// <summary>False for a request of the same command that does not introduce a Guest (a Member invitation).</summary>
    bool IntroducesGuest { get; }
}
