using Notrelix.Application.EventMappers.WorkManagement;

namespace Notrelix.Application.Tests.EventMappers;

/// <summary>AUT-022: facts committed while an Automation execution acts carry that execution as causation.</summary>
public class BoardItemMovedCausationTests
{
    private static BoardItemMovedDomainEvent Moved() =>
        new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "a", Guid.NewGuid(), DateTimeOffset.UtcNow, 3);

    [Fact]
    public void MovedFact_CarriesTheExecutionAsCausation_WhenTheContextHasOne()
    {
        var executionId = Guid.NewGuid();
        var correlation = new Mock<ICorrelationContext>();
        correlation.SetupGet(c => c.CausationId).Returns(executionId);

        new BoardEventMapper(correlation.Object).Map(Moved())!.CausationId.Should().Be(executionId);
    }

    [Fact]
    public void MovedFact_HasNoCausation_ForUserRequests()
    {
        var correlation = new Mock<ICorrelationContext>();
        correlation.SetupGet(c => c.CausationId).Returns((Guid?)null);

        new BoardEventMapper(correlation.Object).Map(Moved())!.CausationId.Should().BeNull();
        new BoardEventMapper().Map(Moved())!.CausationId.Should().BeNull();
    }
}
