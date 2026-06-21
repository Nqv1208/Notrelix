using Notrelix.Domain.Common;

namespace Notrelix.Domain.Workspaces.Invitations;

public sealed class InvitationTokenHash : ValueObject
{
    public string Value { get; }

    private InvitationTokenHash() { }    private InvitationTokenHash(string value)
    {
        Value = value;
    }

    public static InvitationTokenHash Create(string hash)
    {
        Guard.NotNullOrWhiteSpace(hash);
        return new InvitationTokenHash(hash.Trim());
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}
