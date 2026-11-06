using Notrelix.Application.Features.Governance.Public.Visibility;
using Notrelix.Application.Features.Identity.Public.Queries;
using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Application.Features.Workspaces.Public.Members;

namespace Notrelix.Application.Features.WorkManagement.Transfer;

/// <summary>
/// Who an import file may name in a Person column (UC-WM-B29): a person matches only by exact e-mail, and only if
/// the assignee picker of the board offers them (an active non-Guest member of the board's Workspace who can see
/// the board and is not a board Viewer). Matching reads e-mail, so it follows the one Workspaces e-mail rule:
/// only an active Owner or Admin importer may match; for anyone else the answer is null and Person columns stay locked.
/// </summary>
public sealed class ImportPeopleDirectory(
    IWorkspaceMemberFacts members, IIdentityUserFacts identity, IResourceVisibility visibility, IWorkManagementDbContext context)
{
    public async Task<IReadOnlyDictionary<string, Guid>?> ResolveAsync(
        Guid workspaceId, Guid boardId, Guid importerId, TabularData data, CancellationToken ct)
    {
        var importer = await members.FindAsync(workspaceId, importerId, ct);
        if (importer is not { IsActive: true } || importer.Role is not ("Owner" or "Admin")) return null;

        var cells = new HashSet<string>(StringComparer.Ordinal);
        foreach (var row in data.Rows)
            foreach (var cell in row.Cells)
                foreach (var part in (cell ?? "").Split([';', '|', '\n'], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
                    cells.Add(part.ToLowerInvariant());

        var colleagues = await members.ListActiveColleagueIdsAsync(workspaceId, ct);
        var emails = await identity.FindEmailsAsync(workspaceId, colleagues, ct);
        var named = emails.Where(e => cells.Contains(e.Value)).ToList();

        var viewers = await context.BoardMembers.AsNoTracking()
            .Where(m => m.BoardId == boardId && BoardRoles.Viewers.Contains(m.Role)).Select(m => m.UserId).ToListAsync(ct);
        var people = new Dictionary<string, Guid>(StringComparer.Ordinal);
        foreach (var (userId, email) in named)
        {
            if (viewers.Contains(userId)) continue;
            if (!(await visibility.VisibleBoardIdsAsync(workspaceId, userId, ct)).Contains(boardId)) continue;
            people[email] = userId;
        }

        return people;
    }
}
