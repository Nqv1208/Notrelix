using Notrelix.Application.Common.Abstractions.Rls;

namespace Notrelix.Infrastructure.Tests.Data.Rls;

public class RlsOptionsTests
{
    [Fact]
    public void Defaults_DisableRuntimeRlsAndStartupPolicyApplication()
    {
        var options = new RlsOptions();

        options.Enabled.Should().BeFalse();
        options.ApplyPoliciesOnStartup.Should().BeFalse();
        options.SetSessionContext.Should().BeFalse();
    }
}
