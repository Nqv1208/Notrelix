using Notrelix.Application.Features.WorkManagement.BoardFields.Commands.CreateBoardField;
using Notrelix.Application.Features.WorkManagement.BoardFields.Commands.UpdateBoardField;
using Notrelix.Application.Features.WorkManagement.Boards.Commands.CreateBoardInWorkspace;
using Notrelix.Application.Features.WorkManagement.Boards.Commands.UpdateBoard;

namespace Notrelix.Application.Tests.Features.WorkManagement.Validation;

/// <summary>
/// MAP-VALID-001 (WM-V3-DATA-004, WMREQ094–097). Application validators must accept
/// every Domain-valid boundary value and must not allow a value the Domain rejects:
/// Board title 255 / description 5000, BoardField name 100.
/// </summary>
public sealed class WorkValidatorDomainParityTests
{
    private const int BoardTitleMax = 255;
    private const int BoardDescriptionMax = 5000;
    private const int FieldNameMax = 100;

    [Theory]
    [InlineData(BoardTitleMax, BoardDescriptionMax, true)]
    [InlineData(BoardTitleMax + 1, 10, false)]
    [InlineData(10, BoardDescriptionMax + 1, false)]
    public void CreateBoard_Validator_MatchesDomainLimits(int titleLength, int descriptionLength, bool valid)
    {
        var command = new CreateBoardInWorkspaceCommand(Guid.NewGuid(), new string('t', titleLength), new string('d', descriptionLength), null, null);

        new CreateBoardInWorkspaceCommandValidator().Validate(command).IsValid.Should().Be(valid);
    }

    [Theory]
    [InlineData(BoardTitleMax, BoardDescriptionMax, true)]
    [InlineData(BoardTitleMax + 1, 10, false)]
    [InlineData(10, BoardDescriptionMax + 1, false)]
    public void UpdateBoard_Validator_MatchesDomainLimits(int titleLength, int descriptionLength, bool valid)
    {
        var command = new UpdateBoardCommand(Guid.NewGuid(), new string('t', titleLength), new string('d', descriptionLength), null, null, 1);

        new UpdateBoardCommandValidator().Validate(command).IsValid.Should().Be(valid);
    }

    [Theory]
    [InlineData(FieldNameMax, true)]
    [InlineData(FieldNameMax + 1, false)]
    public void BoardFieldName_Validators_MatchDomainLimit(int nameLength, bool valid)
    {
        var name = new string('n', nameLength);

        new CreateBoardFieldCommandValidator().Validate(new CreateBoardFieldCommand(Guid.NewGuid(), name, "Text", null, null))
            .IsValid.Should().Be(valid);
        new UpdateBoardFieldCommandValidator().Validate(new UpdateBoardFieldCommand(Guid.NewGuid(), Guid.NewGuid(), name, null, 1))
            .IsValid.Should().Be(valid);
    }
}
