using FluentAssertions;
using Notrelix.Domain.Documents;
using Notrelix.Domain.Documents.Versions;

namespace Notrelix.Domain.Tests.Documents;

public class DocumentVersionKindTests
{
    private static readonly DocumentSnapshot Snapshot = DocumentSnapshot.Create(JsonValue.EmptyObject());

    private static DocumentVersion Create(DocumentVersionKind kind, string? name) =>
        DocumentVersion.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1, Snapshot, Guid.NewGuid(), DateTimeOffset.UtcNow, name, kind);

    [Fact]
    public void ADefaultVersion_IsAutomatic_AndUnnamed()
    {
        var version = DocumentVersion.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1, Snapshot, Guid.NewGuid(), DateTimeOffset.UtcNow);

        version.Kind.Should().Be(DocumentVersionKind.Automatic);
        version.ChangeSummary.Should().BeNull();
    }

    [Fact]
    public void ANamedVersion_KeepsItsTrimmedName()
    {
        var version = Create(DocumentVersionKind.Named, "  Before the review  ");

        version.Kind.Should().Be(DocumentVersionKind.Named);
        version.ChangeSummary.Should().Be("Before the review");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ANamedVersion_NeedsAName(string? name)
    {
        var act = () => Create(DocumentVersionKind.Named, name);

        act.Should().Throw<BusinessRuleException>().Which.RuleCode.Should().Be(DocumentRuleCodes.Documents_Version_NameRequired);
    }

    [Fact]
    public void AName_IsLimited()
    {
        var act = () => Create(DocumentVersionKind.Named, new string('x', DocumentVersion.MaxNameLength + 1));

        act.Should().Throw<BusinessRuleException>().Which.RuleCode.Should().Be(DocumentRuleCodes.Documents_Version_NameTooLong);
    }

    [Fact]
    public void ARestoreVersion_NeedsNoName_ButMayCarryASummary()
    {
        Create(DocumentVersionKind.Restore, "Restored from version 2").ChangeSummary.Should().Be("Restored from version 2");
        Create(DocumentVersionKind.Restore, null).Kind.Should().Be(DocumentVersionKind.Restore);
    }
}
