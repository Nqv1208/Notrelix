using Microsoft.EntityFrameworkCore;
using Notrelix.Application.Common.Abstractions;
using Notrelix.Domain.Collaboration.Comments;
using Notrelix.Domain.Collaboration.Notifications;
using Notrelix.Domain.Common;
using Notrelix.Domain.Documents.Blocks;
using Notrelix.Domain.Documents.Pages;
using Notrelix.Domain.Identity.Profiles;
using Notrelix.Domain.Identity.Sessions;
using Notrelix.Domain.Identity.Users;
using Notrelix.Domain.SharedKernel;
using Notrelix.Domain.WorkManagement.Boards;
using Notrelix.Domain.WorkManagement.BoardGroups;
using Notrelix.Domain.WorkManagement.Fields;
using Notrelix.Domain.WorkManagement.Items;
using Notrelix.Domain.WorkManagement.Labels;
using Notrelix.Domain.WorkManagement.Views;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Domain.Workspaces.Workspaces;

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

    public static async Task SeedAsync(
        ApplicationDbContext context,
        SeedTargets targets,
        IPasswordHasher passwordHasher,
        CancellationToken ct = default)
    {
        if (await context.Users.AnyAsync(ct))
            return;

        var passwordHash = passwordHasher.HashPassword(DefaultPassword);

        var users = await CreateUsersAsync(context, targets, passwordHash, ct);
        var workspaces = await CreateWorkspacesAsync(context, targets, users, ct);
        var boardData = await CreateBoardStructuresAsync(context, targets, workspaces, users, ct);
        await CreateBoardItemsAsync(context, targets, boardData, users, ct);
        await CreatePagesAsync(context, targets, workspaces, users, ct);
        await CreateCommentsAsync(context, boardData, users, ct);
        await CreateNotificationsAsync(context, targets, users, ct);
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

    // ──────────────── Phase 1: Users ────────────────

    private static async Task<List<User>> CreateUsersAsync(
        ApplicationDbContext context, SeedTargets targets, string passwordHash, CancellationToken ct)
    {
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

            // Create profile
            var profile = UserProfile.Create(user.Id, Epoch.AddDays(i));
            context.UserProfiles.Add(profile);

            // Create session
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

    // ──────────────── Phase 2: Workspaces ────────────────

    private static async Task<List<Workspace>> CreateWorkspacesAsync(
        ApplicationDbContext context, SeedTargets targets, List<User> users, CancellationToken ct)
    {
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

            // Add default users as members of all workspaces
            foreach (var uid in defaultUserIds)
            {
                if (!addedUserIds.Add(uid)) continue;
                var member = WorkspaceMember.Create(
                    ws.Id, uid, WorkspaceRole.Member, owner.Id, Epoch.AddDays(i));
                context.WorkspaceMembers.Add(member);
            }

            // Add other workspace members
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

    // ──────────────── Phase 3: Board structure ────────────────

    private sealed record BoardStructure(
        Board Board,
        List<BoardField> Fields,
        List<BoardGroup> Groups,
        List<BoardView> Views,
        List<Label> Labels);

    private static async Task<List<BoardStructure>> CreateBoardStructuresAsync(
        ApplicationDbContext context, SeedTargets targets, List<Workspace> workspaces, List<User> users, CancellationToken ct)
    {
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

                // Fields
                var fields = new List<BoardField>();

                for (int f = 0; f < targets.BoardFieldCount / targets.BoardCount; f++)
                {
                    var (fName, fType, fIsSystem, fSettings, fOptions) = f switch
                    {
                        0 => ("Title",       FieldType.Text,    true,  emptySettings,      Array.Empty<(string, string)>()),
                        1 => ("Status",      FieldType.Status,  true,  statusSettings,     StatusOptions),
                        2 => ("Assignee",    FieldType.Person,  true,  emptySettings,      Array.Empty<(string, string)>()),
                        3 => ("Due Date",    FieldType.Date,    false, emptySettings,      Array.Empty<(string, string)>()),
                        4 => ("Description", FieldType.LongText, false, emptySettings,      Array.Empty<(string, string)>()),
                        _ => ("Priority",    FieldType.Select,  false, emptySettings,      PriorityOptions),
                    };

                    var field = BoardField.Create(
                        ws.Id, board.Id, fName, fType, fSettings,
                        FractionalIndex.Create($"a{f}"), creator.Id, Epoch.AddDays(boardIndex),
                        isSystem: fIsSystem);
                    context.BoardFields.Add(field);
                    fields.Add(field);

                    // FieldOptions for Status and Select fields
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

                // Groups
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

                // Labels
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

                // Views
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

    // ──────────────── Phase 4: Board Items ────────────────

    private static async Task CreateBoardItemsAsync(
        ApplicationDbContext context, SeedTargets targets,
        List<BoardStructure> structures, List<User> users, CancellationToken ct)
    {
        var itemsPerGroup = targets.BoardItemCount / Math.Max(1, targets.BoardGroupCount);
        var remainingItems = targets.BoardItemCount % Math.Max(1, targets.BoardGroupCount);
        int itemIndex = 0;

        // Pre-build field option lookup per board
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

                    // Item Values for each field
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

                    // Assign some labels
                    if (i % 2 == 0 && bs.Labels.Count > 0)
                    {
                        var label = bs.Labels[i % bs.Labels.Count];
                        var itemLabel = BoardItemLabel.Create(
                            bs.Board.WorkspaceId, bs.Board.Id, item.Id, label.Id,
                            creator.Id, Epoch.AddDays(itemIndex));
                        context.BoardItemLabels.Add(itemLabel);
                    }

                    // Assign some members
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

    // ──────────────── Phase 5: Pages ────────────────

    private static async Task CreatePagesAsync(
        ApplicationDbContext context, SeedTargets targets,
        List<Workspace> workspaces, List<User> users, CancellationToken ct)
    {
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

                // Create blocks for each page
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
    }

    // ──────────────── Phase 6: Comments ────────────────

    private static async Task CreateCommentsAsync(
        ApplicationDbContext context, List<BoardStructure> structures,
        List<User> users, CancellationToken ct)
    {
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
    }

    // ──────────────── Phase 7: Notifications ────────────────

    private static async Task CreateNotificationsAsync(
        ApplicationDbContext context, SeedTargets targets,
        List<User> users, CancellationToken ct)
    {
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
                var nType = (NotificationType)(i % Enum.GetValues<NotificationType>().Length);
                var createdAt = Epoch.AddDays(i);

                var notification = Notification.Create(
                    user.Id, wsId, nType,
                    $"Notification {i + 1} for {user.Email.Value}",
                    $"This is a {nType} notification body.",
                    createdAt);
                context.Notifications.Add(notification);

                // NotificationDelivery
                var delivery = NotificationDelivery.Create(
                    notification.Id, wsId, user.Id,
                    NotificationChannel.InApp, createdAt);
                context.NotificationDeliveries.Add(delivery);
            }
        }

        await ClearAndSaveAsync(context, ct);
    }
}
