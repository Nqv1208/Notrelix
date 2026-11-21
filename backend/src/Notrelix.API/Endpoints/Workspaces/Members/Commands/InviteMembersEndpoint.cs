using Notrelix.Application.Features.Workspaces.Invitations.Services;
using Notrelix.API.Contracts.Workspaces.Members.Requests;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.Workspaces.Invitations.Commands.InviteMembers;

namespace Notrelix.API.Endpoints.Workspaces.Members.Commands;

public static class InviteMembersEndpoint
{
    public static IEndpointRouteBuilder MapInviteMembers(this IEndpointRouteBuilder group)
    {
        group.MapWorkspacePost("/bulk", HandleAsync)
            .WithIdempotencyKey()
            .WithName("Workspaces.Members.InviteMembers")
            .WithTags("Workspaces.Members")
            .WithSummary("Invite many people at once")
            .WithDescription(
                "Owner or Admin, like a single invitation. Needs an Idempotency-Key: a retry with the same key returns the first result and sends no second email. " +
                "Send 1 to 50 entries (email and role Member or Guest); more answers 400. An address repeated in the list is invited once. " +
                "The batch is not all-or-nothing: the answer lists one outcome per address (sent, already-member, already-pending, invalid, refused). " +
                "While the workspace has guest invitations off, the Guest entries are refused one by one with the code workspace.guest_invites_off and the Member entries still go out. " +
                "An entry may name initial Team and Space entries, authorized as for a single invitation (ManageTeams, ManageSpaces; the whole request is refused with 403 when the caller lacks one); a Guest entry naming a Team is refused on its own with its rule code. " +
                "No seat is checked, as for a single invitation. An archived workspace refuses the whole request.")
            .Produces<MemberInvitationBatchResultDto>(StatusCodes.Status200OK, "application/json");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid workspaceId,
        InviteMembersRequest body,
        ISender sender)
    {
        var result = await sender.Send(new InviteMembersCommand(
            workspaceId,
            body.Entries?.Select(e => new MemberInvitationEntry(e.Email, e.Role, e.InitialEntries?.Select(i => new InvitationInitialEntryInput(i.Kind, i.TargetId, i.Role)).ToList())).ToList() ?? []));
        return result.ToApiResult();
    }
}
