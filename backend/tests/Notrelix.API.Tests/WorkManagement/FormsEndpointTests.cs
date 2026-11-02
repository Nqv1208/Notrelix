using System.Net;
using System.Text;
using System.Text.Json;
using MediatR;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Notrelix.API.Tests.Contracts;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Forms.Commands.AddFormQuestion;
using Notrelix.Application.Features.WorkManagement.Forms.Commands.CloseForm;
using Notrelix.Application.Features.WorkManagement.Forms.Commands.CreateForm;
using Notrelix.Application.Features.WorkManagement.Forms.Commands.DeleteForm;
using Notrelix.Application.Features.WorkManagement.Forms.Commands.PublishForm;
using Notrelix.Application.Features.WorkManagement.Forms.Commands.RejectFormSubmission;
using Notrelix.Application.Features.WorkManagement.Forms.Commands.RemoveFormQuestion;
using Notrelix.Application.Features.WorkManagement.Forms.Commands.RestoreForm;
using Notrelix.Application.Features.WorkManagement.Forms.Commands.SubmitForm;
using Notrelix.Application.Features.WorkManagement.Forms.Commands.UpdateFormDetails;
using Notrelix.Application.Features.WorkManagement.Forms.Commands.UpdateFormQuestion;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.API.Tests.WorkManagement;

/// <summary>
/// The HTTP-to-command mapping of the form writes, with the handlers replaced by recorders so the assertion
/// targets the transport seam while the real endpoint filters and Application pipeline still run: every
/// versioned write carries the caller's form version and refuses to run without one, creating a form carries
/// its description, a response decision carries its reason, and creating or submitting needs an idempotency key.
/// </summary>
public class FormsEndpointTests : IClassFixture<NotrelixApiFactory>
{
    private static readonly Guid AccountId = Guid.Parse("A0000000-0000-0000-0000-000000000001");
    private static readonly Guid WorkspaceId = Guid.Parse("A0000000-0000-0000-0000-000000000001");

    private readonly NotrelixApiFactory _factory;

    public FormsEndpointTests(NotrelixApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Publish_CarriesTheVersionFromTheQuery()
    {
        var recorder = new Recorder<PublishFormCommand>();
        var client = CreateClient(recorder);

        var response = await client.PostAsync($"/api/v1/forms/{Guid.NewGuid()}/publish?expectedVersion=5", null);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        recorder.Received.Should().ContainSingle().Which.ExpectedVersion.Should().Be(5);
    }

    [Fact]
    public async Task Close_CarriesTheVersionFromTheQuery()
    {
        var recorder = new Recorder<CloseFormCommand>();
        var client = CreateClient(recorder);

        var response = await client.PostAsync($"/api/v1/forms/{Guid.NewGuid()}/close?expectedVersion=6", null);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        recorder.Received.Should().ContainSingle().Which.ExpectedVersion.Should().Be(6);
    }

    [Fact]
    public async Task Delete_CarriesTheVersionFromTheQuery()
    {
        var recorder = new Recorder<DeleteFormCommand>();
        var client = CreateClient(recorder);

        var response = await client.DeleteAsync($"/api/v1/forms/{Guid.NewGuid()}?expectedVersion=7");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        recorder.Received.Should().ContainSingle().Which.ExpectedVersion.Should().Be(7);
    }

    [Fact]
    public async Task RemoveQuestion_CarriesTheVersionFromTheQuery()
    {
        var recorder = new Recorder<RemoveFormQuestionCommand>();
        var client = CreateClient(recorder);

        var response = await client.DeleteAsync($"/api/v1/forms/{Guid.NewGuid()}/questions/{Guid.NewGuid()}?expectedVersion=8");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        recorder.Received.Should().ContainSingle().Which.ExpectedVersion.Should().Be(8);
    }

    [Fact]
    public async Task UpdateDetails_CarriesTitleDescriptionAndVersion()
    {
        var recorder = new Recorder<UpdateFormDetailsCommand>();
        var client = CreateClient(recorder);

        var response = await client.PatchAsync($"/api/v1/forms/{Guid.NewGuid()}", Json(new { Title = "IT support", Description = "Ask us", ExpectedVersion = 3 }));

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var command = recorder.Received.Should().ContainSingle().Which;
        (command.Title, command.Description, command.ExpectedVersion).Should().Be(("IT support", "Ask us", 3L));
    }

    [Fact]
    public async Task AddQuestion_CarriesTheMappingAndTheVersion()
    {
        var recorder = new Recorder<AddFormQuestionCommand>();
        var client = CreateClient(recorder);
        var field = Guid.NewGuid();

        var response = await client.PostAsync($"/api/v1/forms/{Guid.NewGuid()}/questions",
            Json(new { QuestionKey = "title", QuestionType = "ShortText", Label = "Title", IsRequired = true, BoardFieldId = field, ExpectedVersion = 4 }));

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var command = recorder.Received.Should().ContainSingle().Which;
        (command.QuestionKey, command.IsRequired, command.BoardFieldId, command.ExpectedVersion).Should().Be(("title", true, (Guid?)field, 4L));
    }

    [Fact]
    public async Task UpdateQuestion_CarriesTheMappingAndTheVersion()
    {
        var recorder = new Recorder<UpdateFormQuestionCommand>();
        var client = CreateClient(recorder);
        var field = Guid.NewGuid();

        var response = await client.PatchAsync($"/api/v1/forms/{Guid.NewGuid()}/questions/{Guid.NewGuid()}",
            Json(new { Label = "What do you need?", IsRequired = true, BoardFieldId = field, ExpectedVersion = 9 }));

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var command = recorder.Received.Should().ContainSingle().Which;
        (command.Label, command.BoardFieldId, command.ExpectedVersion).Should().Be(("What do you need?", (Guid?)field, 9L));
    }

    [Theory]
    [InlineData("POST", "publish")]
    [InlineData("POST", "close")]
    [InlineData("DELETE", "")]
    public async Task AVersionedStateWrite_WithoutTheVersion_IsRefusedBeforeTheHandler(string method, string action)
    {
        var publish = new Recorder<PublishFormCommand>();
        var close = new Recorder<CloseFormCommand>();
        var delete = new Recorder<DeleteFormCommand>();
        var client = CreateClient(publish, close, delete);
        var path = $"/api/v1/forms/{Guid.NewGuid()}{(action.Length > 0 ? "/" + action : "")}";

        var response = await client.SendAsync(new HttpRequestMessage(new HttpMethod(method), path));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (publish.Received.Count + close.Received.Count + delete.Received.Count).Should().Be(0, "a write with no version never reaches the handler");
    }

    [Fact]
    public async Task AQuestionWriteWithoutTheVersion_IsRefusedBeforeTheHandler()
    {
        var add = new Recorder<AddFormQuestionCommand>();
        var update = new Recorder<UpdateFormQuestionCommand>();
        var client = CreateClient(add, update);

        var added = await client.PostAsync($"/api/v1/forms/{Guid.NewGuid()}/questions", Json(new { QuestionKey = "title", QuestionType = "ShortText", Label = "Title" }));
        var updated = await client.PatchAsync($"/api/v1/forms/{Guid.NewGuid()}/questions/{Guid.NewGuid()}", Json(new { Label = "Title" }));

        added.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        updated.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (add.Received.Count + update.Received.Count).Should().Be(0);
    }

    [Fact]
    public async Task Restore_NeedsNoVersion()
    {
        var recorder = new Recorder<RestoreFormCommand>();
        var client = CreateClient(recorder);

        var response = await client.PostAsync($"/api/v1/forms/{Guid.NewGuid()}/restore", null);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        recorder.Received.Should().ContainSingle();
    }

    [Fact]
    public async Task RejectSubmission_CarriesItsReason()
    {
        var recorder = new Recorder<RejectFormSubmissionCommand>();
        var client = CreateClient(recorder);

        var response = await client.PostAsync($"/api/v1/form-submissions/{Guid.NewGuid()}/reject", Json(new { Reason = "Duplicate of ERP-168" }));

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        recorder.Received.Should().ContainSingle().Which.Reason.Should().Be("Duplicate of ERP-168");
    }

    [Fact]
    public async Task CreateForm_CarriesTheTitleAndTheDescription_AndAnswers201()
    {
        var recorder = new Recorder<CreateFormCommand, Guid>(Guid.NewGuid());
        var client = CreateClient(recorder);

        var response = await client.PostAsync($"/api/v1/boards/{Guid.NewGuid()}/forms", Json(new { Title = "IT support", Description = "Ask us" }));

        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        var command = recorder.Received.Should().ContainSingle().Which;
        (command.Title, command.Description).Should().Be(("IT support", "Ask us"));
    }

    [Fact]
    public async Task SubmitForm_CarriesTheAnswersByQuestionKey_AndAnswers201()
    {
        var recorder = new Recorder<SubmitFormCommand, Guid>(Guid.NewGuid());
        var client = CreateClient(recorder);

        var response = await client.PostAsync($"/api/v1/forms/{Guid.NewGuid()}/submissions", Json(new { Answers = new { title = "Buy chairs", qty = 3 } }));

        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        var answers = recorder.Received.Should().ContainSingle().Which.Answers;
        answers.Keys.Should().BeEquivalentTo("title", "qty");
        answers["qty"].GetInt32().Should().Be(3);
    }

    [Fact]
    public async Task CreatingOrSubmittingWithoutAnIdempotencyKey_IsRefusedBeforeTheHandler()
    {
        var create = new Recorder<CreateFormCommand, Guid>(Guid.NewGuid());
        var submit = new Recorder<SubmitFormCommand, Guid>(Guid.NewGuid());
        var client = CreateClient(withKey: false, create, submit);

        var created = await client.PostAsync($"/api/v1/boards/{Guid.NewGuid()}/forms", Json(new { Title = "IT support" }));
        var submitted = await client.PostAsync($"/api/v1/forms/{Guid.NewGuid()}/submissions", Json(new { Answers = new { title = "x" } }));

        created.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        submitted.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (create.Received.Count + submit.Received.Count).Should().Be(0);
    }

    [Fact]
    public async Task CreateForm_WithAnEmptyTitle_IsRefusedWithAValidationProblem()
    {
        var recorder = new Recorder<CreateFormCommand, Guid>(Guid.NewGuid());
        var client = CreateClient(recorder);

        var response = await client.PostAsync($"/api/v1/boards/{Guid.NewGuid()}/forms", Json(new { Title = "" }));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        recorder.Received.Should().BeEmpty();
    }

    private HttpClient CreateClient(params object[] recorders) => CreateClient(true, recorders);

    private HttpClient CreateClient(bool withKey, params object[] recorders)
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

                foreach (var recorder in recorders)
                {
                    var handler = recorder.GetType().GetInterfaces().Single(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>));
                    services.RemoveAll(handler);
                    services.AddSingleton(handler, recorder);
                }
            });
        });

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Auth", "true");
        if (withKey) client.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString("N"));
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

    private sealed class Recorder<TCommand, TData>(TData data) : IRequestHandler<TCommand, Result<TData>>
        where TCommand : IRequest<Result<TData>>
    {
        public List<TCommand> Received { get; } = [];

        public Task<Result<TData>> Handle(TCommand request, CancellationToken cancellationToken)
        {
            Received.Add(request);
            return Task.FromResult(Result<TData>.Success(data));
        }
    }
}
