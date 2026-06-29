using Microsoft.EntityFrameworkCore;

namespace Notrelix.Application.Features.Identity.Abstractions;

public interface IIdentityDbContext
{
    DbSet<User> Users { get; }
    DbSet<UserSession> Sessions { get; }
}
