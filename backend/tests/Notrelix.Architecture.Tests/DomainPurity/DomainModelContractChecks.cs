namespace Notrelix.Architecture.Tests;

public class DomainModelContractChecks : ArchitectureTestBase
{
    [Fact]
    public void ResourceType_IncludesLabel()
    {
        var content = File.ReadAllText(Path.Combine(GetDomainPath(), "SharedKernel", "ResourceType.cs"));
        content.Should().Contain("Label", "ResourceType enum must include Label for label resources");
    }

    [Fact]
    public void PermissionContext_HasAccountId()
    {
        var content = File.ReadAllText(Path.Combine(GetApplicationPath(), "Common", "Security", "PermissionContext.cs"));
        content.Should().Contain("Guid AccountId", "PermissionContext record must have AccountId field");
    }
}
