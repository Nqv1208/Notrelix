using Notrelix.Domain.FixtureA;
using Notrelix.Domain.FixtureB;

namespace Notrelix.Architecture.Tests.DomainPurity;

public class DomainReferenceGraphTests
{
    private static readonly IReadOnlySet<Type> NoApprovals = new HashSet<Type>();

    private static readonly IReadOnlyCollection<string> FixtureContexts =
        new[] { "FixtureA", "FixtureB" };

    private static IReadOnlyList<DomainReferenceViolation> Analyze(params Type[] types)
    {
        return DomainReferenceGraph.Analyze(types, NoApprovals);
    }

    [Fact]
    public void InternalClassField_IsDetected()
    {
        var violations = Analyze(typeof(InternalFieldFixture));

        violations.Should().Contain(v =>
            v.DeclaringType == typeof(InternalFieldFixture).FullName
            && v.Surface == "field:Field"
            && v.ReferencedType == typeof(FixtureBType).FullName);
    }

    [Fact]
    public void InterfaceInheritance_IsDetected()
    {
        var violations = Analyze(typeof(InterfaceInheritanceFixture), typeof(IFixtureAChild));

        violations.Should().Contain(v =>
            v.DeclaringType == typeof(InterfaceInheritanceFixture).FullName
            && v.Surface == "interface"
            && v.ReferencedType == typeof(IFixtureB).FullName);

        violations.Should().Contain(v =>
            v.DeclaringType == typeof(IFixtureAChild).FullName
            && v.Surface == "interface"
            && v.ReferencedType == typeof(IFixtureB).FullName);
    }

    [Fact]
    public void PrivateProperty_IsDetected()
    {
        var violations = Analyze(typeof(PrivatePropertyFixture));

        violations.Should().Contain(v =>
            v.DeclaringType == typeof(PrivatePropertyFixture).FullName
            && v.Surface == "property:Value"
            && v.ReferencedType == typeof(FixtureBType).FullName);
    }

    [Fact]
    public void StaticMethodReturn_IsDetected()
    {
        var violations = Analyze(typeof(StaticMethodReturnFixture));

        violations.Should().Contain(v =>
            v.DeclaringType == typeof(StaticMethodReturnFixture).FullName
            && v.Surface == "method-return:Get"
            && v.ReferencedType == typeof(FixtureBType).FullName);
    }

    [Fact]
    public void ConstructorParameter_IsDetected()
    {
        var violations = Analyze(typeof(ConstructorParameterFixture));

        violations.Should().Contain(v =>
            v.DeclaringType == typeof(ConstructorParameterFixture).FullName
            && v.Surface.StartsWith("constructor:", StringComparison.Ordinal)
            && v.Surface.Contains(typeof(FixtureBType).FullName!, StringComparison.Ordinal)
            && v.ReferencedType == typeof(FixtureBType).FullName);
    }

    [Fact]
    public void GenericArgument_IsDetected()
    {
        var violations = Analyze(typeof(GenericArgumentFixture));

        violations.Should().Contain(v =>
            v.DeclaringType == typeof(GenericArgumentFixture).FullName
            && v.Surface == "method-param:Set:values"
            && v.ReferencedType == typeof(FixtureBType).FullName);
    }

    [Fact]
    public void GenericConstraint_IsDetected()
    {
        var violations = Analyze(typeof(GenericConstraintFixture<>));

        violations.Should().Contain(v =>
            v.DeclaringType == typeof(GenericConstraintFixture<>).FullName
            && v.Surface == "generic-constraint:T"
            && v.ReferencedType == typeof(FixtureBConstraint).FullName);
    }

    [Fact]
    public void EventDelegatePayload_IsDetected()
    {
        var violations = Analyze(typeof(EventDelegatePayloadFixture));

        violations.Should().Contain(v =>
            v.DeclaringType == typeof(EventDelegatePayloadFixture).FullName
            && v.Surface == "event:Changed"
            && v.ReferencedType == typeof(FixtureBEvent).FullName);
    }

    [Fact]
    public void AttributeType_IsDetected()
    {
        var violations = Analyze(typeof(AttributeTypeUsageFixture));

        violations.Should().Contain(v =>
            v.DeclaringType == typeof(AttributeTypeUsageFixture).FullName
            && v.Surface == $"attribute:{typeof(FixtureBAttribute).FullName}"
            && v.ReferencedType == typeof(FixtureBAttribute).FullName);
    }

    [Fact]
    public void AttributeConstructorArgument_IsDetected()
    {
        var violations = Analyze(typeof(AttributeConstructorArgumentFixture));

        violations.Should().Contain(v =>
            v.DeclaringType == typeof(AttributeConstructorArgumentFixture).FullName
            && v.Surface == $"attribute-argument:{typeof(FixtureAAttribute).FullName}"
            && v.ReferencedType == typeof(FixtureBType).FullName);
    }

    [Fact]
    public void AttributeNamedArgument_IsDetected()
    {
        var violations = Analyze(typeof(AttributeNamedArgumentFixture));

        violations.Should().Contain(v =>
            v.DeclaringType == typeof(AttributeNamedArgumentFixture).FullName
            && v.Surface == $"attribute-argument:{typeof(FixtureAAttribute).FullName}"
            && v.ReferencedType == typeof(FixtureBType).FullName);
    }

    [Fact]
    public void ArrayAndByRefNestedTypes_AreDetected()
    {
        var violations = Analyze(typeof(ArrayByRefFixture));

        violations.Should().Contain(v =>
            v.DeclaringType == typeof(ArrayByRefFixture).FullName
            && v.Surface == "method-param:Go:values"
            && v.ReferencedType == typeof(FixtureBType).FullName);

        violations.Should().Contain(v =>
            v.DeclaringType == typeof(ArrayByRefFixture).FullName
            && v.Surface == "method-param:Go:item"
            && v.ReferencedType == typeof(FixtureBType).FullName);
    }

    [Fact]
    public void DelegateInvokeSignature_IsDetected()
    {
        var violations = Analyze(typeof(FixtureADelegate));

        violations.Should().Contain(v =>
            v.DeclaringType == typeof(FixtureADelegate).FullName
            && v.Surface == "method-return:Invoke"
            && v.ReferencedType == typeof(FixtureBType).FullName);
    }

    [Fact]
    public void SameContext_Common_SharedKernel_System_AreAllowed()
    {
        var violations = Analyze(
            typeof(FixtureASelfReferenceFixture),
            typeof(CommonFieldFixture),
            typeof(SharedKernelFieldFixture),
            typeof(SystemBclFixture));

        violations.Should().BeEmpty();
    }

    [Fact]
    public void FullyQualifiedSourceName_IsDetected()
    {
        var violations = ScanSource(
            "class C { Notrelix.Domain.FixtureB.SomeType Field; }");

        violations.Should().Contain(v => v.Contains("reference Notrelix.Domain.FixtureB.SomeType", StringComparison.Ordinal));
    }

    [Fact]
    public void GlobalQualifiedSourceName_IsDetected()
    {
        var violations = ScanSource(
            "class C { global::Notrelix.Domain.FixtureB.SomeType Field; }");

        violations.Should().Contain(v => v.Contains("global-qualified Notrelix.Domain.FixtureB.SomeType", StringComparison.Ordinal));
    }

    [Fact]
    public void ForeignUsingAlias_IsDetected()
    {
        var violations = ScanSource(
            "using FB = Notrelix.Domain.FixtureB;\nclass C { FB.SomeType Field; }");

        violations.Should().Contain(v => v.Contains("using-alias FB = Notrelix.Domain.FixtureB", StringComparison.Ordinal));
        violations.Should().Contain(v => v.Contains("alias-qualified FB: Notrelix.Domain.FixtureB", StringComparison.Ordinal));
    }

    [Fact]
    public void UsingStatic_IsDetected()
    {
        var violations = ScanSource(
            "using static Notrelix.Domain.FixtureB.SomeStatic;");

        violations.Should().Contain(v => v.Contains("using-static Notrelix.Domain.FixtureB.SomeStatic", StringComparison.Ordinal));
    }

    [Fact]
    public void GlobalUsing_IsDetected()
    {
        var violations = ScanSource(
            "global using Notrelix.Domain.FixtureB;");

        violations.Should().Contain(v => v.Contains("global-using Notrelix.Domain.FixtureB", StringComparison.Ordinal));
    }

    [Fact]
    public void PlainUsing_IsDetected()
    {
        var violations = ScanSource(
            "using Notrelix.Domain.FixtureB;");

        violations.Should().Contain(v => v.Contains("using Notrelix.Domain.FixtureB", StringComparison.Ordinal));
    }

    [Fact]
    public void SameContextAndCommonUsings_AreAllowed()
    {
        var violations = ScanSource(
            "using Notrelix.Domain.FixtureA;\nusing Notrelix.Domain.Common;");

        violations.Should().BeEmpty();
    }

    [Fact]
    public void NamespaceDeclaration_IsNotReported()
    {
        var violations = ScanSource(
            "namespace Notrelix.Domain.FixtureA { class C { } }");

        violations.Should().BeEmpty();
    }

    [Fact]
    public void StringLiteral_IsNotReported()
    {
        var violations = ScanSource(
            "class C { const string S = \"Notrelix.Domain.FixtureB.Something\"; }");

        violations.Should().BeEmpty();
    }

    private static IReadOnlyList<string> ScanSource(string source)
    {
        return DomainReferenceGraph.ScanSource(source, "FixtureA", FixtureContexts, "fixture.cs");
    }
}
