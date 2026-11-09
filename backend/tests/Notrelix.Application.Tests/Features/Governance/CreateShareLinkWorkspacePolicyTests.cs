using Notrelix.Application.Features.Governance.Abstractions;
using Notrelix.Application.Features.Governance.ShareLinks.Commands.CreateShareLink;

namespace Notrelix.Application.Tests.Features.Governance;

public class CreateShareLinkWorkspacePolicyTests
{
    [Fact]
    public async Task WhenTheWorkspaceTurnedShareLinksOff_TheLinkIsRefusedWithItsRuleCode_AndNothingIsWritten()
    {
        var workspaceId = Guid.CreateVersion7();
        var context = new Mock<IGovernanceDbContext>(MockBehavior.Strict);
        var requestContext = new Mock<ICurrentRequestContext>();
        requestContext.Setup(r => r.UserId).Returns(Guid.CreateVersion7());
        requestContext.Setup(r => r.RequireWorkspaceId()).Returns(workspaceId);
        requestContext.Setup(r => r.RequireAccountId()).Returns(Guid.CreateVersion7());
        var policy = new Mock<IWorkspaceSharingPolicy>();
        policy.Setup(p => p.AllowsShareLinksAsync(workspaceId, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var handler = new CreateShareLinkCommandHandler(
            context.Object,
            requestContext.Object,
            new Mock<IDateTimeProvider>().Object,
            policy.Object);

        var command = new CreateShareLinkCommand("work-management.board", Guid.CreateVersion7(), "Viewer");

        var refusal = await handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<Notrelix.Domain.Common.Exceptions.BusinessRuleException>();
        refusal.Which.RuleCode.Should().Be("ShareLinksTurnedOff");

        // The strict mock fails on any context access, so no share link was added or read.
        context.VerifyNoOtherCalls();
    }
}
