using Notrelix.Application.Features.Documents.Abstractions;
using Notrelix.Application.Features.Documents.Templates.Commands.ArchivePageTemplate;
using Notrelix.Application.Features.Documents.Templates.Commands.DeletePageTemplate;
using Notrelix.Application.Features.Documents.Templates.Commands.UpdatePageTemplate;
using Notrelix.Application.Features.Documents.Templates.Queries.ListPageTemplates;
using Notrelix.Application.Features.Workspaces.Public.Members;
using Notrelix.Domain.Documents.Templates;
using Notrelix.Domain.Governance.Permissions;
using Notrelix.Testing.Application.Fakes;
using TestDbSet = Notrelix.Application.Tests.Features.Collaboration.TestDbSet;

namespace Notrelix.Application.Tests.Features.Documents;

/// <summary>Renaming, archiving and deleting a template: the creator or a Workspace Admin, version-guarded.</summary>
public sealed class PageTemplateManageHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 16, 9, 0, 0, TimeSpan.Zero);
    private static readonly Guid WorkspaceId = Guid.CreateVersion7();
    private static readonly Guid Creator = Guid.CreateVersion7();
    private static readonly Guid Admin = Guid.CreateVersion7();
    private static readonly Guid Member = Guid.CreateVersion7();

    private sealed class Clock : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => Now.AddMinutes(5);
    }

    private sealed class Fixture
    {
        public List<PageTemplate> Templates { get; } = [];
        public Mock<IDocumentDbContext> Context { get; } = new();
        public Mock<IWorkspaceMemberFacts> Members { get; } = new();
        public PageTemplate Template { get; }

        public Fixture()
        {
            Template = PageTemplate.Create("Plan", JsonValue.Create("{\"title\":\"Plan\",\"icon\":\"x\"}"), JsonValue.EmptyArray(), Now, WorkspaceId, Creator, "Weekly plan");
            Template.Publish(Creator, Now);
            Templates.Add(Template);
            var set = TestDbSet.Create(Templates);
            set.Setup(s => s.Remove(It.IsAny<PageTemplate>())).Callback<PageTemplate>(t => Templates.Remove(t));
            Context.Setup(c => c.PageTemplates).Returns(set.Object);
            Members.Setup(m => m.FindAsync(WorkspaceId, Admin, It.IsAny<CancellationToken>())).ReturnsAsync(new WorkspaceMemberFact(Admin, "Admin", true));
            Members.Setup(m => m.FindAsync(WorkspaceId, Member, It.IsAny<CancellationToken>())).ReturnsAsync(new WorkspaceMemberFact(Member, "Member", true));
            Members.Setup(m => m.FindAsync(WorkspaceId, Creator, It.IsAny<CancellationToken>())).ReturnsAsync(new WorkspaceMemberFact(Creator, "Member", true));
        }

        public FakeCurrentRequestContext As(Guid user)
        {
            var request = new FakeCurrentRequestContext().AsUser(user);
            request.Tenant.SetWorkspace(Guid.CreateVersion7(), WorkspaceId, user);
            return request;
        }

        public UpdatePageTemplateCommandHandler Update(Guid user) => new(Context.Object, As(user), Members.Object, new Clock());
        public ArchivePageTemplateCommandHandler Archive(Guid user) => new(Context.Object, As(user), Members.Object, new Clock());
        public DeletePageTemplateCommandHandler Delete(Guid user) => new(Context.Object, As(user), Members.Object, new Clock());
        public ListPageTemplatesQueryHandler List(Guid user) => new(Context.Object, As(user), Members.Object);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task TheCreatorOrAnAdmin_CanRenameATemplate(bool byCreator)
    {
        var f = new Fixture();
        var before = f.Template.Version;

        var result = await f.Update(byCreator ? Creator : Admin).Handle(
            new UpdatePageTemplateCommand(WorkspaceId, f.Template.Id, "  Sprint plan ", "  ", before), CancellationToken.None);

        result.Data!.Name.Should().Be("Sprint plan");
        result.Data.Description.Should().BeNull();
        f.Template.Version.Should().Be(before + 1);
    }

    [Fact]
    public async Task TheSameNameAndDescription_IsANoOp_WithNoVersionBump()
    {
        var f = new Fixture();
        var version = f.Template.Version;

        await f.Update(Creator).Handle(new UpdatePageTemplateCommand(WorkspaceId, f.Template.Id, "Plan", "Weekly plan", version), CancellationToken.None);

        f.Template.Version.Should().Be(version);
    }

    [Fact]
    public async Task AnotherMember_IsRefused_ForEveryChange_AndNothingChanges()
    {
        var f = new Fixture();
        var version = f.Template.Version;

        var rename = () => f.Update(Member).Handle(new UpdatePageTemplateCommand(WorkspaceId, f.Template.Id, "Mine", null, version), CancellationToken.None);
        var archive = () => f.Archive(Member).Handle(new ArchivePageTemplateCommand(WorkspaceId, f.Template.Id, version), CancellationToken.None);
        var delete = () => f.Delete(Member).Handle(new DeletePageTemplateCommand(WorkspaceId, f.Template.Id, version), CancellationToken.None);

        await rename.Should().ThrowAsync<ForbiddenException>();
        await archive.Should().ThrowAsync<ForbiddenException>();
        await delete.Should().ThrowAsync<ForbiddenException>();
        var kept = f.Templates.Should().ContainSingle().Subject;
        kept.Name.Should().Be("Plan");
        kept.Status.Should().Be(PageTemplateStatus.Published);
        kept.Version.Should().Be(version);
    }

    [Fact]
    public async Task AnArchivedTemplate_LeavesTheList_AndArchivingItAgainIsANoOp()
    {
        var f = new Fixture();
        await f.Archive(Admin).Handle(new ArchivePageTemplateCommand(WorkspaceId, f.Template.Id, f.Template.Version), CancellationToken.None);
        var archivedVersion = f.Template.Version;

        await f.Archive(Admin).Handle(new ArchivePageTemplateCommand(WorkspaceId, f.Template.Id, archivedVersion), CancellationToken.None);

        f.Template.Status.Should().Be(PageTemplateStatus.Archived);
        f.Template.Version.Should().Be(archivedVersion);
        var listed = await f.List(Creator).Handle(new ListPageTemplatesQuery(WorkspaceId), CancellationToken.None);
        listed.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task Delete_RemovesTheTemplate()
    {
        var f = new Fixture();

        await f.Delete(Creator).Handle(new DeletePageTemplateCommand(WorkspaceId, f.Template.Id, f.Template.Version), CancellationToken.None);

        f.Templates.Should().BeEmpty();
    }

    [Fact]
    public async Task ATemplateOfAnotherWorkspace_IsNotFound()
    {
        var f = new Fixture();

        var act = () => f.Delete(Admin).Handle(new DeletePageTemplateCommand(Guid.CreateVersion7(), f.Template.Id, f.Template.Version), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        f.Templates.Should().HaveCount(1);
    }

    [Fact]
    public async Task TheList_SaysWhoCanManageEachTemplate()
    {
        var f = new Fixture();

        var asMember = await f.List(Member).Handle(new ListPageTemplatesQuery(WorkspaceId), CancellationToken.None);
        var asAdmin = await f.List(Admin).Handle(new ListPageTemplatesQuery(WorkspaceId), CancellationToken.None);
        var asCreator = await f.List(Creator).Handle(new ListPageTemplatesQuery(WorkspaceId), CancellationToken.None);

        asMember.Data!.Single().CanManage.Should().BeFalse();
        asAdmin.Data!.Single().CanManage.Should().BeTrue();
        asCreator.Data!.Single().CanManage.Should().BeTrue();
        asAdmin.Data!.Single().Version.Should().Be(f.Template.Version);
    }

    [Fact]
    public void TheCommands_AreAuthorizedOnTheWorkspace_AndGuardedOnTheTemplate()
    {
        var id = Guid.CreateVersion7();
        var commands = new (IRequirePermission Permission, ResourceRef Version)[]
        {
            (new UpdatePageTemplateCommand(WorkspaceId, id, "x", null, 1), new UpdatePageTemplateCommand(WorkspaceId, id, "x", null, 1).Resource),
            (new ArchivePageTemplateCommand(WorkspaceId, id, 1), new ArchivePageTemplateCommand(WorkspaceId, id, 1).Resource),
            (new DeletePageTemplateCommand(WorkspaceId, id, 1), new DeletePageTemplateCommand(WorkspaceId, id, 1).Resource),
        };
        foreach (var (permission, version) in commands)
        {
            permission.Action.Should().Be(PermissionAction.ViewWorkspace);
            permission.Resource.Kind.Value.Should().Be("workspaces.workspace");
            version.Kind.Value.Should().Be("documents.page-template");
            version.ResourceId.Should().Be(id);
        }
    }

    [Fact]
    public void TheValidators_RefuseAnEmptyNameAndAMissingVersion()
    {
        var id = Guid.CreateVersion7();
        new UpdatePageTemplateCommandValidator().Validate(new UpdatePageTemplateCommand(WorkspaceId, id, " ", null, 1)).IsValid.Should().BeFalse();
        new UpdatePageTemplateCommandValidator().Validate(new UpdatePageTemplateCommand(WorkspaceId, id, "x", null, 0)).IsValid.Should().BeFalse();
        new DeletePageTemplateCommandValidator().Validate(new DeletePageTemplateCommand(WorkspaceId, id, 0)).IsValid.Should().BeFalse();
    }
}
