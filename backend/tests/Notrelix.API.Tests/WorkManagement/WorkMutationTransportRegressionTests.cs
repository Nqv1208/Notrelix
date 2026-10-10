using System.Net;
using System.Text;
using System.Text.Json;
using MediatR;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Notrelix.API.Tests.Contracts;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.BoardFields.Commands.UpdateBoardField;
using Notrelix.Application.Features.WorkManagement.BoardGroups.Commands.DeleteBoardGroup;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.API.Tests.WorkManagement;

/// <summary>
/// FIX-MUT-001 (WM-V3-FIX-001). Proves the HTTP-to-command mapping of two Work
/// mutations that were unreachable or lossy: a rename-only Field PATCH must not
/// erase settings, and DeleteBoardGroup must carry the caller's expected version
/// instead of a constant that the pipeline always rejects. The handlers are
/// replaced by recorders so the assertion targets exactly the transport seam;
/// the real endpoint filters and Application pipeline still run.
/// </summary>
public class WorkMutationTransportRegressionTests : IClassFixture<NotrelixApiFactory>
{
    private static readonly Guid AccountId = Guid.Parse("A0000000-0000-0000-0000-000000000001");
    private static readonly Guid WorkspaceId = Guid.Parse("A0000000-0000-0000-0000-000000000001");

    private readonly NotrelixApiFactory _factory;

    public WorkMutationTransportRegressionTests(NotrelixApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task UpdateField_RenameOnly_DoesNotSendReplacementSettings()
    {
        var recorder = new Recorder<UpdateBoardFieldCommand>();
        var client = CreateClient(recorder);

        var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Patch,
            $"/api/v1/boards/{Guid.NewGuid()}/fields/{Guid.NewGuid()}")
        {
            Content = Json(new { Name = "Renamed", ExpectedVersion = 3 })
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        recorder.Received.Should().ContainSingle()
            .Which.Settings.Should().BeNull("an omitted settings field means unchanged, not an empty object");
    }

    [Fact]
    public async Task DeleteBoardGroup_CarriesTheCallersExpectedVersion()
    {
        var recorder = new Recorder<DeleteBoardGroupCommand>();
        var client = CreateClient(recorder);

        var response = await client.DeleteAsync($"/api/v1/board-groups/{Guid.NewGuid()}?expectedVersion=4");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        recorder.Received.Should().ContainSingle().Which.ExpectedVersion.Should().Be(4);
    }

    [Fact]
    public async Task VersionedMutation_WithoutExpectedVersion_IsRejectedAsAClientError()
    {
        // WM-V3-CONC-001: a missing version binds as 0 and is a 400, not a guaranteed 412.
        var recorder = new Recorder<UpdateBoardFieldCommand>();
        var client = CreateClient(recorder);

        var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Patch,
            $"/api/v1/boards/{Guid.NewGuid()}/fields/{Guid.NewGuid()}")
        {
            Content = Json(new { Name = "Renamed" })
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        recorder.Received.Should().BeEmpty("validation rejects the request before the handler");
    }

    private HttpClient CreateClient<TCommand>(Recorder<TCommand> recorder)
        where TCommand : IRequest<Result>
    {
        var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IResourceLocator>();
                services.AddScoped<IResourceLocator>(_ =>
                {
                    var locator = new Mock<IResourceLocator>();
                    locator.Setup(x => x.LocateAsync(It.IsAny<ResourceRef>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync((ResourceRef resource, Guid _, CancellationToken _) =>
                            new ResourceLocation(resource.Kind, resource.ResourceId, AccountId, WorkspaceId));
                    return locator.Object;
                });

                services.RemoveAll<IRequestHandler<TCommand, Result>>();
                services.AddSingleton<IRequestHandler<TCommand, Result>>(recorder);
            });
        });

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Auth", "true");
        client.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString("N"));
        return client;
    }

    private static StringContent Json(object body) =>
        new(JsonSerializer.Serialize(body, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }),
            Encoding.UTF8, "application/json");

    private sealed class Recorder<TCommand> : IRequestHandler<TCommand, Result>
        where TCommand : IRequest<Result>
    {
        public List<TCommand> Received { get; } = [];

        public Task<Result> Handle(TCommand request, CancellationToken cancellationToken)
        {
            Received.Add(request);
            return Task.FromResult(Result.Success());
        }
    }
}
