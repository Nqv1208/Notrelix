using Notrelix.Application.Features.Workspaces.Invitations.Services;
using Notrelix.Application.Features.Workspaces.Invitations.Commands.InviteMembers;

namespace Notrelix.Application.Tests.Features.Workspaces.Invitations.Commands;

public class InviteMembersCommandValidatorTests
{
    private static InviteMembersCommand Batch(int count) =>
        new(Guid.NewGuid(), Enumerable.Range(0, count).Select(i => new MemberInvitationEntry($"p{i}@example.com", "Member")).ToList());

    [Theory]
    [InlineData(1, true)]
    [InlineData(50, true)]
    [InlineData(0, false)]
    [InlineData(51, false)]
    public async Task TheBatchHoldsOneToFiftyEntries(int count, bool valid)
    {
        var result = await new InviteMembersCommandValidator().ValidateAsync(Batch(count));

        result.IsValid.Should().Be(valid);
    }

    [Fact]
    public async Task ABadAddressDoesNotFailTheBatch_ItIsThatEntrysOwnOutcome()
    {
        var command = new InviteMembersCommand(Guid.NewGuid(), [new MemberInvitationEntry("not-an-address", "Wizard")]);

        (await new InviteMembersCommandValidator().ValidateAsync(command)).IsValid.Should().BeTrue();
    }
}
