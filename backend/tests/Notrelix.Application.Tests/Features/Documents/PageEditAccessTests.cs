using Notrelix.Application.Features.Documents.Abstractions;
using Notrelix.Application.Features.Documents.Pages.Commands.UpdatePage;
using Notrelix.Application.Features.Documents.Pages.Queries.GetPage;
using Notrelix.Application.Features.Documents.Pages.Services;
using Notrelix.Application.Features.Governance.Public.Visibility;
using Notrelix.Domain.Documents.Pages;
using Notrelix.Domain.Governance.Permissions;
using Notrelix.Testing.Application.Fakes;
using TestDbSet = Notrelix.Application.Tests.Features.Collaboration.TestDbSet;

namespace Notrelix.Application.Tests.Features.Documents;

/// <summary>
/// The page read tells the client whether the caller may edit: the decision a write of the page gets (the
/// <c>UpdatePage</c> action), never a rule of its own. A Viewer reads <c>false</c>, an Editor <c>true</c>, a
/// trashed page <c>false</c> for everyone; a person who may not read the page never reaches the handler.
/// </summary>
public sealed class PageEditAccessTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 16, 9, 0, 0, TimeSpan.Zero);
    private static readonly Guid AccountId = Guid.CreateVersion7();
    private static readonly Guid WorkspaceId = Guid.CreateVersion7();
    private static readonly Guid UserId = Guid.CreateVersion7();

    private static (PageEditAccess Sut, List<(RequestDescriptor Descriptor, ExecutionContextSnapshot Snapshot, object Request)> Evaluated) Create(
        AccessDecision decision, Guid pageId)
    {
        var descriptor = (RequestDescriptor)null!;
        var descriptors = new Mock<IRequestDescriptorRegistry>();
        descriptors.Setup(d => d.GetRequired(typeof(UpdatePageCommand))).Returns(descriptor);
        var evaluated = new List<(RequestDescriptor, ExecutionContextSnapshot, object)>();
        var facts = new Mock<IAccessFactsProvider>();
        facts.Setup(f => f.ResolveAsync(It.IsAny<RequestDescriptor>(), It.IsAny<ExecutionContextSnapshot>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .Callback<RequestDescriptor, ExecutionContextSnapshot, object, CancellationToken>((d, s, r, _) => evaluated.Add((d, s, r)))
            .ReturnsAsync((AccessFacts)null!);
        var policy = new Mock<IAccessPolicyEvaluator>();
        policy.Setup(p => p.Evaluate(It.IsAny<RequestDescriptor>(), It.IsAny<ExecutionContextSnapshot>(), It.IsAny<AccessFacts>(), It.IsAny<object>())).Returns(decision);
        var context = new Mock<IExecutionContextReader>();
        context.SetupGet(c => c.Snapshot).Returns(new ExecutionContextSnapshot(
            UserId, AccountId, WorkspaceId, ResourceRef.Create(ResourceKind.Create("documents.page"), pageId, WorkspaceId),
            ApplicationPrincipalKind.Authenticated, ApplicationScopeKind.Resource, "trace"));
        return (new PageEditAccess(descriptors.Object, facts.Object, policy.Object, context.Object), evaluated);
    }

    [Fact]
    public async Task AnAllowedWrite_MeansTheCallerCanEdit_AndItIsAskedAsAWriteOfThatPage()
    {
        var pageId = Guid.CreateVersion7();
        var (sut, evaluated) = Create(AccessDecision.Allow(), pageId);

        (await sut.CanEditAsync(pageId, CancellationToken.None)).Should().BeTrue();

        var asked = evaluated.Single();
        asked.Request.Should().BeOfType<UpdatePageCommand>().Which.Action.Should().Be(PermissionAction.UpdatePage);
        asked.Snapshot.UserId.Should().Be(UserId, "the decision is the caller's own");
        asked.Snapshot.Resource!.ResourceId.Should().Be(pageId);
    }

    [Theory]
    [InlineData(AccessDecisionKind.Forbidden)]
    [InlineData(AccessDecisionKind.NotFound)]
    [InlineData(AccessDecisionKind.Unauthorized)]
    [InlineData(AccessDecisionKind.SecurityMisconfiguration)]
    public async Task AnyOtherDecision_MeansTheCallerCannotEdit(AccessDecisionKind kind)
    {
        var pageId = Guid.CreateVersion7();
        var (sut, _) = Create(AccessDecision.Deny(kind, "no"), pageId);

        (await sut.CanEditAsync(pageId, CancellationToken.None)).Should().BeFalse();
    }

    [Theory]
    [InlineData(true, false, true)]
    [InlineData(false, false, false)]
    [InlineData(true, true, false)]
    public async Task GetPage_ReportsCanEdit_FromTheWriteDecision_AndNeverForATrashedPage(bool mayWrite, bool trashed, bool expected)
    {
        var page = Page.Create(AccountId, WorkspaceId, "Runbook", UserId, Now);
        if (trashed) page.Archive(UserId, Now.AddMinutes(1));
        var context = new Mock<IDocumentDbContext>();
        context.Setup(c => c.Pages).Returns(TestDbSet.Create(new List<Page> { page }).Object);
        var visibility = new Mock<IResourceVisibility>();
        visibility.Setup(v => v.VisibleSpaceIdsAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Guid>());
        var access = new Mock<IPageEditAccess>();
        access.Setup(a => a.CanEditAsync(page.Id, It.IsAny<CancellationToken>())).ReturnsAsync(mayWrite);
        var requestContext = new FakeCurrentRequestContext().AsUser(UserId);
        requestContext.Tenant.SetWorkspace(AccountId, WorkspaceId, UserId);
        var sut = new GetPageQueryHandler(context.Object, visibility.Object, requestContext, access.Object);

        var dto = (await sut.Handle(new GetPageQuery(page.Id), CancellationToken.None)).Data!;

        dto.CanEdit.Should().Be(expected);
    }
}
