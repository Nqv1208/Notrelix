using Notrelix.Application.Common.Requests.Scoping;
using Notrelix.Application.Common.Models;

namespace Notrelix.Application.Features.Identity.Registration.Commands.Register;

public sealed record RegisterCommand
    : ICommand<Result<AuthResult>>,
      IAnonymousRequest,
      IGlobalRequest,
      IWriteRequest
{
    public required string Email { get; init; }
    public required string Password { get; init; }
    public required string Name { get; init; }
}
