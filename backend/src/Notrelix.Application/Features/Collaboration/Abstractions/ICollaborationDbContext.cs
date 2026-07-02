using Notrelix.Domain.Collaboration.Presence;
using Notrelix.Domain.Collaboration.ReadStates;
using Notrelix.Domain.Collaboration.Watchers;
// ReSharper disable InconsistentNaming — intentionally mirrors DbContext member naming

namespace Notrelix.Application.Features.Collaboration.Abstractions;

public interface ICollaborationDbContext
{
    DbSet<Comment> Comments { get; }
    DbSet<Mention> PageMentions { get; }
    DbSet<Reaction> Reactions { get; }
    DbSet<Attachment> Attachments { get; }
    DbSet<ResourceWatcher> ResourceWatchers { get; }
    DbSet<PresenceSession> PresenceSessions { get; }
    DbSet<ResourceReadState> ResourceReadStates { get; }
}