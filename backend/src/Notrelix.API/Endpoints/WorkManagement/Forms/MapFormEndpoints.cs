using Notrelix.API.Endpoints.WorkManagement.Forms.Commands;
using Notrelix.API.Endpoints.WorkManagement.Forms.Queries;

namespace Notrelix.API.Endpoints.WorkManagement.Forms;

public static class MapFormEndpoints
{
    public static IEndpointRouteBuilder MapForms(this IEndpointRouteBuilder app)
    {
        var boardGroup = app
            .MapGroup("/api/v1/boards/{boardId:guid}/forms")
            .WithTags("WorkManagement.Forms")
            .WithOpenApi();

        boardGroup.MapCreateForm();
        boardGroup.MapListForms();

        var group = app
            .MapGroup("/api/v1/forms/{formId:guid}")
            .WithTags("WorkManagement.Forms")
            .WithOpenApi();

        group.MapUpdateFormDetails();
        group.MapPublishForm();
        group.MapCloseForm();
        group.MapSoftDeleteForm();
        group.MapRestoreForm();
        group.MapAddFormQuestion();
        group.MapUpdateFormQuestion();
        group.MapListFormSubmissions();

        var submissionGroup = app
            .MapGroup("/api/v1/form-submissions/{submissionId:guid}")
            .WithTags("WorkManagement.Forms")
            .WithOpenApi();

        submissionGroup.MapProcessFormSubmission();
        submissionGroup.MapRejectFormSubmission();
        submissionGroup.MapMarkFormSubmissionAsSpam();
        submissionGroup.MapDeleteFormSubmission();

        return app;
    }
}
