using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Notrelix.Application.Common.Abstractions;
using Notrelix.Infrastructure.Data;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Integration.Tests.Data;

public class SeedDataInitialiserTests
{
    [Fact]
    public void Seed_profile_mapping_uses_exact_top_level_targets()
    {
        SeedTargets.ForProfile(SeedProfile.Small).Should().Be(
            new SeedTargets(10, 5, 20, 80, 120, 400, 40, 60, 100, 500, 400, 30));
        SeedTargets.ForProfile(SeedProfile.Medium).Should().Be(
            new SeedTargets(50, 10, 50, 200, 400, 2_000, 100, 150, 500, 2_500, 4_000, 250));
        SeedTargets.ForProfile(SeedProfile.Large).Should().Be(
            new SeedTargets(200, 20, 200, 800, 1_600, 12_000, 400, 600, 2_000, 10_000, 24_000, 1_000));
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
        (await context.Pages.CountAsync()).Should().Be(100);
        (await context.Blocks.CountAsync()).Should().Be(500);
        (await context.Boards.CountAsync()).Should().Be(20);
        (await context.BoardGroups.CountAsync()).Should().Be(80);
        (await context.BoardFields.CountAsync()).Should().Be(120);
        (await context.BoardViews.CountAsync()).Should().Be(40);
        (await context.Labels.CountAsync()).Should().Be(60);
        (await context.BoardItems.CountAsync()).Should().Be(400);
        (await context.Comments.CountAsync()).Should().Be(400);
        (await context.Notifications.CountAsync()).Should().Be(30);
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
    public async Task Seed_is_idempotent_and_does_not_backfill_when_sentinel_exists()
    {
        await using var context = CreateContext();

        await CreateInitialiser(context).SeedAsync();

        var fields = await context.BoardFields.ToListAsync();
        context.BoardItemValues.RemoveRange(await context.BoardItemValues.ToListAsync());
        context.FieldOptions.RemoveRange(await context.FieldOptions.ToListAsync());
        context.BoardFields.RemoveRange(fields);
        await context.SaveChangesAsync();
        (await context.BoardFields.CountAsync()).Should().Be(0);

        await CreateInitialiser(context).SeedAsync();

        (await context.BoardFields.CountAsync()).Should().Be(0);
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

            membershipCount.Should().Be(workspaceCount - 1);
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
        (await context.BoardItems.CountAsync()).Should().Be(400);
        (await context.Pages.CountAsync()).Should().Be(100);
        (await context.BoardFields.CountAsync()).Should().Be(120);
    }

    [Fact]
    public async Task Seeded_group_and_item_positions_are_valid()
    {
        await using var context = CreateContext();
        await CreateInitialiser(context).SeedAsync();

        var boardId = await context.Boards
            .OrderBy(board => board.Title)
            .Select(board => board.Id)
            .FirstAsync();

        var groups = await context.BoardGroups
            .Where(g => g.BoardId == boardId)
            .OrderBy(g => g.Position.Value)
            .ToListAsync();

        groups.Should().OnlyContain(g => g.Position != null);
        groups.Select(g => g.Position.Value).Should().BeInAscendingOrder();

        var groupIds = groups.Select(g => g.Id).ToList();
        var items = await context.BoardItems
            .Where(item => groupIds.Contains(item.GroupId))
            .OrderBy(item => item.Position.Value)
            .ToListAsync();

        items.Should().OnlyContain(item => item.Position != null);
        items.Select(item => item.Position.Value).Should().BeInAscendingOrder();

        var blocks = await context.Blocks
            .OrderBy(b => b.Position.Value)
            .Take(20)
            .ToListAsync();

        blocks.Should().OnlyContain(block => block.Position != null);
        blocks.Select(b => b.Position.Value).Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task Seeded_entities_with_json_value_objects_exist()
    {
        await using var context = CreateContext();
        await CreateInitialiser(context).SeedAsync();

        (await context.Blocks.CountAsync()).Should().Be(500);
        (await context.BoardFields.CountAsync()).Should().Be(120);
        (await context.BoardViews.CountAsync()).Should().Be(40);
        (await context.Notifications.CountAsync()).Should().Be(30);
        (await context.Notifications.CountAsync(n => !string.IsNullOrWhiteSpace(n.Content))).Should().Be(30);
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"Notrelix-seed-{Guid.NewGuid():N}")
            .Options;

        var currentWorkspace = new FakeCurrentWorkspace();
        currentWorkspace.EnterSystemContext();
        return new TestApplicationDbContext(options, currentWorkspace);
    }

    private class TestApplicationDbContext : ApplicationDbContext
    {
        public TestApplicationDbContext(DbContextOptions<ApplicationDbContext> options, ICurrentWorkspace currentWorkspace)
            : base(options, currentWorkspace) { }
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
            await context.BoardItems.CountAsync(),
            await context.Pages.CountAsync(),
            await context.BoardFields.CountAsync());
    }

    private sealed record SeedCountSnapshot(
        int Users,
        int Workspaces,
        int Boards,
        int BoardItems,
        int Pages,
        int BoardFields);

    private sealed class DeterministicPasswordHasher : IPasswordHasher
    {
        public string HashPassword(string password) => "hashed-" + password;
        public bool VerifyPassword(string password, string hashedPassword) => hashedPassword == HashPassword(password);
    }
}
