using System.Text.Json;
using Notrelix.Application.Features.Workspaces.Onboarding.Commands.ClearOnboardingDraft;
using Notrelix.Application.Features.Workspaces.Onboarding.Commands.DismissGettingStarted;
using Notrelix.Application.Features.Workspaces.Onboarding.Commands.SaveOnboardingDraft;
using Notrelix.Application.Features.Workspaces.Onboarding.Queries.CheckWorkspaceSlug;
using Notrelix.Application.Features.Workspaces.Onboarding.Queries.GetGettingStarted;
using Notrelix.Application.Features.Workspaces.Onboarding.Queries.GetOnboardingDraft;
using Notrelix.Application.Features.Workspaces.Workspaces.Commands.CreateWorkspace;
using Notrelix.Application.Features.Workspaces.Workspaces.Queries.GetWorkspace;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Notrelix.Domain.SharedKernel;
using Notrelix.Domain.Workspaces.Onboarding;
using Notrelix.Infrastructure.BackgroundJobs;
using Notrelix.Domain.SharedKernel.Ordering;
using Notrelix.Domain.WorkManagement.BoardGroups;
using Notrelix.Domain.WorkManagement.Boards;
using Notrelix.Domain.WorkManagement.Items;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Domain.Workspaces.Workspaces;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Integration.Tests.Workspaces.OrgTree;
using AppForbidden = Notrelix.Application.Common.Exceptions.ForbiddenException;

namespace Notrelix.Integration.Tests.Workspaces;

/// <summary>
/// Onboarding owned by Workspaces on PostgreSQL through the real pipeline (NRX-003, NRX-004, NRX-012):
/// the address check is unique per Account only; the draft is personal (one per user, 30-day TTL, RLS on
/// the user); the getting-started card is derived from the Boards the caller can read and its dismissal
/// is stored per user and workspace.
/// </summary>
[Collection("Database")]
[Trait("Category", "Integration")]
public sealed class OnboardingIntegrationTests : IAsyncLifetime
{
    private readonly PostgresTestContainer _db;
    private readonly OrgTreeTestHost _host;

    public OnboardingIntegrationTests(PostgresTestContainer db)
    {
        _db = db;
        _host = new OrgTreeTestHost(db);
    }

    public async Task InitializeAsync() => await new DatabaseReset(_db.OwnerConnectionString).ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    // ── slug check ────────────────────────────────────────────────────────

    [Fact]
    public async Task SlugCheck_IsUniquePerAccountOnly_AndSuggestsTheNextFreeAddress()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var other = await _host.SeedWorkspaceAsync();
        await SeedWorkspaceSlugAsync(stack, "acme");
        await SeedWorkspaceSlugAsync(stack, "acme-2");

        var taken = await SlugAsync(stack, stack.OwnerId, "acme");
        var free = await SlugAsync(stack, stack.OwnerId, "acme-inc");
        var otherAccount = await SlugAsync(other, other.OwnerId, "acme");
        var normalized = await SlugAsync(stack, stack.OwnerId, "  ACME ");

        taken.Should().Be(new WorkspaceSlugCheckDto("taken", "acme-3"));
        normalized.Should().Be(taken);
        free.Should().Be(new WorkspaceSlugCheckDto("free", null));
        otherAccount.Should().Be(new WorkspaceSlugCheckDto("free", null), "an address is unique within the Account, not globally");
    }

    [Fact]
    public async Task SlugCheck_ADeletedWorkspaceReleasesItsAddress_AndABadSlugIsInvalid()
    {
        var stack = await _host.SeedWorkspaceAsync();
        await SeedWorkspaceSlugAsync(stack, "gone", deleted: true);

        (await SlugAsync(stack, stack.OwnerId, "gone")).Status.Should().Be("free");
        foreach (var bad in new[] { null, "", "  ", "-x", "a b", "a--b" })
            (await SlugAsync(stack, stack.OwnerId, bad)).Should().Be(
                new WorkspaceSlugCheckDto("invalid", null, "SharedKernel_Slug_InvalidFormat"), $"'{bad}'");
    }

    [Fact]
    public async Task SlugCheck_EveryReservedWordIsInvalid_EvenWhenNoWorkspaceHoldsIt()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var reserved = new[]
        {
            "admin", "api", "app", "www", "settings", "login", "signup", "billing", "support", "help",
            "new", "invite", "join", "onboarding", "workspaces",
        };

        foreach (var word in reserved)
            (await SlugAsync(stack, stack.OwnerId, word)).Should().Be(
                new WorkspaceSlugCheckDto("invalid", null, "Workspaces_Workspace_SlugReserved"), word);
        (await SlugAsync(stack, stack.OwnerId, " Admin ")).Reason.Should().Be("Workspaces_Workspace_SlugReserved");
        (await SlugAsync(stack, stack.OwnerId, "admin-team")).Status.Should().Be("free", "only the exact word is reserved");
    }

    [Fact]
    public async Task SlugCheck_LengthIsThreeToSixtyThree()
    {
        var stack = await _host.SeedWorkspaceAsync();

        (await SlugAsync(stack, stack.OwnerId, "ab")).Should().Be(new WorkspaceSlugCheckDto("invalid", null, "Workspaces_Workspace_SlugTooShort"));
        (await SlugAsync(stack, stack.OwnerId, "abc")).Status.Should().Be("free");
        (await SlugAsync(stack, stack.OwnerId, new string('x', 63))).Status.Should().Be("free");
        (await SlugAsync(stack, stack.OwnerId, new string('x', 64))).Should().Be(new WorkspaceSlugCheckDto("invalid", null, "Workspaces_Workspace_SlugTooLong"));
        (await SlugAsync(stack, stack.OwnerId, new string('x', 129))).Reason.Should().Be("Workspaces_Workspace_SlugTooLong");
    }

    [Fact]
    public async Task CreateWorkspace_EnforcesTheAddressRule_StoresSizeAndLanguage_AndLeavesLegacyAddressesWorking()
    {
        var stack = await _host.SeedWorkspaceAsync();
        // The seeded workspace now holds an address the rule would refuse (it predates the rule).
        await _host.WithSystemContextAsync(async c =>
            typeof(Workspace).GetProperty(nameof(Workspace.Slug))!.SetValue(await c.Workspaces.SingleAsync(w => w.Id == stack.WorkspaceId), "ab"));
        using var provider = _host.CreateProvider(stack.AccountId, stack.OwnerId, services =>
            services.AddScoped<Notrelix.Application.Features.Workspaces.Members.Services.IWorkspaceGrantProjectionService>(sp =>
                new Notrelix.Infrastructure.Data.Authz.WorkspaceGrantProjectionServiceAdapter(
                    new Notrelix.Infrastructure.Data.Authz.AccessGrantProjectionService(sp.GetRequiredService<Notrelix.Infrastructure.Data.ApplicationDbContext>()))));

        var reserved = () => OrgTreeTestHost.QueryAsync(provider, new CreateWorkspaceCommand("Acme", null, false, Slug: "admin"));
        var taken = () => OrgTreeTestHost.QueryAsync(provider, new CreateWorkspaceCommand("Acme", null, false, Slug: "ab"));
        await reserved.Should().ThrowAsync<Exception>();
        await taken.Should().ThrowAsync<Exception>();
        (await _host.ReadAsync(c => c.Workspaces.CountAsync())).Should().Be(1, "a refused creation leaves nothing behind");

        var id = await OrgTreeTestHost.QueryAsync(provider, new CreateWorkspaceCommand("Acme", null, false, Slug: "acme", Size: "11-50", Language: "vi"));
        var created = await _host.ReadAsync(c => c.Workspaces.AsNoTracking().SingleAsync(w => w.Id == id));
        (created.Slug, created.Settings.Size, created.Settings.Language).Should().Be(("acme", "11-50", "vi"));

        var derived = await OrgTreeTestHost.QueryAsync(provider, new CreateWorkspaceCommand("Help", null, false));
        (await _host.ReadAsync(c => c.Workspaces.AsNoTracking().SingleAsync(w => w.Id == derived))).Slug.Should().StartWith("help-");

        var legacy = await OrgTreeTestHost.QueryAsync(provider, new GetWorkspaceQuery(stack.WorkspaceId));
        legacy.Slug.Should().Be("ab", "a workspace created before the rule keeps its address and keeps working");
        (await SlugAsync(stack, stack.OwnerId, "ab")).Reason.Should().Be("Workspaces_Workspace_SlugTooShort");
    }

    [Fact]
    public async Task SlugCheck_APersonOutsideTheAccountIsRefused_AndLearnsNothingAboutItsAddresses()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var other = await _host.SeedWorkspaceAsync();
        await SeedWorkspaceSlugAsync(stack, "acme");

        var act = () => SlugAsync(stack, other.OwnerId, "acme");

        await act.Should().ThrowAsync<AppForbidden>();
    }

    // ── draft ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Draft_IsSavedReplacedReadAndDropped_PerUser()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var workspaceId = Guid.NewGuid();
        using var provider = _host.CreateProvider(stack.AccountId, stack.OwnerId);

        (await OrgTreeTestHost.QueryAsync(provider, new GetOnboardingDraftQuery())).Draft.Should().BeNull();

        await OrgTreeTestHost.SendAsync(provider, new SaveOnboardingDraftCommand("company", null, Json("{\"name\":\"Acme\"}")));
        await OrgTreeTestHost.SendAsync(provider, new SaveOnboardingDraftCommand("departments", workspaceId, Json("{\"names\":[\"Sales\"]}")));

        var draft = (await OrgTreeTestHost.QueryAsync(provider, new GetOnboardingDraftQuery())).Draft!;
        draft.Step.Should().Be("departments");
        draft.WorkspaceId.Should().Be(workspaceId);
        draft.Data.GetProperty("names")[0].GetString().Should().Be("Sales");
        (await _host.ReadAsync(c => c.OnboardingDrafts.CountAsync())).Should().Be(1, "one draft per user");

        await OrgTreeTestHost.SendAsync(provider, new ClearOnboardingDraftCommand());
        await OrgTreeTestHost.SendAsync(provider, new ClearOnboardingDraftCommand());
        (await OrgTreeTestHost.QueryAsync(provider, new GetOnboardingDraftQuery())).Draft.Should().BeNull();
    }

    [Fact]
    public async Task Draft_OfAnotherUserIsInvisibleAndUntouchable_ByTheHandler()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var other = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Other");
        using var asOwner = _host.CreateProvider(stack.AccountId, stack.OwnerId);
        using var asOther = _host.CreateProvider(stack.AccountId, other);
        await OrgTreeTestHost.SendAsync(asOwner, new SaveOnboardingDraftCommand("company", null, Json("{\"secret\":1}")));

        (await OrgTreeTestHost.QueryAsync(asOther, new GetOnboardingDraftQuery())).Draft.Should().BeNull();
        await OrgTreeTestHost.SendAsync(asOther, new ClearOnboardingDraftCommand());
        await OrgTreeTestHost.SendAsync(asOther, new SaveOnboardingDraftCommand("invites", null, Json("{}")));

        (await OrgTreeTestHost.QueryAsync(asOwner, new GetOnboardingDraftQuery())).Draft!.Step.Should().Be("company");
        (await _host.ReadAsync(c => c.OnboardingDrafts.CountAsync())).Should().Be(2);
    }

    [Fact]
    public async Task Draft_ExpiresThirtyDaysAfterTheLastSave_AndAnInvalidSaveChangesNothing()
    {
        var stack = await _host.SeedWorkspaceAsync();
        using var provider = _host.CreateProvider(stack.AccountId, stack.OwnerId);
        await OrgTreeTestHost.SendAsync(provider, new SaveOnboardingDraftCommand("company", null, Json("{\"a\":1}")));

        var invalid = () => OrgTreeTestHost.SendAsync(provider, new SaveOnboardingDraftCommand("company", null, Json("[1]")));
        await invalid.Should().ThrowAsync<Exception>();
        (await OrgTreeTestHost.QueryAsync(provider, new GetOnboardingDraftQuery())).Draft!.Data.GetProperty("a").GetInt32().Should().Be(1);

        await _host.WithSystemContextAsync(async c =>
        {
            var row = await c.OnboardingDrafts.SingleAsync();
            typeof(Notrelix.Domain.Workspaces.Onboarding.OnboardingDraft).GetProperty(nameof(row.ExpiresAt))!.SetValue(row, _host.Now.AddSeconds(-1));
        });
        (await OrgTreeTestHost.QueryAsync(provider, new GetOnboardingDraftQuery())).Draft.Should().BeNull("an expired draft reads as absent");

        await OrgTreeTestHost.SendAsync(provider, new SaveOnboardingDraftCommand("invites", null, Json("{}")));
        (await OrgTreeTestHost.QueryAsync(provider, new GetOnboardingDraftQuery())).Draft!.Step.Should().Be("invites");
        (await _host.ReadAsync(c => c.OnboardingDrafts.CountAsync())).Should().Be(1, "saving over an expired draft reuses the one row");
    }

    // ── draft sweep ───────────────────────────────────────────────────────

    [Fact]
    public async Task DraftSweep_DeletesOnlyExpiredDrafts_InBatches_UnderTheWorkerScope_AndASecondPassFindsNothing()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var now = _host.Now;
        var expiredUsers = Enumerable.Range(0, 3).Select(_ => Guid.NewGuid()).ToList();
        var boundaryUser = Guid.NewGuid();
        var freshUser = Guid.NewGuid();
        var almostUser = Guid.NewGuid();
        await _host.WithSystemContextAsync(c =>
        {
            foreach (var user in expiredUsers)
                c.OnboardingDrafts.Add(OnboardingDraft.Create(user, "company", null, null, now.AddDays(-31)));
            // Expires exactly now: expired, because a draft reads as absent from its expiry instant.
            c.OnboardingDrafts.Add(OnboardingDraft.Create(boundaryUser, "company", null, null, now - OnboardingDraft.Retention));
            c.OnboardingDrafts.Add(OnboardingDraft.Create(freshUser, "company", null, null, now));
            c.OnboardingDrafts.Add(OnboardingDraft.Create(almostUser, "company", null, null, now - OnboardingDraft.Retention + TimeSpan.FromSeconds(1)));
            return Task.CompletedTask;
        });
        using var provider = _host.CreateProvider(stack.AccountId, stack.OwnerId, services =>
        {
            services.AddSingleton<IOptions<OnboardingDraftSweepOptions>>(Options.Create(new OnboardingDraftSweepOptions { BatchSize = 2 }));
            services.AddScoped<OnboardingDraftSweeper>();
        });

        int first, second;
        using (var scope = provider.CreateScope())
            first = await scope.ServiceProvider.GetRequiredService<OnboardingDraftSweeper>().RunOnceAsync(CancellationToken.None);
        using (var scope = provider.CreateScope())
            second = await scope.ServiceProvider.GetRequiredService<OnboardingDraftSweeper>().RunOnceAsync(CancellationToken.None);

        first.Should().Be(4, "three long-expired drafts and the one expiring this very instant, in batches of two");
        second.Should().Be(0);
        (await _host.ReadAsync(c => c.OnboardingDrafts.Select(d => d.UserId).ToListAsync()))
            .Should().BeEquivalentTo([freshUser, almostUser]);
    }

    [Fact]
    public void DraftSweepOptions_DefaultToAnHourlyBatchedSweep_AndAreValidated()
    {
        var defaults = new OnboardingDraftSweepOptions();
        defaults.Enabled.Should().BeTrue();
        defaults.Interval.Should().Be(TimeSpan.FromHours(1));
        defaults.BatchSize.Should().Be(500);

        var validator = new OnboardingDraftSweepOptionsValidator();
        validator.Validate(null, defaults).Succeeded.Should().BeTrue();
        validator.Validate(null, new OnboardingDraftSweepOptions { Interval = TimeSpan.FromSeconds(1) }).Failed.Should().BeTrue();
        validator.Validate(null, new OnboardingDraftSweepOptions { Interval = TimeSpan.FromDays(2) }).Failed.Should().BeTrue();
        validator.Validate(null, new OnboardingDraftSweepOptions { BatchSize = 0 }).Failed.Should().BeTrue();
        validator.Validate(null, new OnboardingDraftSweepOptions { BatchSize = 10_001 }).Failed.Should().BeTrue();
    }

    // ── getting started ───────────────────────────────────────────────────

    [Fact]
    public async Task GettingStarted_FirstItemIsDerivedFromReadableBoards_AndAHiddenBoardNeverMakesItDone()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var member = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Member");
        var empty = await _host.SeedBoardAsync(stack, "Empty board");
        var secret = await _host.SeedBoardAsync(stack, "Secret", BoardVisibility.Private);
        await SeedItemAsync(stack, secret);

        var before = (await GettingStartedAsync(stack, member)).Items.Single(i => i.Key == "first-item");
        before.Should().Be(new GettingStartedItemDto("first-item", false, empty, "Empty board"));

        var shared = await _host.SeedBoardAsync(stack, "Shared");
        await SeedItemAsync(stack, shared);
        var after = await GettingStartedAsync(stack, member);

        after.Items.Single(i => i.Key == "first-item").Should().Be(new GettingStartedItemDto("first-item", true, shared, "Shared"));
        after.Dismissed.Should().BeFalse();
        (await GettingStartedAsync(stack, stack.OwnerId)).Items.Single(i => i.Key == "first-item").Done.Should().BeTrue("the owner reads the secret board too");
    }

    [Fact]
    public async Task GettingStarted_ServesFirstItemAndInviteMore_NeverTheMobileItem_ToEveryMember()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var member = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Member");
        var guest = await _host.AddMemberAsync(stack, WorkspaceRole.Guest, "Guest");

        foreach (var user in new[] { stack.OwnerId, member, guest })
        {
            var card = await GettingStartedAsync(stack, user);
            card.Items.Select(i => i.Key).Should().Equal("first-item", "invite-more");
            card.Items.Should().NotContain(i => i.Key == "mobile");
            card.Visible.Should().BeTrue();
            card.Dismissed.Should().BeFalse();
        }
    }

    [Fact]
    public async Task GettingStarted_InviteMoreIsDoneOnceAnyInvitationWasSent_WhateverBecameOfIt()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var other = await _host.SeedWorkspaceAsync();
        await SeedInvitationAsync(other, "elsewhere@example.com");

        (await GettingStartedAsync(stack, stack.OwnerId)).Items.Single(i => i.Key == "invite-more")
            .Should().Be(new GettingStartedItemDto("invite-more", false, null, null), "another workspace's invitation does not count");

        await SeedInvitationAsync(stack, "first@example.com", revoke: true);

        (await GettingStartedAsync(stack, stack.OwnerId)).Items.Single(i => i.Key == "invite-more").Done.Should().BeTrue("a revoked invitation was still sent");
    }

    [Fact]
    public async Task GettingStarted_IsHiddenWhenDismissedOrWhenTheFirstItemIsDone_NotByInviteMore()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var member = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Member");
        await SeedInvitationAsync(stack, "first@example.com");

        var invited = await GettingStartedAsync(stack, member);
        invited.Items.Single(i => i.Key == "invite-more").Done.Should().BeTrue();
        invited.Visible.Should().BeTrue("invite-more alone never hides the card");

        using var asMember = _host.CreateProvider(stack.AccountId, member);
        await OrgTreeTestHost.SendAsync(asMember, new DismissGettingStartedCommand(stack.WorkspaceId));
        var dismissed = await GettingStartedAsync(stack, member);
        dismissed.Visible.Should().BeFalse();
        dismissed.Dismissed.Should().BeTrue();

        var board = await _host.SeedBoardAsync(stack, "Work");
        await SeedItemAsync(stack, board);
        var done = await GettingStartedAsync(stack, stack.OwnerId);
        done.Dismissed.Should().BeFalse();
        done.Visible.Should().BeFalse("the first item is done");
    }

    [Fact]
    public async Task GettingStarted_DismissalIsPerUserAndWorkspace_AndRepeatingItIsANoOp()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var other = await _host.SeedWorkspaceAsync();
        var member = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Member");
        using var asMember = _host.CreateProvider(stack.AccountId, member);

        await OrgTreeTestHost.SendAsync(asMember, new DismissGettingStartedCommand(stack.WorkspaceId));
        await OrgTreeTestHost.SendAsync(asMember, new DismissGettingStartedCommand(stack.WorkspaceId));

        (await GettingStartedAsync(stack, member)).Dismissed.Should().BeTrue();
        (await GettingStartedAsync(stack, stack.OwnerId)).Dismissed.Should().BeFalse("dismissal is the member's own");
        (await _host.ReadAsync(c => c.GettingStartedDismissals.CountAsync())).Should().Be(1);

        var crossWorkspace = () => OrgTreeTestHost.SendAsync(asMember, new DismissGettingStartedCommand(other.WorkspaceId));
        await crossWorkspace.Should().ThrowAsync<Notrelix.Application.Common.Exceptions.NotFoundException>("another workspace is not the member's");
    }

    [Fact]
    public async Task GettingStarted_AnOutsiderOfTheWorkspaceIsRefused()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var outsider = await _host.AddOutsiderAsync(stack);
        using var provider = _host.CreateProvider(stack.AccountId, outsider);

        var read = () => OrgTreeTestHost.QueryAsync(provider, new GetGettingStartedQuery(stack.WorkspaceId));
        var dismiss = () => OrgTreeTestHost.SendAsync(provider, new DismissGettingStartedCommand(stack.WorkspaceId));

        await read.Should().ThrowAsync<Notrelix.Application.Common.Exceptions.NotFoundException>();
        await dismiss.Should().ThrowAsync<Notrelix.Application.Common.Exceptions.NotFoundException>();
    }

    // ── helpers ───────────────────────────────────────────────────────────

    private static JsonElement Json(string text) => JsonDocument.Parse(text).RootElement.Clone();

    private async Task<WorkspaceSlugCheckDto> SlugAsync(OrgStack stack, Guid userId, string? slug)
    {
        using var provider = _host.CreateProvider(stack.AccountId, userId);
        return await OrgTreeTestHost.QueryAsync(provider, new CheckWorkspaceSlugQuery(slug));
    }

    private async Task<GettingStartedDto> GettingStartedAsync(OrgStack stack, Guid userId)
    {
        using var provider = _host.CreateProvider(stack.AccountId, userId);
        return await OrgTreeTestHost.QueryAsync(provider, new GetGettingStartedQuery(stack.WorkspaceId));
    }

    // A slug that breaks today's rule (a workspace created before it) cannot go through Workspace.Create, so it
    // is written the way the database holds it.
    private Task SeedWorkspaceSlugAsync(OrgStack stack, string slug, bool deleted = false) =>
        _host.WithSystemContextAsync(c =>
        {
            var workspace = Workspace.Create(stack.AccountId, stack.OwnerId, "Seeded", $"seeded-{Guid.NewGuid():N}", OrgTreeTestHost.BaseTime);
            typeof(Workspace).GetProperty(nameof(Workspace.Slug))!.SetValue(workspace, slug);
            if (deleted)
                workspace.Delete(stack.OwnerId, OrgTreeTestHost.BaseTime);
            c.Workspaces.Add(workspace);
            return Task.CompletedTask;
        });

    private Task SeedInvitationAsync(OrgStack stack, string email, bool revoke = false) =>
        _host.WithSystemContextAsync(c =>
        {
            var invitation = Notrelix.Domain.Workspaces.Invitations.WorkspaceInvitation.Create(
                stack.AccountId, stack.WorkspaceId, email, WorkspaceRole.Member,
                Notrelix.Domain.Workspaces.Invitations.InvitationTokenHash.Create(Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N")),
                1, stack.OwnerId, OrgTreeTestHost.BaseTime);
            if (revoke)
                invitation.Revoke(stack.OwnerId, OrgTreeTestHost.BaseTime);
            c.WorkspaceInvitations.Add(invitation);
            return Task.CompletedTask;
        });

    private Task SeedItemAsync(OrgStack stack, Guid board) =>
        _host.WithSystemContextAsync(c =>
        {
            var group = BoardGroup.Create(stack.AccountId, stack.WorkspaceId, board, "Group", Color.Create("#0079BF"), FractionalIndex.Create("a0"), stack.OwnerId, OrgTreeTestHost.BaseTime);
            c.BoardGroups.Add(group);
            c.BoardItems.Add(BoardItem.CreateRoot(stack.AccountId, stack.WorkspaceId, board, group.Id, "First", FractionalIndex.Create("a1"), stack.OwnerId, OrgTreeTestHost.BaseTime));
            return Task.CompletedTask;
        });
}
