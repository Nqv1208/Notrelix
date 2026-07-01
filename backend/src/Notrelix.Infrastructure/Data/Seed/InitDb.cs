using Microsoft.EntityFrameworkCore;
using Notrelix.Application.Common.Abstractions;
using Notrelix.Domain.Collaboration.Comments;
using Notrelix.Domain.Documents.Blocks;
using Notrelix.Domain.Documents.Pages;
using Notrelix.Domain.Identity.Profiles;
using Notrelix.Domain.Identity.Sessions;
using Notrelix.Domain.Identity.Users;
using Notrelix.Domain.Notifications.NotificationItems;
using Notrelix.Domain.Notifications.NotificationRecipients;
using Notrelix.Domain.SharedKernel;
using Notrelix.Domain.WorkManagement.Boards;
using Notrelix.Domain.WorkManagement.BoardGroups;
using Notrelix.Domain.WorkManagement.Fields;
using Notrelix.Domain.WorkManagement.Items;
using Notrelix.Domain.WorkManagement.Labels;
using Notrelix.Domain.WorkManagement.Views;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Domain.Workspaces.Workspaces;
using Notrelix.Infrastructure.Data.Authz;

namespace Notrelix.Infrastructure.Data.Seed;

internal static class InitDb
{
    private static readonly DateTimeOffset Epoch = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private const string DefaultPassword = "Notrelix@123";
    private static readonly Random Rng = new(42);

    private static readonly (string Name, string Color)[] StatusOptions = [
        ("Backlog",     "#9E9E9E"),
        ("To Do",       "#1E88E5"),
        ("In Progress", "#F9A825"),
        ("Done",        "#43A047"),
    ];

    private static readonly (string Name, string Color)[] PriorityOptions = [
        ("Low",      "#616161"),
        ("Medium",   "#F9A825"),
        ("High",     "#EF6C00"),
        ("Critical", "#E53935"),
    ];

    private static readonly (string Name, string Color)[] LabelTemplates = [
        ("Bug",        "#E53935"),
        ("Feature",    "#1E88E5"),
        ("Improvement","#43A047"),
    ];

    public static async Task<SeedResult> SeedAsync(
        ApplicationDbContext context,
        SeedTargets targets,
        IPasswordHasher passwordHasher,
        CancellationToken ct = default)
    {
        var result = new SeedResult();

        var passwordHash = passwordHasher.HashPassword(DefaultPassword);

        var users = await CreateUsersAsync(context, targets, passwordHash, ct);
        result = result with { UsersCreated = users.Count };

        if (users.Count == 0)
        {
            result = result with { Skipped = true };
            return result;
        }

        var workspaces = await CreateWorkspacesAsync(context, targets, users, ct);
        result = result with { WorkspacesCreated = workspaces.Count };

        await SeedAuthzGrantsAsync(context, ct);

        var boardData = await CreateBoardStructuresAsync(context, targets, workspaces, users, ct);
        result = result with { BoardsCreated = boardData.Count };

        var itemsCreated = await CreateBoardItemsAsync(context, targets, boardData, users, ct);
        result = result with { BoardItemsCreated = itemsCreated };

        var pagesCreated = await CreatePagesAsync(context, targets, workspaces, users, ct);
        result = result with { PagesCreated = pagesCreated };

        var commentsCreated = await CreateCommentsAsync(context, boardData, users, ct);
        result = result with { CommentsCreated = commentsCreated };

        var notificationsCreated = await CreateNotificationsAsync(context, targets, users, ct);
        result = result with { NotificationsCreated = notificationsCreated };

        return result;
    }

    private static async Task ClearAndSaveAsync(ApplicationDbContext context, CancellationToken ct)
    {
        ClearDomainEvents(context);
        await context.SaveChangesAsync(ct);
    }

    private static void ClearDomainEvents(ApplicationDbContext context)
    {
        foreach (var entity in context.ChangeTracker.Entries<Entity>().Select(e => e.Entity))
            entity.ClearDomainEvents();
    }

    private static async Task<List<User>> CreateUsersAsync(
        ApplicationDbContext context, SeedTargets targets, string passwordHash, CancellationToken ct)
    {
        if (await context.Users.AnyAsync(ct))
        {
            return [];
        }

        var defaultEmails = new[] { "admin@notrelix.com", "demo@notrelix.com", "member@notrelix.com" };
        var users = new List<User>(targets.UserCount);

        for (int i = 0; i < targets.UserCount; i++)
        {
            var email = i < defaultEmails.Length
                ? defaultEmails[i]
                : $"user{i + 1}@notrelix.io";
            var name = i < defaultEmails.Length
                ? defaultEmails[i].Split('@')[0]
                : $"User {i + 1}";

            var user = User.Create(email, name, passwordHash, Epoch.AddDays(i));
            users.Add(user);

            var profile = UserProfile.Create(user.Id, Epoch.AddDays(i));
            context.UserProfiles.Add(profile);

            var tokenHash = RefreshTokenHash.Create($"seed-refresh-token-{i}");
            var createdAt = Epoch.AddDays(i);
            var session = UserSession.Create(
                user.Id, tokenHash, createdAt.AddDays(30), createdAt,
                "127.0.0.1", "SeedAgent/1.0");
            context.Sessions.Add(session);
        }

        context.Users.AddRange(users);
        await ClearAndSaveAsync(context, ct);
        return users;
    }

    private static async Task<List<Workspace>> CreateWorkspacesAsync(
        ApplicationDbContext context, SeedTargets targets, List<User> users, CancellationToken ct)
    {
        if (await context.Workspaces.AnyAsync(ct))
        {
            return [];
        }

        var workspaces = new List<Workspace>(targets.WorkspaceCount);
        var defaultUserIds = users.Take(3).Select(u => u.Id).ToHashSet();

        for (int i = 0; i < targets.WorkspaceCount; i++)
        {
            var owner = users[i % users.Count];
            var ws = Workspace.Create(
                owner.Id,
                $"Workspace {i + 1}",
                $"workspace-{i + 1}",
                Epoch.AddDays(i),
                isPersonal: i == 0);
            workspaces.Add(ws);

            var addedUserIds = new HashSet<Guid> { owner.Id };

            foreach (var uid in defaultUserIds)
            {
                if (!addedUserIds.Add(uid)) continue;
                var member = WorkspaceMember.Create(
                    ws.Id, uid, WorkspaceRole.Member, owner.Id, Epoch.AddDays(i));
                context.WorkspaceMembers.Add(member);
            }

            var memberCount = Math.Min(3 + i % 3, users.Count / 2);
            for (int m = 0; m < memberCount; m++)
            {
                var mid = users[(i + m + 1) % users.Count].Id;
                if (!addedUserIds.Add(mid)) continue;
                var role = m == 0 ? WorkspaceRole.Admin
                    : m == 1 ? WorkspaceRole.Member
                    : WorkspaceRole.Guest;
                var member = WorkspaceMember.Create(
                    ws.Id, mid, role, owner.Id, Epoch.AddDays(i));
                context.WorkspaceMembers.Add(member);
            }
        }

        context.Workspaces.AddRange(workspaces);
        await ClearAndSaveAsync(context, ct);
        return workspaces;
    }

    private static async Task SeedAuthzGrantsAsync(ApplicationDbContext context, CancellationToken ct)
    {
        if (await context.Set<WorkspaceAccessGrant>().AnyAsync(ct)) return;

        var members = await context.WorkspaceMembers
            .Where(m => m.Status == WorkspaceMemberStatus.Active)
            .ToListAsync(ct);

        var grants = new List<WorkspaceAccessGrant>(members.Count);
        foreach (var member in members)
        {
            var isAdmin = member.Role == WorkspaceRole.Owner || member.Role == WorkspaceRole.Admin;
            var isOwner = member.Role == WorkspaceRole.Owner;
            grants.Add(new WorkspaceAccessGrant(
                member.WorkspaceId,
                member.UserId,
                "Workspace",
                "Active",
                [member.Role.ToString()],
                [],
                isAdmin,
                member.CreatedAt,
                null,
                null,
                null));
        }

        context.Set<WorkspaceAccessGrant>().AddRange(grants);
        await ClearAndSaveAsync(context, ct);
    }

    private sealed record BoardStructure(
        Board Board,
        List<BoardField> Fields,
        List<BoardGroup> Groups,
        List<BoardView> Views,
        List<Label> Labels);

    private static async Task<List<BoardStructure>> CreateBoardStructuresAsync(
        ApplicationDbContext context, SeedTargets targets, List<Workspace> workspaces, List<User> users, CancellationToken ct)
    {
        if (await context.Boards.AnyAsync(ct))
        {
            return [];
        }

        var structures = new List<BoardStructure>(targets.BoardCount);
        var statusSettings = FieldSettings.Create(JsonValue.Create("""{"transitions":[]}"""));
        var emptySettings = FieldSettings.Create(JsonValue.EmptyObject());

        var boardNames = new[] {
            "Product Roadmap", "Engineering Tasks", "Marketing Campaign",
            "Design Sprints", "Bug Tracker", "Content Calendar",
            "Sales Pipeline", "HR Onboarding", "Customer Support",
            "Feature Requests"
        };

        int boardIndex = 0;
        foreach (var ws in workspaces)
        {
            int boardsForWorkspace = targets.BoardCount / workspaces.Count;
            int remaining = targets.BoardCount % workspaces.Count;
            var count = boardsForWorkspace + (boardIndex < remaining ? 1 : 0);

            for (int b = 0; b < count && boardIndex < targets.BoardCount; b++, boardIndex++)
            {
                var creator = users[boardIndex % users.Count];
                var boardName = boardNames[boardIndex % boardNames.Length];
                var wsSuffix = ws.Id.ToString()[..4];

                var board = Board.Create(
                    ws.Id, creator.Id, $"{boardName} ({wsSuffix})", null,
                    Epoch.AddDays(boardIndex), BoardVisibility.Workspace);
                context.Boards.Add(board);

                var fields = new List<BoardField>();

                for (int f = 0; f < targets.BoardFieldCount / targets.BoardCount; f++)
                {
                    var (fName, fType, fIsSystem, fSettings, fOptions) = f switch
                    {
                        0 => ("Title", FieldType.Text, true, emptySettings, Array.Empty<(string, string)>()),
                        1 => ("Status", FieldType.Status, true, statusSettings, StatusOptions),
                        2 => ("Assignee", FieldType.Person, true, emptySettings, Array.Empty<(string, string)>()),
                        3 => ("Due Date", FieldType.Date, false, emptySettings, Array.Empty<(string, string)>()),
                        4 => ("Description", FieldType.LongText, false, emptySettings, Array.Empty<(string, string)>()),
                        _ => ("Priority", FieldType.Select, false, emptySettings, PriorityOptions),
                    };

                    var field = BoardField.Create(
                        ws.Id, board.Id, fName, fType, fSettings,
                        FractionalIndex.Create($"a{f}"), creator.Id, Epoch.AddDays(boardIndex),
                        isSystem: fIsSystem);
                    context.BoardFields.Add(field);
                    fields.Add(field);

                    if (fOptions.Length > 0)
                    {
                        for (int o = 0; o < fOptions.Length; o++)
                        {
                            var (optName, optColor) = fOptions[o];
                            var option = FieldOption.Create(
                                field.Id, optName, Color.Create(optColor),
                                FractionalIndex.Create($"a{o}"));
                            context.FieldOptions.Add(option);
                        }
                    }
                }

                var groups = new List<BoardGroup>();
                var groupNames = new[] { "Backlog", "To Do", "In Progress", "Done" };
                for (int g = 0; g < groupNames.Length; g++)
                {
                    var group = BoardGroup.Create(
                        ws.Id, board.Id, groupNames[g], Color.Create(StatusOptions[g].Color),
                        FractionalIndex.Create($"a{g}"), creator.Id, Epoch.AddDays(boardIndex));
                    context.BoardGroups.Add(group);
                    groups.Add(group);
                }

                var labels = new List<Label>();
                for (int l = 0; l < LabelTemplates.Length; l++)
                {
                    var (lName, lColor) = LabelTemplates[l];
                    var label = Label.Create(
                        ws.Id, board.Id, lName, LabelColor.Create(lColor),
                        creator.Id, Epoch.AddDays(boardIndex));
                    context.Labels.Add(label);
                    labels.Add(label);
                }

                var views = new List<BoardView>();
                var tableView = BoardView.Create(
                    ws.Id, board.Id, "Table", ViewType.Table,
                    BoardViewConfig.Create(JsonValue.EmptyObject()), creator.Id,
                    Epoch.AddDays(boardIndex), isDefault: true);
                context.BoardViews.Add(tableView);
                views.Add(tableView);

                var kanbanView = BoardView.Create(
                    ws.Id, board.Id, "Kanban", ViewType.Kanban,
                    BoardViewConfig.Create(JsonValue.EmptyObject()), creator.Id,
                    Epoch.AddDays(boardIndex));
                context.BoardViews.Add(kanbanView);
                views.Add(kanbanView);

                structures.Add(new BoardStructure(board, fields, groups, views, labels));
            }
        }

        await ClearAndSaveAsync(context, ct);
        return structures;
    }

    private static async Task<int> CreateBoardItemsAsync(
        ApplicationDbContext context, SeedTargets targets,
        List<BoardStructure> structures, List<User> users, CancellationToken ct)
    {
        if (await context.BoardItems.AnyAsync(ct) || structures.Count == 0)
        {
            return 0;
        }

        var itemsPerGroup = targets.BoardItemCount / Math.Max(1, targets.BoardGroupCount);
        var remainingItems = targets.BoardItemCount % Math.Max(1, targets.BoardGroupCount);
        int itemIndex = 0;

        var statusOptionsByBoard = new Dictionary<Guid, List<FieldOption>>();
        var priorityOptionsByBoard = new Dictionary<Guid, List<FieldOption>>();
        foreach (var bs in structures)
        {
            var statusField = bs.Fields.FirstOrDefault(f => f.Type == FieldType.Status);
            var priorityField = bs.Fields.FirstOrDefault(f => f.Type == FieldType.Select);
            if (statusField != null)
                statusOptionsByBoard[bs.Board.Id] = await context.FieldOptions
                    .Where(o => o.FieldId == statusField.Id).ToListAsync(ct);
            if (priorityField != null)
                priorityOptionsByBoard[bs.Board.Id] = await context.FieldOptions
                    .Where(o => o.FieldId == priorityField.Id).ToListAsync(ct);
        }

        foreach (var bs in structures)
        {
            var boardUsers = users.OrderBy(_ => Rng.Next()).Take(3).ToList();

            foreach (var group in bs.Groups)
            {
                var count = itemsPerGroup + (remainingItems > 0 ? 1 : 0);
                if (remainingItems > 0) remainingItems--;

                for (int i = 0; i < count && itemIndex < targets.BoardItemCount; i++, itemIndex++)
                {
                    var creator = boardUsers[i % boardUsers.Count];
                    var itemName = group.Title switch
                    {
                        "Backlog" => $"[Idea] Task {itemIndex + 1}",
                        "To Do" => $"Task {itemIndex + 1}",
                        "In Progress" => $"[WIP] Task {itemIndex + 1}",
                        "Done" => $"[Done] Task {itemIndex + 1}",
                        _ => $"Task {itemIndex + 1}"
                    };

                    var item = BoardItem.Create(
                        bs.Board.WorkspaceId, bs.Board.Id, group.Id,
                        itemName, FractionalIndex.Create($"a{i}"),
                        creator.Id, Epoch.AddDays(itemIndex));

                    context.BoardItems.Add(item);

                    foreach (var field in bs.Fields)
                    {
                        var value = field.Type switch
                        {
                            FieldType.Text => FieldValue.Create(
                                JsonValue.Create($"\"{itemName}\"")),
                            FieldType.Status => CreateOptionFieldValue(
                                field.Id, statusOptionsByBoard, bs.Board.Id),
                            FieldType.Person => FieldValue.Create(
                                JsonValue.Create($"\"{boardUsers[i % boardUsers.Count].Id}\"")),
                            FieldType.Date => FieldValue.Create(
                                JsonValue.Create($"\"2026-{(itemIndex % 12) + 1:D2}-{(itemIndex % 28) + 1:D2}\"")),
                            FieldType.Select => CreateOptionFieldValue(
                                field.Id, priorityOptionsByBoard, bs.Board.Id),
                            FieldType.LongText => FieldValue.Create(
                                JsonValue.Create($"\"Description for {itemName}\"")),
                            _ => FieldValue.Create(JsonValue.Null()),
                        };

                        var itemValue = BoardItemValue.Create(item.Id, field.Id, value);
                        context.BoardItemValues.Add(itemValue);
                    }

                    if (i % 2 == 0 && bs.Labels.Count > 0)
                    {
                        var label = bs.Labels[i % bs.Labels.Count];
                        var itemLabel = BoardItemLabel.Create(
                            bs.Board.WorkspaceId, bs.Board.Id, item.Id, label.Id,
                            creator.Id, Epoch.AddDays(itemIndex));
                        context.BoardItemLabels.Add(itemLabel);
                    }

                    if (i % 3 == 0)
                    {
                        var assignee = boardUsers[(i + 1) % boardUsers.Count];
                        var itemMember = BoardItemMember.Create(
                            bs.Board.WorkspaceId, bs.Board.Id, item.Id, assignee.Id,
                            creator.Id, Epoch.AddDays(itemIndex));
                        context.BoardItemMembers.Add(itemMember);
                    }
                }
            }
        }

        await ClearAndSaveAsync(context, ct);
        return itemIndex;
    }

    private static FieldValue CreateOptionFieldValue(
        Guid fieldId, Dictionary<Guid, List<FieldOption>> optionsByBoard, Guid boardId)
    {
        if (optionsByBoard.TryGetValue(boardId, out var options) && options.Count > 0)
        {
            var option = options[Rng.Next(options.Count)];
            return FieldValue.Create(JsonValue.Create($"\"{option.Id}\""));
        }
        return FieldValue.Create(JsonValue.Null());
    }

    private static async Task<int> CreatePagesAsync(
        ApplicationDbContext context, SeedTargets targets,
        List<Workspace> workspaces, List<User> users, CancellationToken ct)
    {
        if (await context.Pages.AnyAsync(ct) || workspaces.Count == 0)
        {
            return 0;
        }

        var pagesPerWorkspace = targets.PageCount / workspaces.Count;
        var remaining = targets.PageCount % workspaces.Count;
        int pageIndex = 0;

        foreach (var ws in workspaces)
        {
            var count = pagesPerWorkspace + (remaining > 0 ? 1 : 0);
            if (remaining > 0) remaining--;

            for (int i = 0; i < count && pageIndex < targets.PageCount; i++, pageIndex++)
            {
                var creator = users[pageIndex % users.Count];

                var page = Page.Create(
                    ws.Id,
                    $"Page {pageIndex + 1}",
                    creator.Id,
                    Epoch.AddDays(pageIndex));
                context.Pages.Add(page);

                var blocksPerPage = targets.BlockCount / targets.PageCount;
                for (int b = 0; b < blocksPerPage; b++)
                {
                    var block = Block.Create(
                        ws.Id, page.Id, BlockType.Text,
                        BlockContent.Create(JsonValue.Create($"\"Content block {b + 1} for page {pageIndex + 1}\"")),
                        FractionalIndex.Create($"a{b}"),
                        creator.Id, Epoch.AddDays(pageIndex).AddMinutes(b));
                    context.Blocks.Add(block);
                }
            }
        }

        await ClearAndSaveAsync(context, ct);
        return pageIndex;
    }

    private static async Task<int> CreateCommentsAsync(
        ApplicationDbContext context, List<BoardStructure> structures,
        List<User> users, CancellationToken ct)
    {
        if (await context.Comments.AnyAsync(ct) || structures.Count == 0)
        {
            return 0;
        }

        var items = await context.BoardItems
            .OrderBy(i => i.CreatedAt)
            .Take(400)
            .ToListAsync(ct);

        var commentTexts = new[] {
            "\"Looks good to me!\"",
            "\"Let me review this in detail.\"",
            "\"Can we discuss this in the next standup?\"",
            "\"I've updated the description with more details.\"",
            "\"This needs more clarification.\"",
            "\"Great progress on this task!\"",
            "\"Blocked by the API changes.\"",
            "\"Added some notes in the design doc.\"",
            "\"Please review the latest changes.\"",
            "\"Ready for QA.\"",
            "\"Need input from the design team.\"",
            "\"Let's prioritize this for the next sprint.\"",
        };

        foreach (var item in items)
        {
            var author = users[Math.Abs(item.GetHashCode()) % users.Count];
            var text = commentTexts[Math.Abs(item.GetHashCode() * 7) % commentTexts.Length];

            var comment = Comment.Create(
                item.WorkspaceId,
                ResourceRef.Create(ResourceType.BoardItem, item.Id),
                text, author.Id, Epoch.AddDays(1));
            context.Comments.Add(comment);
        }

        await ClearAndSaveAsync(context, ct);
        return items.Count;
    }

    private static async Task<int> CreateNotificationsAsync(
        ApplicationDbContext context, SeedTargets targets,
        List<User> users, CancellationToken ct)
    {
        if (await context.NotificationItems.AnyAsync(ct) || users.Count == 0)
        {
            return 0;
        }

        var notificationsPerUser = targets.NotificationCount / users.Count;
        var remaining = targets.NotificationCount % users.Count;

        var workspaceIds = await context.Workspaces.Select(w => w.Id).ToListAsync(ct);

        foreach (var user in users)
        {
            var count = notificationsPerUser + (remaining > 0 ? 1 : 0);
            if (remaining > 0) remaining--;

            for (int i = 0; i < count; i++)
            {
                var wsId = workspaceIds[i % workspaceIds.Count];
                var createdAt = Epoch.AddDays(i);

                var item = NotificationItem.Create(
                    wsId,
                    "System",
                    "General",
                    NotificationSeverity.Info,
                    $"Notification {i + 1} for {user.Email.Value}",
                    createdAt,
                    body: $"This is a general notification body.",
                    actorUserId: user.Id);
                context.NotificationItems.Add(item);

                var recipient = NotificationRecipient.Create(
                    item.Id, wsId, user.Id, createdAt,
                    recipientEmail: user.Email.Value,
                    recipientName: user.Name);
                context.NotificationRecipients.Add(recipient);
            }
        }

        await ClearAndSaveAsync(context, ct);
        return users.Count * notificationsPerUser;
    }
}
