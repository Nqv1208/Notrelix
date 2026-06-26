using FluentAssertions;
using Notrelix.Domain.Workspaces.Rules;

namespace Notrelix.Domain.Tests.Workspaces;

public class WorkspaceInvitationRulesTests
{
    [Fact]
    public void StaticClass_ShouldExist()
    {
        typeof(WorkspaceInvitationRules).Should().NotBeNull();
    }
}
