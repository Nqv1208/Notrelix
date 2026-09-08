using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Accounts.Abstractions;
using Notrelix.Domain.Accounts.Accounts;

namespace Notrelix.Application.Features.Accounts.Accounts.Commands.RenameAccount;

public class RenameAccountCommandHandler : IRequestHandler<RenameAccountCommand, Result>
{
    private readonly IAccountDbContext _context;
    private readonly ICurrentRequestContext _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public RenameAccountCommandHandler(
        IAccountDbContext context,
        ICurrentRequestContext currentUser,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(RenameAccountCommand request, CancellationToken cancellationToken)
    {
        var accountId = _currentUser.RequireAccountId();

        var account = await _context.Accounts
            .FirstOrDefaultAsync(a => a.Id == accountId, cancellationToken);

        if (account is null)
        {
            throw new NotFoundException(nameof(Account), accountId);
        }

        account.Rename(request.Name, _currentUser.UserId, _dateTimeProvider.UtcNow);
        return Result.Success();
    }
}
