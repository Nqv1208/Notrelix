using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Notrelix.Application.Common.Interfaces;
using Notrelix.Domain.Entities.Boards;
using Notrelix.Domain.Entities.Calendar;
using Notrelix.Domain.Entities.Document;
using Notrelix.Domain.Entities.Identity;
using Notrelix.Domain.Entities.Shared;
using Notrelix.Domain.Entities.Workspaces;
using Notrelix.Domain.Enums;

namespace Notrelix.Infrastructure.Data;

public class ApplicationDbContextInitialiser
{
    private const int RandomSeed = 731_2026;
    private const string DefaultPassword = "Notrelix@123";
    private const string AdminEmail = "admin@notrelix.com";
    private const string DemoEmail = "demo@notrelix.com";
    private const string MemberEmail = "member@notrelix.com";
    private const string SeedUserDomain = "notrelix.local";
    private const string LegacyDemoEmail = "demo@notrelix.local";
    private const string LegacyMemberEmail = "seed-user-0003@notrelix.local";
    private const string SeedWorkspaceSlugPrefix = "seed-workspace-";
    private static readonly SeedLoginAccount[] DefaultLoginAccounts =
    [
        new(AdminEmail, "Admin User", WorkspaceRole.Admin),
        new(DemoEmail, "Demo User", WorkspaceRole.Member),
        new(MemberEmail, "Member User", WorkspaceRole.Member)
    ];

    private static readonly string[] DefaultLoginEmails = DefaultLoginAccounts
        .Select(account => account.Email)
        .ToArray();

    private readonly ILogger<ApplicationDbContextInitialiser> _logger;
    private readonly ApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly SeedDataOptions _options;
    private Random _random = new(RandomSeed);

    public ApplicationDbContextInitialiser(
        ILogger<ApplicationDbContextInitialiser> logger,
        ApplicationDbContext context,
        IPasswordHasher passwordHasher,
        IOptions<SeedDataOptions> options)
    {
        _logger = logger;
        _context = context;
        _passwordHasher = passwordHasher;
        _options = options.Value;
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
        if (!_options.Enabled)
        {
            _logger.LogInformation("SeedData is disabled. Skipping seed pipeline.");
            return;
        }

        try
        {
            await TrySeedAsync(_options.GetTargets());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while seeding the database.");
            throw;
        }
    }

    private async Task TrySeedAsync(SeedTargets targets)
    {
        var executionStrategy = _context.Database.CreateExecutionStrategy();

        await executionStrategy.ExecuteAsync(async () =>
        {
            _random = new Random(RandomSeed);

            if (!_options.ResetBeforeSeed && await SeedSentinelExistsAsync())
            {
                await EnsureDefaultAccountsAndWorkspaceAccessAsync();
                await EnsureBoardMetadataAsync();
                _logger.LogInformation("Seed sentinel data exists. Ensured board metadata and skipped full seed pipeline.");
                return;
            }

            await using var transaction = await BeginTransactionIfSupportedAsync();
            var autoDetectChanges = _context.ChangeTracker.AutoDetectChangesEnabled;

            try
            {
                _context.ChangeTracker.AutoDetectChangesEnabled = false;

                if (_options.ResetBeforeSeed)
                {
                    await DeleteSeedDataAsync();
                }

                _logger.LogInformation(
                    "Seeding Notrelix data with {Profile} profile: {@Targets}",
                    _options.Profile,
                    targets);

                var users = await SeedIdentityAsync(targets);
                var workspaceData = await SeedWorkspacesAsync(targets, users);
                var documentData = await SeedDocumentsAsync(targets, users, workspaceData.Workspaces);
                var boardData = await SeedBoardsAsync(targets, users, workspaceData.Workspaces, workspaceData.MembersByWorkspace);

                await SeedPermissionsAsync(users, workspaceData.Workspaces, documentData.Pages, boardData.Boards);
                await SeedCalendarAsync(targets, users, workspaceData.Workspaces, boardData.Cards);
                var comments = await SeedCollaborationAsync(targets, users, workspaceData.Workspaces, documentData.Pages, boardData.Cards);
                await SeedActivityLogsAsync(targets, users, workspaceData.Workspaces, documentData.Pages, boardData.Cards, comments);

                if (transaction is not null)
                {
                    await transaction.CommitAsync();
                }

                _logger.LogInformation("Seed pipeline completed successfully.");
            }
            catch
            {
                if (transaction is not null)
                {
                    await transaction.RollbackAsync();
                }

                throw;
            }
            finally
            {
                _context.ChangeTracker.AutoDetectChangesEnabled = autoDetectChanges;
                _context.ChangeTracker.Clear();
            }
        });
    }

    private async Task<IDbContextTransaction?> BeginTransactionIfSupportedAsync()
    {
        if (!_context.Database.IsRelational())
        {
            return null;
        }

        return await _context.Database.BeginTransactionAsync();
    }

    private async Task<bool> SeedSentinelExistsAsync()
    {
        var adminExists = await _context.Users.AnyAsync(user => user.Email.Value == AdminEmail);
        if (adminExists)
        {
            return true;
        }

        return await _context.Workspaces.AnyAsync(workspace =>
            workspace.Slug.StartsWith(SeedWorkspaceSlugPrefix));
    }

    private async Task DeleteSeedDataAsync()
    {
        var workspaceIds = await _context.Workspaces
            .Where(workspace => workspace.Slug.StartsWith(SeedWorkspaceSlugPrefix))
            .Select(workspace => workspace.Id)
            .ToListAsync();

        var userIds = await _context.Users
            .Where(user =>
                DefaultLoginEmails.Contains(user.Email.Value) ||
                user.Email.Value.EndsWith("@" + SeedUserDomain))
            .Select(user => user.Id)
            .ToListAsync();

        if (workspaceIds.Count == 0 && userIds.Count == 0)
        {
            return;
        }

        var boardIds = await _context.Boards
            .Where(board => workspaceIds.Contains(board.WorkspaceId))
            .Select(board => board.Id)
            .ToListAsync();

        var listIds = await _context.BoardLists
            .Where(list => boardIds.Contains(list.BoardId))
            .Select(list => list.Id)
            .ToListAsync();

        var cardIds = await _context.Cards
            .Where(card => listIds.Contains(card.ListId))
            .Select(card => card.Id)
            .ToListAsync();

        var labelIds = await _context.Labels
            .Where(label => boardIds.Contains(label.BoardId))
            .Select(label => label.Id)
            .ToListAsync();

        var pageIds = await _context.Pages
            .Where(page => workspaceIds.Contains(page.WorkspaceId))
            .Select(page => page.Id)
            .ToListAsync();

        var blockIds = await _context.Blocks
            .Where(block => pageIds.Contains(block.PageId))
            .Select(block => block.Id)
            .ToListAsync();

        var checklistIds = await _context.Checklists
            .Where(checklist => cardIds.Contains(checklist.CardId))
            .Select(checklist => checklist.Id)
            .ToListAsync();

        var commentIds = await _context.Comments
            .Where(comment => workspaceIds.Contains(comment.WorkspaceId))
            .Select(comment => comment.Id)
            .ToListAsync();

        var calendarIntegrationIds = await _context.CalendarIntegrations
            .Where(integration =>
                userIds.Contains(integration.UserId) ||
                (integration.WorkspaceId.HasValue && workspaceIds.Contains(integration.WorkspaceId.Value)))
            .Select(integration => integration.Id)
            .ToListAsync();

        await DeleteRangeAsync(_context.CalendarEvents.Where(x => calendarIntegrationIds.Contains(x.IntegrationId)));
        await DeleteRangeAsync(_context.PageMentions.Where(x =>
            pageIds.Contains(x.PageId) ||
            (x.BlockId.HasValue && blockIds.Contains(x.BlockId.Value)) ||
            userIds.Contains(x.MentionedUserId) ||
            userIds.Contains(x.MentionedBy)));
        await DeleteRangeAsync(_context.Reactions.Where(x =>
            userIds.Contains(x.UserId) ||
            (x.ResourceType == ResourceType.Card && cardIds.Contains(x.ResourceId)) ||
            (x.ResourceType == ResourceType.Page && pageIds.Contains(x.ResourceId)) ||
            (x.ResourceType == ResourceType.Block && blockIds.Contains(x.ResourceId)) ||
            (x.ResourceType == ResourceType.Comment && commentIds.Contains(x.ResourceId))));
        await DeleteRangeAsync(_context.Attachments.Where(x => workspaceIds.Contains(x.WorkspaceId)));
        await DeleteRangeAsync(_context.Comments.Where(x => workspaceIds.Contains(x.WorkspaceId)));
        await DeleteRangeAsync(_context.Notifications.Where(x => workspaceIds.Contains(x.WorkspaceId) || userIds.Contains(x.UserId)));
        await DeleteRangeAsync(_context.ActivityLogs.Where(x => workspaceIds.Contains(x.WorkspaceId) || userIds.Contains(x.ActorId)));
        await DeleteRangeAsync(_context.Permissions.Where(x => workspaceIds.Contains(x.WorkspaceId)));
        await DeleteRangeAsync(_context.CardLinks.Where(x => cardIds.Contains(x.SourceCardId) || cardIds.Contains(x.TargetCardId)));
        await DeleteRangeAsync(_context.ChecklistItems.Where(x => checklistIds.Contains(x.ChecklistId)));
        await DeleteRangeAsync(_context.Checklists.Where(x => cardIds.Contains(x.CardId)));
        await DeleteRangeAsync(_context.CardLabels.Where(x => cardIds.Contains(x.CardId) || labelIds.Contains(x.LabelId)));
        await DeleteRangeAsync(_context.CardMembers.Where(x => cardIds.Contains(x.CardId) || userIds.Contains(x.UserId)));
        await DeleteRangeAsync(_context.Cards.Where(x => listIds.Contains(x.ListId)));
        await DeleteRangeAsync(_context.Labels.Where(x => boardIds.Contains(x.BoardId)));
        await DeleteRangeAsync(_context.BoardColumns.Where(x => boardIds.Contains(x.BoardId)));
        await DeleteRangeAsync(_context.BoardViews.Where(x => boardIds.Contains(x.BoardId) || userIds.Contains(x.UserId)));
        await DeleteRangeAsync(_context.BoardMembers.Where(x => boardIds.Contains(x.BoardId) || userIds.Contains(x.UserId)));
        await DeleteRangeAsync(_context.BoardLists.Where(x => boardIds.Contains(x.BoardId)));
        await DeleteRangeAsync(_context.Boards.Where(x => workspaceIds.Contains(x.WorkspaceId)));
        await DeleteRangeAsync(_context.Blocks.Where(x => pageIds.Contains(x.PageId)));
        await DeleteRangeAsync(_context.Pages.Where(x => workspaceIds.Contains(x.WorkspaceId)));
        await DeleteRangeAsync(_context.WorkspaceInvitations.Where(x => workspaceIds.Contains(x.WorkspaceId)));
        await DeleteRangeAsync(_context.WorkspaceMembers.Where(x => workspaceIds.Contains(x.WorkspaceId) || userIds.Contains(x.UserId)));
        await DeleteRangeAsync(_context.Workspaces.Where(x => workspaceIds.Contains(x.Id)));
        await DeleteRangeAsync(_context.OAuthAccounts.Where(x => userIds.Contains(x.UserId)));
        await DeleteRangeAsync(_context.Sessions.Where(x => userIds.Contains(x.UserId)));
        await DeleteRangeAsync(_context.UserProfiles.Where(x => userIds.Contains(x.UserId)));
        await DeleteRangeAsync(_context.Users.Where(x => userIds.Contains(x.Id)));
    }

    private async Task DeleteRangeAsync<T>(IQueryable<T> query)
        where T : class
    {
        if (_context.Database.IsRelational())
        {
            await query.ExecuteDeleteAsync();
            return;
        }

        var entities = await query.ToListAsync();
        if (entities.Count == 0)
        {
            return;
        }

        _context.Set<T>().RemoveRange(entities);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();
    }

    private async Task EnsureBoardMetadataAsync()
    {
        var boardIds = await _context.Boards
            .Select(board => board.Id)
            .ToListAsync();

        if (boardIds.Count == 0)
        {
            return;
        }

        var existingColumns = await _context.BoardColumns
            .Select(column => new { column.BoardId, column.Name })
            .ToListAsync();

        var existingColumnKeys = existingColumns
            .Select(column => (column.BoardId, column.Name))
            .ToHashSet();

        var missingColumns = boardIds.SelectMany(boardId =>
            BoardColumn.CreateDefaults(boardId)
                .Where(column => !existingColumnKeys.Contains((column.BoardId, column.Name))));

        await AddInBatchesAsync(missingColumns, 2_000);
    }

    private async Task EnsureDefaultAccountsAndWorkspaceAccessAsync()
    {
        var defaultUsersByEmail = await EnsureDefaultAccountsAsync();
        await EnsureDefaultWorkspaceMembershipsAsync(defaultUsersByEmail);
    }

    private async Task<Dictionary<string, Guid>> EnsureDefaultAccountsAsync()
    {
        var lookupEmails = DefaultLoginEmails
            .Concat([LegacyDemoEmail, LegacyMemberEmail])
            .ToArray();

        var users = await _context.Users
            .Where(user => lookupEmails.Contains(user.Email.Value))
            .ToListAsync();

        var existingByEmail = users.ToDictionary(user => user.Email.Value, StringComparer.OrdinalIgnoreCase);
        var changed = false;

        if (!existingByEmail.ContainsKey(DemoEmail) && existingByEmail.TryGetValue(LegacyDemoEmail, out var legacyDemo))
        {
            legacyDemo.UpdateEmail(DemoEmail);
            existingByEmail.Remove(LegacyDemoEmail);
            existingByEmail[DemoEmail] = legacyDemo;
            changed = true;
        }

        if (!existingByEmail.ContainsKey(MemberEmail) && existingByEmail.TryGetValue(LegacyMemberEmail, out var legacyMember))
        {
            legacyMember.UpdateEmail(MemberEmail);
            existingByEmail.Remove(LegacyMemberEmail);
            existingByEmail[MemberEmail] = legacyMember;
            changed = true;
        }

        foreach (var account in DefaultLoginAccounts)
        {
            if (!existingByEmail.TryGetValue(account.Email, out var user))
            {
                var created = User.Create(account.Email, account.Name, _passwordHasher.HashPassword(DefaultPassword));
                _context.Users.Add(created);
                existingByEmail[account.Email] = created;
                changed = true;
                continue;
            }

            if (user.Name != account.Name)
            {
                user.UpdateProfile(account.Name, user.Avatar);
                changed = true;
            }

            if (!_passwordHasher.VerifyPassword(DefaultPassword, user.PasswordHash))
            {
                user.UpdatePassword(_passwordHasher.HashPassword(DefaultPassword));
                changed = true;
            }
        }

        if (changed)
        {
            await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear();
        }

        var defaultUserIds = await _context.Users
            .Where(user => DefaultLoginEmails.Contains(user.Email.Value))
            .Select(user => new { user.Email.Value, user.Id })
            .ToDictionaryAsync(user => user.Value, user => user.Id, StringComparer.OrdinalIgnoreCase);

        var existingProfileUserIds = await _context.UserProfiles
            .Where(profile => defaultUserIds.Values.Contains(profile.UserId))
            .Select(profile => profile.UserId)
            .ToListAsync();

        var profiles = defaultUserIds.Values
            .Where(userId => !existingProfileUserIds.Contains(userId))
            .Select(UserProfile.Create);

        await AddInBatchesAsync(profiles, 100);

        return defaultUserIds;
    }

    private async Task EnsureDefaultWorkspaceMembershipsAsync(IReadOnlyDictionary<string, Guid> defaultUsersByEmail)
    {
        var workspaces = await _context.Workspaces
            .Include(workspace => workspace.Members)
            .Where(workspace => workspace.Slug.StartsWith(SeedWorkspaceSlugPrefix))
            .ToListAsync();

        if (workspaces.Count == 0)
        {
            return;
        }

        foreach (var workspace in workspaces)
        {
            foreach (var account in DefaultLoginAccounts)
            {
                if (!defaultUsersByEmail.TryGetValue(account.Email, out var userId) ||
                    workspace.OwnerId == userId ||
                    workspace.IsMember(userId))
                {
                    continue;
                }

                workspace.AddMember(
                    userId,
                    workspace.IsPersonal ? WorkspaceRole.Guest : account.TeamRole);
            }
        }

        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();
    }

    private async Task<List<SeedUser>> SeedIdentityAsync(SeedTargets targets)
    {
        var passwordHash = _passwordHasher.HashPassword(DefaultPassword);
        var users = new List<User>(targets.UserCount);
        var userRefs = new List<SeedUser>(targets.UserCount);

        for (var index = 0; index < targets.UserCount; index++)
        {
            var defaultAccount = index < DefaultLoginAccounts.Length
                ? DefaultLoginAccounts[index]
                : null;

            var email = defaultAccount?.Email ?? (index switch
            {
                _ => $"seed-user-{index + 1:D4}@{SeedUserDomain}"
            });

            var name = defaultAccount?.Name ?? $"Seed User {index + 1:D4}";
            var user = User.Create(email, name, passwordHash);
            users.Add(user);
            userRefs.Add(new SeedUser(user.Id, email, name));
        }

        await AddInBatchesAsync(users, 1_000);

        var profiles = userRefs.Select((user, index) =>
        {
            var profile = UserProfile.Create(user.Id);
            profile.UpdateTimezone(index % 3 == 0 ? "Asia/Ho_Chi_Minh" : "UTC");
            profile.UpdateLocale(index % 4 == 0 ? "en" : "vi");
            profile.UpdateTheme(index % 2 == 0 ? "dark" : "system");
            profile.UpdatePreferences(ToJson(new
            {
                density = index % 3 == 0 ? "compact" : "comfortable",
                seed = true
            }));
            return profile;
        }).ToList();

        await AddInBatchesAsync(profiles, 2_000);

        var expiresAt = DateTime.UtcNow.AddDays(30);
        var sessions = userRefs.Select((user, index) => Session.Create(
            user.Id,
            $"seed-refresh-token-{index + 1:D5}",
            expiresAt,
            $"Seed Browser {index % 4 + 1}",
            $"10.20.{index / 255}.{index % 255 + 1}"));

        await AddInBatchesAsync(sessions, 2_000);

        var oauthAccounts = userRefs
            .Where((_, index) => index % 5 == 0)
            .Select((user, index) => OAuthAccount.Create(
                user.Id,
                "google",
                $"seed-google-{index + 1:D5}",
                rawProfile: ToJson(new { email = user.Email, seed = true })));

        await AddInBatchesAsync(oauthAccounts, 1_000);

        return userRefs;
    }

    private async Task<WorkspaceSeedData> SeedWorkspacesAsync(SeedTargets targets, IReadOnlyList<SeedUser> users)
    {
        var workspaces = new List<Workspace>(targets.WorkspaceCount);
        var workspaceRefs = new List<SeedWorkspace>(targets.WorkspaceCount);
        var membersByWorkspace = new Dictionary<Guid, List<Guid>>();
        var membersPerTeam = Math.Min(users.Count, Math.Max(3, Math.Min(30, targets.UserCount / 4)));

        for (var index = 0; index < targets.WorkspaceCount; index++)
        {
            var owner = users[index % users.Count];
            var isPersonal = index % 10 == 0;
            var workspace = isPersonal
                ? Workspace.CreatePersonal($"Seed Personal {index + 1:D3}", owner.Id)
                : Workspace.CreateTeam(
                    $"Seed Workspace {index + 1:D3}",
                    owner.Id,
                    $"Deterministic seed workspace {index + 1:D3} for {targets.BoardCount} boards.");

            workspace.UpdateSlug($"{SeedWorkspaceSlugPrefix}{index + 1:D3}");
            workspace.UpdatePlan(index % 4 == 0 ? WorkspacePlan.Pro : WorkspacePlan.Free);
            workspace.UpdateSettings(ToJson(new
            {
                seed = true,
                profile = _options.Profile.ToString(),
                defaultView = index % 2 == 0 ? "boards" : "docs"
            }));

            if (!workspace.IsPersonal)
            {
                EnsureDefaultWorkspaceMembers(workspace, users);

                for (var offset = 1; offset < membersPerTeam; offset++)
                {
                    var user = users[(index + offset) % users.Count];
                    if (user.Id == owner.Id || workspace.IsMember(user.Id))
                    {
                        continue;
                    }

                    var role = offset == 1 ? WorkspaceRole.Admin : WorkspaceRole.Member;
                    workspace.AddMember(user.Id, role);
                }
            }
            else
            {
                EnsureDefaultWorkspaceMembers(workspace, users);
            }

            workspaces.Add(workspace);
            workspaceRefs.Add(new SeedWorkspace(workspace.Id, workspace.Slug, owner.Id));
            membersByWorkspace[workspace.Id] = workspace.Members.Select(member => member.UserId).ToList();
        }

        await AddInBatchesAsync(workspaces, 250);

        var invitationsPerTeam = _options.Profile switch
        {
            SeedProfile.Small => 2,
            SeedProfile.Medium => 5,
            SeedProfile.Large => 10,
            _ => 2
        };

        var invitations = workspaceRefs
            .Where((_, index) => index % 10 != 0)
            .SelectMany(workspace => Enumerable.Range(1, invitationsPerTeam)
                .Select(offset => WorkspaceInvitation.Create(
                    workspace.Id,
                    workspace.OwnerId,
                    $"invite-{workspace.Slug}-{offset:D2}@{SeedUserDomain}",
                    WorkspaceRole.Member,
                    TimeSpan.FromDays(14))));

        await AddInBatchesAsync(invitations, 1_000);

        return new WorkspaceSeedData(workspaceRefs, membersByWorkspace);
    }

    private async Task<DocumentSeedData> SeedDocumentsAsync(
        SeedTargets targets,
        IReadOnlyList<SeedUser> users,
        IReadOnlyList<SeedWorkspace> workspaces)
    {
        var pageDistribution = Distribute(targets.PageCount, workspaces.Count);
        var pages = new List<Page>(targets.PageCount);
        var pageRefs = new List<SeedPage>(targets.PageCount);
        var pageIndex = 0;

        for (var workspaceIndex = 0; workspaceIndex < workspaces.Count; workspaceIndex++)
        {
            var workspace = workspaces[workspaceIndex];
            var workspaceRootPages = new List<Guid>();

            for (var index = 0; index < pageDistribution[workspaceIndex]; index++)
            {
                var author = users[(pageIndex + workspaceIndex) % users.Count];
                var parentId = index > 0 && index % 4 == 0
                    ? workspaceRootPages[(index / 4 - 1) % workspaceRootPages.Count]
                    : (Guid?)null;

                var page = Page.Create(
                    workspace.Id,
                    author.Id,
                    parentId.HasValue
                        ? $"Seed child page {index + 1:D4}"
                        : $"Seed page {index + 1:D4} in {workspace.Slug}",
                    parentId);

                page.Move(parentId, (index + 1) * 1024d);
                page.UpdateIcon("seed", Pick(new[] { "doc", "pin", "note", "check" }));
                if (index % 8 == 0)
                {
                    page.SetDeadline(DateTime.UtcNow.Date.AddDays(index % 30 + 1));
                }

                if (!parentId.HasValue)
                {
                    workspaceRootPages.Add(page.Id);
                }

                pages.Add(page);
                pageRefs.Add(new SeedPage(page.Id, workspace.Id, author.Id));
                pageIndex++;
            }
        }

        await AddInBatchesAsync(pages, 1_000);

        var blocksPerPage = _options.Profile switch
        {
            SeedProfile.Small => 5,
            SeedProfile.Medium => 6,
            SeedProfile.Large => 5,
            _ => 5
        };

        var blockRefs = new List<SeedBlock>(pageRefs.Count);
        var blocks = pageRefs.SelectMany((page, pageOffset) =>
            Enumerable.Range(1, blocksPerPage).Select(blockOffset =>
            {
                var type = blockOffset switch
                {
                    1 => "heading1",
                    2 => "paragraph",
                    3 => "todo",
                    4 => "bulletedlist",
                    _ => "paragraph"
                };

                var block = Block.Create(
                    page.Id,
                    page.CreatedByUserId,
                    type,
                    ToJson(new
                    {
                        text = $"Seed block {blockOffset} for page {pageOffset + 1:D5}",
                        checkedValue = type == "todo" && pageOffset % 2 == 0,
                        seed = true
                    }),
                    blockOffset * 1024d);

                if (blockOffset == 1)
                {
                    blockRefs.Add(new SeedBlock(block.Id, page.Id));
                }

                return block;
            }));

        await AddInBatchesAsync(blocks, 2_000);

        var pageMentions = pageRefs
            .Where((_, index) => index % 10 == 0)
            .Select((page, index) => PageMention.Create(
                page.Id,
                users[(index + 1) % users.Count].Id,
                page.CreatedByUserId,
                blockRefs.Count == 0 ? null : blockRefs[index % blockRefs.Count].Id));

        await AddInBatchesAsync(pageMentions, 1_000);

        return new DocumentSeedData(pageRefs, blockRefs);
    }

    private async Task<BoardSeedData> SeedBoardsAsync(
        SeedTargets targets,
        IReadOnlyList<SeedUser> users,
        IReadOnlyList<SeedWorkspace> workspaces,
        IReadOnlyDictionary<Guid, List<Guid>> membersByWorkspace)
    {
        var boardDistribution = Distribute(targets.BoardCount, workspaces.Count);
        var boards = new List<Board>(targets.BoardCount);
        var boardRefs = new List<SeedBoard>(targets.BoardCount);
        var boardOrdinal = 0;

        for (var workspaceIndex = 0; workspaceIndex < workspaces.Count; workspaceIndex++)
        {
            var workspace = workspaces[workspaceIndex];
            for (var index = 0; index < boardDistribution[workspaceIndex]; index++)
            {
                var creator = users[(boardOrdinal + workspaceIndex) % users.Count];
                var board = Board.Create(
                    workspace.Id,
                    creator.Id,
                    $"Seed Board {boardOrdinal + 1:D5}",
                    $"Seed board {boardOrdinal + 1:D5} in {workspace.Slug}",
                    boardOrdinal % 7 == 0 ? BoardVisibility.Private : BoardVisibility.Workspace);

                board.UpdateBackground(ToJson(new
                {
                    type = "color",
                    value = Pick(new[] { "#0079BF", "#22C55E", "#F59E0B", "#6366F1", "#EF4444" })
                }));

                boards.Add(board);
                boardRefs.Add(new SeedBoard(board.Id, workspace.Id, creator.Id));
                boardOrdinal++;
            }
        }

        await AddInBatchesAsync(boards, 500);

        var boardMembers = boardRefs.SelectMany(board =>
        {
            var candidates = membersByWorkspace.TryGetValue(board.WorkspaceId, out var members)
                ? members
                : users.Select(user => user.Id).ToList();

            return candidates
                .Take(Math.Min(5, candidates.Count))
                .Select((userId, index) => BoardMember.Create(
                    board.Id,
                    userId,
                    index == 0 ? BoardRole.Admin : BoardRole.Member));
        });

        await AddInBatchesAsync(boardMembers, 2_000);

        var boardViews = boardRefs.SelectMany(board =>
        {
            var candidates = membersByWorkspace.TryGetValue(board.WorkspaceId, out var members)
                ? members
                : users.Select(user => user.Id).ToList();

            return candidates.Take(Math.Min(3, candidates.Count)).Select((userId, index) =>
            {
                var view = BoardView.Create(board.Id, userId, Pick(new[] { ViewMode.Kanban, ViewMode.List, ViewMode.Calendar }));
                view.UpdateFilters(ToJson(new
                {
                    assignedToMe = index == 0,
                    hideDone = index == 2,
                    seed = true
                }));
                return view;
            });
        });

        await AddInBatchesAsync(boardViews, 2_000);

        var listsPerBoard = 5;
        var listRefs = new List<SeedList>(boardRefs.Count * listsPerBoard);
        var lists = boardRefs.SelectMany(board =>
            Enumerable.Range(1, listsPerBoard).Select(index =>
            {
                var list = BoardList.Create(board.Id, PickListTitle(index), index * 1024d);
                listRefs.Add(new SeedList(list.Id, board.Id, board.WorkspaceId));
                return list;
            }));

        await AddInBatchesAsync(lists, 2_000);

        var boardColumns = boardRefs.SelectMany(board => BoardColumn.CreateDefaults(board.Id));
        await AddInBatchesAsync(boardColumns, 2_000);

        var labelDefinitions = new[]
        {
            ("Bug", "#EF4444"),
            ("Feature", "#22C55E"),
            ("Research", "#06B6D4"),
            ("Design", "#A855F7"),
            ("Ops", "#F59E0B"),
            ("Docs", "#64748B")
        };

        var labelRefs = new List<SeedLabel>(boardRefs.Count * labelDefinitions.Length);
        var labels = boardRefs.SelectMany(board => labelDefinitions.Select(definition =>
        {
            var label = Label.Create(board.Id, definition.Item2, definition.Item1);
            labelRefs.Add(new SeedLabel(label.Id, board.Id));
            return label;
        }));

        await AddInBatchesAsync(labels, 2_000);

        var cards = new List<Card>(targets.CardCount);
        var cardRefs = new List<SeedCard>(targets.CardCount);
        var labelsByBoard = labelRefs.GroupBy(label => label.BoardId).ToDictionary(group => group.Key, group => group.ToList());
        var listsByBoard = listRefs.GroupBy(list => list.BoardId).ToDictionary(group => group.Key, group => group.ToList());
        var cardDistribution = Distribute(targets.CardCount, boardRefs.Count);
        var cardOrdinal = 0;

        for (var boardIndex = 0; boardIndex < boardRefs.Count; boardIndex++)
        {
            var board = boardRefs[boardIndex];
            var boardLists = listsByBoard[board.Id];
            var cardsOnBoard = cardDistribution[boardIndex];
            var cardsByList = Distribute(cardsOnBoard, boardLists.Count);

            for (var listIndex = 0; listIndex < boardLists.Count; listIndex++)
            {
                var list = boardLists[listIndex];
                for (var cardIndex = 0; cardIndex < cardsByList[listIndex]; cardIndex++)
                {
                    var creator = users[(cardOrdinal + boardIndex + cardIndex) % users.Count];
                    var card = Card.Create(
                        list.Id,
                        creator.Id,
                        $"Seed Card {cardOrdinal + 1:D6}",
                        (cardIndex + 1) * 1024d);

                    card.UpdateDescription($"Seed card {cardOrdinal + 1:D6} generated for board {boardIndex + 1:D5}.");
                    card.UpdatePriority(Pick(new CardPriority?[] { CardPriority.Low, CardPriority.Medium, CardPriority.High, CardPriority.Urgent }));
                    card.UpdateStatus(Pick(new[] { CardStatus.Open, CardStatus.InProgress, CardStatus.InReview, CardStatus.Done }));
                    card.UpdateCover(ToJson(new
                    {
                        type = "color",
                        value = Pick(new[] { "#E0F2FE", "#DCFCE7", "#FEF3C7", "#FCE7F3" })
                    }));

                    cards.Add(card);
                    cardRefs.Add(new SeedCard(card.Id, list.Id, board.Id, board.WorkspaceId, creator.Id, cardOrdinal));
                    cardOrdinal++;
                }
            }
        }

        await AddInBatchesAsync(cards, 2_000);

        var cardMembers = cardRefs.Select(card =>
        {
            var members = membersByWorkspace.TryGetValue(card.WorkspaceId, out var workspaceMembers) && workspaceMembers.Count > 0
                ? workspaceMembers
                : users.Select(user => user.Id).ToList();

            return CardMember.Create(card.Id, members[card.Ordinal % members.Count], card.CreatedByUserId);
        });

        await AddInBatchesAsync(cardMembers, 3_000);

        var cardLabels = cardRefs.SelectMany(card =>
        {
            var labelsForBoard = labelsByBoard[card.BoardId];
            return new[]
            {
                CardLabel.Create(card.Id, labelsForBoard[card.Ordinal % labelsForBoard.Count].Id),
                CardLabel.Create(card.Id, labelsForBoard[(card.Ordinal + 1) % labelsForBoard.Count].Id)
            };
        });

        await AddInBatchesAsync(cardLabels, 4_000);

        var checklistRefs = new List<SeedChecklist>();
        var checklists = cardRefs
            .Where(card => card.Ordinal % 5 == 0)
            .Select(card =>
            {
                var checklist = Checklist.Create(card.Id, "Definition of done", 1024d);
                checklistRefs.Add(new SeedChecklist(checklist.Id, card.Id));
                return checklist;
            });

        await AddInBatchesAsync(checklists, 2_000);

        var checklistItems = checklistRefs.SelectMany((checklist, checklistIndex) =>
            Enumerable.Range(1, 4).Select(index =>
            {
                var item = ChecklistItem.Create(
                    checklist.Id,
                    $"Seed checklist item {index}",
                    index * 1024d,
                    users[(checklistIndex + index) % users.Count].Id);

                if ((checklistIndex + index) % 3 == 0)
                {
                    item.Check();
                }

                return item;
            }));

        await AddInBatchesAsync(checklistItems, 4_000);

        var cardLinks = cardRefs
            .Where((_, index) => index % 25 == 0 && index + 1 < cardRefs.Count)
            .Select((card, index) => CardLink.Create(
                card.Id,
                cardRefs[index * 25 + 1].Id,
                Pick(new[] { CardLinkType.RelatesTo, CardLinkType.Blocks, CardLinkType.DuplicateOf }),
                card.CreatedByUserId));

        await AddInBatchesAsync(cardLinks, 1_000);

        return new BoardSeedData(boardRefs, listRefs, cardRefs, labelRefs);
    }

    private async Task SeedPermissionsAsync(
        IReadOnlyList<SeedUser> users,
        IReadOnlyList<SeedWorkspace> workspaces,
        IReadOnlyList<SeedPage> pages,
        IReadOnlyList<SeedBoard> boards)
    {
        var pagesByWorkspace = pages.GroupBy(page => page.WorkspaceId).ToDictionary(group => group.Key, group => group.First());
        var boardsByWorkspace = boards.GroupBy(board => board.WorkspaceId).ToDictionary(group => group.Key, group => group.First());

        var permissions = workspaces.SelectMany((workspace, index) =>
        {
            var user = users[(index + 1) % users.Count];
            var workspacePermissions = new List<Permission>();

            if (pagesByWorkspace.TryGetValue(workspace.Id, out var page))
            {
                workspacePermissions.Add(Permission.CreateForUser(
                    workspace.Id,
                    ResourceType.Page,
                    page.Id,
                    user.Id,
                    PermissionLevel.Editor));
            }

            if (boardsByWorkspace.TryGetValue(workspace.Id, out var board))
            {
                workspacePermissions.Add(Permission.CreateForUser(
                    workspace.Id,
                    ResourceType.Board,
                    board.Id,
                    user.Id,
                    PermissionLevel.Commenter));
            }

            return workspacePermissions;
        });

        await AddInBatchesAsync(permissions, 1_000);
    }

    private async Task SeedCalendarAsync(
        SeedTargets targets,
        IReadOnlyList<SeedUser> users,
        IReadOnlyList<SeedWorkspace> workspaces,
        IReadOnlyList<SeedCard> cards)
    {
        var integrationCount = Math.Min(users.Count, Math.Max(workspaces.Count, targets.UserCount / 5));
        var integrations = Enumerable.Range(0, integrationCount).Select(index =>
        {
            var user = users[index % users.Count];
            return CalendarIntegration.Create(
                user.Id,
                CalendarProvider.Google,
                $"seed-access-token-{index + 1:D5}",
                $"seed-refresh-token-{index + 1:D5}",
                workspaces[index % workspaces.Count].Id,
                index % 3 == 0 ? SyncDirection.Pull : SyncDirection.Both);
        }).ToList();

        var integrationRefs = integrations.Select(integration =>
            new SeedCalendarIntegration(integration.Id, integration.UserId, integration.WorkspaceId)).ToList();

        await AddInBatchesAsync(integrations, 1_000);

        var eventCount = Math.Min(cards.Count, Math.Max(integrationRefs.Count, targets.CardCount / 20));
        var calendarEvents = Enumerable.Range(0, eventCount).Select(index =>
        {
            var integration = integrationRefs[index % integrationRefs.Count];
            var card = cards[(index * 7) % cards.Count];
            return CalendarEvent.Create(
                integration.Id,
                $"seed-calendar-event-{index + 1:D6}",
                ResourceType.Card,
                card.Id,
                $"seed-sync-hash-{index + 1:D6}");
        });

        await AddInBatchesAsync(calendarEvents, 2_000);
    }

    private async Task<List<SeedComment>> SeedCollaborationAsync(
        SeedTargets targets,
        IReadOnlyList<SeedUser> users,
        IReadOnlyList<SeedWorkspace> workspaces,
        IReadOnlyList<SeedPage> pages,
        IReadOnlyList<SeedCard> cards)
    {
        var commentCount = Math.Max(workspaces.Count, targets.CardCount / 10);
        var comments = Enumerable.Range(0, commentCount).Select(index =>
        {
            var card = cards[(index * 3) % cards.Count];
            return Comment.Create(
                card.WorkspaceId,
                ResourceType.Card,
                card.Id,
                users[index % users.Count].Id,
                $"Seed comment {index + 1:D6} on card {card.Ordinal + 1:D6}.");
        }).ToList();

        var commentRefs = comments
            .Select(comment => new SeedComment(comment.Id, comment.WorkspaceId, comment.ResourceType, comment.ResourceId))
            .ToList();

        await AddInBatchesAsync(comments, 2_000);

        var attachmentCount = Math.Max(workspaces.Count, targets.CardCount / 20);
        var attachments = Enumerable.Range(0, attachmentCount).Select(index =>
        {
            var card = cards[(index * 5) % cards.Count];
            var workspace = workspaces.First(item => item.Id == card.WorkspaceId);
            return Attachment.Create(
                card.WorkspaceId,
                ResourceType.Card,
                card.Id,
                users[index % users.Count].Id,
                $"seed-attachment-{index + 1:D6}.pdf",
                $"https://r2.notrelix.example/{workspace.Slug}/cards/{card.Id:N}/seed-attachment-{index + 1:D6}.pdf",
                24_576 + index,
                "application/pdf");
        });

        await AddInBatchesAsync(attachments, 1_000);

        var reactionCount = Math.Max(commentRefs.Count, targets.CardCount / 20);
        var reactions = Enumerable.Range(0, reactionCount).Select(index =>
        {
            var comment = commentRefs[index % commentRefs.Count];
            return Reaction.Create(
                ResourceType.Comment,
                comment.Id,
                users[index % users.Count].Id,
                Pick(new[] { "+1", "heart", "eyes", "rocket" }));
        });

        await AddInBatchesAsync(reactions, 2_000);

        var notificationCount = Math.Max(users.Count, targets.UserCount * 3);
        var notifications = Enumerable.Range(0, notificationCount).Select(index =>
        {
            var user = users[index % users.Count];
            var card = cards[(index * 11) % cards.Count];
            return Notification.Create(
                card.WorkspaceId,
                user.Id,
                "seed.card.updated",
                card.CreatedByUserId,
                ToJson(new
                {
                    title = $"Seed Card {card.Ordinal + 1:D6}",
                    action = "updated",
                    seed = true
                }),
                ResourceType.Card,
                card.Id);
        });

        await AddInBatchesAsync(notifications, 2_000);

        return commentRefs;
    }

    private async Task SeedActivityLogsAsync(
        SeedTargets targets,
        IReadOnlyList<SeedUser> users,
        IReadOnlyList<SeedWorkspace> workspaces,
        IReadOnlyList<SeedPage> pages,
        IReadOnlyList<SeedCard> cards,
        IReadOnlyList<SeedComment> comments)
    {
        var activityCount = Math.Max(workspaces.Count * 10, targets.CardCount / 2);
        var activityLogs = Enumerable.Range(0, activityCount).Select(index =>
        {
            if (index % 5 == 0 && pages.Count > 0)
            {
                var page = pages[index % pages.Count];
                return ActivityLog.Create(
                    page.WorkspaceId,
                    users[index % users.Count].Id,
                    "page.updated",
                    ResourceType.Page,
                    page.Id,
                    $"Seed page {index + 1:D6}",
                    ToJson(new { section = "docs", seed = true, index }),
                    $"10.30.{index / 255}.{index % 255 + 1}");
            }

            if (index % 7 == 0 && comments.Count > 0)
            {
                var comment = comments[index % comments.Count];
                return ActivityLog.Create(
                    comment.WorkspaceId,
                    users[index % users.Count].Id,
                    "comment.created",
                    ResourceType.Comment,
                    comment.Id,
                    "Seed comment",
                    ToJson(new { section = "collaboration", seed = true, index }),
                    $"10.31.{index / 255}.{index % 255 + 1}");
            }

            var card = cards[index % cards.Count];
            return ActivityLog.Create(
                card.WorkspaceId,
                users[index % users.Count].Id,
                "card.updated",
                ResourceType.Card,
                card.Id,
                $"Seed Card {card.Ordinal + 1:D6}",
                ToJson(new { section = "boards", seed = true, index }),
                $"10.32.{index / 255}.{index % 255 + 1}");
        });

        await AddInBatchesAsync(activityLogs, 4_000);
    }

    private async Task AddInBatchesAsync<T>(IEnumerable<T> entities, int batchSize)
        where T : class
    {
        foreach (var batch in entities.Chunk(batchSize))
        {
            _context.Set<T>().AddRange(batch);
            await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear();
        }
    }

    private static int[] Distribute(int total, int buckets)
    {
        if (buckets <= 0)
        {
            return [];
        }

        var result = new int[buckets];
        var baseline = total / buckets;
        var remainder = total % buckets;

        for (var index = 0; index < buckets; index++)
        {
            result[index] = baseline + (index < remainder ? 1 : 0);
        }

        return result;
    }

    private static string PickListTitle(int index) => index switch
    {
        1 => "Backlog",
        2 => "Ready",
        3 => "In progress",
        4 => "Review",
        _ => "Done"
    };

    private T Pick<T>(IReadOnlyList<T> source)
    {
        return source[_random.Next(source.Count)];
    }

    private static void EnsureDefaultWorkspaceMembers(Workspace workspace, IReadOnlyList<SeedUser> users)
    {
        foreach (var account in DefaultLoginAccounts)
        {
            var user = users.FirstOrDefault(seedUser => seedUser.Email == account.Email);
            if (user is null || workspace.OwnerId == user.Id || workspace.IsMember(user.Id))
            {
                continue;
            }

            workspace.AddMember(
                user.Id,
                workspace.IsPersonal ? WorkspaceRole.Guest : account.TeamRole);
        }
    }

    private static string ToJson<T>(T value)
    {
        return JsonSerializer.Serialize(value);
    }

    private sealed record SeedUser(Guid Id, string Email, string Name);
    private sealed record SeedWorkspace(Guid Id, string Slug, Guid OwnerId);
    private sealed record SeedPage(Guid Id, Guid WorkspaceId, Guid CreatedByUserId);
    private sealed record SeedBlock(Guid Id, Guid PageId);
    private sealed record SeedBoard(Guid Id, Guid WorkspaceId, Guid CreatedByUserId);
    private sealed record SeedList(Guid Id, Guid BoardId, Guid WorkspaceId);
    private sealed record SeedLabel(Guid Id, Guid BoardId);
    private sealed record SeedCard(Guid Id, Guid ListId, Guid BoardId, Guid WorkspaceId, Guid CreatedByUserId, int Ordinal);
    private sealed record SeedChecklist(Guid Id, Guid CardId);
    private sealed record SeedComment(Guid Id, Guid WorkspaceId, ResourceType ResourceType, Guid ResourceId);
    private sealed record SeedCalendarIntegration(Guid Id, Guid UserId, Guid? WorkspaceId);
    private sealed record SeedLoginAccount(string Email, string Name, WorkspaceRole TeamRole);

    private sealed record WorkspaceSeedData(
        List<SeedWorkspace> Workspaces,
        Dictionary<Guid, List<Guid>> MembersByWorkspace);

    private sealed record DocumentSeedData(
        List<SeedPage> Pages,
        List<SeedBlock> Blocks);

    private sealed record BoardSeedData(
        List<SeedBoard> Boards,
        List<SeedList> Lists,
        List<SeedCard> Cards,
        List<SeedLabel> Labels);
}
