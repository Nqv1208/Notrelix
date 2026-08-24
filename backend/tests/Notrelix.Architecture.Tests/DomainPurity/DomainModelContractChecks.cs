namespace Notrelix.Architecture.Tests;

public class DomainModelContractChecks : ArchitectureTestBase
{
    [Fact]
    public void ResourceKind_Label_Kind_Is_Canonical()
    {
        var content = File.ReadAllText(
            Path.Combine(GetInfrastructurePath(), "Services", "ResourceLocator.cs"));
        content.Should().Contain("work-management.label",
            "label resources must map through the canonical ResourceKind kind string");
    }

    [Fact]
    public void AccessFacts_HasAccountMemberRole()
    {
        var content = File.ReadAllText(Path.Combine(GetApplicationPath(), "Common", "Security", "AccessFacts.cs"));
        content.Should().Contain("AccountMemberRole", "AccessFacts record must expose the account member role");
    }
}
