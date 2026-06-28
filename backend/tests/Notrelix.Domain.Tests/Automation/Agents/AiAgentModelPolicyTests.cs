using FluentAssertions;
using Notrelix.Domain.Automation.Agents;

namespace Notrelix.Domain.Tests.Automation;

public class AiAgentModelPolicyTests
{
    [Fact]
    public void Create_ShouldSucceed()
    {
        var policy = AiAgentModelPolicy.Create("gpt-4");

        policy.ModelId.Should().Be("gpt-4");
        policy.MaxTokens.Should().BeNull();
        policy.Temperature.Should().BeNull();
        policy.Provider.Should().BeNull();
    }

    [Fact]
    public void Create_WithAllFields_ShouldSetThem()
    {
        var policy = AiAgentModelPolicy.Create("claude-3", 4096, 0.7, "anthropic");

        policy.MaxTokens.Should().Be(4096);
        policy.Temperature.Should().Be(0.7);
        policy.Provider.Should().Be("anthropic");
    }

    [Fact]
    public void Create_WithTemperatureBoundary_ShouldSucceed()
    {
        var min = AiAgentModelPolicy.Create("m", temperature: 0.0);
        var max = AiAgentModelPolicy.Create("m", temperature: 2.0);

        min.Temperature.Should().Be(0.0);
        max.Temperature.Should().Be(2.0);
    }

    [Fact]
    public void Create_WithTemperatureOutOfRange_ShouldThrow()
    {
        var actLow = () => AiAgentModelPolicy.Create("m", temperature: -0.1);
        actLow.Should().Throw<BusinessRuleException>();

        var actHigh = () => AiAgentModelPolicy.Create("m", temperature: 2.1);
        actHigh.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Create_WithEmptyModelId_ShouldThrow()
    {
        var act = () => AiAgentModelPolicy.Create("");
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void FromJson_ShouldParse()
    {
        var policy = AiAgentModelPolicy.FromJson("{\"modelId\":\"gpt-4\",\"maxTokens\":2048,\"temperature\":0.5,\"provider\":\"openai\"}");

        policy.ModelId.Should().Be("gpt-4");
        policy.MaxTokens.Should().Be(2048);
        policy.Temperature.Should().Be(0.5);
        policy.Provider.Should().Be("openai");
    }

    [Fact]
    public void FromJson_WithInvalidJson_ShouldThrow()
    {
        var act = () => AiAgentModelPolicy.FromJson("bad");
        act.Should().Throw<BusinessRuleException>().WithMessage("*JSON*");
    }

    [Fact]
    public void ToJson_ShouldRoundTrip()
    {
        var policy = AiAgentModelPolicy.Create("gpt-4", 4096, 1.5, "openai");

        var json = policy.ToJson();
        var parsed = AiAgentModelPolicy.FromJson(json);

        parsed.Should().Be(policy);
    }

    [Fact]
    public void Equality_SameValues_ShouldBeEqual()
    {
        var p1 = AiAgentModelPolicy.Create("gpt-4", 2048, 0.5);
        var p2 = AiAgentModelPolicy.Create("gpt-4", 2048, 0.5);

        p1.Should().Be(p2);
    }
}
