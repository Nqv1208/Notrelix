using System.Text.Json;
using Notrelix.Application.Features.Workspaces.Onboarding.Commands.ClearOnboardingDraft;
using Notrelix.Application.Features.Workspaces.Onboarding.Commands.DismissGettingStarted;
using Notrelix.Application.Features.Workspaces.Onboarding.Commands.SaveOnboardingDraft;
using Notrelix.Application.Features.Workspaces.Onboarding.Queries.CheckWorkspaceSlug;
using Notrelix.Application.Features.Workspaces.Onboarding.Queries.GetGettingStarted;
using Notrelix.Application.Features.Workspaces.Onboarding.Queries.GetOnboardingDraft;
using Notrelix.Domain.SharedKernel;
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

    public async Task InitializeAsync() => await new DatabaseReset(_db.ConnectionString).ResetAsync();

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
        foreach (var bad in new[] { null, "", "  ", "-x", "a b", "a--b", "x".PadRight(129, 'x') })
            (await SlugAsync(stack, stack.OwnerId, bad)).Should().Be(new WorkspaceSlugCheckDto("invalid", null), $"'{bad}'");
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

    // ── getting started ───────────────────────────────────────────────────

    [Fact]
    public async Task GettingStarted_FirstItemIsDerivedFromReadableBoards_AndAHiddenBoardNeverMakesItDone()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var member = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Member");
        var empty = await _host.SeedBoardAsync(stack, "Empty board");
        var secret = await _host.SeedBoardAsync(stack, "Secret", BoardVisibility.Private);
        await SeedItemAsync(stack, secret);

        var before = (await GettingStartedAsync(stack, member)).Items.Single();
        before.Should().Be(new GettingStartedItemDto("first-item", false, empty, "Empty board"));

        var shared = await _host.SeedBoardAsync(stack, "Shared");
        await SeedItemAsync(stack, shared);
        var after = await GettingStartedAsync(stack, member);

        after.Items.Single().Should().Be(new GettingStartedItemDto("first-item", true, shared, "Shared"));
        after.Dismissed.Should().BeFalse();
        (await GettingStartedAsync(stack, stack.OwnerId)).Items.Single().Done.Should().BeTrue("the owner reads the secret board too");
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
        await crossWorkspace.Should().ThrowAsync<AppForbidden>("another workspace is not the member's");
    }

    [Fact]
    public async Task GettingStarted_AnOutsiderOfTheWorkspaceIsRefused()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var outsider = await _host.AddOutsiderAsync(stack);
        using var provider = _host.CreateProvider(stack.AccountId, outsider);

        var read = () => OrgTreeTestHost.QueryAsync(provider, new GetGettingStartedQuery(stack.WorkspaceId));
        var dismiss = () => OrgTreeTestHost.SendAsync(provider, new DismissGettingStartedCommand(stack.WorkspaceId));

        await read.Should().ThrowAsync<AppForbidden>();
        await dismiss.Should().ThrowAsync<AppForbidden>();
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

    private Task SeedWorkspaceSlugAsync(OrgStack stack, string slug, bool deleted = false) =>
        _host.WithSystemContextAsync(c =>
        {
            var workspace = Workspace.Create(stack.AccountId, stack.OwnerId, slug, slug, OrgTreeTestHost.BaseTime);
            if (deleted)
                workspace.Delete(stack.OwnerId, OrgTreeTestHost.BaseTime);
            c.Workspaces.Add(workspace);
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
