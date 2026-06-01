using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Notrelix.Application.Common.Interfaces;
using Notrelix.Infrastructure.Data;

namespace Notrelix.Infrastructure.Tests.Data;

public class SeedDataInitialiserTests
{
    [Fact]
    public void Seed_profile_mapping_uses_exact_top_level_targets()
    {
        SeedTargets.ForProfile(SeedProfile.Small).Should().Be(new SeedTargets(5, 10, 20, 500, 100));
        SeedTargets.ForProfile(SeedProfile.Medium).Should().Be(new SeedTargets(20, 100, 100, 10_000, 2_000));
        SeedTargets.ForProfile(SeedProfile.Large).Should().Be(new SeedTargets(100, 1_000, 1_000, 100_000, 20_000));
    }

    [Fact]
    public async Task Small_profile_creates_required_records_across_major_tables()
    {
        await using var context = CreateContext();
        var initialiser = CreateInitialiser(context);

        await initialiser.SeedAsync();

        (await context.Users.CountAsync()).Should().Be(10);
        (await context.UserProfiles.CountAsync()).Should().Be(10);
        (await context.Sessions.CountAsync()).Should().Be(10);
        (await context.Workspaces.CountAsync()).Should().Be(5);
        (await context.WorkspaceMembers.CountAsync()).Should().BeGreaterThan(5);
        (await context.WorkspaceInvitations.CountAsync()).Should().BeGreaterThan(0);
        (await context.Pages.CountAsync()).Should().Be(100);
        (await context.Blocks.CountAsync()).Should().BeGreaterThan(100);
        (await context.Boards.CountAsync()).Should().Be(20);
        (await context.BoardLists.CountAsync()).Should().Be(100);
        (await context.BoardColumns.CountAsync()).Should().Be(120);
        (await context.BoardViews.CountAsync()).Should().BeGreaterThan(20);
        (await context.Labels.CountAsync()).Should().Be(120);
        (await context.Cards.CountAsync()).Should().Be(500);
        (await context.CardMembers.CountAsync()).Should().Be(500);
        (await context.CardLabels.CountAsync()).Should().Be(1_000);
        (await context.Checklists.CountAsync()).Should().Be(100);
        (await context.ChecklistItems.CountAsync()).Should().Be(400);
        (await context.CardLinks.CountAsync()).Should().BeGreaterThan(0);
        (await context.Comments.CountAsync()).Should().BeGreaterThan(0);
        (await context.Attachments.CountAsync()).Should().BeGreaterThan(0);
        (await context.Notifications.CountAsync()).Should().BeGreaterThan(0);
        (await context.ActivityLogs.CountAsync()).Should().BeGreaterThan(0);
        (await context.CalendarIntegrations.CountAsync()).Should().BeGreaterThan(0);
        (await context.CalendarEvents.CountAsync()).Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Seed_without_reset_is_idempotent_when_sentinel_exists()
    {
        await using var context = CreateContext();
        var initialiser = CreateInitialiser(context);

        await initialiser.SeedAsync();
        var countsAfterFirstSeed = await SnapshotCountsAsync(context);

        await initialiser.SeedAsync();
        var countsAfterSecondSeed = await SnapshotCountsAsync(context);

        countsAfterSecondSeed.Should().BeEquivalentTo(countsAfterFirstSeed);
    }

    [Fact]
    public async Task Seed_without_reset_backfills_board_metadata_when_sentinel_exists()
    {
        await using var context = CreateContext();

        await CreateInitialiser(context).SeedAsync();

        context.BoardColumns.RemoveRange(await context.BoardColumns.ToListAsync());
        await context.SaveChangesAsync();
        (await context.BoardColumns.CountAsync()).Should().Be(0);

        await CreateInitialiser(context).SeedAsync();

        (await context.BoardColumns.CountAsync()).Should().Be(120);
    }

    [Fact]
    public async Task Default_accounts_can_access_seed_workspaces_after_seed()
    {
        await using var context = CreateContext();

        await CreateInitialiser(context).SeedAsync();

        var defaultEmails = new List<string>
        {
            "admin@notrelix.com",
            "demo@notrelix.com",
            "member@notrelix.com"
        };

        var users = await context.Users
            .Where(user => defaultEmails.Contains(user.Email.Value))
            .Select(user => new { user.Id, user.Email.Value, user.PasswordHash })
            .ToListAsync();

        users.Should().HaveCount(3);
        users.Should().OnlyContain(user => user.PasswordHash == "hashed-Notrelix@123");

        var workspaceCount = await context.Workspaces.CountAsync();
        foreach (var user in users)
        {
            var membershipCount = await context.WorkspaceMembers
                .CountAsync(member => member.UserId == user.Id);

            membershipCount.Should().Be(workspaceCount);
        }
    }

    [Fact]
    public async Task Seed_with_reset_replaces_seed_owned_data_without_duplicates()
    {
        await using var context = CreateContext();

        await CreateInitialiser(context).SeedAsync();
        await CreateInitialiser(context, resetBeforeSeed: true).SeedAsync();

        (await context.Users.CountAsync(user => user.Email.Value == "admin@notrelix.com")).Should().Be(1);
        (await context.Users.CountAsync()).Should().Be(10);
        (await context.Workspaces.CountAsync()).Should().Be(5);
        (await context.Boards.CountAsync()).Should().Be(20);
        (await context.Cards.CountAsync()).Should().Be(500);
        (await context.Pages.CountAsync()).Should().Be(100);
        (await context.BoardColumns.CountAsync()).Should().Be(120);
    }

    [Fact]
    public async Task Seeded_list_card_and_page_positions_are_ordered_doubles()
    {
        await using var context = CreateContext();
        await CreateInitialiser(context).SeedAsync();

        var boardId = await context.Boards
            .OrderBy(board => board.Title)
            .Select(board => board.Id)
            .FirstAsync();

        var listPositions = await context.BoardLists
            .Where(list => list.BoardId == boardId)
            .OrderBy(list => list.Position)
            .Select(list => list.Position)
            .ToListAsync();

        listPositions.Should().BeInAscendingOrder();
        listPositions.Should().OnlyContain(position => position > 0);

        var listId = await context.BoardLists
            .Where(list => list.BoardId == boardId)
            .OrderBy(list => list.Position)
            .Select(list => list.Id)
            .FirstAsync();

        var cardPositions = await context.Cards
            .Where(card => card.ListId == listId)
            .OrderBy(card => card.Position)
            .Select(card => card.Position)
            .ToListAsync();

        cardPositions.Should().BeInAscendingOrder();
        cardPositions.Should().OnlyContain(position => position > 0);

        var pagePositions = await context.Pages
            .OrderBy(page => page.Position)
            .Select(page => page.Position)
            .Take(20)
            .ToListAsync();

        pagePositions.Should().BeInAscendingOrder();
        pagePositions.Should().OnlyContain(position => position > 0);
    }

    [Fact]
    public async Task Seeded_json_fields_and_attachment_urls_are_valid()
    {
        await using var context = CreateContext();
        await CreateInitialiser(context).SeedAsync();

        AssertJson(await context.Blocks.Select(block => block.Properties).Take(50).ToListAsync());
        AssertJson(await context.BoardColumns.Select(column => column.Settings).Take(50).ToListAsync());
        AssertJson(await context.BoardViews.Select(view => view.Filters).Take(50).ToListAsync());
        AssertJson(await context.Cards.Where(card => card.Cover != null).Select(card => card.Cover!).Take(50).ToListAsync());
        AssertJson(await context.Notifications.Select(notification => notification.Payload).Take(50).ToListAsync());
        AssertJson(await context.ActivityLogs.Select(activity => activity.Metadata).Take(50).ToListAsync());

        var attachmentUrls = await context.Attachments.Select(attachment => attachment.Url).Take(20).ToListAsync();
        attachmentUrls.Should().OnlyContain(url =>
            url.StartsWith("https://r2.notrelix.example/", StringComparison.Ordinal) &&
            !url.Contains("base64", StringComparison.OrdinalIgnoreCase));
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"Notrelix-seed-{Guid.NewGuid():N}")
            .Options;

        return new ApplicationDbContext(options);
    }

    private static ApplicationDbContextInitialiser CreateInitialiser(
        ApplicationDbContext context,
        bool resetBeforeSeed = false,
        SeedProfile profile = SeedProfile.Small)
    {
        return new ApplicationDbContextInitialiser(
            NullLogger<ApplicationDbContextInitialiser>.Instance,
            context,
            new DeterministicPasswordHasher(),
            Options.Create(new SeedDataOptions
            {
                Enabled = true,
                Profile = profile,
                ResetBeforeSeed = resetBeforeSeed
            }));
    }

    private static async Task<SeedCountSnapshot> SnapshotCountsAsync(ApplicationDbContext context)
    {
        return new SeedCountSnapshot(
            await context.Users.CountAsync(),
            await context.Workspaces.CountAsync(),
            await context.Boards.CountAsync(),
            await context.Cards.CountAsync(),
            await context.Pages.CountAsync(),
            await context.BoardColumns.CountAsync());
    }

    private static void AssertJson(IEnumerable<string> values)
    {
        foreach (var value in values)
        {
            using var _ = JsonDocument.Parse(value);
        }
    }

    private sealed record SeedCountSnapshot(
        int Users,
        int Workspaces,
        int Boards,
        int Cards,
        int Pages,
        int BoardColumns);

    private sealed class DeterministicPasswordHasher : IPasswordHasher
    {
        public string HashPassword(string password) => "hashed-" + password;
        public bool VerifyPassword(string password, string hashedPassword) => hashedPassword == HashPassword(password);
    }
}
