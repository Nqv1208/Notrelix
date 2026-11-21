namespace Notrelix.Application.Features.Workspaces.Invitations.Services;

/// <param name="Role"><c>Member</c> or <c>Guest</c>, as the single invitation takes it.</param>
/// <param name="InitialEntries">Optional Team/Space entries for this address, checked as a single invitation checks them.</param>
public record MemberInvitationEntry(string Email, string Role, IReadOnlyList<InvitationInitialEntryInput>? InitialEntries = null);

/// <summary>What happened to one address of the batch.</summary>
/// <param name="Outcome"><c>sent</c>, <c>already-member</c>, <c>already-pending</c>, <c>invalid</c> or <c>refused</c>.</param>
/// <param name="Code">The stable code of a refusal (<c>workspace.guest_invites_off</c>, a rule code); null otherwise.</param>
public record MemberInvitationOutcomeDto(string Email, string Role, string Outcome, string? Code, Guid? InvitationId);

public record MemberInvitationBatchResultDto(IReadOnlyList<MemberInvitationOutcomeDto> Outcomes);

public static class MemberInvitationOutcomes
{
    public const string Sent = "sent";
    public const string AlreadyMember = "already-member";
    public const string AlreadyPending = "already-pending";
    public const string Invalid = "invalid";
    public const string Refused = "refused";
}
