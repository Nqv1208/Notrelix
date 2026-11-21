namespace Notrelix.API.Contracts.Workspaces.Members.Requests;

/// <param name="Entries">One to 50 addresses with their role; an address repeated in the list is invited once.</param>
public record InviteMembersRequest(IReadOnlyList<InviteMembersEntryRequest> Entries);

/// <param name="Role"><c>Member</c> or <c>Guest</c>, as for a single invitation.</param>
/// <param name="InitialEntries">Optional Team/Space entries for this address; naming them needs ManageTeams or ManageSpaces, as for a single invitation, and a Guest entry cannot name a Team.</param>
public record InviteMembersEntryRequest(string Email, string Role, IReadOnlyList<InvitationInitialEntryRequest>? InitialEntries = null);
