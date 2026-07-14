using Notrelix.API.Contracts.Workspaces.Settings.Requests;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.Workspaces.Settings.Commands.UpdateWorkspaceSettings;

namespace Notrelix.API.Endpoints.Workspaces.Settings.Commands;

public static class UpdateWorkspaceSettingsEndpoint
{
    public static IEndpointRouteBuilder MapUpdateWorkspaceSettings(this IEndpointRouteBuilder group)
    {
        group.MapWorkspacePut("/", HandleAsync)
            .WithName("Workspaces.Settings.UpdateWorkspaceSettings")
            .WithTags("Workspaces.Settings")
            .WithSummary("Update workspace settings");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid workspaceId,
        UpdateWorkspaceSettingsRequest request,
        ISender sender)
    {
        var result = await sender.Send(new UpdateWorkspaceSettingsCommand(
            workspaceId,
            request.AllowPublicSharing,
            request.EnforceMfa,
            request.AllowGuestInvites,
            request.DefaultMemberRole,
            request.InvitationExpiryDays,
            request.ExpectedVersion));
        return result.ToNoContentResult();
    }
}
