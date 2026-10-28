using Notrelix.Application.Common.Realtime;
using Notrelix.Application.Features.WorkManagement.Public.BoardRealtime;

namespace Notrelix.Architecture.Tests.WorkManagement;

/// <summary>
/// WM-032: a second viewer converges only when every material Work Management mutation announces itself on the
/// Board stream. A write command in the Work Management command folders must hand its change to
/// <see cref="IBoardRealtimeNotifier"/> (or own a realtime mapper), unless it is named below with the reason it
/// does not belong on a Board stream. A new command that is neither fails here, so coverage cannot drift.
/// </summary>
public sealed class BoardRealtimeCoverageArchitectureTests
{
    private static readonly IReadOnlyDictionary<string, string> NotOnABoardStream = new Dictionary<string, string>
    {
        ["CreateBoardInWorkspaceCommand"] = "announced on the Workspace stream by its own mapper",
        ["CreateBoardFromTemplateCommand"] = "creates a Board: announced to the Workspace, nobody views it yet",
        ["CreateBoardTemplateCommand"] = "templates are Workspace assets, not Board content",
        ["ArchiveBoardTemplateCommand"] = "templates are Workspace assets, not Board content",
        ["DeleteBoardTemplateCommand"] = "templates are Workspace assets, not Board content",
        ["PublishBoardTemplateCommand"] = "templates are Workspace assets, not Board content",
        ["CreateBoardRelationCommand"] = "follow-up: relations have no Board read to refresh yet",
        ["DeleteBoardRelationCommand"] = "follow-up: relations have no Board read to refresh yet",
        ["PauseBoardRelationCommand"] = "follow-up: relations have no Board read to refresh yet",
        ["ResumeBoardRelationCommand"] = "follow-up: relations have no Board read to refresh yet",
        ["RemindBoardItemOwnersCommand"] = "a notification, no Board state changes",
    };

    private static readonly string[] OwnStreams =
    [
        ".Forms.", // Forms announce on their own stream when W6 builds their UI
        ".BoardPreferences.", // personal preferences are nobody else's view
    ];

    private static readonly string[] CollaborationBoardCommands =
    [
        "CreateCommentCommand", "UpdateCommentCommand", "DeleteCommentCommand", "ResolveCommentCommand", "ReopenCommentCommand",
        "AddReactionCommand", "RemoveReactionCommand", "CreateBoardItemAttachmentCommand", "UploadBoardItemAttachmentCommand",
        "DeleteAttachmentCommand",
    ];

    [Fact]
    public void EveryWorkManagementWriteCommand_AnnouncesItsChange_OrIsNamedAsNotBelongingOnABoardStream()
    {
        var uncovered = HandlerOf(IsWorkManagementWriteCommand)
            .Where(pair => !OwnStreams.Any(s => pair.Request.FullName!.Contains(s)))
            .Where(pair => !NotOnABoardStream.ContainsKey(pair.Request.Name))
            .Where(pair => !Announces(pair.Handler))
            .Select(pair => pair.Request.Name)
            .ToList();

        uncovered.Should().BeEmpty("these write commands change a Board but tell none of its viewers");
    }

    [Fact]
    public void TheCollaborationCommandsOnBoardItems_AnnounceTheirChange()
    {
        var found = HandlerOf(request => CollaborationBoardCommands.Contains(request.Name)).ToList();

        found.Select(pair => pair.Request.Name).Should().BeEquivalentTo(CollaborationBoardCommands);
        found.Where(pair => !Announces(pair.Handler)).Select(pair => pair.Request.Name).Should().BeEmpty();
    }

    [Fact]
    public void TheExemptionList_NamesOnlyCommandsThatExist()
    {
        var names = HandlerOf(IsWorkManagementWriteCommand).Select(pair => pair.Request.Name).ToHashSet();

        NotOnABoardStream.Keys.Where(name => !names.Contains(name)).Should().BeEmpty();
    }

    private static bool IsWorkManagementWriteCommand(Type request) =>
        request.Namespace is { } ns
        && ns.StartsWith("Notrelix.Application.Features.WorkManagement.", StringComparison.Ordinal)
        && ns.Contains(".Commands.", StringComparison.Ordinal)
        && typeof(IWriteRequest).IsAssignableFrom(request);

    private static bool Announces(Type handler) =>
        handler.GetConstructors().SelectMany(c => c.GetParameters()).Any(p =>
            p.ParameterType == typeof(IBoardRealtimeNotifier)
            || (p.ParameterType.IsGenericType && p.ParameterType.GetGenericTypeDefinition() == typeof(IRealtimeChangeMapper<,>)));

    private static IEnumerable<(Type Request, Type Handler)> HandlerOf(Func<Type, bool> requests)
    {
        var assembly = typeof(IBoardRealtimeNotifier).Assembly;
        foreach (var handler in assembly.GetTypes().Where(t => t is { IsAbstract: false, IsClass: true }))
        {
            foreach (var contract in handler.GetInterfaces().Where(i =>
                         i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>)))
            {
                var request = contract.GenericTypeArguments[0];
                if (requests(request))
                    yield return (request, handler);
            }
        }
    }
}
