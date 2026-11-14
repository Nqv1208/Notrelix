using Notrelix.Application.Features.Governance.Public.Visibility;
using Notrelix.Application.Features.Identity.Public.Queries;
using Notrelix.Application.Features.WorkManagement.Transfer;
using Notrelix.Application.Features.Workspaces.Public.Members;

namespace Notrelix.Application.Tests.Features.WorkManagement.Transfer;

/// <summary>Who a file may name in a Person column: the people the assignee picker offers, by exact e-mail, for an Owner or Admin importer only.</summary>
public class ImportPeopleDirectoryTests : WorkManagementHandlerTestBase
{
    private readonly Mock<IWorkspaceMemberFacts> _members = new();
    private readonly Mock<IIdentityUserFacts> _identity = new();
    private readonly Mock<IResourceVisibility> _visibility = new();
    private readonly Guid _board = Guid.CreateVersion7();
    private readonly Guid _importer = Guid.CreateVersion7();
    private readonly Guid _ana = Guid.CreateVersion7();
    private readonly Guid _viewer = Guid.CreateVersion7();
    private readonly Guid _noAccess = Guid.CreateVersion7();
    private readonly Guid _stranger = Guid.CreateVersion7();

    private static readonly TabularData File = new("csv", ["Name", "Who"],
        [new TabularRow(1, ["A", "ana@example.com"]), new TabularRow(2, ["B", "viewer@example.com"]),
         new TabularRow(3, ["C", "noaccess@example.com"]), new TabularRow(4, ["D", "stranger@other.com"])], 4);

    public ImportPeopleDirectoryTests()
    {
        SetupBoardMembers(BoardMember.Create(_board, _viewer, BoardRole.Observer, TestNow));
        _members.Setup(m => m.ListActiveColleagueIdsAsync(TestWorkspaceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([_ana, _viewer, _noAccess]);
        _identity.Setup(i => i.FindEmailsAsync(TestWorkspaceId, It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, string> { [_ana] = "ana@example.com", [_viewer] = "viewer@example.com", [_noAccess] = "noaccess@example.com" });
        _visibility.Setup(v => v.VisibleBoardIdsAsync(TestWorkspaceId, It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync([_board]);
        _visibility.Setup(v => v.VisibleBoardIdsAsync(TestWorkspaceId, _noAccess, It.IsAny<CancellationToken>())).ReturnsAsync([]);
    }

    private void Importer(string role, bool active = true) =>
        _members.Setup(m => m.FindAsync(TestWorkspaceId, _importer, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkspaceMemberFact(_importer, role, active));

    private Task<IReadOnlyDictionary<string, Guid>?> Resolve() =>
        new ImportPeopleDirectory(_members.Object, _identity.Object, _visibility.Object, DbContextMock.Object)
            .ResolveAsync(TestWorkspaceId, _board, _importer, File, CancellationToken.None);

    [Theory]
    [InlineData("Owner")]
    [InlineData("Admin")]
    public async Task AnOwnerOrAdminImporter_MatchesOnlyPeopleThePickerOffers(string role)
    {
        Importer(role);

        var people = await Resolve();

        people.Should().NotBeNull();
        people!.Should().ContainSingle().Which.Should().Be(new KeyValuePair<string, Guid>("ana@example.com", _ana));
    }

    [Theory]
    [InlineData("Member", true)]
    [InlineData("Admin", false)]
    public async Task AnyoneElse_GetsNoDirectory_AndEmailIsNeverRead(string role, bool active)
    {
        Importer(role, active);

        (await Resolve()).Should().BeNull();

        _identity.Verify(i => i.FindEmailsAsync(It.IsAny<Guid>(), It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AnImporterWhoIsNotAMemberAtAll_GetsNoDirectory()
    {
        _members.Setup(m => m.FindAsync(TestWorkspaceId, _importer, It.IsAny<CancellationToken>())).ReturnsAsync((WorkspaceMemberFact?)null);

        (await Resolve()).Should().BeNull();
    }
}
