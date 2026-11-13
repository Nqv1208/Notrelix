namespace Notrelix.Architecture.Tests.Partitioning;

/// <summary>
/// Partitioning readiness (scale plan section 13, item 2). Chat messages are partitioned by space id, so a
/// read of the messages set must name a space. The rule fails on any new unscoped read and on any allow-list
/// entry that no longer matches a read (so the list shrinks as reads are fixed; it never grows silently).
/// </summary>
public class ChatMessagePartitionArchitectureTests : ArchitectureTestBase
{
    /// <summary>
    /// Reads that do not name a space id in their statement, each with the reason it is safe today and the
    /// target state. Every entry is a by-id lookup or a read whose space is taken from the loaded row.
    /// </summary>
    private static readonly IReadOnlyDictionary<string, string> AllowList = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["src/Notrelix.Application/Features/Chat/Pins/Commands/PinMessage/PinMessage.cs::Handle"] =
            "Looks the message up by its id within the workspace, then authorises against message.SpaceId (ChannelAccess.LoadLiveAsync and RequireVisible) before any effect. Target: a space-scoped message read helper.",
        ["src/Notrelix.Application/Features/Chat/Pins/Commands/UnpinMessage/UnpinMessage.cs::Handle"] =
            "Looks the message up by its id within the workspace, then authorises against message.SpaceId (ChannelAccess.LoadLiveAsync and RequireVisible) before any effect. Target: a space-scoped message read helper.",
        ["src/Notrelix.Application/Features/Chat/Messages/Commands/DeleteMessage/DeleteMessage.cs::Handle"] =
            "Looks the message up by its id within the workspace, then authorises against message.SpaceId (ChannelAccess.LoadLiveAsync and RequireVisible) before any effect. Target: a space-scoped message read helper.",
        ["src/Notrelix.Application/Features/Chat/Messages/Commands/ModerateDeleteMessage/ModerateDeleteMessage.cs::Handle"] =
            "Looks the message up by its id within the workspace, then authorises against message.SpaceId (ChannelAccess.LoadLiveAsync and RequireVisible) before any effect. Target: a space-scoped message read helper.",
        ["src/Notrelix.Application/Features/Chat/Messages/Commands/EditMessage/EditMessage.cs::Handle"] =
            "Looks the message up by its id within the workspace, then authorises against message.SpaceId (ChannelAccess.LoadLiveAsync and RequireVisible) before any effect. Target: a space-scoped message read helper.",
        ["src/Notrelix.Application/Features/Chat/Reactions/Queries/ListReactors/ListReactors.cs::Handle"] =
            "Looks the message up by its id within the workspace, then authorises against message.SpaceId (ChannelAccess.LoadLiveAsync and RequireVisible) before any effect. Target: a space-scoped message read helper.",
        ["src/Notrelix.Application/Features/Chat/Reactions/Commands/RemoveReaction/RemoveReaction.cs::Handle"] =
            "Looks the message up by its id within the workspace, then authorises against message.SpaceId (ChannelAccess.LoadLiveAsync and RequireVisible) before any effect. Target: a space-scoped message read helper.",
        ["src/Notrelix.Application/Features/Chat/Reactions/Commands/AddReaction/AddReaction.cs::Handle"] =
            "Looks the message up by its id within the workspace, then authorises against message.SpaceId (ChannelAccess.LoadLiveAsync and RequireVisible) before any effect. Target: a space-scoped message read helper.",
        ["src/Notrelix.Application/Features/Chat/Threads/Commands/ReplyInThread/ReplyInThread.cs::Handle"] =
            "Loads the root by id within the workspace and authorises root.SpaceId (LoadLiveAsync, membership) before use. The second read re-reads that same authorised root under the send lock. Target: a space-scoped read helper.",
        ["src/Notrelix.Application/Features/Chat/Attachments/Queries/GetMessageAttachmentContent/GetMessageAttachmentContent.cs::Handle"] =
            "Looks the message up by its id within the workspace, then authorises against message.SpaceId (ChannelAccess.LoadLiveAsync and RequireVisible) before any effect. Target: a space-scoped message read helper.",
        ["src/Notrelix.Application/Features/Chat/Attachments/Queries/ListMessageAttachments/ListMessageAttachments.cs::Handle"] =
            "Looks the message up by its id within the workspace, then authorises against message.SpaceId (ChannelAccess.LoadLiveAsync and RequireVisible) before any effect. Target: a space-scoped message read helper.",
        ["src/Notrelix.Application/Features/Chat/Attachments/Commands/UploadMessageAttachment/UploadMessageAttachment.cs::Handle"] =
            "Looks the message up by its id within the workspace, then authorises against message.SpaceId (ChannelAccess.LoadLiveAsync and RequireVisible) before any effect. Target: a space-scoped message read helper.",
        ["src/Notrelix.Application/Features/Chat/Attachments/Commands/DeleteMessageAttachment/DeleteMessageAttachment.cs::Handle"] =
            "Looks the message up by its id within the workspace, then authorises against message.SpaceId (ChannelAccess.LoadLiveAsync and RequireVisible) before any effect. Target: a space-scoped message read helper.",
        ["src/Notrelix.Application/Features/Chat/Threads/Queries/ListThreadReplies/ListThreadReplies.cs::Handle"] =
            "The root is loaded by id within the workspace and authorised against root.SpaceId before the replies read. That read filters by ThreadRootId, which is the authorised root. Target: a space-scoped read helper.",
        ["src/Notrelix.Application/Features/Chat/Threads/Queries/ListFollowedThreads/ListFollowedThreads.cs::Handle"] =
            "Root ids come from the caller's own followed threads, and the query keeps only threads in spaces where the caller is a live member. The root read then uses those ids within the workspace. Target: a space-scoped read helper.",
        ["src/Notrelix.Application/Features/Chat/Messages/Services/ReplyPreviews.cs::LoadAsync"] =
            "Reads quoted messages by id within the workspace to build previews. Quoted ids come from messages on a page the caller already reads in its space, and a reply target must be in the same space on write (SendMessage, ReplyInThread). Target: a space-scoped preview read.",
        ["src/Notrelix.Application/Features/Chat/Notifications/Services/ChatDigestFacts.cs::DeliverableAsync"] =
            "Reads the digest's message ids within the workspace, then drops any row whose SpaceId differs from its candidate before use. Target: a space-scoped read per candidate space.",
        ["src/Notrelix.Application/Features/Chat/Notifications/Services/ChatNotificationHandoff.cs::HandOffAsync"] =
            "Event-driven delivery: loads the message by id within the workspace, then loads its space by message.SpaceId and checks membership for each recipient. Target: a space-scoped read helper.",
    };

    [Fact]
    public void EveryChatMessageReadNamesASpaceIdOrIsAllowListed()
    {
        var reads = ScanChatSources();

        var unexpected = reads
            .Where(read => !AllowList.ContainsKey(read.Key))
            .Select(read => read.ToString())
            .ToArray();

        Assert.True(
            unexpected.Length == 0,
            "every read of ChatMessages must filter by space id (scale plan section 13.2). Fix the query, or add the " +
            "member to the allow-list with a reason. Unscoped reads found:\n" + string.Join("\n", unexpected));
    }

    [Fact]
    public void EveryAllowListEntryStillMatchesAnUnscopedRead()
    {
        var keys = ScanChatSources().Select(read => read.Key).ToHashSet(StringComparer.Ordinal);

        var stale = AllowList.Keys.Where(key => !keys.Contains(key)).ToArray();

        Assert.True(stale.Length == 0, "remove allow-list entries whose read is now space-scoped:\n" + string.Join("\n", stale));
    }

    [Fact]
    public void EveryAllowListEntryHasAReason()
    {
        AllowList.Should().OnlyContain(pair => !string.IsNullOrWhiteSpace(pair.Value));
    }

    private static List<UnscopedMessageRead> ScanChatSources()
    {
        var root = Path.GetDirectoryName(GetSrcPath())!;
        var files = Directory.GetFiles(GetSrcPath(), "*.cs", SearchOption.AllDirectories)
            .Where(file => !file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                        && !file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
                        && !file.Contains($"{Path.DirectorySeparatorChar}Migrations{Path.DirectorySeparatorChar}"));

        var reads = new List<UnscopedMessageRead>();
        foreach (var file in files)
        {
            var source = File.ReadAllText(file);
            if (!source.Contains("ChatMessages", StringComparison.Ordinal))
            {
                continue;
            }

            var relative = Path.GetRelativePath(root, file).Replace(Path.DirectorySeparatorChar, '/');
            reads.AddRange(ChatMessageSpaceFilterScanner.ScanFile(relative, source));
        }

        return reads;
    }
}
