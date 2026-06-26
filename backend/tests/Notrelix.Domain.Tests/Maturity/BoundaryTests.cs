using FluentAssertions;
using Notrelix.Domain.Workspaces.Workspaces;

namespace Notrelix.Domain.Tests.Maturity;

public class BoundaryTests
{
    [Fact]
    public void Create_WithEmptyName_ShouldThrow()
    {
        var act = () => Workspace.Create(Guid.NewGuid(), "", "my-workspace", DateTimeOffset.UtcNow);

        act.Should().Throw<BusinessRuleException>().WithMessage("*null or whitespace*");
    }

    [Fact]
    public void Create_WithNullName_ShouldThrow()
    {
        var act = () => Workspace.Create(Guid.NewGuid(), null!, "my-workspace", DateTimeOffset.UtcNow);

        act.Should().Throw<BusinessRuleException>().WithMessage("*null or whitespace*");
    }

    [Fact]
    public void Create_WithWhitespaceName_ShouldThrow()
    {
        var act = () => Workspace.Create(Guid.NewGuid(), "   ", "my-workspace", DateTimeOffset.UtcNow);

        act.Should().Throw<BusinessRuleException>().WithMessage("*null or whitespace*");
    }

    [Fact]
    public void Create_WithEmptySlug_ShouldThrow()
    {
        var act = () => Workspace.Create(Guid.NewGuid(), "My Workspace", "", DateTimeOffset.UtcNow);

        act.Should().Throw<BusinessRuleException>().WithMessage("*null or whitespace*");
    }

    [Fact]
    public void Create_WithNullSlug_ShouldThrow()
    {
        var act = () => Workspace.Create(Guid.NewGuid(), "My Workspace", null!, DateTimeOffset.UtcNow);

        act.Should().Throw<BusinessRuleException>().WithMessage("*null or whitespace*");
    }

    [Fact]
    public void Create_WithWhitespaceSlug_ShouldThrow()
    {
        var act = () => Workspace.Create(Guid.NewGuid(), "My Workspace", "   ", DateTimeOffset.UtcNow);

        act.Should().Throw<BusinessRuleException>().WithMessage("*null or whitespace*");
    }

    [Fact]
    public void Create_WithMixedCaseSlug_ShouldBeLowercase()
    {
        var workspace = Workspace.Create(Guid.NewGuid(), "My Workspace", "My-Workspace", DateTimeOffset.UtcNow);

        workspace.Slug.Should().Be("my-workspace");
    }

    [Fact]
    public void Create_WithNameHasExtraSpaces_ShouldTrim()
    {
        var workspace = Workspace.Create(Guid.NewGuid(), "  My Workspace  ", "my-workspace", DateTimeOffset.UtcNow);

        workspace.Name.Should().Be("My Workspace");
    }

    [Fact]
    public void Rename_WithEmptyName_ShouldThrow()
    {
        var workspace = Workspace.Create(Guid.NewGuid(), "My Workspace", "my-workspace", DateTimeOffset.UtcNow);

        var act = () => workspace.Rename("", Guid.NewGuid(), DateTimeOffset.UtcNow);

        act.Should().Throw<BusinessRuleException>().WithMessage("*null or whitespace*");
    }

    [Fact]
    public void Rename_WithNullName_ShouldThrow()
    {
        var workspace = Workspace.Create(Guid.NewGuid(), "My Workspace", "my-workspace", DateTimeOffset.UtcNow);

        var act = () => workspace.Rename(null!, Guid.NewGuid(), DateTimeOffset.UtcNow);

        act.Should().Throw<BusinessRuleException>().WithMessage("*null or whitespace*");
    }

    [Fact]
    public void Rename_WithWhitespaceName_ShouldThrow()
    {
        var workspace = Workspace.Create(Guid.NewGuid(), "My Workspace", "my-workspace", DateTimeOffset.UtcNow);

        var act = () => workspace.Rename("   ", Guid.NewGuid(), DateTimeOffset.UtcNow);

        act.Should().Throw<BusinessRuleException>().WithMessage("*null or whitespace*");
    }

    [Fact]
    public void Rename_WithNameHasExtraSpaces_ShouldTrim()
    {
        var workspace = Workspace.Create(Guid.NewGuid(), "My Workspace", "my-workspace", DateTimeOffset.UtcNow);

        workspace.Rename("  New Name  ", Guid.NewGuid(), DateTimeOffset.UtcNow);

        workspace.Name.Should().Be("New Name");
    }
}
