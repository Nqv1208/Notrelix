using Notrelix.Domain.Chat.Spaces;
using Space = Notrelix.Domain.Chat.Spaces.Space;

namespace Notrelix.Application.Features.Chat.Abstractions;

public interface IChatDbContext
{
    DbSet<Space> ChatSpaces { get; }
    DbSet<Membership> ChatMemberships { get; }
    DbSet<ChannelGroup> ChatChannelGroups { get; }
}
