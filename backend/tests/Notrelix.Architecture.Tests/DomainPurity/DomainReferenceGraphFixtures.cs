namespace Notrelix.Domain.FixtureB
{
    public class FixtureBType { }

    public class FixtureBBase { }

    public interface IFixtureB { }

    public sealed class FixtureBEvent { }

    public class FixtureBConstraint { }

    public sealed class FixtureBAttribute : System.Attribute { }

    public static class SomeStatic
    {
        public static void Go()
        {
        }
    }
}

namespace Notrelix.Domain.FixtureA
{
    using System;
    using System.Collections.Generic;
    using Notrelix.Domain.FixtureB;

    public class InternalFieldFixture
    {
        internal FixtureBType? Field;
    }

    public class InterfaceInheritanceFixture : IFixtureB
    {
    }

    public interface IFixtureAChild : IFixtureB
    {
    }

    public class PrivatePropertyFixture
    {
        private FixtureBType? Value { get; set; }
    }

    public class StaticMethodReturnFixture
    {
        public static FixtureBType Get() => new();
    }

    public class ConstructorParameterFixture
    {
        public ConstructorParameterFixture(FixtureBType value)
        {
        }
    }

    public class GenericArgumentFixture
    {
        public void Set(IEnumerable<FixtureBType> values)
        {
        }
    }

    public class GenericConstraintFixture<T>
        where T : FixtureBConstraint
    {
    }

    public class EventDelegatePayloadFixture
    {
        public event EventHandler<FixtureBEvent>? Changed;
    }

    public sealed class FixtureAAttribute : Attribute
    {
        public FixtureAAttribute(Type? type)
        {
        }

        public Type? Named { get; set; }
    }

    public class AttributeTypeUsageFixture
    {
        [FixtureBAttribute]
        public void Go()
        {
        }
    }

    public class AttributeConstructorArgumentFixture
    {
        [FixtureAAttribute(typeof(FixtureBType))]
        public void Go()
        {
        }
    }

    public class AttributeNamedArgumentFixture
    {
        [FixtureAAttribute(null, Named = typeof(FixtureBType))]
        public void Go()
        {
        }
    }

    public class ArrayByRefFixture
    {
        public void Go(FixtureBType[] values, ref FixtureBType item)
        {
        }
    }

    public class BaseClassFixture : FixtureBBase
    {
    }

    public delegate FixtureBType FixtureADelegate();

    public class FixtureASelfReferenceFixture
    {
        public FixtureASelfReferenceFixture? Next;
    }

    public class CommonFieldFixture
    {
        public Notrelix.Domain.Common.DomainEvent? Value;
    }

    public class SharedKernelFieldFixture
    {
        public Notrelix.Domain.SharedKernel.Slug? Value;
    }

    public class SystemBclFixture
    {
        public string? Text;
        public Guid Id;
        public List<string>? Items;
        public DateTimeOffset At;
    }
}
