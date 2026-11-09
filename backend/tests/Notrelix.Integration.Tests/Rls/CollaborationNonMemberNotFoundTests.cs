using System.Net;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Notrelix.API;
using Notrelix.Domain.Collaboration.Comments;
using Notrelix.Domain.Collaboration.Watchers;
using Notrelix.Domain.Identity.Users;
using Notrelix.Domain.SharedKernel;
using Notrelix.Domain.SharedKernel.Ordering;
using Notrelix.Domain.WorkManagement.BoardGroups;
using Notrelix.Domain.WorkManagement.Items;
using Notrelix.Infrastructure.Data.Notifications;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Integration.Tests.Rls;

/// <summary>
/// UC-COL-C05: every Collaboration endpoint that takes a comment, reaction, watcher or inbox id answers a person
/// outside the Workspace with the same status and the same body whether the id belongs to another tenant or was
/// never issued. Proven per endpoint over the production graph on the application role, for a person of another
/// account and a person of the same account who is not in the Workspace. The status is not hard-coded: a refusal
/// is either Forbidden or NotFound, and the two answers must match each other. Request-specific fields
/// (traceId, correlationId, instance) and ids are removed before the bodies are compared.
/// </summary>
[Collection("DatabaseAppRole")]
[Trait("Category", "RlsAppRole")]
public sealed class CollaborationNonMemberNotFoundTests : IAsyncLifetime
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;
    private static readonly HttpStatusCode[] Refusals = [HttpStatusCode.Forbidden, HttpStatusCode.NotFound];
    private const string ItemKind = "work-management.board-item";
    private const string ThumbsUp = "thumbsup";
    private const string GuidPattern = "[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}|[0-9a-f]{32}|0H[0-9A-Z]{11}";

    private readonly AppRoleTestContainer _fixture;
    private AppRoleTenant _a = null!;
    private AppRoleTenant _b = null!;
    private User _sameAccountOutsider = null!;
    private Guid _itemId;
    private Guid _commentId;
    private Guid _notificationId;
    private WebApplicationFactory<Program>? _factory;

    public CollaborationNonMemberNotFoundTests(AppRoleTestContainer fixture) => _fixture = fixture;

    /// <summary>The ids a route is built from: the real ones of tenant A, or made-up ones.</summary>
    private sealed record Ids(Guid Workspace, Guid Item, Guid Comment, Guid Notification);

    public async Task InitializeAsync()
    {
        await _fixture.ResetAsync();
        _a = await AppRoleTenants.SeedAsync(_fixture, "a-collab-c05@example.com", "Aaa", "hash");
        _b = await AppRoleTenants.SeedAsync(_fixture, "b-collab-c05@example.com", "Bbb", "hash");
        _sameAccountOutsider = await AppRoleUsers.AddAsync(_fixture, "outsider-collab-c05@example.com", "Out", "hash");

        var owner = _a.User.Id;
        var group = BoardGroup.Create(_a.AccountId, _a.WorkspaceId, _a.BoardId, "Todo", Color.Create("#808080"), FractionalIndex.Create("a0"), owner, Now);
        var item = BoardItem.CreateRoot(_a.AccountId, _a.WorkspaceId, _a.BoardId, group.Id, "Task", FractionalIndex.Create("a0"), owner, Now);
        var target = ResourceRef.Create(ResourceKind.Create(ItemKind), item.Id, _a.WorkspaceId);
        var comment = Comment.Create(_a.AccountId, _a.WorkspaceId, target, "A comment", owner, Now);
        var notification = NotificationItemRecord.Create(
            _a.AccountId, _a.WorkspaceId, "collaboration", "mention.created", NotificationSeverity.Info, "Mentioned", Now,
            resourceType: ItemKind, resourceId: item.Id, body: "You were mentioned");

        var system = new FakeCurrentTenantContext();
        system.SetSystem();
        await using var ctx = _fixture.CreateOwnerContext(system);
        ctx.BoardGroups.Add(group);
        ctx.BoardItems.Add(item);
        ctx.Comments.Add(comment);
        ctx.ResourceWatchers.Add(ResourceWatcher.Create(_a.AccountId, _a.WorkspaceId, target, owner, owner, Now));
        ctx.NotificationItems.Add(notification);
        ctx.NotificationRecipients.Add(NotificationRecipientRecord.Create(_a.AccountId, notification.Id, _a.WorkspaceId, owner, Now));
        await ctx.SaveChangesAsync();

        _itemId = item.Id;
        _commentId = comment.Id;
        _notificationId = notification.Id;
    }

    public Task DisposeAsync()
    {
        _factory?.Dispose();
        return Task.CompletedTask;
    }

    private WebApplicationFactory<Program> Factory => _factory ??= AppRoleHost.CreateFactory(_fixture, _fixture.AppConnectionString);

    private Ids Existing() => new(_a.WorkspaceId, _itemId, _commentId, _notificationId);

    private static Ids MadeUp() => new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

    /// <summary>Every write or read a person outside the Workspace can send with a comment, reaction, watcher or inbox id.</summary>
    private static IEnumerable<(string Name, HttpMethod Method, Func<Ids, string> Url, string? Json)> Routes()
    {
        yield return ("comments of an item", HttpMethod.Get, i => $"/api/v1/board-items/{i.Item}/comments", null);
        yield return ("comments page of an item", HttpMethod.Get, i => $"/api/v1/board-items/{i.Item}/comments/page", null);
        yield return ("edit a comment", HttpMethod.Patch, i => $"/api/v1/comments/{i.Comment}", "{\"contentMd\":\"x\",\"expectedVersion\":1}");
        yield return ("delete a comment", HttpMethod.Delete, i => $"/api/v1/comments/{i.Comment}", null);
        yield return ("resolve a comment", HttpMethod.Post, i => $"/api/v1/comments/{i.Comment}/resolve", null);
        yield return ("reopen a comment", HttpMethod.Post, i => $"/api/v1/comments/{i.Comment}/reopen", null);
        yield return ("react to a comment", HttpMethod.Put, i => $"/api/v1/board-items/{i.Item}/comments/{i.Comment}/reactions/{ThumbsUp}", null);
        yield return ("withdraw a reaction", HttpMethod.Delete, i => $"/api/v1/board-items/{i.Item}/comments/{i.Comment}/reactions/{ThumbsUp}", null);
        yield return ("read who follows an item", HttpMethod.Get, i => $"/api/v1/board-items/{i.Item}/following", null);
        yield return ("follow an item", HttpMethod.Put, i => $"/api/v1/board-items/{i.Item}/following", null);
        yield return ("unfollow an item", HttpMethod.Delete, i => $"/api/v1/board-items/{i.Item}/following", null);
        yield return ("list the workspace inbox", HttpMethod.Get, i => $"/api/v1/workspaces/{i.Workspace}/inbox", null);
        yield return ("mark an inbox entry read", HttpMethod.Post, i => $"/api/v1/workspaces/{i.Workspace}/inbox/{i.Notification}/read", null);
        yield return ("mark an inbox entry done", HttpMethod.Post, i => $"/api/v1/workspaces/{i.Workspace}/inbox/{i.Notification}/done", null);
        yield return ("mark the workspace inbox read", HttpMethod.Post, i => $"/api/v1/workspaces/{i.Workspace}/inbox/read-all", null);
    }

    private async Task<(HttpStatusCode Status, string Body)> SendAsync(User who, HttpMethod method, string url, string? json, Func<HttpContent>? form = null)
    {
        var factory = Factory;
        string token;
        using (var scope = factory.Services.CreateScope())
            token = scope.ServiceProvider.GetRequiredService<IJwtService>().GenerateAccessToken(who);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", token);
        using var request = new HttpRequestMessage(method, url);
        // Every write carries a fresh key, so a replayed answer can never stand in for the one being compared.
        if (method != HttpMethod.Get) request.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString("D"));
        if (form is not null) request.Content = form();
        else if (json is not null) request.Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        using var response = await client.SendAsync(request);
        return (response.StatusCode, Normalize(await response.Content.ReadAsStringAsync()));
    }

    // Drop the per-request fields of a problem body, then the ids it may echo.
    private static string Normalize(string body)
    {
        var text = body;
        try
        {
            if (JsonNode.Parse(body) is JsonObject obj)
            {
                var noise = obj.Select(kv => kv.Key)
                    .Where(key => key is "traceId" or "correlationId" or "instance" or "requestId")
                    .ToList();
                foreach (var key in noise) obj.Remove(key);
                text = obj.ToJsonString();
            }
        }
        catch (System.Text.Json.JsonException)
        {
            // Not JSON (an empty body): compare the text as it is.
        }
        return Regex.Replace(text, GuidPattern, "<id>");
    }

    private async Task AssertSameRefusalAsync(User who, string because)
    {
        foreach (var (name, method, url, json) in Routes())
        {
            var found = await SendAsync(who, method, url(Existing()), json);
            var made = await SendAsync(who, method, url(MadeUp()), json);
            Refusals.Should().Contain(made.Status, $"{name}: a made-up id is refused as Forbidden or NotFound");
            found.Status.Should().Be(made.Status, $"{name}: {because}");
            found.Body.Should().Be(made.Body, $"{name}: {because}");
        }
    }

    [Fact]
    public async Task MemberOfAnotherAccount_GetsTheSameRefusal_ForAnotherTenantAndMadeUpIds() =>
        await AssertSameRefusalAsync(_b.User, "a person of another account learns nothing about another tenant's comment, reaction, watcher or inbox entry");

    [Fact]
    public async Task SameAccountOutsider_GetsTheSameRefusal_ForAnotherTenantAndMadeUpIds() =>
        await AssertSameRefusalAsync(_sameAccountOutsider, "a person of the same account outside the Workspace learns nothing about its comments, reactions, watchers or inbox");
}
