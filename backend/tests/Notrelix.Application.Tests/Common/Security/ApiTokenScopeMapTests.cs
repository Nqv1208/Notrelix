using Notrelix.Application.Features.Chat.Messages.Commands.SendMessage;
using Notrelix.Application.Features.Chat.Messages.Queries.ListMessages;
using Notrelix.Application.Features.Collaboration.Comments.Commands.CreateComment;
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
    [InlineData(typeof(CreateCommentCommand))]
    public void A_request_of_a_context_no_scope_covers_is_refused_to_every_token(Type requestType)
    {
        ScopeOf(requestType).Should().BeNull();
    }

    [Fact]
    public void A_request_outside_the_feature_namespaces_is_refused_to_every_token()
    {
        ApiTokenScopeMap.RequiredScope(
                typeof(ApiTokenScopeMapTests), ApplicationRequestKind.Query, ApplicationDataAccessKind.Read)
            .Should().BeNull();
    }
}
