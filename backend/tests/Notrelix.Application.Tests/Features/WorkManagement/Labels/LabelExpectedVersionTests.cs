using Notrelix.Application.Features.WorkManagement.Labels.Commands.DeleteLabel;
using Notrelix.Application.Features.WorkManagement.Labels.Commands.UpdateLabel;

namespace Notrelix.Application.Tests.Features.WorkManagement.Labels;

/// <summary>A label write must carry the version the caller saw; a write without one never reaches the handler.</summary>
public class LabelExpectedVersionTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void UpdateLabel_WithoutAnAuthoritativeVersion_IsRefused(long version)
    {
        var result = new UpdateLabelCommandValidator()
            .Validate(new UpdateLabelCommand(Guid.CreateVersion7(), "Bug", null, version));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateLabelCommand.ExpectedVersion));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void DeleteLabel_WithoutAnAuthoritativeVersion_IsRefused(long version)
    {
        var result = new DeleteLabelCommandValidator()
            .Validate(new DeleteLabelCommand(Guid.CreateVersion7(), version));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(DeleteLabelCommand.ExpectedVersion));
    }

    [Fact]
    public void LabelWrites_WithTheSeenVersion_PassValidation()
    {
        new UpdateLabelCommandValidator()
            .Validate(new UpdateLabelCommand(Guid.CreateVersion7(), "Bug", null, 3)).IsValid.Should().BeTrue();
        new DeleteLabelCommandValidator()
            .Validate(new DeleteLabelCommand(Guid.CreateVersion7(), 3)).IsValid.Should().BeTrue();
    }
}
