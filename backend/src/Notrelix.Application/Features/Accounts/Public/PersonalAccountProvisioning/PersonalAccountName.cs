namespace Notrelix.Application.Features.Accounts.Public.PersonalAccountProvisioning;

/// <summary>
/// The name of a personal Account, derived from the person's display name. An Account name holds 160
/// characters and a display name 200, so the display name is cut to fit: a long name must not make
/// registration fail.
/// </summary>
public static class PersonalAccountName
{
    private const string Suffix = "'s Account";
    private const int MaxAccountNameLength = 160;

    public static string For(string displayName)
    {
        var name = displayName.Trim();
        var room = MaxAccountNameLength - Suffix.Length;
        if (name.Length > room)
            name = name[..room].TrimEnd();
        return name + Suffix;
    }
}
