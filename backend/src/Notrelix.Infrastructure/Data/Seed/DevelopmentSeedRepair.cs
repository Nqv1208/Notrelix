using System.Text.RegularExpressions;
using Notrelix.Domain.SharedKernel.Ordering;
using Notrelix.Domain.WorkManagement.Boards;
using Notrelix.Domain.WorkManagement.Fields;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Domain.Workspaces.Spaces;
using Notrelix.Domain.Workspaces.Workspaces;
using Notrelix.Infrastructure.Data.Authz;

namespace Notrelix.Infrastructure.Data.Seed;

/// <summary>
/// Development data only. Heals a development database that was seeded before the seed created
/// confirmed users, owner members and department spaces, without a reseed. It touches the seed
/// accounts only (the seed's own email list, the "workspace-N" workspaces of the seed account and
/// the seed's board names); every other user, workspace and board is left alone. It never deletes
/// and a second run changes nothing. The caller must only invoke it in Development.
/// </summary>
internal static partial class DevelopmentSeedRepair
{
    private const string SeedAccountSlug = "notrelix";
    private static readonly DateTimeOffset SeedEpoch = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [GeneratedRegex(@"^(admin|demo|member)@notrelix\.com$|^user\d+@notrelix\.io$", RegexOptions.IgnoreCase)]
    private static partial Regex SeedEmailPattern();

    [GeneratedRegex(@"^workspace-\d+$")]
    private static partial Regex SeedWorkspaceSlugPattern();

    public static bool IsSeedEmail(string email) => SeedEmailPattern().IsMatch(email);

    public static async Task<SeedRepairResult> RunAsync(
        ApplicationDbContext context, DateTimeOffset now, CancellationToken ct = default)
    {
        var emailsConfirmed = await ConfirmSeedEmailsAsync(context, now, ct);
        var ownersRepaired = await EnsureSeedOwnersAsync(context, now, ct);
        var (spacesCreated, boardsPlaced) = await EnsureSeedSpacesAsync(context, now, ct);
        var fieldsRenamed = await RenameDuplicateSeedFieldsAsync(context, now, ct);
        var dueDatesMarked = await MarkSeedDueDateFieldsSystemAsync(context, ct);
        return new SeedRepairResult(
            emailsConfirmed, ownersRepaired, spacesCreated, boardsPlaced, fieldsRenamed, dueDatesMarked);
    }

    private static async Task<int> ConfirmSeedEmailsAsync(
        ApplicationDbContext context, DateTimeOffset now, CancellationToken ct)
    {
        var unconfirmed = await context.Users.Where(u => !u.EmailConfirmed).ToListAsync(ct);
        var changed = 0;
        foreach (var user in unconfirmed.Where(u => IsSeedEmail(u.Email.Value)))
        {
            user.ConfirmEmail(null, now);
            changed++;
        }

        if (changed > 0)
        {
            await SaveAsync(context, ct);
        }

        return changed;
    }

    private static async Task<List<Workspace>?> LoadSeedWorkspacesAsync(
        ApplicationDbContext context, CancellationToken ct)
    {
        var account = await context.Accounts.FirstOrDefaultAsync(a => a.Slug == SeedAccountSlug, ct);
        if (account is null)
        {
            return null;
        }

        var seedUserIds = (await context.Users.ToListAsync(ct))
            .Where(u => IsSeedEmail(u.Email.Value))
            .Select(u => u.Id)
            .ToHashSet();

        return (await context.Workspaces.Where(w => w.AccountId == account.Id).ToListAsync(ct))
            .Where(w => SeedWorkspaceSlugPattern().IsMatch(w.Slug)
                        && w.CreatedBy is { } creator && seedUserIds.Contains(creator))
            .ToList();
    }

    private static async Task<int> EnsureSeedOwnersAsync(
        ApplicationDbContext context, DateTimeOffset now, CancellationToken ct)
    {
        if (await LoadSeedWorkspacesAsync(context, ct) is not { } workspaces)
        {
            return 0;
        }

        var repaired = 0;
        foreach (var workspace in workspaces)
        {
            var ownerId = workspace.CreatedBy!.Value;
            var member = await context.WorkspaceMembers
                .FirstOrDefaultAsync(m => m.WorkspaceId == workspace.Id && m.UserId == ownerId, ct);

            if (member is null)
            {
                context.WorkspaceMembers.Add(WorkspaceMember.Create(
                    workspace.AccountId, workspace.Id, ownerId, WorkspaceRole.Owner, ownerId, now));
            }
            else if (member.Status == WorkspaceMemberStatus.Active && member.Role != WorkspaceRole.Owner)
            {
                member.PromoteToOwner(ownerId, now);
            }
            else
            {
                // Already the Owner, or a removed/suspended membership somebody chose; leave it.
                continue;
            }

            await EnsureAccessGrantAsync(context, workspace, ownerId, now, ct);
            await EnsureBoardOwnerRolesAsync(context, workspace, ownerId, now, ct);
            repaired++;
        }

        if (repaired > 0)
        {
            await SaveAsync(context, ct);
        }

        return repaired;
    }

    private static async Task EnsureAccessGrantAsync(
        ApplicationDbContext context, Workspace workspace, Guid ownerId, DateTimeOffset now, CancellationToken ct)
    {
        var roleCodes = AccessGrantProjectionMapping.RoleCodes(WorkspaceRole.Owner);
        var grant = await context.AccessGrants.FirstOrDefaultAsync(
            g => g.WorkspaceId == workspace.Id && g.UserId == ownerId && g.SourceContext == "Workspace", ct);

        if (grant is null)
        {
            context.AccessGrants.Add(new AccessGrant(
                workspace.AccountId, workspace.Id, ownerId, "Workspace", "Active", roleCodes, [],
                false, AccessGrantProjectionMapping.IsWorkspaceAdmin(WorkspaceRole.Owner), now));
            return;
        }

        grant.Activate(
            "Active", roleCodes, grant.PermissionCodes, grant.IsAccountAdmin,
            AccessGrantProjectionMapping.IsWorkspaceAdmin(WorkspaceRole.Owner), now,
            grant.SourceEventId, grant.SourceVersion);
    }

    private static async Task EnsureBoardOwnerRolesAsync(
        ApplicationDbContext context, Workspace workspace, Guid ownerId, DateTimeOffset now, CancellationToken ct)
    {
        var boardIds = await context.Boards
            .Where(b => b.WorkspaceId == workspace.Id)
            .Select(b => b.Id)
            .ToListAsync(ct);
        var byBoard = (await context.BoardMembers
                .Where(m => m.UserId == ownerId && boardIds.Contains(m.BoardId))
                .ToListAsync(ct))
            .ToDictionary(m => m.BoardId);

        foreach (var boardId in boardIds)
        {
            if (!byBoard.TryGetValue(boardId, out var boardMember))
            {
                context.BoardMembers.Add(BoardMember.Create(boardId, ownerId, BoardRole.Owner, now));
            }
            else if (boardMember.Role is BoardRole.Member or BoardRole.Observer or BoardRole.Guest)
            {
                boardMember.UpdateRole(BoardRole.Owner);
            }
        }
    }

    private static async Task<(int SpacesCreated, int BoardsPlaced)> EnsureSeedSpacesAsync(
        ApplicationDbContext context, DateTimeOffset now, CancellationToken ct)
    {
        if (await LoadSeedWorkspacesAsync(context, ct) is not { } workspaces)
        {
            return (0, 0);
        }

        var spacesCreated = 0;
        var boardsPlaced = 0;
        foreach (var workspace in workspaces)
        {
            var unplaced = (await context.Boards
                    .Where(b => b.WorkspaceId == workspace.Id && b.SpaceId == null && !b.IsArchived)
                    .ToListAsync(ct))
                .Select(b => (Board: b, Department: InitDb.DepartmentForBoardName(b.Title)))
                .Where(x => x.Department is not null)
                .ToList();
            if (unplaced.Count == 0)
            {
                continue;
            }

            var ownerId = workspace.CreatedBy!.Value;
            var departments = await context.Spaces
                .Where(s => s.WorkspaceId == workspace.Id && s.SpaceType == SpaceType.Department)
                .ToDictionaryAsync(s => s.Name, ct);

            for (var d = 0; d < InitDb.DepartmentNames.Length; d++)
            {
                var name = InitDb.DepartmentNames[d];
                if (departments.ContainsKey(name))
                {
                    continue;
                }

                var department = Space.Create(
                    workspace.AccountId, workspace.Id, name, SpaceVisibility.Workspace, ownerId,
                    SeedEpoch, SpaceType.Department,
                    placement: SpacePlacement.Root(FractionalIndex.Create($"a{d}"), departments.Keys.ToList()));
                context.Spaces.Add(department);
                departments[name] = department;
                spacesCreated++;
            }

            foreach (var (board, department) in unplaced)
            {
                board.PlaceInSpace(departments[department!].Id, ownerId, now);
                boardsPlaced++;
            }
        }

        if (spacesCreated > 0 || boardsPlaced > 0)
        {
            await SaveAsync(context, ct);
        }

        return (spacesCreated, boardsPlaced);
    }

    // Names the seed gives the second, third... Select field of a board it once named "Priority" three times.
    private static readonly string[] DuplicateSelectFieldNames = ["Severity", "Urgency"];

    /// <summary>
    /// An earlier seed named every extra field of a board "Priority", so a board showed three fields with the
    /// same name. Renames the later duplicates of a seed board's field name (never the first, never a system
    /// field); nothing is deleted and a second run finds no duplicate left.
    /// </summary>
    private static async Task<int> RenameDuplicateSeedFieldsAsync(
        ApplicationDbContext context, DateTimeOffset now, CancellationToken ct)
    {
        if (await LoadSeedWorkspacesAsync(context, ct) is not { } workspaces)
        {
            return 0;
        }

        var renamed = 0;
        foreach (var workspace in workspaces)
        {
            var ownerId = workspace.CreatedBy!.Value;
            var seedBoardIds = (await context.Boards.Where(b => b.WorkspaceId == workspace.Id).ToListAsync(ct))
                .Where(b => InitDb.DepartmentForBoardName(b.Title) is not null)
                .Select(b => b.Id)
                .ToList();
            if (seedBoardIds.Count == 0)
            {
                continue;
            }

            var fields = await context.BoardFields
                .Where(f => seedBoardIds.Contains(f.BoardId) && !f.IsSystem)
                .ToListAsync(ct);
            foreach (var board in fields.GroupBy(f => f.BoardId))
            {
                var taken = board.Select(f => f.Name.ToLowerInvariant()).ToHashSet();
                foreach (var sameName in board.GroupBy(f => f.Name.ToLowerInvariant()).Where(g => g.Count() > 1))
                {
                    var later = sameName
                        .OrderBy(f => f.Position.Value, StringComparer.Ordinal)
                        .ThenBy(f => f.Id)
                        .Skip(1)
                        .ToList();
                    for (var i = 0; i < later.Count; i++)
                    {
                        var candidate = i < DuplicateSelectFieldNames.Length
                            ? DuplicateSelectFieldNames[i]
                            : $"{later[i].Name} {i + 2}";
                        while (!taken.Add(candidate.ToLowerInvariant()))
                        {
                            candidate += " *";
                        }

                        later[i].Rename(candidate, ownerId, now);
                        renamed++;
                    }
                }
            }
        }

        if (renamed > 0)
        {
            await SaveAsync(context, ct);
        }

        return renamed;
    }

    /// <summary>
    /// A board made through the product has the system Due Date field, the one that writes the Item's own due
    /// date; the seed once made it an ordinary date field, so editing it never changed the Item's due date.
    /// </summary>
    private static async Task<int> MarkSeedDueDateFieldsSystemAsync(ApplicationDbContext context, CancellationToken ct)
    {
        if (await LoadSeedWorkspacesAsync(context, ct) is not { } workspaces)
        {
            return 0;
        }

        var workspaceIds = workspaces.Select(w => w.Id).ToList();
        var seedBoardIds = (await context.Boards.Where(b => workspaceIds.Contains(b.WorkspaceId)).ToListAsync(ct))
            .Where(b => InitDb.DepartmentForBoardName(b.Title) is not null)
            .Select(b => b.Id)
            .ToList();
        var fields = await context.BoardFields
            .Where(f => seedBoardIds.Contains(f.BoardId) && !f.IsSystem && f.Name == "Due Date" && f.Type == FieldType.Date)
            .ToListAsync(ct);
        foreach (var field in fields)
        {
            context.Entry(field).Property(f => f.IsSystem).CurrentValue = true;
        }

        if (fields.Count > 0)
        {
            await SaveAsync(context, ct);
        }

        return fields.Count;
    }

    private static async Task SaveAsync(ApplicationDbContext context, CancellationToken ct)
    {
        // Seed repair applies no product use case, so the domain events its changes raise are dropped,
        // exactly as the seed does.
        foreach (var entity in context.ChangeTracker.Entries<Entity>().Select(e => e.Entity))
        {
            if (entity is IHasDomainEvents hasDomainEvents)
            {
                hasDomainEvents.ClearDomainEvents();
            }
        }

        await context.SaveChangesAsync(ct);
    }
}

internal sealed record SeedRepairResult(
    int EmailsConfirmed, int OwnersRepaired, int SpacesCreated, int BoardsPlaced, int FieldsRenamed = 0, int DueDatesMarked = 0)
{
    public bool ChangedAnything =>
        EmailsConfirmed + OwnersRepaired + SpacesCreated + BoardsPlaced + FieldsRenamed + DueDatesMarked > 0;
}
