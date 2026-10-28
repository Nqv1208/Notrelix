using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Collaboration.Abstractions;
using Notrelix.Application.Features.Collaboration.Comments.Commands.CreateComment;
using Notrelix.Application.Features.Collaboration.Comments.Queries.GetComments;
using Notrelix.Application.Features.Collaboration.Reactions;
using Notrelix.Application.Features.Collaboration.Reactions.Commands.AddReaction;
using Notrelix.Application.Features.Collaboration.Reactions.Commands.RemoveReaction;
using Notrelix.Application.Features.Documents.Public.PageBlocks;
using Notrelix.Application.Features.Governance.Public.Visibility;
using Notrelix.Application.Features.WorkManagement.Public.ItemNotifications;
using Notrelix.Domain.Collaboration.Comments;
using Notrelix.Domain.Collaboration.Reactions;

namespace Notrelix.Application.Tests.Features.Collaboration;

/// <summary>Reactions on item comments: authorization parity with commenting, palette, idempotent toggles, scope, and one-level replies.</summary>
public class CommentReactionTests
{
    private static readonly DateTimeOffset Now = new(2025, 1, 15, 10, 0, 0, TimeSpan.Zero);

    private readonly Guid _me = Guid.CreateVersion7();
    private readonly Guid _other = Guid.CreateVersion7();
    private readonly Guid _account = Guid.CreateVersion7();
    private readonly Guid _workspace = Guid.CreateVersion7();
    private readonly Guid _item = Guid.CreateVersion7();
    private readonly List<Comment> _comments = [];
    private readonly List<Reaction> _reactions = [];
    private readonly Mock<ICollaborationDbContext> _context = new();
    private readonly Mock<ICurrentRequestContext> _request = new();
    private readonly Mock<ICurrentUser> _user = new();
    private readonly Mock<IDateTimeProvider> _clock = new();

    public CommentReactionTests()
    {
        _request.Setup(r => r.UserId).Returns(_me);
        _request.Setup(r => r.RequireAccountId()).Returns(_account);
        _request.Setup(r => r.RequireWorkspaceId()).Returns(_workspace);
        _user.Setup(u => u.UserId).Returns(_me);
        _clock.Setup(c => c.UtcNow).Returns(Now);
        _context.Setup(c => c.Comments).Returns(TestDbSet.Create(_comments).Object);
        _context.Setup(c => c.Reactions).Returns(TestDbSet.Create(_reactions).Object);
    }

    private ResourceRef ItemRef(Guid? item = null) =>
        ResourceRef.Create(ResourceKind.Create("work-management.board-item"), item ?? _item, _workspace);

    private Comment OnItem(Guid? item = null)
    {
        var comment = Comment.Create(_account, _workspace, ItemRef(item), "Hello", _me, Now);
        _comments.Add(comment);
        return comment;
    }

    private void Holds(Comment comment, Guid user, string emoji) =>
        _reactions.Add(Reaction.Create(_account, _workspace,
            ResourceRef.Create(ResourceKind.Create("collaboration.comment"), comment.Id, _workspace), user, Emoji.Create(emoji), Now));

    private Task<Result> Add(Guid commentId, string emoji) =>
        new AddReactionCommandHandler(_context.Object, _request.Object, _clock.Object)
            .Handle(new AddReactionCommand(_item, commentId, emoji), CancellationToken.None);

    private Task<Result> Remove(Guid commentId, string emoji) =>
        new RemoveReactionCommandHandler(_context.Object, _user.Object, _clock.Object)
            .Handle(new RemoveReactionCommand(_item, commentId, emoji), CancellationToken.None);

    [Fact]
    public async Task Adding_AReaction_StoresOneInTheWorkspaceOfTheComment()
    {
        var comment = OnItem();

        var result = await Add(comment.Id, "👍");

        result.Succeeded.Should().BeTrue();
        var stored = _reactions.Should().ContainSingle().Subject;
        stored.UserId.Should().Be(_me);
        stored.WorkspaceId.Should().Be(_workspace);
        stored.Emoji.Code.Should().Be("👍");
        stored.Target.ResourceId.Should().Be(comment.Id);
    }

    [Fact]
    public async Task Adding_AReactionAlreadyHeld_IsANoOp()
    {
        var comment = OnItem();
        Holds(comment, _me, "🎉");

        var result = await Add(comment.Id, "🎉");

        result.Succeeded.Should().BeTrue();
        _reactions.Should().ContainSingle();
    }

    [Fact]
    public async Task Adding_TheSameEmojiAsSomeoneElse_IsAllowed()
    {
        var comment = OnItem();
        Holds(comment, _other, "🎉");

        await Add(comment.Id, "🎉");

        _reactions.Should().HaveCount(2);
    }

    [Fact]
    public async Task Removing_AHeldReaction_DeletesOnlyTheCallersOwn()
    {
        var comment = OnItem();
        Holds(comment, _me, "🚀");
        Holds(comment, _other, "🚀");

        var result = await Remove(comment.Id, "🚀");

        result.Succeeded.Should().BeTrue();
        _reactions.Should().ContainSingle().Which.UserId.Should().Be(_other);
    }

    [Fact]
    public async Task Removing_AMissingReaction_IsANoOp()
    {
        var comment = OnItem();
        Holds(comment, _other, "🚀");

        var result = await Remove(comment.Id, "🚀");

        result.Succeeded.Should().BeTrue();
        _reactions.Should().ContainSingle();
    }

    [Fact]
    public async Task Adding_ToACommentOnAnotherItem_IsNotFound()
    {
        var comment = OnItem(Guid.CreateVersion7());

        var act = () => Add(comment.Id, "👍");

        await act.Should().ThrowAsync<NotFoundException>();
        _reactions.Should().BeEmpty();
    }

    [Theory]
    [InlineData("👍", true)]
    [InlineData("❤️", true)]
    [InlineData("😄", true)]
    [InlineData("🎉", true)]
    [InlineData("👀", true)]
    [InlineData("🚀", true)]
    [InlineData("😡", false)]
    [InlineData("", false)]
    [InlineData("thumbsup", false)]
    public void Validator_AcceptsOnlyTheSixEmojiPalette(string emoji, bool valid)
    {
        new AddReactionCommandValidator().Validate(new AddReactionCommand(_item, Guid.NewGuid(), emoji)).IsValid.Should().Be(valid);
        new RemoveReactionCommandValidator().Validate(new RemoveReactionCommand(_item, Guid.NewGuid(), emoji)).IsValid.Should().Be(valid);
    }

    [Fact]
    public void ThePalette_HasExactlySixEmoji() => ReactionPalette.Emojis.Should().HaveCount(6);

    // Reacting is authorized exactly like creating a comment on the same item: same action, same resource.
    [Fact]
    public void Reacting_RequiresTheSamePermissionAsCommentingOnTheItem()
    {
        var comment = CreateCommentCommand.ForBoardItem(_item, "Hello", null);
        foreach (IRequirePermission reaction in new IRequirePermission[]
                 {
                     new AddReactionCommand(_item, Guid.NewGuid(), "👍"),
                     new RemoveReactionCommand(_item, Guid.NewGuid(), "👍")
                 })
        {
            reaction.Action.Should().Be(comment.Action);
            reaction.Resource.Kind.Should().Be(comment.Resource.Kind);
            reaction.Resource.ResourceId.Should().Be(comment.Resource.ResourceId);
        }
    }

    [Theory]
    [InlineData("Member")]
    [InlineData("Guest")]
    [InlineData("Viewer")]
    [InlineData(null)]
    public void Reacting_IsDecidedLikeCommenting_ForEveryWorkspaceRole(string? role)
    {
        var facts = new AccessFacts(
            UserExists: true, EmailVerified: true, AccountExists: true, AccountMemberRole: "Owner",
            WorkspaceExists: true, WorkspaceMemberRole: role, ResourceExists: true, ResourceAudience: null,
            ResourceMemberRole: null, HasExplicitResourcePermission: false, PermissionRules: [],
            SubscriptionRequirementSatisfied: false, FeatureEnabled: false, ActiveResourcePermissionRank: null, TargetPermissionRank: null);
        var snapshot = new ExecutionContextSnapshot(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null,
            ApplicationPrincipalKind.Authenticated, ApplicationScopeKind.Resource, Guid.NewGuid().ToString("D"));
        var engine = new AccessPolicyEngine();

        var comment = CreateCommentCommand.ForBoardItem(_item, "Hello", null);
        var expected = engine.Evaluate(RequestDescriptorValidator.Create(typeof(CreateCommentCommand)), snapshot, facts, comment).Kind;

        engine.Evaluate(RequestDescriptorValidator.Create(typeof(AddReactionCommand)), snapshot, facts, new AddReactionCommand(_item, Guid.NewGuid(), "👍")).Kind
            .Should().Be(expected);
        engine.Evaluate(RequestDescriptorValidator.Create(typeof(RemoveReactionCommand)), snapshot, facts, new RemoveReactionCommand(_item, Guid.NewGuid(), "👍")).Kind
            .Should().Be(expected);
    }

    [Fact]
    public async Task TheCommentList_ShowsCountsWhoReactedAndWhetherTheCallerDid()
    {
        var comment = OnItem();
        Holds(comment, _me, "👍");
        Holds(comment, _other, "👍");
        Holds(comment, _other, "🎉");
        var actors = new Mock<IActorLookupService>();
        actors.Setup(a => a.FindManyAsync(It.IsAny<IReadOnlyList<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new ActorSnapshot(_me, "Me", null), new ActorSnapshot(_other, "Other", null)]);

        var result = await new GetCommentsQueryHandler(_context.Object, actors.Object, _user.Object)
            .Handle(GetCommentsQuery.ForBoardItem(_item), CancellationToken.None);

        var reactions = result.Data!.Single().Reactions.ToDictionary(r => r.Emoji);
        reactions["👍"].Count.Should().Be(2);
        reactions["👍"].ReactedByMe.Should().BeTrue();
        reactions["👍"].Users.Select(u => u.Name).Should().BeEquivalentTo("Me", "Other");
        reactions["🎉"].Count.Should().Be(1);
        reactions["🎉"].ReactedByMe.Should().BeFalse();
    }

    [Fact]
    public async Task ReplyingToAReply_JoinsTheTopLevelComment()
    {
        var top = OnItem();
        var reply = Comment.CreateReply(_account, _workspace, ItemRef(), "first reply", _me, Now,
            ParentCommentContext.Create(_account, _workspace, top.Id, ItemRef()));
        _comments.Add(reply);
        _context.Setup(c => c.PageMentions).Returns(TestDbSet.Create<Notrelix.Domain.Collaboration.Mentions.Mention>().Object);
        var handler = new CreateCommentCommandHandler(_context.Object, _request.Object, _clock.Object,
            new Mock<IPageBlockFacts>().Object, new Mock<IWorkItemNotificationFacts>().Object, new Mock<IResourceVisibility>().Object);

        var result = await handler.Handle(CreateCommentCommand.ForBoardItem(_item, "reply to reply", reply.Id), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        _comments.Single(c => c.Id == result.Data).ParentId.Should().Be(top.Id);
    }
}
