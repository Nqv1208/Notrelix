using System.Reflection;
using Notrelix.Domain.Common;

#pragma warning disable CS0649

namespace Notrelix.Architecture.Tests;

public class DomainTypeGraphWalkerTests
{
    private static readonly Assembly DomainAssembly = typeof(AggregateRoot).Assembly;

    private static readonly Func<Type, bool> CollectTestOrDomainTypes = type =>
        type.Assembly == typeof(DomainTypeGraphWalkerTests).Assembly
        || type.Namespace?.StartsWith("Notrelix.Domain.", StringComparison.Ordinal) == true;

    [Fact]
    public void Walker_ShouldResolveBaseType()
    {
        var referenced = DomainTypeGraphWalker.GetReferencedTypes(typeof(AggregateRoot));
        referenced.Should().Contain(t => t == typeof(Entity));
    }

    [Fact]
    public void Walker_ShouldResolveInterfaces()
    {
        var referenced = DomainTypeGraphWalker.GetReferencedTypes(typeof(AggregateRoot));
        referenced.Should().Contain(t => t == typeof(Entity));
    }

    [Fact]
    public void Walker_ShouldResolveGenericArguments()
    {
        var referenced = DomainTypeGraphWalker.GetReferencedTypes(typeof(MyGenericType), CollectTestOrDomainTypes);
        referenced.Should().Contain(t => t == typeof(SomeValue));
    }

    [Fact]
    public void Walker_ShouldHandleCycles()
    {
        var referenced = DomainTypeGraphWalker.GetReferencedTypes(typeof(CycleTypeA), CollectTestOrDomainTypes);
        referenced.Should().NotBeEmpty();
    }

    [Fact]
    public void Walker_ShouldResolveFieldTypes()
    {
        var referenced = DomainTypeGraphWalker.GetReferencedTypes(typeof(FieldTypeHolder), CollectTestOrDomainTypes);
        referenced.Should().Contain(t => t == typeof(SomeValue));
    }

    [Fact]
    public void Walker_ShouldResolvePropertyTypes()
    {
        var referenced = DomainTypeGraphWalker.GetReferencedTypes(typeof(PropertyTypeHolder), CollectTestOrDomainTypes);
        referenced.Should().Contain(t => t == typeof(SomeValue));
    }

    [Fact]
    public void Walker_ShouldResolveMethodReturnTypes()
    {
        var referenced = DomainTypeGraphWalker.GetReferencedTypes(typeof(MethodReturnTypeHolder), CollectTestOrDomainTypes);
        referenced.Should().Contain(t => t == typeof(SomeValue));
    }

    [Fact]
    public void Walker_ShouldResolveMethodParameterTypes()
    {
        var referenced = DomainTypeGraphWalker.GetReferencedTypes(typeof(MethodParameterHolder), CollectTestOrDomainTypes);
        referenced.Should().Contain(t => t == typeof(SomeValue));
    }

    [Fact]
    public void Walker_ShouldResolveNestedGenericArguments()
    {
        var referenced = DomainTypeGraphWalker.GetReferencedTypes(
            typeof(IReadOnlyDictionary<,>).MakeGenericType(typeof(Guid), typeof(List<>).MakeGenericType(typeof(SomeValue))),
            CollectTestOrDomainTypes);
        var names = referenced.Select(t => t.FullName).ToHashSet();
        names.Should().Contain(n => n!.Contains("SomeValue"));
    }
}

internal class SomeValue
{
    public string? Data { get; set; }
}

#pragma warning disable CS0649
internal class FieldTypeHolder
{
    internal SomeValue? _field;
}
#pragma warning restore CS0649

internal class PropertyTypeHolder
{
    internal SomeValue? Prop { get; set; }
}

internal class MethodReturnTypeHolder
{
    internal SomeValue? GetValue() => null;
}

internal class MethodParameterHolder
{
    internal void SetValue(SomeValue value) { }
}

internal class MyGenericType : IComparable<SomeValue>
{
    public int CompareTo(SomeValue? other) => 0;
}

internal class CycleTypeA
{
    internal CycleTypeB? B { get; set; }
}

internal class CycleTypeB
{
    internal CycleTypeA? A { get; set; }
}
