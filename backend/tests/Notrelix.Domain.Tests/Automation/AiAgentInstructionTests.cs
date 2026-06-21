using FluentAssertions;
using Notrelix.Domain.Automation.Agents;
using Notrelix.Domain.Common.Exceptions;
using Xunit;

namespace Notrelix.Domain.Tests.Automation;

public class AiAgentInstructionTests
{
    [Fact]
    public void Create_ShouldSucceed()
    {
        var instruction = AiAgentInstruction.Create("You are a helpful assistant.");

        instruction.SystemPrompt.Should().Be("You are a helpful assistant.");
        instruction.AgentDescription.Should().BeNull();
        instruction.Guidelines.Should().BeEmpty();
        instruction.ToneVoice.Should().BeNull();
    }

    [Fact]
    public void Create_WithAllFields_ShouldSetThem()
    {
        var guidelines = new[] { "Be concise", "Use markdown" };
        var instruction = AiAgentInstruction.Create("System prompt", "Assistant", guidelines, "Professional");

        instruction.AgentDescription.Should().Be("Assistant");
        instruction.Guidelines.Should().BeEquivalentTo(guidelines);
        instruction.ToneVoice.Should().Be("Professional");
    }

    [Fact]
    public void Create_WithEmptySystemPrompt_ShouldThrow()
    {
        var act = () => AiAgentInstruction.Create("");
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void FromJson_ShouldParse()
    {
        var json = "{\"systemPrompt\":\"Hello\",\"agentDescription\":\"Bot\",\"guidelines\":[\"Be nice\"],\"toneVoice\":\"Friendly\"}";

        var instruction = AiAgentInstruction.FromJson(json);

        instruction.SystemPrompt.Should().Be("Hello");
        instruction.AgentDescription.Should().Be("Bot");
        instruction.Guidelines.Should().ContainSingle("Be nice");
        instruction.ToneVoice.Should().Be("Friendly");
    }

    [Fact]
    public void FromJson_WithMinimalJson_ShouldSucceed()
    {
        var instruction = AiAgentInstruction.FromJson("{\"systemPrompt\":\"Hi\"}");

        instruction.SystemPrompt.Should().Be("Hi");
    }

    [Fact]
    public void FromJson_WithInvalidJson_ShouldThrow()
    {
        var act = () => AiAgentInstruction.FromJson("not-json");
        act.Should().Throw<BusinessRuleException>().WithMessage("*JSON*");
    }

    [Fact]
    public void ToJson_ShouldProduceValidJson()
    {
        var instruction = AiAgentInstruction.Create("Prompt", "Agent", new[] { "Guide" }, "Casual");

        var json = instruction.ToJson();
        var parsed = AiAgentInstruction.FromJson(json);

        parsed.Should().Be(instruction);
    }

    [Fact]
    public void Equality_SameValues_ShouldBeEqual()
    {
        var i1 = AiAgentInstruction.Create("Prompt", "Agent", new[] { "Guide" }, "Casual");
        var i2 = AiAgentInstruction.Create("Prompt", "Agent", new[] { "Guide" }, "Casual");

        i1.Should().Be(i2);
    }

    [Fact]
    public void Equality_DifferentPrompt_ShouldNotBeEqual()
    {
        var i1 = AiAgentInstruction.Create("A");
        var i2 = AiAgentInstruction.Create("B");

        i1.Should().NotBe(i2);
    }
}
