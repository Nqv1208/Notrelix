using Notrelix.Application.Features.Collaboration.Activity;
using Notrelix.Application.Features.WorkManagement.Public.ItemActivity;

namespace Notrelix.Application.Tests.Features.Collaboration;

/// <summary>
/// A field change reads as short words: option names, not ids; truncated text; and no values at all for fields whose
/// values must not be copied into the history (sensitive, long text, people, computed).
/// </summary>
public class ItemChangeWordsTests
{
    private static readonly Guid Todo = Guid.CreateVersion7();
    private static readonly Guid Doing = Guid.CreateVersion7();
    private static readonly Guid Gone = Guid.CreateVersion7();

    private static WorkFieldActivityFacts Field(WorkFieldValueMode mode) =>
        new("Field", false, mode, new Dictionary<Guid, string> { [Todo] = "To do", [Doing] = "Doing" });

    [Fact]
    public void AnOption_ReadsAsTheOptionName()
    {
        var (from, to) = ItemChangeWords.Describe(Field(WorkFieldValueMode.Option), $"\"{Todo}\"", $"\"{Doing}\"");

        from.Should().Be("To do");
        to.Should().Be("Doing");
    }

    [Fact]
    public void ARemovedOption_AndAClearedValue_AreUnnamed()
    {
        var (from, to) = ItemChangeWords.Describe(Field(WorkFieldValueMode.Option), $"\"{Gone}\"", "null");

        from.Should().BeNull();
        to.Should().BeNull();
    }

    [Fact]
    public void AnOptionList_ListsTheKnownNames()
    {
        var (_, to) = ItemChangeWords.Describe(Field(WorkFieldValueMode.OptionList), null, $"[\"{Todo}\",\"{Doing}\",\"{Gone}\"]");

        to.Should().Be("To do, Doing");
    }

    [Fact]
    public void Text_IsTruncated_AndNumbersAndCheckboxesReadPlainly()
    {
        var longText = new string('x', 200);

        ItemChangeWords.Describe(Field(WorkFieldValueMode.Plain), "\"a\"", $"\"{longText}\"").To!.Length
            .Should().Be(ItemChangeWords.MaxValueLength);
        ItemChangeWords.Describe(Field(WorkFieldValueMode.Plain), "3", "4.5").Should().Be(("3", "4.5"));
        ItemChangeWords.Describe(Field(WorkFieldValueMode.Plain), "false", "true").Should().Be(("false", "true"));
    }

    [Fact]
    public void AHiddenField_NeverReportsValues_AndAnUnknownFieldNeither()
    {
        ItemChangeWords.Describe(Field(WorkFieldValueMode.Hidden), "\"1\"", "\"2\"").Should().Be((null, null));
        ItemChangeWords.Describe(null, "\"1\"", "\"2\"").Should().Be((null, null));
    }

    [Fact]
    public void MalformedStoredJson_IsUnnamed_NotAnError()
    {
        ItemChangeWords.Describe(Field(WorkFieldValueMode.Plain), "{not json", "\"ok\"").Should().Be((null, "ok"));
    }
}
