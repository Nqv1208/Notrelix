namespace Notrelix.Architecture.Tests;

public class DomainModelContractChecks : ArchitectureTestBase
{
    [Fact]
    public void ResourceKind_Label_Kind_Is_Canonical()
    {
        var content = File.ReadAllText(
            Path.Combine(GetInfrastructurePath(), "Services", "ResourceScopeResolver.cs"));
        content.Should().Contain("work-management.label",
            "label resources must map through the canonical ResourceKind kind string");
    }

    [Fact]
    public void PermissionContext_HasAccountId()
    {
        var content = File.ReadAllText(Path.Combine(GetApplicationPath(), "Common", "Security", "PermissionContext.cs"));
        content.Should().Contain("Guid AccountId", "PermissionContext record must have AccountId field");
    }
}
