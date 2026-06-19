using FluentAssertions;
using Notrelix.Domain.Workspaces.Rules;
using Xunit;

namespace Notrelix.Domain.Tests.Workspaces;

public class WorkspaceInvitationRulesTests
{
    [Fact]
    public void StaticClass_ShouldExist()
    {
        typeof(WorkspaceInvitationRules).Should().NotBeNull();
    }
}
