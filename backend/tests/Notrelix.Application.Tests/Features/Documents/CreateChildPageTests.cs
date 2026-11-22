using Notrelix.Application.Features.Documents.Abstractions;
using Notrelix.Application.Features.Documents.Pages.Commands.CreatePage;
using Notrelix.Application.Features.Documents.Pages.Services;
using Notrelix.Application.Features.Governance.Public.Access;
using Notrelix.Domain.Documents.Pages;
using Notrelix.Testing.Application.Fakes;
using TestDbSet = Notrelix.Application.Tests.Features.Collaboration.TestDbSet;

namespace Notrelix.Application.Tests.Features.Documents;

/// <summary>
/// Creating a page under a chosen parent: the child follows the parent's Space, a missing, archived or
/// foreign parent is NotFound and adds nothing, the permission is checked on the parent page, and the
/// request shape refuses a Space sent together with a parent.
/// </summary>
public sealed class CreateChildPageTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 9, 9, 0, 0, TimeSpan.Zero);
    private static readonly Guid AccountId = Guid.CreateVersion7();
    private static readonly Guid WorkspaceId = Guid.CreateVersion7();
    private static readonly Guid UserId = Guid.CreateVersion7();

    [Fact]
    public async Task ChildOfAParent_HasThatParentAndTheParentsSpace()
    {
        var spaceId = Guid.CreateVersion7();
        var parent = Page.Create(AccountId, WorkspaceId, "Parent", UserId, Now, null, spaceId);
        var (sut, pages) = CreateSut(parent);

        var result = await sut.Handle(new CreatePageCommand(WorkspaceId, "Child", parent.Id), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        var child = pages.Single(p => p.Id == result.Data);
        child.ParentId.Should().Be(parent.Id);
        child.SpaceId.Should().Be(spaceId);
    }

    [Fact]
    public async Task AMissingParent_IsNotFound_AndNothingIsAdded()
    {
        var (sut, pages) = CreateSut();

        var act = () => sut.Handle(new CreatePageCommand(WorkspaceId, "Child", Guid.CreateVersion7()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        pages.Should().BeEmpty();
    }

    [Fact]
    public async Task AnArchivedParent_IsNotFound_AndNothingIsAdded()
    {
        var parent = Page.Create(AccountId, WorkspaceId, "Parent", UserId, Now);
        parent.Archive(UserId, Now.AddMinutes(1));
        var (sut, pages) = CreateSut(parent);

        var act = () => sut.Handle(new CreatePageCommand(WorkspaceId, "Child", parent.Id), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        pages.Should().ContainSingle();
    }

    [Fact]
    public async Task AParentInAnotherWorkspace_IsNotFound_AndNothingIsAdded()
    {
        var foreign = Page.Create(AccountId, Guid.CreateVersion7(), "Foreign", UserId, Now);
        var (sut, pages) = CreateSut(foreign);

        var act = () => sut.Handle(new CreatePageCommand(WorkspaceId, "Child", foreign.Id), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        pages.Should().ContainSingle();
    }

    [Fact]
    public void WithAParent_TheCreatePermissionIsCheckedOnThatParentPage()
    {
        var parentId = Guid.CreateVersion7();

        var resource = new CreatePageCommand(WorkspaceId, "Child", parentId).Resource;

        resource.ResourceId.Should().Be(parentId);
        resource.Kind.Value.Should().Be("documents.page");
    }

    [Fact]
    public void ASpaceSentTogetherWithAParent_IsRejected_AndAnEmptyParentToo()
    {
        var validator = new CreatePageCommandValidator();
        var id = Guid.CreateVersion7();

        validator.Validate(new CreatePageCommand(WorkspaceId, "Child", id, id)).IsValid.Should().BeFalse();
        validator.Validate(new CreatePageCommand(WorkspaceId, "Child", Guid.Empty)).IsValid.Should().BeFalse();
        validator.Validate(new CreatePageCommand(WorkspaceId, "Child", id)).IsValid.Should().BeTrue();
    }

    private static (CreatePageCommandHandler Sut, List<Page> Pages) CreateSut(params Page[] existing)
    {
        var pages = existing.ToList();
        var context = new Mock<IDocumentDbContext>();
        context.Setup(c => c.Pages).Returns(TestDbSet.Create(pages).Object);
        var requestContext = new FakeCurrentRequestContext().AsUser(UserId);
        requestContext.Tenant.SetWorkspace(AccountId, WorkspaceId, UserId);
        var sut = new CreatePageCommandHandler(context.Object, new Mock<IPageSpacePlacement>().Object,
            new InMemoryPageSubtreeStore(existing), new Mock<IResourceCreatorGrant>().Object, requestContext, new FixedClock());
        return (sut, pages);
    }

    private sealed class FixedClock : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => Now.AddMinutes(5);
    }
}
