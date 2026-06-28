using Microsoft.EntityFrameworkCore;
using Notrelix.Domain.Identity.Users;
using Notrelix.Domain.Identity.Sessions;

namespace Notrelix.Application.Features.Identity.Abstractions;

public interface IIdentityDbContext
{
    DbSet<User> Users { get; }
    DbSet<UserSession> Sessions { get; }
}
