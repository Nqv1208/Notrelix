using Notrelix.Domain.Chat.Messages;
using Notrelix.Domain.Chat.Spaces;
using Space = Notrelix.Domain.Chat.Spaces.Space;

namespace Notrelix.Application.Features.Chat.Abstractions;

public interface IChatDbContext
{
    DbSet<Space> ChatSpaces { get; }
    DbSet<Membership> ChatMemberships { get; }
    DbSet<ChannelGroup> ChatChannelGroups { get; }
    DbSet<Message> ChatMessages { get; }
    DbSet<MessageThread> ChatThreads { get; }
    DbSet<ThreadFollower> ChatThreadFollowers { get; }
    DbSet<Pin> ChatPins { get; }
    DbSet<Notrelix.Domain.Chat.Messages.Reaction> ChatReactions { get; }
    DbSet<MessageMention> ChatMessageMentions { get; }
    DbSet<DirectPair> ChatDirectPairs { get; }
    DbSet<PersonBlock> ChatBlocks { get; }
    DbSet<Notrelix.Domain.Chat.Permissions.ChannelPermissionOverride> ChatPermissionOverrides { get; }
}
