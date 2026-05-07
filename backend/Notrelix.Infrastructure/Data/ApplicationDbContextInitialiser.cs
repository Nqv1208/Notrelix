using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Notrelix.Application.Common.Interfaces;
using Notrelix.Domain.Entities.Identity;
using Notrelix.Domain.Entities.Workspaces;
using Notrelix.Domain.Entities.Document;
using Notrelix.Domain.Entities.Boards;
using Notrelix.Domain.Entities.Calendar;
using Notrelix.Domain.Entities.Shared;
using Notrelix.Domain.Enums;
using Notrelix.Domain.ValueObjects;

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
            if (_context.Database.IsNpgsql())
            {
                await _context.Database.MigrateAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while initialising the database.");
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
            _logger.LogError(ex, "An error occurred while seeding the database.");
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
                _logger.LogInformation("Database already seeded. Skipping.");
                return;
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var scale = BuildScale();
                _logger.LogInformation("Seeding metadata with scale: {@Scale}", scale);

                // 1. Identity
                var users = await SeedUsersAsync(scale.UserCount);
                await SeedProfilesAndSessionsAsync(users, scale.SessionsPerUser);
                await SeedOAuthAccountsAsync(users.Take(5).ToList());

                // 2. Workspace
                var workspaces = await SeedWorkspacesAsync(users, scale.WorkspaceCount, scale.MembersPerWorkspace);
                await SeedInvitationsAsync(workspaces, users, scale.InvitationsPerWorkspace);

                // 3. Document
                var pages = await SeedPagesAndBlocksAsync(workspaces, users, scale.PagesPerWorkspace, scale.BlocksPerPage);
                await SeedPageMentionsAsync(pages, users);

                // 4. Board
                var boardData = await SeedBoardsAsync(
                    workspaces,
                    users,
                    scale.BoardsPerWorkspace,
                    scale.ListsPerBoard,
                    scale.CardsPerList,
                    scale.ChecklistsPerCard,
                    scale.ItemsPerChecklist);
                
                await SeedBoardViewsAsync(boardData.Boards, users);
                await SeedCardLinksAsync(boardData.Cards);

                // 5. Calendar
                await SeedCalendarDataAsync(users, boardData.Cards);

                // 6. Shared / Collaboration
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

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                
                _logger.LogInformation("Database seeded successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during seeding transaction.");
                await transaction.RollbackAsync();
                throw;
            }
        });
    }

    #region 1. Identity

    private async Task<List<User>> SeedUsersAsync(int userCount)
    {
        var users = new List<User>
        {
            User.Create("admin@notrelix.com", "Admin User", _passwordHasher.HashPassword("Admin@123")),
            User.Create("demo@notrelix.com", "Demo User", _passwordHasher.HashPassword("Demo@123")),
            User.Create("test@notrelix.com", "Test User", _passwordHasher.HashPassword("Test@123"))
        };

        for (var i = 1; i <= userCount; i++)
        {
            var user = User.Create(
                $"user{i:D3}@notrelix.local",
                $"User {i:D3}",
                _passwordHasher.HashPassword("User@123"));
            
            users.Add(user);
        }

        _context.Users.AddRange(users);
        await _context.SaveChangesAsync();
        return users;
    }

    private async Task SeedProfilesAndSessionsAsync(List<User> users, int sessionsPerUser)
    {
        var profiles = users.Select(user => UserProfile.Create(user.Id)).ToList();
        foreach(var profile in profiles) profile.UpdateTheme("dark");
        _context.UserProfiles.AddRange(profiles);

        var sessions = new List<Session>();
        foreach (var user in users)
        {
            for (var i = 0; i < sessionsPerUser; i++)
            {
                sessions.Add(Session.Create(
                    user.Id,
                    TimeSpan.FromDays(30),
                    $"Device-{i + 1}",
                    $"192.168.1.{_random.Next(1, 255)}"));
            }
        }
        _context.Sessions.AddRange(sessions);
        await Task.CompletedTask;
    }

    private async Task SeedOAuthAccountsAsync(List<User> users)
    {
        foreach (var user in users)
        {
            _context.OAuthAccounts.Add(OAuthAccount.Create(user.Id, "google", $"google-id-{user.Id:N}", user.Email));
        }
        await Task.CompletedTask;
    }

    #endregion

    #region 2. Workspace

    private async Task<List<Workspace>> SeedWorkspacesAsync(List<User> users, int workspaceCount, int membersPerWorkspace)
    {
        var workspaces = new List<Workspace>();
        for (var i = 0; i < Math.Min(users.Count, workspaceCount); i++)
        {
            var owner = users[i];
            var workspace = i % 3 == 0
                ? Workspace.CreatePersonal($"Personal {i + 1}", owner.Id)
                : Workspace.CreateTeam($"Team {i + 1}", owner.Id, $"Workspace {i + 1} for testing");

            workspace.UpdateSlug(Slug.Create(workspace.Name).Value);
            workspace.UpdatePlan(i % 5 == 0 ? WorkspacePlan.Pro : WorkspacePlan.Free);

            if (!workspace.IsPersonal)
            {
                var candidates = users.Where(u => u.Id != owner.Id).OrderBy(_ => _random.Next()).Take(membersPerWorkspace).ToList();
                foreach (var candidate in candidates)
                {
                    workspace.AddMember(candidate.Id, _random.Next(0, 10) > 8 ? WorkspaceRole.Admin : WorkspaceRole.Member);
                }
            }
            workspaces.Add(workspace);
        }

        _context.Workspaces.AddRange(workspaces);
        await _context.SaveChangesAsync();
        return workspaces;
    }

    private async Task SeedInvitationsAsync(List<Workspace> workspaces, List<User> users, int invitationsPerWorkspace)
    {
        foreach (var workspace in workspaces.Where(w => !w.IsPersonal))
        {
            for (var i = 0; i < invitationsPerWorkspace; i++)
            {
                var inviter = users.First(u => u.Id == workspace.OwnerId);
                _context.WorkspaceInvitations.Add(WorkspaceInvitation.Create(
                    workspace.Id,
                    inviter.Id,
                    $"invite-{i}@test.com",
                    WorkspaceRole.Member,
                    TimeSpan.FromDays(7)));
            }
        }
        await Task.CompletedTask;
    }

    #endregion

    #region 3. Document

    private async Task<List<Page>> SeedPagesAndBlocksAsync(List<Workspace> workspaces, List<User> users, int pagesPerWorkspace, int blocksPerPage)
    {
        var pages = new List<Page>();
        foreach (var workspace in workspaces)
        {
            for (var i = 0; i < pagesPerWorkspace; i++)
            {
                var page = Page.Create(workspace.Id, Pick(users).Id, $"Page {i + 1} in {workspace.Name}");
                pages.Add(page);
            }
        }
        _context.Pages.AddRange(pages);
        await _context.SaveChangesAsync();

        foreach (var page in pages)
        {
            for (var i = 0; i < blocksPerPage; i++)
            {
                _context.Blocks.Add(Block.Create(
                    page.Id, 
                    page.CreatedByUserId, 
                    (i % 2 == 0 ? BlockType.Paragraph : BlockType.Heading1).ToString().ToLower(), 
                    "{\"text\": \"Content block " + i + "\"}", 
                    i + 1));
            }
        }
        return pages;
    }

    private async Task SeedPageMentionsAsync(List<Page> pages, List<User> users)
    {
        foreach (var page in pages.Take(10))
        {
            _context.PageMentions.Add(PageMention.Create(page.Id, Pick(users).Id, Pick(users).Id));
        }
        await Task.CompletedTask;
    }

    #endregion

    #region 4. Board

    private async Task<BoardSeedData> SeedBoardsAsync(
        List<Workspace> workspaces,
        List<User> users,
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
                boards.Add(Board.Create(workspace.Id, Pick(users).Id, $"Board {i + 1} - {workspace.Name}", $"Description for Board {i + 1} - {workspace.Name}"));
            }
        }
        _context.Boards.AddRange(boards);
        await _context.SaveChangesAsync();

        var lists = new List<BoardList>();
        foreach (var board in boards)
        {
            for (var i = 0; i < listsPerBoard; i++)
            {
                lists.Add(BoardList.Create(board.Id, $"List {i + 1}", i + 1));
            }
        }
        _context.BoardLists.AddRange(lists);
        await _context.SaveChangesAsync();

        var cards = new List<Card>();
        foreach (var list in lists)
        {
            for (var i = 0; i < cardsPerList; i++)
            {
                var card = Card.Create(list.Id, Pick(users).Id, $"Card {i + 1} in {list.Title}", i + 1);
                card.UpdateStatus(i % 5 == 0 ? CardStatus.Done : CardStatus.InProgress);
                card.UpdatePriority(i % 3 == 0 ? CardPriority.High : CardPriority.Medium);
                cards.Add(card);
            }
        }
        _context.Cards.AddRange(cards);
        await _context.SaveChangesAsync();

        // Seed Board Members
        foreach (var board in boards)
        {
            var wsMembers = await _context.WorkspaceMembers.Where(m => m.WorkspaceId == board.WorkspaceId).ToListAsync();
            foreach (var member in wsMembers.Take(5))
            {
                _context.BoardMembers.Add(BoardMember.Create(board.Id, member.UserId, BoardRole.Member));
            }
        }

        return new BoardSeedData(boards, lists, cards);
    }

    private async Task SeedBoardViewsAsync(List<Board> boards, List<User> users)
    {
        foreach (var board in boards)
        {
            _context.BoardViews.Add(BoardView.Create(board.Id, Pick(users).Id, Pick(new[] { ViewMode.Kanban, ViewMode.List, ViewMode.Calendar })));
        }
        await Task.CompletedTask;
    }

    private async Task SeedCardLinksAsync(List<Card> cards)
    {
        for (var i = 0; i < cards.Count - 1; i += 10)
        {
            _context.CardLinks.Add(CardLink.Create(cards[i].Id, cards[i + 1].Id, CardLinkType.RelatesTo));
        }
        await Task.CompletedTask;
    }

    #endregion

    #region 5. Calendar

    private async Task SeedCalendarDataAsync(List<User> users, List<Card> cards)
    {
        foreach (var user in users.Take(5))
        {
            var integration = CalendarIntegration.Create(user.Id, CalendarProvider.Google, "google-calendar-id");
            _context.CalendarIntegrations.Add(integration);
            
            var card = cards.FirstOrDefault();
            if (card != null)
            {
                _context.CalendarEvents.Add(CalendarEvent.Create(
                    integration.Id, 
                    "ext-" + Guid.NewGuid().ToString("N"),
                    ResourceType.Card,
                    card.Id));
            }
        }
        await Task.CompletedTask;
    }

    #endregion

    #region 6. Shared / Collaboration

    private async Task SeedPermissionsAsync(List<Workspace> workspaces, List<User> users, List<Page> pages, List<Board> boards, int permissionsPerWorkspace)
    {
        foreach (var workspace in workspaces.Take(5))
        {
            var page = pages.FirstOrDefault(p => p.WorkspaceId == workspace.Id);
            if (page != null)
                _context.Permissions.Add(Permission.CreateForUser(workspace.Id, ResourceType.Page, page.Id, Pick(users).Id, PermissionLevel.Editor));
        }
        await Task.CompletedTask;
    }

    private async Task SeedCollaborationAsync(
        List<Workspace> workspaces,
        List<User> users,
        List<Page> pages,
        List<BoardList> lists,
        List<Card> cards,
        int commentsPerWorkspace,
        int attachmentsPerWorkspace,
        int reactionsPerWorkspace,
        int notificationsPerUser)
    {
        foreach (var workspace in workspaces.Take(5))
        {
            var card = cards.FirstOrDefault();
            if (card != null)
            {
                _context.Comments.Add(Comment.Create(workspace.Id, ResourceType.Card, card.Id, Pick(users).Id, "Great progress on this card!"));
                
                _context.Attachments.Add(Attachment.Create(
                    workspace.Id, 
                    ResourceType.Card, 
                    card.Id, 
                    Pick(users).Id, 
                    "spec.pdf", 
                    "https://storage.notrelix.com/spec.pdf"));
            }
        }

        foreach (var user in users.Take(10))
        {
            _context.Notifications.Add(Notification.Create(
                workspaces.First().Id, 
                user.Id, 
                "test.notification", 
                users.First().Id, 
                "{\"msg\": \"Hello\"}"));
        }
        await Task.CompletedTask;
    }

    private async Task SeedActivityLogsAsync(List<Workspace> workspaces, List<User> users, List<Page> pages, List<Card> cards, int logsPerWorkspace)
    {
        foreach (var workspace in workspaces.Take(5))
        {
            var page = pages.FirstOrDefault(p => p.WorkspaceId == workspace.Id);
            if (page != null)
            {
                _context.ActivityLogs.Add(ActivityLog.Create(
                    workspace.Id, 
                    Pick(users).Id, 
                    "page.updated", 
                    ResourceType.Page, 
                    page.Id, 
                    page.Title, 
                    "{}"));
            }
        }
        await Task.CompletedTask;
    }

    #endregion

    private SeedScale BuildScale()
    {
        return new SeedScale(
            UserCount: GetEnvInt("SEED_USERS", 20),
            SessionsPerUser: GetEnvInt("SEED_SESSIONS_PER_USER", 1),
            WorkspaceCount: GetEnvInt("SEED_WORKSPACES", 5),
            MembersPerWorkspace: GetEnvInt("SEED_MEMBERS_PER_WORKSPACE", 5),
            InvitationsPerWorkspace: GetEnvInt("SEED_INVITES_PER_WORKSPACE", 2),
            PagesPerWorkspace: GetEnvInt("SEED_PAGES_PER_WORKSPACE", 5),
            BlocksPerPage: GetEnvInt("SEED_BLOCKS_PER_PAGE", 5),
            BoardsPerWorkspace: GetEnvInt("SEED_BOARDS_PER_WORKSPACE", 2),
            ListsPerBoard: GetEnvInt("SEED_LISTS_PER_BOARD", 3),
            CardsPerList: GetEnvInt("SEED_CARDS_PER_LIST", 5),
            ChecklistsPerCard: GetEnvInt("SEED_CHECKLISTS_PER_CARD", 1),
            ItemsPerChecklist: GetEnvInt("SEED_ITEMS_PER_CHECKLIST", 3),
            PermissionsPerWorkspace: GetEnvInt("SEED_PERMISSIONS_PER_WORKSPACE", 5),
            CommentsPerWorkspace: GetEnvInt("SEED_COMMENTS_PER_WORKSPACE", 10),
            AttachmentsPerWorkspace: GetEnvInt("SEED_ATTACHMENTS_PER_WORKSPACE", 5),
            ReactionsPerWorkspace: GetEnvInt("SEED_REACTIONS_PER_WORKSPACE", 10),
            NotificationsPerUser: GetEnvInt("SEED_NOTIFICATIONS_PER_USER", 5),
            ActivityLogsPerWorkspace: GetEnvInt("SEED_ACTIVITY_LOGS_PER_WORKSPACE", 10));
    }

    private static int GetEnvInt(string key, int fallback)
    {
        var raw = Environment.GetEnvironmentVariable(key);
        return int.TryParse(raw, out var value) && value > 0 ? value : fallback;
    }

    private T Pick<T>(IReadOnlyList<T> source)
    {
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
        List<Board> Boards,
        List<BoardList> Lists,
        List<Card> Cards);
}
