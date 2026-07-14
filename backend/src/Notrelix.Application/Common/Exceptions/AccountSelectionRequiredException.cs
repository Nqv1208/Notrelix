namespace Notrelix.Application.Common.Exceptions;

public class AccountSelectionRequiredException : Exception
{
    public AccountSelectionRequiredException()
        : base("Account selection is required. Provide account context via route, header, or session.") { }

    public AccountSelectionRequiredException(string message) : base(message) { }
}
