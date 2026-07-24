namespace Notrelix.Domain.Workspaces.Invitations;

public sealed class InvitationTokenHash : ValueObject
{
    public const int HashLength = 64;

    public string Value { get; } = null!;

    private InvitationTokenHash() { }
    private InvitationTokenHash(string value)
    {
        Value = value;
    }

    public static InvitationTokenHash Create(string hash)
    {
        Guard.NotNullOrWhiteSpace(hash);

        var normalized = hash.Trim().ToLowerInvariant();

        if (normalized.Length != HashLength ||
            normalized.Any(c => !Uri.IsHexDigit(c)))
        {
            throw new BusinessRuleException(
                BusinessRuleCodes.Workspaces_InvitationTokenHash_InvalidFormat,
                "Invitation token hash must be a valid SHA-256 hexadecimal value.");
        }

        return new InvitationTokenHash(normalized);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}
