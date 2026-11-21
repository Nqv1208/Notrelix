using Notrelix.Application.Features.Documents.Versions.Commands.CreatePageVersion;
using Notrelix.Domain.Documents.Versions;

namespace Notrelix.Application.Tests.Features.Documents;

/// <summary>A named version needs a name that fits the domain limit; the API answers 400 otherwise.</summary>
public sealed class PageVersionNameValidatorTests
{
    private readonly CreatePageVersionCommandValidator _validator = new();

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void AnEmptyName_IsRejected(string name) =>
        _validator.Validate(new CreatePageVersionCommand(Guid.CreateVersion7(), name)).IsValid.Should().BeFalse();

    [Fact]
    public void ANameOverTheLimit_IsRejected_AndOneAtTheLimitPasses()
    {
        var id = Guid.CreateVersion7();
        _validator.Validate(new CreatePageVersionCommand(id, new string('a', DocumentVersion.MaxNameLength + 1))).IsValid.Should().BeFalse();
        _validator.Validate(new CreatePageVersionCommand(id, new string('a', DocumentVersion.MaxNameLength))).IsValid.Should().BeTrue();
    }
}
