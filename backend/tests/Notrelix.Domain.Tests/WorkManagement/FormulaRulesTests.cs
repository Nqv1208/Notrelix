using FluentAssertions;
using Notrelix.Domain.Common.Exceptions;
using Notrelix.Domain.WorkManagement.Formulas;
using Xunit;

namespace Notrelix.Domain.Tests.WorkManagement;

public class FormulaRulesTests
{
    [Fact]
    public void EnsureNoCircularDependency_WhenFieldNotInDependencies_ShouldNotThrow()
    {
        var fieldId = Guid.NewGuid();
        var deps = new[] { Guid.NewGuid(), Guid.NewGuid() };

        var act = () => FormulaRules.EnsureNoCircularDependency(fieldId, deps);
        act.Should().NotThrow();
    }

    [Fact]
    public void EnsureNoCircularDependency_WhenEmptyDependencies_ShouldNotThrow()
    {
        var fieldId = Guid.NewGuid();
        var deps = Array.Empty<Guid>();

        var act = () => FormulaRules.EnsureNoCircularDependency(fieldId, deps);
        act.Should().NotThrow();
    }

    [Fact]
    public void EnsureNoCircularDependency_WhenFieldReferencesItself_ShouldThrow()
    {
        var fieldId = Guid.NewGuid();
        var deps = new[] { fieldId };

        var act = () => FormulaRules.EnsureNoCircularDependency(fieldId, deps);
        act.Should().Throw<DomainException>().WithMessage("*Circular dependency*");
    }

    [Fact]
    public void EnsureNoCircularDependency_WhenFieldInMultipleDependencies_ShouldThrow()
    {
        var fieldId = Guid.NewGuid();
        var deps = new[] { Guid.NewGuid(), fieldId, Guid.NewGuid() };

        var act = () => FormulaRules.EnsureNoCircularDependency(fieldId, deps);
        act.Should().Throw<DomainException>().WithMessage("*Circular dependency*");
    }
}
