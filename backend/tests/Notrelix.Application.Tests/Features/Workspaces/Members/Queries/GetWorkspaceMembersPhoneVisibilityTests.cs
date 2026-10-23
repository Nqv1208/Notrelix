using Notrelix.Application.Features.Workspaces.DTOs;
using Notrelix.Application.Features.Workspaces.Members.Queries.GetWorkspaceMembers;

namespace Notrelix.Application.Tests.Features.Workspaces.Members.Queries;

/// <summary>
/// D15 phone visibility on the Workspace member list: a phone is returned only when the person
/// opted in AND the reader is an active non-guest member of this Workspace; a guest reader and a
/// guest subject never get one. Negative cases are the point of this class.
/// </summary>
public sealed class GetWorkspaceMembersPhoneVisibilityTests : WorkspaceHandlerTestBase
{
    private const string SharedPhone = "+84901234567";

    private readonly Mock<IActorLookupService> _actors = new();
    private readonly Mock<IMemberPhoneLookup> _phones = new();
    private readonly Guid _colleague = Guid.CreateVersion7();
    private readonly Guid _guest = Guid.CreateVersion7();

    public GetWorkspaceMembersPhoneVisibilityTests()
    {
        _actors.Setup(a => a.FindManyAsync(It.IsAny<IReadOnlyList<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Guid> ids, CancellationToken _) =>
                ids.Select(id => new ActorSnapshot(id, $"User {id:N}", null)).ToList());
        // The port is the opt-in half only: it answers for whichever ids it is asked about.
        _phones.Setup(p => p.FindSharedPhonesAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyCollection<Guid> ids, CancellationToken _) =>
                (IReadOnlyDictionary<Guid, string>)ids.ToDictionary(id => id, _ => SharedPhone));
        SetupWorkspaces(CreateWorkspace(TestWorkspaceId));
    }

    private async Task<List<WorkspaceMemberDto>> ListAsync() =>
        (await new GetWorkspaceMembersQueryHandler(DbContextMock.Object, _actors.Object, _phones.Object, RequestContextMock.Object)
            .Handle(new GetWorkspaceMembersQuery(TestWorkspaceId), CancellationToken.None)).Data!;

    [Fact]
    public async Task MemberReader_SeesTheOptedInPhonesOfNonGuestMembers()
    {
        SetupMembers(CreateMember(WorkspaceRole.Member), CreateMember(WorkspaceRole.Member, _colleague));

        var list = await ListAsync();

        list.Should().OnlyContain(m => m.Phone == SharedPhone);
    }

    [Fact]
    public async Task GuestReader_NeverSeesAnyPhone_AndThePortIsNotEvenAsked()
    {
        SetupMembers(CreateMember(WorkspaceRole.Guest), CreateMember(WorkspaceRole.Member, _colleague));

        var list = await ListAsync();

        list.Should().OnlyContain(m => m.Phone == null);
        _phones.Verify(p => p.FindSharedPhonesAsync(It.Is<IReadOnlyCollection<Guid>>(ids => ids.Count > 0), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GuestSubject_PhoneIsNeverReturnedToAMember()
    {
        SetupMembers(CreateMember(WorkspaceRole.Member), CreateMember(WorkspaceRole.Guest, _guest));

        var list = await ListAsync();

        list.Single(m => m.UserId == _guest).Phone.Should().BeNull();
        list.Single(m => m.UserId == TestUserId).Phone.Should().Be(SharedPhone);
    }

    [Fact]
    public async Task SuspendedMember_DoesNotExposeAPhone()
    {
        var suspended = CreateMember(WorkspaceRole.Member, _colleague);
        suspended.Suspend(TestUserId, TestNow.AddMinutes(1), activeOwnerCount: 1);
        SetupMembers(CreateMember(WorkspaceRole.Member), suspended);

        var list = await ListAsync();

        list.Single(m => m.UserId == _colleague).Phone.Should().BeNull();
    }

    [Fact]
    public async Task MembersWhoDidNotOptIn_HaveNoPhone()
    {
        _phones.Setup(p => p.FindSharedPhonesAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, string>());
        SetupMembers(CreateMember(WorkspaceRole.Member), CreateMember(WorkspaceRole.Member, _colleague));

        var list = await ListAsync();

        list.Should().OnlyContain(m => m.Phone == null);
    }

    [Fact]
    public async Task List_ExposesJobTitleAndTheMemberVersion()
    {
        var member = CreateMember(WorkspaceRole.Member);
        member.ChangeJobTitle("Designer", TestUserId, TestNow.AddMinutes(1));
        SetupMembers(member);

        var dto = (await ListAsync()).Single();

        dto.JobTitle.Should().Be("Designer");
        dto.Version.Should().Be(member.Version).And.BeGreaterThan(1);
    }
}
