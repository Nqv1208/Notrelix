using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Notrelix.Application.Common.Interfaces;
using Notrelix.Domain.Entities;
using Notrelix.Domain.Enums;

namespace Notrelix.Infrastructure.Data;

public class ApplicationDbContextInitialiser
{
    private readonly ILogger<ApplicationDbContextInitialiser> _logger;
    private readonly ApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly Random _random = new();

    public ApplicationDbContextInitialiser(
        ILogger<ApplicationDbContextInitialiser> logger,
        ApplicationDbContext context,
        IPasswordHasher passwordHasher)
    {
        _logger = logger;
        _context = context;
        _passwordHasher = passwordHasher;
    }

    public async Task InitialiseAsync()
    {
        try
        {
            await _context.Database.MigrateAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while initialising database.");
            throw;
        }
    }

    public async Task SeedAsync()
    {
        try
        {
            await TrySeedAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while seeding database.");
            throw;
        }
    }

    private async Task TrySeedAsync()
    {
        var executionStrategy = _context.Database.CreateExecutionStrategy();
        await executionStrategy.ExecuteAsync(async () =>
        {
            if (await _context.Users.AnyAsync())
            {
                _logger.LogInformation("Database already seeded. Skip.");
                await LogSeedSummaryAsync("skip");
                return;
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var scale = BuildScale();
                _logger.LogInformation("Seeding metadata with scale: {@Scale}", scale);

                var users = await SeedUsersAsync(scale.UserCount);
                await SeedProfilesAndSessionsAsync(users, scale.SessionsPerUser);

                var workspaces = await SeedWorkspacesAsync(users, scale.WorkspaceCount, scale.MembersPerWorkspace);
                await SeedInvitationsAsync(workspaces, users, scale.InvitationsPerWorkspace);

                var pages = await SeedPagesAndBlocksAsync(workspaces, users, scale.PagesPerWorkspace, scale.BlocksPerPage);
                var boardData = await SeedBoardsAsync(
                    workspaces,
                    users,
                    scale.BoardsPerWorkspace,
                    scale.ListsPerBoard,
                    scale.CardsPerList,
                    scale.ChecklistsPerCard,
                    scale.ItemsPerChecklist);

                await SeedPermissionsAsync(workspaces, users, pages, boardData.Boards, scale.PermissionsPerWorkspace);
                await SeedCollaborationAsync(
                    workspaces,
                    users,
                    pages,
                    boardData.Lists,
                    boardData.Cards,
                    scale.CommentsPerWorkspace,
                    scale.AttachmentsPerWorkspace,
                    scale.ReactionsPerWorkspace,
                    scale.NotificationsPerUser);
                await SeedActivityLogsAsync(workspaces, users, pages, boardData.Cards, scale.ActivityLogsPerWorkspace);

                await transaction.CommitAsync();
                await LogSeedSummaryAsync("seeded");
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        });
    }

    private async Task<List<User>> SeedUsersAsync(int userCount)
    {
        var users = new List<User>
        {
            User.Create("admin@Notrelix.com", "Admin User", _passwordHasher.HashPassword("Admin@123")),
            User.Create("demo@Notrelix.com", "Demo User", _passwordHasher.HashPassword("Demo@123")),
            User.Create("test@Notrelix.com", "Test User", _passwordHasher.HashPassword("Test@123"))
        };

        for (var i = 1; i <= userCount; i++)
        {
            var user = User.Create(
                $"user{i:D3}@Notrelix.local",
                $"User {i:D3}",
                _passwordHasher.HashPassword("User@123"));

            if (i % 9 == 0) user.Suspend();
            if (i % 13 == 0) user.Deactivate();
            user.RecordLogin();
            users.Add(user);
        }

        _context.Users.AddRange(users);
        await _context.SaveChangesAsync(default);
        return users;
    }

    private async Task SeedProfilesAndSessionsAsync(IReadOnlyList<User> users, int sessionsPerUser)
    {
        var profiles = users.Select((user, index) =>
            UserProfile.Create(
                user.Id,
                timezone: index % 2 == 0 ? "UTC" : "Asia/Ho_Chi_Minh",
                locale: index % 3 == 0 ? "vi" : "en"))
            .ToList();
        _context.UserProfiles.AddRange(profiles);

        var sessions = new List<Session>();
        foreach (var user in users)
        {
            for (var i = 0; i < sessionsPerUser; i++)
            {
                sessions.Add(Session.Create(
                    user.Id,
                    TimeSpan.FromDays(30),
                    $"device-{i + 1}",
                    $"10.0.{_random.Next(1, 250)}.{_random.Next(1, 250)}"));
            }
        }
        _context.Sessions.AddRange(sessions);
        await _context.SaveChangesAsync(default);
    }

    private async Task<List<Workspace>> SeedWorkspacesAsync(IReadOnlyList<User> users, int workspaceCount, int membersPerWorkspace)
    {
        var workspaces = new List<Workspace>();
        var ownerCount = Math.Min(users.Count, workspaceCount);
        for (var i = 0; i < ownerCount; i++)
        {
            var owner = users[i];
            var workspace = i % 3 == 0
                ? Workspace.CreatePersonal($"Personal {i + 1}", owner.Id)
                : Workspace.CreateTeam($"Team {i + 1}", owner.Id, $"Workspace {i + 1} for metadata load test");

            workspace.UpdateSlug($"workspace-{i + 1:D3}");
            workspace.UpdateSettings($$"""{"theme":"{{(i % 2 == 0 ? "dark" : "light")}}","locale":"{{(i % 2 == 0 ? "vi" : "en")}}"}""");

            if (workspace.Type == WorkspaceType.Team)
            {
                var candidates = users
                    .Where(u => u.Id != workspace.OwnerId)
                    .OrderBy(_ => _random.Next())
                    .Take(membersPerWorkspace)
                    .ToList();

                for (var j = 0; j < candidates.Count; j++)
                {
                    var role = j == 0 ? MemberRole.Admin : (j % 4 == 0 ? MemberRole.Guest : MemberRole.Member);
                    workspace.AddMember(candidates[j].Id, role);
                }
            }

            workspaces.Add(workspace);
        }

        _context.Workspaces.AddRange(workspaces);
        await _context.SaveChangesAsync(default);
        return workspaces;
    }

    private async Task SeedInvitationsAsync(IReadOnlyList<Workspace> workspaces, IReadOnlyList<User> users, int invitationsPerWorkspace)
    {
        var invitations = new List<WorkspaceInvitation>();
        foreach (var workspace in workspaces)
        {
            for (var i = 0; i < invitationsPerWorkspace; i++)
            {
                var inviter = Pick(users);
                invitations.Add(WorkspaceInvitation.Create(
                    workspace.Id,
                    inviter.Id,
                    $"invite-{workspace.Id:N}-{i + 1}@Notrelix.local",
                    i % 3 == 0 ? "admin" : "member",
                    $"{workspace.Id:N}-{Guid.NewGuid():N}",
                    DateTime.UtcNow.AddDays(7 + i)));
            }
        }

        _context.WorkspaceInvitations.AddRange(invitations);
        await _context.SaveChangesAsync(default);
    }

    private async Task<List<Page>> SeedPagesAndBlocksAsync(IReadOnlyList<Workspace> workspaces, IReadOnlyList<User> users, int pagesPerWorkspace, int blocksPerPage)
    {
        var pages = new List<Page>();
        foreach (var workspace in workspaces)
        {
            Page? rootPage = null;
            for (var i = 0; i < pagesPerWorkspace; i++)
            {
                var page = Page.Create(
                    workspace.Id,
                    Pick(users).Id,
                    $"Page {i + 1:D2} - {workspace.Name}",
                    i > 0 && i % 4 != 0 ? rootPage?.Id : null);

                pages.Add(page);
                if (i == 0) rootPage = page;
            }
        }

        _context.Pages.AddRange(pages);
        await _context.SaveChangesAsync(default);

        var blockTypes = new[] { "heading1", "heading2", "paragraph", "quote", "code", "todo" };
        var blocks = new List<Block>();
        foreach (var page in pages)
        {
            for (var i = 0; i < blocksPerPage; i++)
            {
                var type = blockTypes[i % blockTypes.Length];
                var payload = type == "code"
                    ? $$"""{"language":"csharp","text":"// block {{i + 1}} in {{page.Title}}"}"""
                    : $$"""{"text":"Block {{i + 1}} in {{page.Title}}"}""";

                blocks.Add(Block.Create(page.Id, Pick(users).Id, type, payload, i + 1));
            }
        }

        _context.Blocks.AddRange(blocks);
        await _context.SaveChangesAsync(default);
        return pages;
    }

    private async Task<BoardSeedData> SeedBoardsAsync(
        IReadOnlyList<Workspace> workspaces,
        IReadOnlyList<User> users,
        int boardsPerWorkspace,
        int listsPerBoard,
        int cardsPerList,
        int checklistsPerCard,
        int itemsPerChecklist)
    {
        var boards = new List<Board>();
        foreach (var workspace in workspaces)
        {
            for (var i = 0; i < boardsPerWorkspace; i++)
            {
                boards.Add(Board.Create(workspace.Id, Pick(users).Id, $"Board {i + 1:D2} - {workspace.Name}"));
            }
        }
        _context.Boards.AddRange(boards);
        await _context.SaveChangesAsync(default);

        var boardMembers = new List<BoardMember>();
        var workspaceMembers = await _context.WorkspaceMembers.AsNoTracking().ToListAsync();
        foreach (var board in boards)
        {
            var members = workspaceMembers
                .Where(m => m.WorkspaceId == board.WorkspaceId)
                .OrderBy(_ => _random.Next())
                .Take(Math.Max(2, listsPerBoard))
                .ToList();

            foreach (var member in members)
            {
                boardMembers.Add(BoardMember.Create(board.Id, member.UserId, member.Role == MemberRole.Admin ? "admin" : "member"));
            }
        }
        _context.BoardMembers.AddRange(boardMembers);
        await _context.SaveChangesAsync(default);

        var lists = new List<BoardList>();
        foreach (var board in boards)
        {
            for (var i = 0; i < listsPerBoard; i++)
            {
                lists.Add(BoardList.Create(board.Id, $"List {i + 1:D2}", i + 1));
            }
        }
        _context.BoardLists.AddRange(lists);
        await _context.SaveChangesAsync(default);

        var cards = new List<Card>();
        foreach (var list in lists)
        {
            for (var i = 0; i < cardsPerList; i++)
            {
                cards.Add(Card.Create(list.Id, Pick(users).Id, $"Card {i + 1:D2} - {list.Title}", i + 1));
            }
        }
        _context.Cards.AddRange(cards);

        var labels = new List<Label>();
        foreach (var board in boards)
        {
            labels.Add(Label.Create(board.Id, "#ef4444", "Bug"));
            labels.Add(Label.Create(board.Id, "#10b981", "Feature"));
            labels.Add(Label.Create(board.Id, "#3b82f6", "API"));
        }
        _context.Labels.AddRange(labels);
        await _context.SaveChangesAsync(default);

        var listById = lists.ToDictionary(x => x.Id);
        var labelsByBoard = labels.GroupBy(x => x.BoardId).ToDictionary(g => g.Key, g => g.ToList());
        var boardMembersByBoard = boardMembers.GroupBy(x => x.BoardId).ToDictionary(g => g.Key, g => g.ToList());

        var cardMembers = new List<CardMember>();
        var cardLabels = new List<CardLabel>();
        foreach (var card in cards)
        {
            var boardId = listById[card.ListId].BoardId;
            foreach (var member in boardMembersByBoard[boardId].OrderBy(_ => _random.Next()).Take(2))
            {
                cardMembers.Add(CardMember.Create(card.Id, member.UserId, card.CreatedByUserId));
            }

            foreach (var label in labelsByBoard[boardId].OrderBy(_ => _random.Next()).Take(2))
            {
                cardLabels.Add(CardLabel.Create(card.Id, label.Id));
            }
        }
        _context.CardMembers.AddRange(cardMembers);
        _context.CardLabels.AddRange(cardLabels);
        await _context.SaveChangesAsync(default);

        var checklists = new List<Checklist>();
        foreach (var card in cards)
        {
            for (var i = 0; i < checklistsPerCard; i++)
            {
                checklists.Add(Checklist.Create(card.Id, $"Checklist {i + 1}", i + 1));
            }
        }
        _context.Checklists.AddRange(checklists);
        await _context.SaveChangesAsync(default);

        var checklistItems = new List<ChecklistItem>();
        foreach (var checklist in checklists)
        {
            for (var i = 0; i < itemsPerChecklist; i++)
            {
                checklistItems.Add(ChecklistItem.Create(checklist.Id, $"Item {i + 1} - {checklist.Title}", i + 1));
            }
        }
        _context.ChecklistItems.AddRange(checklistItems);
        await _context.SaveChangesAsync(default);

        return new BoardSeedData(boards, lists, cards);
    }

    private async Task SeedPermissionsAsync(IReadOnlyList<Workspace> workspaces, IReadOnlyList<User> users, IReadOnlyList<Page> pages, IReadOnlyList<Board> boards, int permissionsPerWorkspace)
    {
        var permissions = new List<Permission>();
        foreach (var workspace in workspaces)
        {
            var workspaceUsers = users.OrderBy(_ => _random.Next()).Take(Math.Min(permissionsPerWorkspace, users.Count)).ToList();
            var workspacePages = pages.Where(x => x.WorkspaceId == workspace.Id).ToList();
            var workspaceBoards = boards.Where(x => x.WorkspaceId == workspace.Id).ToList();

            foreach (var user in workspaceUsers)
            {
                if (workspacePages.Count > 0)
                {
                    permissions.Add(Permission.CreateForUser(workspace.Id, ResourceType.Page, Pick(workspacePages).Id, user.Id, PermissionLevel.Editor));
                }
                if (workspaceBoards.Count > 0)
                {
                    permissions.Add(Permission.CreateForUser(workspace.Id, ResourceType.Board, Pick(workspaceBoards).Id, user.Id, PermissionLevel.Commenter));
                }
            }
        }

        _context.Permissions.AddRange(permissions);
        await _context.SaveChangesAsync(default);
    }

    private async Task SeedCollaborationAsync(
        IReadOnlyList<Workspace> workspaces,
        IReadOnlyList<User> users,
        IReadOnlyList<Page> pages,
        IReadOnlyList<BoardList> lists,
        IReadOnlyList<Card> cards,
        int commentsPerWorkspace,
        int attachmentsPerWorkspace,
        int reactionsPerWorkspace,
        int notificationsPerUser)
    {
        var blocks = await _context.Blocks.AsNoTracking().ToListAsync();
        var listBoardMap = lists.ToDictionary(x => x.Id, x => x.BoardId);
        var boardWorkspaceMap = await _context.Boards.AsNoTracking().ToDictionaryAsync(x => x.Id, x => x.WorkspaceId);

        var comments = new List<Comment>();
        var attachments = new List<Attachment>();
        var reactions = new List<Reaction>();
        var notifications = new List<Notification>();

        foreach (var workspace in workspaces)
        {
            var workspaceUsers = users.OrderBy(_ => _random.Next()).Take(Math.Min(users.Count, Math.Max(4, commentsPerWorkspace / 2))).ToList();
            var workspacePages = pages.Where(x => x.WorkspaceId == workspace.Id).ToList();
            var workspaceBlocks = blocks.Where(x => workspacePages.Any(p => p.Id == x.PageId)).ToList();
            var workspaceCards = cards.Where(c => boardWorkspaceMap[listBoardMap[c.ListId]] == workspace.Id).ToList();

            for (var i = 0; i < commentsPerWorkspace; i++)
            {
                var user = Pick(workspaceUsers);
                if (i % 2 == 0 && workspaceCards.Count > 0)
                {
                    comments.Add(Comment.Create(workspace.Id, ResourceType.Card, Pick(workspaceCards).Id, user.Id, $"Comment card #{i + 1}"));
                }
                else if (workspacePages.Count > 0)
                {
                    comments.Add(Comment.Create(workspace.Id, ResourceType.Page, Pick(workspacePages).Id, user.Id, $"Comment page #{i + 1}"));
                }
            }

            for (var i = 0; i < attachmentsPerWorkspace; i++)
            {
                if (workspaceCards.Count == 0) break;
                var user = Pick(workspaceUsers);
                attachments.Add(Attachment.Create(
                    workspace.Id,
                    ResourceType.Card,
                    Pick(workspaceCards).Id,
                    user.Id,
                    $"file-{i + 1}.txt",
                    $"https://cdn.Notrelix.local/{workspace.Id:N}/file-{i + 1}.txt"));
            }

            for (var i = 0; i < reactionsPerWorkspace; i++)
            {
                if (workspaceBlocks.Count == 0) break;
                var user = Pick(workspaceUsers);
                reactions.Add(Reaction.Create(ResourceType.Block, Pick(workspaceBlocks).Id, user.Id, i % 2 == 0 ? ":+1:" : ":fire:"));
            }
        }

        foreach (var user in users)
        {
            for (var i = 0; i < notificationsPerUser; i++)
            {
                var workspace = Pick(workspaces);
                notifications.Add(Notification.Create(
                    workspace.Id,
                    user.Id,
                    i % 2 == 0 ? "card.assigned" : "comment.created",
                    $$"""{"message":"Notification #{{i + 1}} for {{user.Name}}"}"""));
            }
        }

        _context.Comments.AddRange(comments);
        _context.Attachments.AddRange(attachments);
        _context.Reactions.AddRange(reactions);
        _context.Notifications.AddRange(notifications);
        await _context.SaveChangesAsync(default);
    }

    private async Task SeedActivityLogsAsync(IReadOnlyList<Workspace> workspaces, IReadOnlyList<User> users, IReadOnlyList<Page> pages, IReadOnlyList<Card> cards, int logsPerWorkspace)
    {
        var logs = new List<ActivityLog>();
        foreach (var workspace in workspaces)
        {
            var workspacePages = pages.Where(x => x.WorkspaceId == workspace.Id).ToList();
            for (var i = 0; i < logsPerWorkspace; i++)
            {
                var actor = Pick(users);
                if (i % 2 == 0 && workspacePages.Count > 0)
                {
                    var page = Pick(workspacePages);
                    logs.Add(ActivityLog.Create(actor.Id, ActivityAction.Update, "page", page.Id, page.Title, workspace.Id, new { field = "title" }));
                }
                else if (cards.Count > 0)
                {
                    var card = Pick(cards);
                    logs.Add(ActivityLog.Create(actor.Id, ActivityAction.Comment, "card", card.Id, card.Title, workspace.Id, new { action = "comment" }));
                }
            }
        }

        _context.ActivityLogs.AddRange(logs);
        await _context.SaveChangesAsync(default);
    }

    private async Task LogSeedSummaryAsync(string stage)
    {
        var dbName = _context.Database.GetDbConnection().Database;
        _logger.LogInformation(
            "Seed summary ({Stage}) on db '{Database}': users={Users}, workspaces={Workspaces}, pages={Pages}, boards={Boards}, cards={Cards}",
            stage,
            dbName,
            await _context.Users.CountAsync(),
            await _context.Workspaces.CountAsync(),
            await _context.Pages.CountAsync(),
            await _context.Boards.CountAsync(),
            await _context.Cards.CountAsync());
    }

    private SeedScale BuildScale()
    {
        return new SeedScale(
            UserCount: GetEnvInt("SEED_USERS", 60),
            SessionsPerUser: GetEnvInt("SEED_SESSIONS_PER_USER", 2),
            WorkspaceCount: GetEnvInt("SEED_WORKSPACES", 12),
            MembersPerWorkspace: GetEnvInt("SEED_MEMBERS_PER_WORKSPACE", 10),
            InvitationsPerWorkspace: GetEnvInt("SEED_INVITES_PER_WORKSPACE", 6),
            PagesPerWorkspace: GetEnvInt("SEED_PAGES_PER_WORKSPACE", 18),
            BlocksPerPage: GetEnvInt("SEED_BLOCKS_PER_PAGE", 10),
            BoardsPerWorkspace: GetEnvInt("SEED_BOARDS_PER_WORKSPACE", 5),
            ListsPerBoard: GetEnvInt("SEED_LISTS_PER_BOARD", 4),
            CardsPerList: GetEnvInt("SEED_CARDS_PER_LIST", 16),
            ChecklistsPerCard: GetEnvInt("SEED_CHECKLISTS_PER_CARD", 2),
            ItemsPerChecklist: GetEnvInt("SEED_ITEMS_PER_CHECKLIST", 5),
            PermissionsPerWorkspace: GetEnvInt("SEED_PERMISSIONS_PER_WORKSPACE", 14),
            CommentsPerWorkspace: GetEnvInt("SEED_COMMENTS_PER_WORKSPACE", 40),
            AttachmentsPerWorkspace: GetEnvInt("SEED_ATTACHMENTS_PER_WORKSPACE", 20),
            ReactionsPerWorkspace: GetEnvInt("SEED_REACTIONS_PER_WORKSPACE", 24),
            NotificationsPerUser: GetEnvInt("SEED_NOTIFICATIONS_PER_USER", 8),
            ActivityLogsPerWorkspace: GetEnvInt("SEED_ACTIVITY_LOGS_PER_WORKSPACE", 60));
    }

    private static int GetEnvInt(string key, int fallback)
    {
        var raw = Environment.GetEnvironmentVariable(key);
        return int.TryParse(raw, out var value) && value > 0 ? value : fallback;
    }

    private T Pick<T>(IReadOnlyList<T> source)
    {
        if (source.Count == 0)
        {
            throw new InvalidOperationException("Cannot pick from an empty source.");
        }

        return source[_random.Next(source.Count)];
    }

    private sealed record SeedScale(
        int UserCount,
        int SessionsPerUser,
        int WorkspaceCount,
        int MembersPerWorkspace,
        int InvitationsPerWorkspace,
        int PagesPerWorkspace,
        int BlocksPerPage,
        int BoardsPerWorkspace,
        int ListsPerBoard,
        int CardsPerList,
        int ChecklistsPerCard,
        int ItemsPerChecklist,
        int PermissionsPerWorkspace,
        int CommentsPerWorkspace,
        int AttachmentsPerWorkspace,
        int ReactionsPerWorkspace,
        int NotificationsPerUser,
        int ActivityLogsPerWorkspace);

    private sealed record BoardSeedData(
        IReadOnlyList<Board> Boards,
        IReadOnlyList<BoardList> Lists,
        IReadOnlyList<Card> Cards);
}
