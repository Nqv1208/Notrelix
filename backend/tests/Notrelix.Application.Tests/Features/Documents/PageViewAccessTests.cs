using Notrelix.Application.Features.Documents.Pages.Queries.GetPage;
using Notrelix.Application.Features.Documents.Pages.Services;

namespace Notrelix.Application.Tests.Features.Documents;

/// <summary>Whether another person may view a page is the same decision as reading the page for them, never the caller's.</summary>
public sealed class PageViewAccessTests
{
    private static readonly Guid AccountId = Guid.CreateVersion7();
    private static readonly Guid WorkspaceId = Guid.CreateVersion7();
    private static readonly Guid PageId = Guid.CreateVersion7();
    private static readonly Guid UserId = Guid.CreateVersion7();

    private static (PageViewAccess Sut, Mock<IAccessPolicyEvaluator> Policy, List<ExecutionContextSnapshot> Evaluated) Create(
        ResourceLocation? location, AccessDecision decision)
    {
        var locator = new Mock<IResourceLocator>();
        locator.Setup(l => l.LocateAsync(It.IsAny<ResourceRef>(), UserId, It.IsAny<CancellationToken>())).ReturnsAsync(location);
        var descriptors = new Mock<IRequestDescriptorRegistry>();
        descriptors.Setup(d => d.GetRequired(typeof(GetPageQuery))).Returns((RequestDescriptor)null!);
        var evaluated = new List<ExecutionContextSnapshot>();
        var facts = new Mock<IAccessFactsProvider>();
        facts.Setup(f => f.ResolveAsync(It.IsAny<RequestDescriptor>(), It.IsAny<ExecutionContextSnapshot>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .Callback<RequestDescriptor, ExecutionContextSnapshot, object, CancellationToken>((_, snapshot, _, _) => evaluated.Add(snapshot))
            .ReturnsAsync((AccessFacts)null!);
        var policy = new Mock<IAccessPolicyEvaluator>();
        policy.Setup(p => p.Evaluate(It.IsAny<RequestDescriptor>(), It.IsAny<ExecutionContextSnapshot>(), It.IsAny<AccessFacts>(), It.IsAny<object>())).Returns(decision);
        return (new PageViewAccess(locator.Object, descriptors.Object, facts.Object, policy.Object), policy, evaluated);
    }

    private static ResourceLocation Located(Guid? workspaceId = null) =>
        new(ResourceKind.Create("documents.page"), PageId, AccountId, workspaceId ?? WorkspaceId);

    [Fact]
    public async Task AnAllowedDecision_MeansThePersonCanView_AndItIsEvaluatedForThatPerson()
    {
        var (sut, _, evaluated) = Create(Located(), AccessDecision.Allow());

        (await sut.CanViewAsync(AccountId, WorkspaceId, PageId, UserId, CancellationToken.None)).Should().BeTrue();

        var snapshot = evaluated.Single();
        snapshot.UserId.Should().Be(UserId);
        snapshot.AccountId.Should().Be(AccountId);
        snapshot.WorkspaceId.Should().Be(WorkspaceId);
        snapshot.Resource!.ResourceId.Should().Be(PageId);
    }

    [Theory]
    [InlineData(AccessDecisionKind.Forbidden)]
    [InlineData(AccessDecisionKind.NotFound)]
    [InlineData(AccessDecisionKind.Unauthorized)]
    [InlineData(AccessDecisionKind.SecurityMisconfiguration)]
    public async Task AnyOtherDecision_MeansTheyCannot(AccessDecisionKind kind)
    {
        var (sut, _, _) = Create(Located(), AccessDecision.Deny(kind, "no"));

        (await sut.CanViewAsync(AccountId, WorkspaceId, PageId, UserId, CancellationToken.None)).Should().BeFalse();
    }

    [Fact]
    public async Task APageThePersonCannotBeLocatedFor_OrInAnotherWorkspace_IsNotViewable_WithoutAskingThePolicy()
    {
        var (missing, policyMissing, _) = Create(null, AccessDecision.Allow());
        var (elsewhere, policyElsewhere, _) = Create(Located(Guid.CreateVersion7()), AccessDecision.Allow());

        (await missing.CanViewAsync(AccountId, WorkspaceId, PageId, UserId, CancellationToken.None)).Should().BeFalse();
        (await elsewhere.CanViewAsync(AccountId, WorkspaceId, PageId, UserId, CancellationToken.None)).Should().BeFalse();
        policyMissing.VerifyNoOtherCalls();
        policyElsewhere.VerifyNoOtherCalls();
    }
}
