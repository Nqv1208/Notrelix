using Notrelix.Application.Features.WorkManagement.Common;
using Notrelix.Domain.SharedKernel.Ordering;

namespace Notrelix.Application.Tests.Features.WorkManagement.Common;

/// <summary>
/// The read path surfaces the Status field's and the priority field's option as
/// Item values. Status = first Status field by position; priority = first Select
/// field by position named priority / ưu tiên / độ ưu tiên / mức ưu tiên
/// (case-, separator- and diacritic-insensitive).
/// </summary>
public class WorkItemSemanticFieldsTests
{
    private static readonly Guid Account = Guid.NewGuid();
    private static readonly Guid Workspace = Guid.NewGuid();
    private static readonly Guid Board = Guid.NewGuid();
    private static readonly Guid Actor = Guid.NewGuid();
    private static readonly DateTimeOffset Now = new(2026, 10, 12, 3, 40, 0, TimeSpan.Zero);

    private static BoardField Field(string name, FieldType type, string position) =>
        BoardField.Create(Account, Workspace, Board, name, type,
            type == FieldType.Status ? FieldSettings.Create(JsonValue.Create("{\"transitions\":{}}")) : FieldSettings.Empty(),
            FractionalIndex.Create(position), Actor, Now);

    [Theory]
    [InlineData("Priority")]
    [InlineData("  PRIORITY ")]
    [InlineData("Ưu tiên")]
    [InlineData("Độ ưu tiên")]
    [InlineData("Mức ưu tiên")]
    [InlineData("mức-ưu-tiên")]
    [InlineData("Muc uu tien")]
    public void APriorityNamedSelectField_IsThePriorityField(string name)
    {
        var priority = Field(name, FieldType.Select, "a2");

        WorkItemSemanticFields.ResolvePriorityField([Field("Area", FieldType.Select, "a1"), priority]).Should().BeSameAs(priority);
    }

    [Theory]
    [InlineData("Priority", FieldType.MultiSelect)]
    [InlineData("Priority", FieldType.Text)]
    [InlineData("Mức độ", FieldType.Select)]
    [InlineData("Priority level", FieldType.Select)]
    public void OtherFields_AreNotThePriorityField(string name, FieldType type)
    {
        WorkItemSemanticFields.ResolvePriorityField([Field(name, type, "a1")]).Should().BeNull();
    }

    [Fact]
    public void TheFirstStatusFieldByPosition_IsTheStatusField()
    {
        var later = Field("Stage", FieldType.Status, "a5");
        var first = Field("Status", FieldType.Status, "a1");

        WorkItemSemanticFields.ResolveStatusField([later, Field("Title", FieldType.Text, "a0"), first]).Should().BeSameAs(first);
    }

    [Fact]
    public void ReadOptionId_ReadsASingleOptionValue_AndIgnoresOtherShapes()
    {
        var id = Guid.NewGuid();

        WorkItemSemanticFields.ReadOptionId(FieldValue.Create(JsonValue.Create($"\"{id}\""))).Should().Be(id);
        WorkItemSemanticFields.ReadOptionId(FieldValue.Empty()).Should().BeNull();
        WorkItemSemanticFields.ReadOptionId(FieldValue.Create(JsonValue.Create($"[\"{id}\"]"))).Should().BeNull();
        WorkItemSemanticFields.ReadOptionId(null).Should().BeNull();
    }
}
