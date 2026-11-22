using Notrelix.Application.Features.Chat.Messages.Commands.SendMessage;
using Notrelix.Application.Features.Chat.Messages.Queries.ListMessages;
using Notrelix.Application.Features.Collaboration.Attachments.Commands.DeleteAttachment;
using Notrelix.Application.Features.Collaboration.Attachments.Commands.UploadPageAttachment;
using Notrelix.Application.Features.Collaboration.Comments.Commands.CreateComment;
using Notrelix.Application.Features.Collaboration.Comments.Commands.DeleteComment;
using Notrelix.Application.Features.Collaboration.Comments.Commands.ReopenComment;
using Notrelix.Application.Features.Collaboration.Comments.Commands.UpdateComment;
using Notrelix.Application.Features.Collaboration.Comments.Commands.ResolveComment;
using Notrelix.Application.Features.Collaboration.Comments.Queries.GetComments;
using Notrelix.Application.Features.Collaboration.Mentions.Queries.GetMentionSuggestions;
using Notrelix.Application.Features.Collaboration.Watchers.Commands.FollowBoardItem;
using Notrelix.Application.Features.Documents.Pages.Commands.PublishPage;
using Notrelix.Application.Features.Identity.ApiTokens.Queries.ListApiTokens;
using Notrelix.Application.Features.WorkManagement.Boards.Queries.GetBoard;
using Notrelix.Application.Features.Workspaces.Spaces.Commands.CreateSpace;
using Notrelix.Application.Features.Workspaces.Workspaces.Queries.GetWorkspace;

namespace Notrelix.Application.Tests.Common.Security;

public sealed class ApiTokenScopeMapTests
{
    private static string? ScopeOf(Type requestType) =>
        RequestDescriptorValidator.Create(requestType).ApiTokenScope;

    [Fact]
    public void A_query_needs_the_read_scope_of_its_context_and_a_command_the_write_scope()
    {
        ScopeOf(typeof(GetBoardQuery)).Should().Be("work:read");
        ScopeOf(typeof(ListMessagesQuery)).Should().Be("chat:read");
        ScopeOf(typeof(SendMessageCommand)).Should().Be("chat:write");
    }

    [Fact]
    public void A_command_that_touches_no_data_is_still_a_write()
    {
        ScopeOf(typeof(PublishPageCommand)).Should().Be("docs:write", "only a query that reads is read");
    }

    [Fact]
    public void The_defined_workspace_read_set_needs_workspace_read()
    {
        ScopeOf(typeof(GetWorkspaceQuery)).Should().Be("workspace:read");
    }

    [Theory]
    [InlineData(typeof(CreateSpaceCommand))]
    [InlineData(typeof(ListApiTokensQuery))]
    [InlineData(typeof(FollowBoardItemCommand))]
    public void A_request_of_a_context_no_scope_covers_is_refused_to_every_token(Type requestType)
    {
        ScopeOf(requestType).Should().BeNull();
    }

    [Fact]
    public void A_comment_or_attachment_takes_the_scope_of_the_kind_of_resource_it_names()
    {
        var itemComment = CreateCommentCommand.ForBoardItem(Guid.NewGuid(), "hi", null);
        var pageComment = CreateCommentCommand.ForPage(Guid.NewGuid(), "hi", null);

        ScopeFor(itemComment).Should().Be("work:write");
        ScopeFor(pageComment).Should().Be("docs:write");
        ScopeFor(GetCommentsQuery.ForBoardItem(Guid.NewGuid())).Should().Be("work:read");
        ScopeFor(GetCommentsQuery.ForPage(Guid.NewGuid())).Should().Be("docs:read");
        ScopeFor(GetMentionSuggestionsQuery.ForBoardItem(Guid.NewGuid(), null, null)).Should().Be("work:read");
        ScopeFor(GetMentionSuggestionsQuery.ForPage(Guid.NewGuid(), null, null)).Should().Be("docs:read");
        ScopeFor(new UploadPageAttachmentCommand(Guid.NewGuid(), "a.txt", Stream.Null)).Should().Be("docs:write");
    }

    [Fact]
    public void A_hosted_request_has_no_fixed_scope_and_one_that_names_no_host_kind_stays_refused()
    {
        var descriptor = RequestDescriptorValidator.Create(typeof(CreateCommentCommand));

        descriptor.ApiTokenScopeFollowsResource.Should().BeTrue();
        descriptor.ApiTokenScope.Should().BeNull();
        var followed = RequestDescriptorValidator.Create(typeof(FollowBoardItemCommand));
        followed.ApiTokenScopeFollowsResource.Should().BeFalse();
        followed.ApiTokenScopeFollowsLocatedHost.Should().BeFalse();
    }

    [Theory]
    [InlineData(typeof(UpdateCommentCommand))]
    [InlineData(typeof(DeleteCommentCommand))]
    [InlineData(typeof(ResolveCommentCommand))]
    [InlineData(typeof(ReopenCommentCommand))]
    [InlineData(typeof(DeleteAttachmentCommand))]
    public void A_request_keyed_by_a_comment_or_attachment_id_follows_the_host_kind_the_locator_returns(Type requestType)
    {
        var descriptor = RequestDescriptorValidator.Create(requestType);

        descriptor.ApiTokenScopeFollowsLocatedHost.Should().BeTrue();
        descriptor.ApiTokenScopeFollowsResource.Should().BeFalse();
        descriptor.ApiTokenScope.Should().BeNull("the scope is decided from the located host kind");
        descriptor.Scope.Should().Be(ApplicationScopeKind.Resource);
    }

    private static string? ScopeFor(IResourceScopedRequest request)
    {
        var descriptor = RequestDescriptorValidator.Create(request.GetType());
        return ApiTokenScopeMap.RequiredScopeForResource(request.Resource.Kind, descriptor.Kind, descriptor.DataAccess);
    }

    [Fact]
    public void A_request_outside_the_feature_namespaces_is_refused_to_every_token()
    {
        ApiTokenScopeMap.RequiredScope(
                typeof(ApiTokenScopeMapTests), ApplicationRequestKind.Query, ApplicationDataAccessKind.Read)
            .Should().BeNull();
    }
}
