namespace Notrelix.API.Contracts.Workspaces.Members.Requests;

/// <param name="Entries">One to 50 addresses with their role; an address repeated in the list is invited once.</param>
public record InviteMembersRequest(IReadOnlyList<InviteMembersEntryRequest> Entries);

/// <param name="Role"><c>Member</c> or <c>Guest</c>, as for a single invitation.</param>
public record InviteMembersEntryRequest(string Email, string Role);
