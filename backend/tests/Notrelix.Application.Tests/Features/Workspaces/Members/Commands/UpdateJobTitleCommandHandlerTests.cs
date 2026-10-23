using Notrelix.Application.Features.Workspaces.Members.Commands.UpdateMemberJobTitle;
using Notrelix.Application.Features.Workspaces.Members.Commands.UpdateMyJobTitle;

namespace Notrelix.Application.Tests.Features.Workspaces.Members.Commands;

public class UpdateJobTitleCommandHandlerTests : WorkspaceHandlerTestBase
{
    private readonly Guid _other = Guid.CreateVersion7();

    [Fact]
    public async Task UpdateMine_ChangesTheCallersOwnMember()
    {
        var mine = CreateMember(WorkspaceRole.Member);
        var other = CreateMember(WorkspaceRole.Member, _other);
        SetupMembers(mine, other);

        var result = await new UpdateMyJobTitleCommandHandler(DbContextMock.Object, RequestContextMock.Object, DateTimeProviderMock.Object)
            .Handle(new UpdateMyJobTitleCommand(TestWorkspaceId, "  Lead   designer ", mine.Version), CancellationToken.None);

        result.Data!.JobTitle.Should().Be("Lead designer");
        mine.JobTitle.Should().Be("Lead designer");
        other.JobTitle.Should().BeNull("the own-title command never touches another member");
    }

    [Fact]
    public async Task UpdateMember_ChangesTheTargetMember_NotTheCaller()
    {
        var admin = CreateMember(WorkspaceRole.Admin);
        var target = CreateMember(WorkspaceRole.Member, _other);
        SetupMembers(admin, target);

        var result = await new UpdateMemberJobTitleCommandHandler(DbContextMock.Object, RequestContextMock.Object, DateTimeProviderMock.Object)
            .Handle(new UpdateMemberJobTitleCommand(TestWorkspaceId, _other, "Accountant", target.Version), CancellationToken.None);

        result.Data!.UserId.Should().Be(_other);
        target.JobTitle.Should().Be("Accountant");
        target.UpdatedBy.Should().Be(TestUserId);
        admin.JobTitle.Should().BeNull();
    }

    [Fact]
    public async Task UpdateMember_WhenTheTargetIsNotAMemberOfTheWorkspace_IsNotFound()
    {
        SetupMembers(CreateMember(WorkspaceRole.Admin));

        var act = () => new UpdateMemberJobTitleCommandHandler(DbContextMock.Object, RequestContextMock.Object, DateTimeProviderMock.Object)
            .Handle(new UpdateMemberJobTitleCommand(TestWorkspaceId, _other, "x", 1), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task UpdateMember_WhenTheTargetWasRemoved_IsNotFound()
    {
        var removed = CreateMember(WorkspaceRole.Member, _other);
        removed.Remove(activeOwnerCount: 1, TestUserId, TestNow.AddMinutes(1));
        SetupMembers(CreateMember(WorkspaceRole.Admin), removed);

        var act = () => new UpdateMemberJobTitleCommandHandler(DbContextMock.Object, RequestContextMock.Object, DateTimeProviderMock.Object)
            .Handle(new UpdateMemberJobTitleCommand(TestWorkspaceId, _other, "x", 1), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task UpdateMine_WhenTheTitleIsUnchanged_DoesNotAdvanceTheVersion()
    {
        var mine = CreateMember(WorkspaceRole.Member);
        mine.ChangeJobTitle("Designer", TestUserId, TestNow.AddMinutes(1));
        var version = mine.Version;
        SetupMembers(mine);

        var result = await new UpdateMyJobTitleCommandHandler(DbContextMock.Object, RequestContextMock.Object, DateTimeProviderMock.Object)
            .Handle(new UpdateMyJobTitleCommand(TestWorkspaceId, " Designer ", version), CancellationToken.None);

        result.Data!.Version.Should().Be(version);
    }

    [Theory]
    [InlineData(null, "x", 1, "WorkspaceId")]
    [InlineData("ok", "x", 0, "ExpectedVersion")]
    [InlineData("line1\nline2", "x", 1, "JobTitle")]
    public void Validators_RejectBadInput(string? title, string _, long version, string property)
    {
        var workspaceId = property == "WorkspaceId" ? Guid.Empty : Guid.NewGuid();
        var mine = new UpdateMyJobTitleCommandValidator().Validate(new UpdateMyJobTitleCommand(workspaceId, title, version));
        var theirs = new UpdateMemberJobTitleCommandValidator().Validate(new UpdateMemberJobTitleCommand(workspaceId, Guid.NewGuid(), title, version));

        mine.Errors.Select(e => e.PropertyName).Should().Contain(property);
        theirs.Errors.Select(e => e.PropertyName).Should().Contain(property);
    }

    [Fact]
    public void Validators_RejectATitleOver100Characters_AndAcceptABlankOne()
    {
        new UpdateMyJobTitleCommandValidator().Validate(new UpdateMyJobTitleCommand(Guid.NewGuid(), new string('a', 101), 1))
            .IsValid.Should().BeFalse();
        new UpdateMyJobTitleCommandValidator().Validate(new UpdateMyJobTitleCommand(Guid.NewGuid(), "   ", 1))
            .IsValid.Should().BeTrue("a blank title clears it");
    }
}
