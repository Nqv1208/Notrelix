using Notrelix.Application.Features.Workspaces.Abstractions;
using Notrelix.Application.Features.Workspaces.DTOs;

namespace Notrelix.Application.Features.Workspaces.Members.Services;

/// <summary>
/// Shared body of the two job-title writes (own title, another member's title). Authorization
/// and the expected-version guard are the pipeline's; this only loads the target member of the
/// Workspace and applies the Domain change.
/// </summary>
internal static class WorkspaceMemberJobTitleWriter
{
    public static async Task<WorkspaceMemberProfileDto> ApplyAsync(
        IWorkspaceDbContext context,
        Guid workspaceId,
        Guid targetUserId,
        string? jobTitle,
        Guid actorUserId,
        DateTimeOffset now,
        CancellationToken ct)
    {
        var member = await context.WorkspaceMembers
            .FirstOrDefaultAsync(m => m.WorkspaceId == workspaceId && m.UserId == targetUserId && m.Status != WorkspaceMemberStatus.Removed, ct)
            ?? throw new NotFoundException("WorkspaceMember", targetUserId);

        member.ChangeJobTitle(jobTitle, actorUserId, now);
        return new WorkspaceMemberProfileDto(member.UserId, member.JobTitle, member.Version);
    }
}
