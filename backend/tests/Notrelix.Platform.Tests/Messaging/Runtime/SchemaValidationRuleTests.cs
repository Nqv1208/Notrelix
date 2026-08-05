using System.Text;
using FluentAssertions;
using Moq;
using Notrelix.Platform.Messaging.Contracts;
using Notrelix.Platform.Messaging.Runtime;
using Xunit;

namespace Notrelix.Platform.Tests.Messaging.Runtime;

public sealed class SchemaValidationRuleTests
{
    private readonly Mock<ICanonicalizer> _canonicalizerMock = new();
    private readonly Mock<IEventDescriptorProvider> _providerMock = new();
    private readonly SchemaValidationRule _sut;

    public SchemaValidationRuleTests()
    {
        _sut = new SchemaValidationRule(
            _canonicalizerMock.Object,
            _providerMock.Object);
    }

    [Fact]
    public void Validate_ShouldPass_WhenNoSchema()
    {
        _providerMock.Setup(p => p.Get("test.event", 1))
            .Returns(new EventDescriptor
            {
                Name = "test.event",
                Version = 1,
                EventType = typeof(object),
            });

        var result = _sut.Validate(Encoding.UTF8.GetBytes("{}"), "test.event", 1);

        result.IsValid.Should().BeTrue();
        result.IsWarning.Should().BeFalse();
    }

    [Fact]
    public void Validate_ShouldWarn_WhenDescriptorNotFound()
    {
        _providerMock.Setup(p => p.Get("unknown.event", 1))
            .Throws(new UnknownEventDescriptorException("not found"));

        var result = _sut.Validate(Encoding.UTF8.GetBytes("{}"), "unknown.event", 1);

        result.IsValid.Should().BeTrue();
        result.IsWarning.Should().BeTrue();
        result.Message.Should().Contain("No schema");
    }

    [Fact]
    public void Validate_ShouldWarn_WhenEventIsDeprecated()
    {
        _providerMock.Setup(p => p.Get("deprecated.event", 1))
            .Returns(new EventDescriptor
            {
                Name = "deprecated.event",
                Version = 1,
                EventType = typeof(object),
                Schema = new SchemaDefinition
                {
                    EventName = "deprecated.event",
                    Version = 1,
                    Schema = "{}",
                },
                Deprecated = true,
                DeprecationDate = new DateOnly(2026, 1, 1),
            });

        var result = _sut.Validate(Encoding.UTF8.GetBytes("{}"), "deprecated.event", 1);

        result.IsValid.Should().BeTrue();
        result.IsWarning.Should().BeTrue();
        result.Message.Should().Contain("deprecated");
    }

    [Fact]
    public void Validate_ShouldCanonicalize_WhenSchemaExists()
    {
        var canonicalBytes = new ReadOnlyMemory<byte>(Encoding.UTF8.GetBytes("{\"a\":1}"));

        _providerMock.Setup(p => p.Get("test.event", 1))
            .Returns(new EventDescriptor
            {
                Name = "test.event",
                Version = 1,
                EventType = typeof(object),
                Schema = new SchemaDefinition
                {
                    EventName = "test.event",
                    Version = 1,
                    Schema = "{}",
                },
            });

        _canonicalizerMock.Setup(c => c.Canonicalize(It.IsAny<ReadOnlyMemory<byte>>()))
            .Returns(canonicalBytes);

        var result = _sut.Validate(Encoding.UTF8.GetBytes("{\"b\":1,\"a\":1}"), "test.event", 1);

        result.IsValid.Should().BeTrue();
        result.CanonicalData.Should().NotBeNull();
    }
}
