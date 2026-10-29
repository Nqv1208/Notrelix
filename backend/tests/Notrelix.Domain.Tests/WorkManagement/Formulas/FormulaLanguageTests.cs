using FluentAssertions;
using Notrelix.Domain.WorkManagement;
using Notrelix.Domain.WorkManagement.Fields;
using Notrelix.Domain.WorkManagement.Formulas;

namespace Notrelix.Domain.Tests.WorkManagement.Formulas;

public class FormulaLanguageTests
{
    private static FormulaValue Eval(string expression, Dictionary<string, FormulaValue>? fields = null) =>
        FormulaEvaluator.Evaluate(FormulaLanguage.Parse(expression),
            name => fields is not null && fields.TryGetValue(name, out var v) ? v : FormulaValue.Error("unknown-field"));

    [Theory]
    [InlineData("1+2*3", 7)]
    [InlineData("(1+2)*3", 9)]
    [InlineData("-4+10/4", -1.5)]
    [InlineData("ROUND(2.345, 2)", 2.35)]
    [InlineData("SUM(1, 2, 3)", 6)]
    [InlineData("MAX(1, 9, 3) - MIN(4, 2)", 7)]
    [InlineData("IF(1 < 2, 10, 20)", 10)]
    public void Numbers_AreComputed(string expression, double expected) =>
        Eval(expression).Number.Should().Be((decimal)expected);

    [Fact]
    public void FieldReferences_AreCaseInsensitive_AndEmptyCountsAsZero()
    {
        var fields = new Dictionary<string, FormulaValue>(StringComparer.OrdinalIgnoreCase) { ["effort"] = FormulaValue.Of(4m), ["Gap"] = FormulaValue.Empty };

        Eval("[Effort]*2", fields).Number.Should().Be(8m);
        Eval("[Gap]+1", fields).Number.Should().Be(1m);
    }

    [Fact]
    public void Text_DatesAndComparisons()
    {
        Eval("\"a\" & 1 & \"b\"").Text.Should().Be("a1b");
        Eval("\"x\" = \"X\"").Flag.Should().BeTrue();
        var fields = new Dictionary<string, FormulaValue>
        {
            ["Due"] = FormulaValue.Of(new DateTimeOffset(2026, 10, 15, 0, 0, 0, TimeSpan.Zero)),
            ["Start"] = FormulaValue.Of(new DateTimeOffset(2026, 10, 12, 9, 0, 0, TimeSpan.Zero)),
        };
        Eval("DAYS([Due], [Start])", fields).Number.Should().Be(3m);
    }

    [Fact]
    public void Failures_AreErrorValues_NotGuesses()
    {
        Eval("1/0").Text.Should().Be(FormulaEvaluator.ErrDivideByZero);
        Eval("1 + \"a\"").Text.Should().Be(FormulaEvaluator.ErrType);
        Eval("[Nope] * 2").Text.Should().Be("unknown-field");
    }

    [Theory]
    [InlineData("")]
    [InlineData("1 +")]
    [InlineData("[Open")]
    [InlineData("BOOM(1)")]
    [InlineData("IF(1, 2)")]
    [InlineData("1 2")]
    public void SyntaxErrors_AreRejected(string expression)
    {
        var act = () => FormulaLanguage.Parse(expression);

        act.Should().Throw<BusinessRuleException>().Which.RuleCode.Should().BeOneOf(
            WorkManagementRuleCodes.WorkManagement_FieldSettings_FormulaSyntaxInvalid,
            WorkManagementRuleCodes.WorkManagement_FieldSettings_FormulaExpressionRequired);
    }

    [Fact]
    public void NodeCap_IsEnforced_AtTwoHundred()
    {
        FormulaLanguage.Parse(string.Join("+", Enumerable.Repeat("1", 100))).Should().NotBeNull(); // 199 nodes
        var act = () => FormulaLanguage.Parse(string.Join("+", Enumerable.Repeat("1", 101))); // 201 nodes

        act.Should().Throw<BusinessRuleException>().Which.RuleCode.Should().Be(WorkManagementRuleCodes.WorkManagement_FieldSettings_FormulaTooComplex);
        var deep = () => FormulaLanguage.Parse(new string('(', 5000) + "1" + new string(')', 5000));
        deep.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Rollup_AggregatesNumbers_AndPropagatesErrors()
    {
        var sum = RollupSpec.Parse($$"""{"function":"sum","fieldId":"{{Guid.NewGuid()}}"}""");
        sum.Aggregate(3, [FormulaValue.Of(3m), FormulaValue.Empty, FormulaValue.Of(4m)]).Number.Should().Be(7m);
        sum.Aggregate(1, [FormulaValue.Error("type")]).IsError.Should().BeTrue();
        RollupSpec.Parse("""{"function":"count"}""").Aggregate(2, []).Number.Should().Be(2m);
        var avg = RollupSpec.Parse($$"""{"function":"avg","fieldId":"{{Guid.NewGuid()}}"}""");
        avg.Aggregate(0, []).Kind.Should().Be(FormulaValueKind.Empty);
        var act = () => RollupSpec.Parse("""{"function":"sum"}""");
        act.Should().Throw<BusinessRuleException>().Which.RuleCode.Should().Be(WorkManagementRuleCodes.WorkManagement_FieldSettings_RollupInvalid);
    }

    [Fact]
    public void Rename_KeepsAFormulaWorking_BecauseReferencesAreStoredAsIds()
    {
        var effort = new ComputedFieldInput(Guid.NewGuid(), "Effort", FieldType.Number, "{}");
        var board = new[] { effort };

        var saved = ComputedFieldGraph.NormalizeToIds("""{"expression":"[effort] * 2 & \"[Effort]\""}""", board);

        ComputedFieldGraph.ExpressionOf(saved).Should().Contain($"[{effort.Id}]").And.Contain("\"[Effort]\"", "text literals are left alone");
        var renamed = new[] { effort with { Name = "Hours" } };
        ComputedFieldGraph.EnsureValid([.. renamed, new ComputedFieldInput(Guid.NewGuid(), "Double", FieldType.Formula, saved)]);
        var node = FormulaLanguage.Parse(ComputedFieldGraph.ExpressionOf(saved));
        FormulaEvaluator.Evaluate(node, r => Guid.Parse(r) == effort.Id ? FormulaValue.Of(4m) : FormulaValue.Error("unknown-field"))
            .Kind.Should().Be(FormulaValueKind.Text);
        ComputedFieldGraph.RenderNames(saved, renamed).Should().Contain("[Hours]");

        var deleted = () => ComputedFieldGraph.EnsureValid([new ComputedFieldInput(Guid.NewGuid(), "Double", FieldType.Formula, saved)]);
        deleted.Should().Throw<BusinessRuleException>().Which.RuleCode.Should().Be(WorkManagementRuleCodes.WorkManagement_Field_FormulaUnknownField);
    }

    [Fact]
    public void Graph_RejectsCycles_AndUnknownReferences()
    {
        ComputedFieldInput Formula(string name, string expression) =>
            new(Guid.NewGuid(), name, FieldType.Formula, $$"""{"expression":"{{expression}}"}""");
        var effort = new ComputedFieldInput(Guid.NewGuid(), "Effort", FieldType.Number, "{}");

        var ok = new[] { effort, Formula("A", "[Effort]+1"), Formula("B", "[A]*2") };
        ComputedFieldGraph.EnsureValid(ok);

        var cycle = new[] { effort, Formula("A", "[B]+1"), Formula("B", "[A]+1") };
        var act = () => ComputedFieldGraph.EnsureValid(cycle);
        act.Should().Throw<BusinessRuleException>().Which.RuleCode.Should().Be(WorkManagementRuleCodes.WorkManagement_Field_ComputedFieldCycle);

        var self = () => ComputedFieldGraph.EnsureValid([Formula("Me", "[Me]+1")]);
        self.Should().Throw<BusinessRuleException>().Which.RuleCode.Should().Be(WorkManagementRuleCodes.WorkManagement_Field_ComputedFieldCycle);

        var unknown = () => ComputedFieldGraph.EnsureValid([Formula("A", "[Missing]")]);
        unknown.Should().Throw<BusinessRuleException>().Which.RuleCode.Should().Be(WorkManagementRuleCodes.WorkManagement_Field_FormulaUnknownField);
    }
}
