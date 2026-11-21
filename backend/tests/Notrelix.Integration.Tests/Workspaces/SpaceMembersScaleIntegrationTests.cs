using Microsoft.Extensions.DependencyInjection;
using Notrelix.Application.Features.Workspaces.Spaces.Queries.GetSpaceMembers;
using Notrelix.Application.Features.Workspaces.Spaces.Queries.GetSpaceMembersExtra;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Domain.Workspaces.Spaces;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Integration.Tests.Workspaces.OrgTree;
using AppNotFound = Notrelix.Application.Common.Exceptions.NotFoundException;

namespace Notrelix.Integration.Tests.Workspaces;

/// <summary>
/// The Space members read at scale on PostgreSQL through the real pipeline (WSP-024): a 500-entry Space pages 50 at a
/// time and the cursor walk reads every entry once; the role and kind filters are applied before the page is cut and
/// only ever remove rows; a filter never changes who may read: a hidden Space, another Workspace's Space and a person
/// outside the Workspace answer exactly as they do without a filter.
/// </summary>
[Collection("Database")]
[Trait("Category", "Integration")]
public sealed class SpaceMembersScaleIntegrationTests : IAsyncLifetime
{
    private readonly PostgresTestContainer _db;
    private readonly OrgTreeTestHost _host;

    public SpaceMembersScaleIntegrationTests(PostgresTestContainer db)
    {
        _db = db;
        _host = new OrgTreeTestHost(db);
    }

    public async Task InitializeAsync() => await new DatabaseReset(_db.OwnerConnectionString).ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    /// <summary>500 entries: the creator and 49 Admin Users, 299 Member Users, 100 Viewer Users, 51 Viewer Teams.</summary>
    private async Task<(OrgStack Stack, Guid SpaceId, HashSet<Guid> SubjectIds)> SeedLargeSpaceAsync()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var subjects = new HashSet<Guid> { stack.OwnerId };
        var spaceId = await _host.SeedSpaceAsync(stack, "Operations", SpaceVisibility.Workspace, stack.OwnerId, type: SpaceType.Department, transition: space =>
        {
            void Add(SpaceMemberSubjectType type, SpaceRole role, int count)
            {
                for (var n = 0; n < count; n++)
                {
                    var id = Guid.NewGuid();
                    space.AddMember(type, id, role, stack.OwnerId, OrgTreeTestHost.BaseTime);
                    subjects.Add(id);
                }
            }

            Add(SpaceMemberSubjectType.User, SpaceRole.Admin, 49);
            Add(SpaceMemberSubjectType.User, SpaceRole.Member, 299);
            Add(SpaceMemberSubjectType.User, SpaceRole.Viewer, 100);
            Add(SpaceMemberSubjectType.Team, SpaceRole.Viewer, 51);
        });
        return (stack, spaceId, subjects);
    }

    private static async Task<List<SpaceMemberDto>> WalkAsync(
        ServiceProvider provider, Guid workspaceId, Guid spaceId, int? limit = null, string? role = null, string? kind = null, List<int>? pageSizes = null)
    {
        var all = new List<SpaceMemberDto>();
        string? cursor = null;
        for (var pages = 0; pages < 100; pages++)
        {
            var page = await OrgTreeTestHost.QueryAsync(provider, new GetSpaceMembersQuery(workspaceId, spaceId, cursor, limit, role, kind));
            pageSizes?.Add(page.Items.Count());
            all.AddRange(page.Items);
            cursor = page.NextCursor;
            if (cursor is null) return all;
        }

        throw new InvalidOperationException("The cursor never ended.");
    }

    [Fact]
    public async Task ALargeSpace_PagesFiftyAtATime_AndTheWalkReadsEveryEntryExactlyOnce()
    {
        var (stack, spaceId, subjects) = await SeedLargeSpaceAsync();
        using var owner = _host.CreateProvider(stack.AccountId, stack.OwnerId);

        var first = await OrgTreeTestHost.QueryAsync(owner, new GetSpaceMembersQuery(stack.WorkspaceId, spaceId));
        first.Items.Should().HaveCount(50);
        first.NextCursor.Should().NotBeNullOrEmpty();
        var second = await OrgTreeTestHost.QueryAsync(owner, new GetSpaceMembersQuery(stack.WorkspaceId, spaceId, first.NextCursor));
        second.Items.Should().HaveCount(50);
        second.Items.Select(m => m.SubjectId).Should().NotIntersectWith(first.Items.Select(m => m.SubjectId), "the next page continues after the first");

        var pageSizes = new List<int>();
        var walked = await WalkAsync(owner, stack.WorkspaceId, spaceId, pageSizes: pageSizes);

        pageSizes.Should().Equal(Enumerable.Repeat(50, 10), "500 entries are ten pages of fifty and the last page carries no cursor");
        walked.Should().HaveCount(500);
        walked.Select(m => m.SubjectId).Should().OnlyHaveUniqueItems("no entry repeats").And.BeEquivalentTo(subjects, "no entry is skipped");
        walked.Count(m => m.Role == "Admin").Should().Be(50);
        walked.Count(m => m.Role == "Member").Should().Be(299);
        walked.Count(m => m.Role == "Viewer").Should().Be(151);
        walked.Count(m => m.SubjectType == "Team").Should().Be(51);
    }

    [Theory]
    [InlineData("Admin", null, 50)]
    [InlineData("member", null, 299)]
    [InlineData("Viewer", null, 151)]
    [InlineData(null, "User", 449)]
    [InlineData(null, "team", 51)]
    [InlineData("Viewer", "Team", 51)]
    [InlineData("Admin", "Team", 0)]
    public async Task ARoleAndKindFilter_IsAppliedBeforeThePageIsCut_AndWalksMatchingEntriesOnce(string? role, string? kind, int expected)
    {
        var (stack, spaceId, subjects) = await SeedLargeSpaceAsync();
        using var owner = _host.CreateProvider(stack.AccountId, stack.OwnerId);
        var pageSizes = new List<int>();

        var walked = await WalkAsync(owner, stack.WorkspaceId, spaceId, limit: 40, role: role, kind: kind, pageSizes: pageSizes);

        walked.Should().HaveCount(expected);
        walked.Select(m => m.SubjectId).Should().OnlyHaveUniqueItems().And.BeSubsetOf(subjects);
        if (role is not null) walked.Should().OnlyContain(m => m.Role.ToLower() == role.ToLower());
        if (kind is not null) walked.Should().OnlyContain(m => m.SubjectType.ToLower() == kind.ToLower());
        pageSizes.Take(pageSizes.Count - 1).Should().OnlyContain(size => size == 40, "every page but the last is full of matching entries");
    }

    [Fact]
    public async Task AFilter_NeverChangesWhoCanRead_HiddenSpaceOtherWorkspaceAndOutsiderAnswerAsWithoutOne()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var other = await _host.SeedWorkspaceAsync();
        var member = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Member");
        var guest = await _host.AddMemberAsync(stack, WorkspaceRole.Guest, "Guest");
        var outsider = await _host.AddOutsiderAsync(stack);
        var hidden = await _host.SeedSpaceAsync(stack, "Board of directors", SpaceVisibility.Private, stack.OwnerId);
        var open = await _host.SeedSpaceAsync(stack, "Finance", SpaceVisibility.Workspace, stack.OwnerId);
        var foreign = await _host.SeedSpaceAsync(other, "Foreign", SpaceVisibility.Workspace, other.OwnerId);
        using var asMember = _host.CreateProvider(stack.AccountId, member);
        using var asGuest = _host.CreateProvider(stack.AccountId, guest);
        using var asOutsider = _host.CreateProvider(stack.AccountId, outsider);
        using var asOwner = _host.CreateProvider(stack.AccountId, stack.OwnerId);

        async Task<Type?> OutcomeAsync(ServiceProvider provider, Guid spaceId, string? role, string? kind)
        {
            try
            {
                await OrgTreeTestHost.QueryAsync(provider, new GetSpaceMembersQuery(stack.WorkspaceId, spaceId, null, null, role, kind));
                return null;
            }
            catch (Exception ex)
            {
                return ex.GetType();
            }
        }

        // A hidden Space, and another Workspace's Space, are NotFound whatever the filter says.
        foreach (var (provider, spaceId) in new[] { (asMember, hidden), (asOwner, foreign) })
        {
            (await OutcomeAsync(provider, spaceId, null, null)).Should().Be(typeof(AppNotFound));
            (await OutcomeAsync(provider, spaceId, "Admin", "User")).Should().Be(typeof(AppNotFound));
        }

        // Guest, outsider and member read the visible Space exactly as before: a filter adds no right and removes none.
        foreach (var provider in new[] { asGuest, asOutsider, asMember })
        {
            var plain = await OutcomeAsync(provider, open, null, null);
            (await OutcomeAsync(provider, open, "Admin", null)).Should().Be(plain);
            (await OutcomeAsync(provider, open, null, "Team")).Should().Be(plain);
            (await OutcomeAsync(provider, open, "Viewer", "User")).Should().Be(plain);
        }

        (await OutcomeAsync(asOutsider, open, "Admin", "User")).Should().Be(typeof(AppNotFound), "a person outside the Workspace is told it does not exist");
        (await OutcomeAsync(asMember, open, "Admin", "User")).Should().BeNull();
        var admins = await OrgTreeTestHost.QueryAsync(asMember, new GetSpaceMembersQuery(stack.WorkspaceId, open, null, null, "Admin", "User"));
        admins.Items.Select(m => m.SubjectId).Should().Equal(stack.OwnerId);
    }

    [Fact]
    public async Task TheExtraRead_ShowsEmailsToAnOwner_NotToAMember_AndNeverCountsAGuest()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var member = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Member");
        var guest = await _host.AddMemberAsync(stack, WorkspaceRole.Guest, "Guest");
        var space = await _host.SeedSpaceAsync(stack, "Finance", SpaceVisibility.Workspace, stack.OwnerId, transition: s =>
        {
            s.AddMember(SpaceMemberSubjectType.User, member, SpaceRole.Member, stack.OwnerId, OrgTreeTestHost.BaseTime);
            s.AddMember(SpaceMemberSubjectType.User, guest, SpaceRole.Member, stack.OwnerId, OrgTreeTestHost.BaseTime);
        });
        using var asOwner = _host.CreateProvider(stack.AccountId, stack.OwnerId);
        using var asMember = _host.CreateProvider(stack.AccountId, member);

        var seenByOwner = await OrgTreeTestHost.QueryAsync(asOwner, new GetSpaceMembersExtraQuery(stack.WorkspaceId, space));
        var seenByMember = await OrgTreeTestHost.QueryAsync(asMember, new GetSpaceMembersExtraQuery(stack.WorkspaceId, space));

        seenByOwner.People.Single(p => p.UserId == member).Email.Should().NotBeNullOrEmpty("an Owner sees a member's email");
        seenByMember.People.Should().OnlyContain(p => p.Email == null, "a member never sees emails");
        seenByOwner.People.Select(p => p.UserId).Should().NotContain(guest, "a Guest is Board-level and not counted");
        seenByOwner.Summary.Members.Should().Be(1);
    }
}
