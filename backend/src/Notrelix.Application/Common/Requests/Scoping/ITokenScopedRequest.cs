namespace Notrelix.Application.Common.Requests;

public interface ITokenScopedRequest
{
    TokenPurpose TokenPurpose { get; }
}
