using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Notrelix.API.Tests.Collaboration;
using Notrelix.API.Tests.Contracts;
using Notrelix.Domain.Common;
using Notrelix.Domain.Identity.Users;
using Notrelix.Infrastructure.Data;

namespace Notrelix.API.Tests.Identity.Profile;

/// <summary>
/// HTTP contract of the profile photo routes through the real endpoint, CSRF, authorization and
/// handler pipeline, with real multipart bodies: format recognised from the bytes (415), the size cap
/// enforced while streaming (413), malformed requests (400), and the serve headers of the avatar read.
/// Object storage is an in-process fake; transaction and RLS mechanics belong to the integration tests.
/// </summary>
public sealed class AvatarEndpointTests : IClassFixture<AttachmentFileEndpointTests.Fixture>
{
    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 1, 2, 3, 4, 5, 6, 7, 8];
    private static readonly Guid CallerId = Guid.Parse(TestAuthHandler.TestUserId);

    private readonly AttachmentFileEndpointTests.Fixture _fixture;

    public AvatarEndpointTests(AttachmentFileEndpointTests.Fixture fixture)
    {
        _fixture = fixture;
        EnsureCallerExists();
    }

    private HttpClient Client => _fixture.Client;

    private void EnsureCallerExists()
    {
        using var scope = _fixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        if (context.Users.IgnoreQueryFilters().Any(u => u.Id == CallerId))
        {
            return;
        }

        var user = User.Create("avatar-caller@example.com", "Caller", "hash", DateTimeOffset.UtcNow, hasPasswordCredential: true);
        typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(user, CallerId);
        context.Users.Add(user);
        context.SaveChanges();
    }

    [Fact]
    public async Task Upload_ReturnsTheServedAddress_AndTheAvatarReadServesTheBytesWithHardeningAndCacheHeaders()
    {
        using var response = await Client.PutAsync("/api/v1/profile/avatar", Form("me.png", Png));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var url = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("avatarUrl").GetString()!;
        url.Should().StartWith($"/api/v1/users/{CallerId:D}/avatar?v=");
        _fixture.Storage.Objects.Keys.Should().Contain(k => k.StartsWith($"users/{CallerId:N}/avatar/", StringComparison.Ordinal));

        using var read = await Client.GetAsync(url);
        read.StatusCode.Should().Be(HttpStatusCode.OK);
        read.Content.Headers.ContentType!.MediaType.Should().Be("image/png");
        (await read.Content.ReadAsByteArrayAsync()).Should().Equal(Png);
        read.Headers.GetValues("X-Content-Type-Options").Should().Equal("nosniff");
        read.Headers.GetValues("Content-Security-Policy").Should().Equal("default-src 'none'; sandbox");
        read.Headers.CacheControl!.Private.Should().BeTrue();
        read.Headers.CacheControl.Public.Should().BeFalse();
        read.Headers.CacheControl.MaxAge.Should().Be(TimeSpan.FromDays(1));
        read.Headers.GetValues("Cache-Control").Single().Should().Contain("immutable");
    }

    [Fact]
    public async Task Upload_OfSomethingThatIsNotAnImage_Is415_WithTheStableCode_WhateverTheDeclaredType()
    {
        var before = _fixture.Storage.Objects.Count;

        using var response = await Client.PutAsync(
            "/api/v1/profile/avatar", Form("me.png", Encoding.UTF8.GetBytes("<svg xmlns='http://www.w3.org/2000/svg'/>"), "image/png"));

        await ExpectProblem(response, HttpStatusCode.UnsupportedMediaType, "identity.avatar.unsupported-format");
        _fixture.Storage.Objects.Count.Should().Be(before);
    }

    [Fact]
    public async Task Upload_AsAJsonBody_Is415()
    {
        using var response = await Client.PutAsync(
            "/api/v1/profile/avatar", new StringContent("{}", Encoding.UTF8, "application/json"));

        response.StatusCode.Should().Be(HttpStatusCode.UnsupportedMediaType);
    }

    [Fact]
    public async Task Upload_AsMultipartWithoutABoundary_Is415_WithTheStableCode()
    {
        using var response = await Client.PutAsync(
            "/api/v1/profile/avatar", new StringContent("x", Encoding.UTF8, "multipart/form-data"));

        await ExpectProblem(response, HttpStatusCode.UnsupportedMediaType, "identity.avatar.unsupported-format");
    }

    [Fact]
    public async Task Upload_OverTheCap_Is413_WithTheStableCode_AndStoresNothing()
    {
        var before = _fixture.Storage.Objects.Count;
        var tooBig = Png.Concat(new byte[2_000_000]).ToArray();

        using var response = await Client.PutAsync("/api/v1/profile/avatar", Form("big.png", tooBig));

        await ExpectProblem(response, HttpStatusCode.RequestEntityTooLarge, "identity.avatar.too-large");
        _fixture.Storage.Objects.Count.Should().Be(before);
    }

    [Fact]
    public async Task Upload_WithoutAFilePart_Is400()
    {
        using var form = new MultipartFormDataContent { { new StringContent("x"), "other" } };

        using var response = await Client.PutAsync("/api/v1/profile/avatar", form);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Remove_ReturnsANullAddress_AndTheAvatarReadIsThenNotFound()
    {
        using var upload = await Client.PutAsync("/api/v1/profile/avatar", Form("me.png", Png));
        var url = (await upload.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("avatarUrl").GetString()!;

        using var remove = await Client.DeleteAsync("/api/v1/profile/avatar");

        remove.StatusCode.Should().Be(HttpStatusCode.OK);
        (await remove.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("avatarUrl").ValueKind.Should().Be(JsonValueKind.Null);
        using var read = await Client.GetAsync(url);
        read.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ReadingAnUnknownUsersAvatar_Is404()
    {
        using var response = await Client.GetAsync($"/api/v1/users/{Guid.NewGuid()}/avatar");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Theory]
    [InlineData("PUT", "/api/v1/profile/avatar")]
    [InlineData("DELETE", "/api/v1/profile/avatar")]
    [InlineData("GET", "/api/v1/users/22222222-2222-2222-2222-222222222222/avatar")]
    public async Task EveryAvatarRoute_RequiresAuthentication(string method, string path)
    {
        using var client = _fixture.CreateClient(withCsrf: true, withAuth: false);
        using var request = new HttpRequestMessage(new HttpMethod(method), path);
        if (method == "PUT")
        {
            request.Content = Form("me.png", Png);
        }

        using var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private static MultipartFormDataContent Form(string fileName, byte[] bytes, string? partContentType = null)
    {
        var part = new ByteArrayContent(bytes);
        if (partContentType is not null)
        {
            part.Headers.ContentType = System.Net.Http.Headers.MediaTypeHeaderValue.Parse(partContentType);
        }

        return new MultipartFormDataContent { { part, "file", fileName } };
    }

    private static async Task ExpectProblem(HttpResponseMessage response, HttpStatusCode status, string errorCode)
    {
        response.StatusCode.Should().Be(status);
        var problem = JsonSerializer.Deserialize<JsonElement>(await response.Content.ReadAsStringAsync());
        problem.GetProperty("errorCode").GetString().Should().Be(errorCode);
    }
}
