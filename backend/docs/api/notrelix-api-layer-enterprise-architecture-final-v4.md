# Notrelix API Layer Enterprise Architecture — Final Standard v4

> **Status:** Final architectural standard for the Notrelix API layer.  
> **Scope:** `Notrelix.API` only.  
> **Goal:** Provide a stable, enterprise-grade API architecture that can scale to hundreds of endpoints without repeated structural refactors.  
> **Core decision:** Routes are resource-oriented, but endpoint code is organized by **bounded context → module → command/query use case**.

---

## 0. Executive Decision

The API layer is the HTTP boundary of Notrelix. It must remain thin, stable, observable, and contract-driven.

Final API architecture:

```txt
Notrelix.API
├── Minimal API
├── Bounded-context/module/use-case endpoint organization
├── API contracts separated from Application DTOs
├── ProblemDetails / application/problem+json for all errors
├── Workspace-first multi-tenant routing
├── Idempotency-Key for retry-sensitive commands
├── If-Match / expectedVersion for concurrency-sensitive commands
├── Cursor pagination for large collections
├── OpenAPI as public contract documentation
├── Health/readiness/liveness endpoints
├── Webhook, realtime, internal, and frontend API surfaces clearly separated
└── API architecture tests to prevent drift
```

The API layer must **not** become a business layer, query layer, or infrastructure orchestration layer.

---

## 1. API Layer Responsibility

The API layer answers:

```txt
- Which HTTP route exposes which Application use case?
- What is the public request/response contract?
- How are authentication, authorization policies, cookies/JWT configured?
- How are exceptions mapped to ProblemDetails?
- How are correlation, rate limit, idempotency, OpenAPI, health checks configured?
- How are workspace/tenant route values passed into Application requests?
```

The API layer does **not** answer:

```txt
- Is this action allowed according to business permission rules?
- How is a board archived internally?
- How is an automation triggered?
- How is a billing entitlement recalculated?
- How is search indexed?
- How are domain invariants enforced?
```

Those belong to Application, Domain, Infrastructure, or background workers.

---

## 2. Layer Boundary Rules

### 2.1 Allowed dependencies

```txt
Notrelix.API
→ Notrelix.Application
→ Notrelix.Domain

Notrelix.API
→ Notrelix.Infrastructure only through composition/registration at startup if required by the solution structure.
```

API may reference Application request/result types and service registration extensions.

### 2.2 Forbidden dependencies and behaviors

API endpoints must not:

```txt
- Inject DbContext.
- Inject repositories directly.
- Query EF Core directly.
- Return EF entities.
- Return Domain aggregate/entities.
- Use Domain events directly.
- Check fine-grained roles/permissions manually.
- Call email/storage/realtime/search/billing providers directly.
- Trigger automation side effects directly.
- Build custom error envelopes outside ProblemDetails.
- Catch all exceptions inside each endpoint.
- Put business rules in endpoint methods.
```

### 2.3 API endpoint responsibility

Every endpoint should do only this:

```txt
1. Bind route/query/body/header values.
2. Create Application command/query.
3. Send it through ISender.
4. Map Application result to API response contract.
5. Return typed HTTP result.
```

---

## 3. Final Root Structure

```txt
Notrelix.API/
├── Program.cs
├── DependencyInjection.cs
├── GlobalUsings.cs
│
├── Endpoints/
│   └── {BoundedContext}/
│       └── {Module}/
│           ├── Map{Module}Endpoints.cs
│           ├── Commands/
│           │   └── {UseCase}Endpoint.cs
│           └── Queries/
│               └── {UseCase}Endpoint.cs
│
├── Contracts/
│   └── {BoundedContext}/
│       └── {Module}/
│           ├── Requests/
│           ├── Responses/
│           └── Mappers/
│
├── ErrorHandling/
│   ├── GlobalExceptionHandler.cs
│   ├── ProblemDetailsMapper.cs
│   ├── ProblemDetailsOptionsSetup.cs
│   ├── ErrorCodes.cs
│   └── ProblemDetailsExtensions.cs
│
├── Auth/
│   ├── AuthenticationSetup.cs
│   ├── AuthorizationSetup.cs
│   ├── CookieOptionsSetup.cs
│   ├── JwtOptionsSetup.cs
│   └── ClaimsMapping.cs
│
├── OpenApi/
│   ├── SwaggerSetup.cs
│   ├── SecuritySchemeSetup.cs
│   ├── ProblemDetailsSchemaSetup.cs
│   ├── IdempotencyHeaderOperationFilter.cs
│   ├── ConcurrencyHeaderOperationFilter.cs
│   └── EndpointDocumentation.cs
│
├── Versioning/
│   └── ApiVersioningSetup.cs
│
├── RateLimiting/
│   ├── RateLimitPolicies.cs
│   └── RateLimitSetup.cs
│
├── HealthChecks/
│   ├── HealthCheckSetup.cs
│   ├── OutboxBacklogHealthCheck.cs
│   └── WorkerHeartbeatHealthCheck.cs
│
├── Middleware/
│   ├── CorrelationIdMiddleware.cs
│   ├── RequestContextMiddleware.cs
│   ├── SecurityHeadersMiddleware.cs
│   └── RawBodyCaptureMiddleware.cs
│
├── Realtime/
│   ├── HubRouteSetup.cs
│   └── RealtimeAuthPolicies.cs
│
├── Webhooks/
│   ├── WebhookSignatureVerificationFilter.cs
│   ├── WebhookIdempotencyFilter.cs
│   └── WebhookRequestReader.cs
│
├── Uploads/
│   ├── FileUploadLimits.cs
│   ├── MultipartSetup.cs
│   └── UploadValidationFilter.cs
│
├── Extensions/
│   ├── EndpointRouteBuilderExtensions.cs
│   ├── ResultExtensions.cs
│   ├── ClaimsPrincipalExtensions.cs
│   └── HttpContextExtensions.cs
│
└── Observability/
    ├── ApiMetrics.cs
    ├── RequestLoggingSetup.cs
    └── ErrorCodeMetrics.cs
```

Root folders are now stable. Do not add new root folders unless there is a clear API-level concern and an ADR approves it.

---

## 4. Program.cs Rule

`Program.cs` is composition only.

Target shape:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration)
    .AddApiLayer(builder.Configuration);

var app = builder.Build();

app.UseApiPipeline();
app.MapApiEndpoints();

app.Run();
```

`Program.cs` must not contain:

```txt
- Large route definitions.
- Large middleware logic.
- Exception mapping logic.
- Swagger customization details.
- Authentication option details.
- Business rules.
```

Put those in extension/setup files.

---

## 5. Endpoint Organization — Final Rule

This is the most important rule for avoiding future refactors.

### 5.1 Route design vs code organization

```txt
HTTP routes are resource-oriented.
Endpoint code is bounded-context/module/use-case-oriented.
```

Do **not** design URLs like use case method names:

```txt
POST /api/v1/create-board
POST /api/v1/update-board-field-value
POST /api/v1/archive-board-item
```

Use resource-oriented URLs:

```txt
POST   /api/v1/workspaces/{workspaceId}/boards
PATCH  /api/v1/boards/{boardId}
POST   /api/v1/boards/{boardId}/archive
PATCH  /api/v1/boards/{boardId}/items/{itemId}/values/{fieldId}
```

But organize code by use case:

```txt
Endpoints/WorkManagement/BoardItems/Commands/UpdateBoardItemFieldValueEndpoint.cs
```

### 5.2 Final endpoint folder pattern

```txt
Endpoints/{BoundedContext}/{Module}/Commands/{UseCase}Endpoint.cs
Endpoints/{BoundedContext}/{Module}/Queries/{UseCase}Endpoint.cs
Endpoints/{BoundedContext}/{Module}/Map{Module}Endpoints.cs
```

Example:

```txt
Endpoints/WorkManagement/Boards/
├── MapBoardEndpoints.cs
├── Commands/
│   ├── CreateBoardEndpoint.cs
│   ├── RenameBoardEndpoint.cs
│   ├── ArchiveBoardEndpoint.cs
│   ├── RestoreBoardEndpoint.cs
│   ├── DuplicateBoardEndpoint.cs
│   └── ChangeBoardVisibilityEndpoint.cs
└── Queries/
    ├── GetBoardEndpoint.cs
    ├── ListWorkspaceBoardsEndpoint.cs
    └── GetBoardOverviewEndpoint.cs
```

### 5.3 Forbidden endpoint structures

Do not create long controller-like files:

```txt
Endpoints/BoardEndpoints.cs
Endpoints/BoardItemEndpoints.cs
Endpoints/BillingEndpoints.cs
Endpoints/AutomationEndpoints.cs
```

Do not create generic CRUD endpoint folders:

```txt
Endpoints/Crud/
Endpoints/CommonCrud/
Endpoints/GenericEndpoints/
```

Do not group by HTTP verb:

```txt
Endpoints/Get/
Endpoints/Post/
Endpoints/Patch/
```

### 5.4 Small module exception

If a module has fewer than five endpoints and is not expected to grow, it may temporarily use:

```txt
Endpoints/{BoundedContext}/{Module}/{Module}Endpoints.cs
```

Once the module reaches five endpoints or contains mixed command/query behavior, split it into:

```txt
Commands/
Queries/
Map{Module}Endpoints.cs
```

---

## 6. Endpoint File Template

Each endpoint file maps one use case.

```csharp
namespace Notrelix.API.Endpoints.WorkManagement.Boards.Commands;

public static class CreateBoardEndpoint
{
    public static IEndpointRouteBuilder MapCreateBoard(this IEndpointRouteBuilder group)
    {
        group.MapPost("/", HandleAsync)
            .WithName("WorkManagement.Boards.Create")
            .WithTags("WorkManagement.Boards")
            .WithSummary("Create a board")
            .Produces<CreateBoardResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid workspaceId,
        CreateBoardRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var command = request.ToCommand(workspaceId);
        var result = await sender.Send(command, cancellationToken);

        return Results.Created(
            $"/api/v1/boards/{result.BoardId}",
            CreateBoardResponse.From(result));
    }
}
```

Rules:

```txt
- Endpoint class is static.
- Endpoint method name starts with Map{UseCase}.
- Handler is private static.
- Endpoint accepts API contract, not Application command directly unless the endpoint is internal/temporary.
- Endpoint calls ISender exactly once for the main use case.
- Endpoint does not query DB.
- Endpoint does not call external services.
- Endpoint does not catch business exceptions locally.
```

---

## 7. Module Route Group Template

`Map{Module}Endpoints.cs` only groups routes and wires use case endpoints.

```csharp
namespace Notrelix.API.Endpoints.WorkManagement.Boards;

public static class MapBoardEndpoints
{
    public static IEndpointRouteBuilder MapBoardEndpoints(this IEndpointRouteBuilder app)
    {
        var workspaceBoards = app
            .MapGroup("/api/v1/workspaces/{workspaceId:guid}/boards")
            .RequireAuthorization()
            .WithTags("WorkManagement.Boards");

        workspaceBoards.MapCreateBoard();
        workspaceBoards.MapListWorkspaceBoards();

        var boards = app
            .MapGroup("/api/v1/boards/{boardId:guid}")
            .RequireAuthorization()
            .WithTags("WorkManagement.Boards");

        boards.MapGetBoard();
        boards.MapRenameBoard();
        boards.MapArchiveBoard();
        boards.MapRestoreBoard();
        boards.MapChangeBoardVisibility();

        return app;
    }
}
```

Rules:

```txt
- No business logic.
- No request mapping logic.
- No result mapping logic.
- Only route group composition.
- Use consistent WithTags and WithName.
```

---

## 8. API Contracts

### 8.1 Purpose

API contracts are the public HTTP boundary. They protect frontend/public API stability from internal Application model changes.

```txt
API Request Contract
→ Application Command/Query
→ Application Handler
→ Application Result/DTO
→ API Response Contract
```

### 8.2 Folder pattern

```txt
Contracts/{BoundedContext}/{Module}/Requests/{UseCase}Request.cs
Contracts/{BoundedContext}/{Module}/Responses/{Resource}Response.cs
Contracts/{BoundedContext}/{Module}/Mappers/{Module}ContractMapper.cs
```

Example:

```txt
Contracts/WorkManagement/Boards/
├── Requests/
│   ├── CreateBoardRequest.cs
│   ├── RenameBoardRequest.cs
│   └── ChangeBoardVisibilityRequest.cs
├── Responses/
│   ├── BoardResponse.cs
│   ├── BoardOverviewResponse.cs
│   └── CreateBoardResponse.cs
└── Mappers/
    └── BoardContractMapper.cs
```

### 8.3 Naming rules

```txt
Request:  {UseCase}Request
Response: {Resource}Response or {UseCase}Response
Mapper:   {Module}ContractMapper
```

Examples:

```txt
CreateBoardRequest
CreateBoardResponse
BoardResponse
BoardItemResponse
BoardSchemaResponse
BoardContractMapper
BoardItemContractMapper
```

### 8.4 Contract rules

API contracts must not include:

```txt
- EF entity types.
- Domain aggregate types.
- Domain event types.
- Infrastructure provider types.
- Internal exception types.
```

API contracts may include:

```txt
- Primitive values.
- Simple enums specifically intended for API.
- Nested response objects.
- Cursor pagination metadata.
- Effective permission summaries.
- ProblemDetails extension metadata.
```

### 8.5 Contract separation policy

Apply strict API contracts to:

```txt
- Stable frontend endpoints.
- Public API endpoints.
- Webhook-facing endpoints.
- Billing endpoints.
- Integration endpoints.
- High-use WorkManagement endpoints.
```

Temporary/internal endpoints may return Application DTOs only when:

```txt
- The endpoint is marked internal/experimental.
- It does not return Domain/EF entities.
- There is a TODO or issue to add API contracts before public release.
```

---

## 9. Route Strategy

### 9.1 Base route

Default frontend API:

```txt
/api/v1/...
```

Do not introduce a versioning package until there is a real need for multiple supported versions.

Initial strategy:

```txt
- Use explicit URL version segment: /api/v1.
- Keep route names stable.
- Add versioning package later only when /v2 is planned.
```

### 9.2 Workspace-first routes

Workspace-scoped collection routes should include workspace ID:

```txt
GET  /api/v1/workspaces/{workspaceId}/boards
POST /api/v1/workspaces/{workspaceId}/boards
GET  /api/v1/workspaces/{workspaceId}/members
GET  /api/v1/workspaces/{workspaceId}/search
```

Resource shortcut routes are allowed only when the backend can safely resolve workspace and permissions:

```txt
GET   /api/v1/boards/{boardId}
PATCH /api/v1/boards/{boardId}
POST  /api/v1/boards/{boardId}/archive
```

### 9.3 Private resource rule

For private resources:

```txt
- User cannot view resource: return 404 if revealing existence is a risk.
- User can view but cannot perform action: return 403.
```

This decision must be handled consistently by Application/authorization mapping, not manually in endpoints.

### 9.4 Route name convention

```txt
{BoundedContext}.{Module}.{UseCase}
```

Examples:

```txt
WorkManagement.Boards.Create
WorkManagement.BoardItems.UpdateFieldValue
Governance.Permissions.Grant
Billing.Checkout.CreateSession
Automation.Rules.Enable
```

### 9.5 Tag convention

```txt
{BoundedContext}.{Module}
```

Examples:

```txt
WorkManagement.Boards
WorkManagement.BoardItems
Documents.Blocks
Billing.Subscriptions
Integrations.Webhooks
```

---

## 10. API Surfaces

Notrelix should not treat all HTTP endpoints as one category.

```txt
Frontend API        /api/v1/...
Public API          /api/v1/public/...        Future, only when needed
Internal Admin API  /internal/...
Inbound Webhooks    /api/v1/integrations/webhooks/{provider}
Billing Webhooks    /api/v1/billing/webhooks/{provider}
Health              /health, /health/live, /health/ready
Realtime Hubs       /hubs/...
```

### 10.1 Frontend API

Used by web/mobile client.

```txt
- Optimized composite read models are allowed.
- Includes effectivePermissions when useful for UI.
- Uses workspace context.
- Uses cookie/JWT auth depending on product decision.
```

### 10.2 Public API

Do not expose public API casually.

When introduced, it requires:

```txt
- Dedicated API token/OAuth scopes.
- Stricter versioning.
- Stable contracts.
- Rate limits per token/app/workspace.
- Public documentation.
- Deprecation policy.
```

### 10.3 Internal Admin API

Internal API must require a separate policy:

```txt
RequireAuthorization("InternalAdmin")
```

Never expose internal endpoints through normal workspace permissions.

### 10.4 Inbound Webhook API

Inbound webhooks require:

```txt
- Raw body preservation.
- Signature verification before JSON parsing when required by provider.
- Idempotency.
- Provider-specific rate limit.
- No normal user authentication requirement unless provider supports OAuth callback flow.
```

---

## 11. Success Response Standard

Do not wrap all success responses in `{ data: ... }` by default.

Use standard HTTP semantics:

```txt
GET collection     → 200 OK with page response
GET resource       → 200 OK with response object
POST create        → 201 Created with response object and Location when useful
PATCH update       → 200 OK if returning updated state, 204 NoContent if not
DELETE             → 204 NoContent
Command action     → 200 OK or 204 NoContent depending on result
Async job command  → 202 Accepted with job response
```

Examples:

```json
{
  "id": "...",
  "name": "Roadmap",
  "visibility": "workspace"
}
```

For paginated collections:

```json
{
  "items": [],
  "nextCursor": "...",
  "hasMore": true
}
```

Avoid a universal wrapper unless there is a concrete product/API requirement.

---

## 12. Error Response Standard — ProblemDetails

### 12.1 Final decision

All API errors must use ProblemDetails:

```txt
Content-Type: application/problem+json
```

Use ASP.NET Core built-in ProblemDetails support:

```txt
AddProblemDetails()
IExceptionHandler / GlobalExceptionHandler
ProblemDetailsMapper
```

Do not add Hellang unless there is a documented future requirement.

Do not introduce a custom `ErrorResponse` envelope.

### 12.2 Required fields

Standard fields:

```txt
type
title
status
detail
instance
```

Required extensions:

```txt
errorCode
traceId
correlationId when available
workspaceId when available
errors for validation errors
```

Example:

```json
{
  "type": "https://docs.notrelix.com/problems/validation-failed",
  "title": "Validation failed",
  "status": 400,
  "detail": "One or more validation errors occurred.",
  "instance": "/api/v1/workspaces/{workspaceId}/boards",
  "errorCode": "validation.failed",
  "traceId": "00-...",
  "correlationId": "...",
  "workspaceId": "...",
  "errors": {
    "name": ["Board name is required."]
  }
}
```

### 12.3 Exception mapping

```txt
ValidationException                 → 400 validation.failed
BusinessRuleViolationException      → 400 business_rule.violation
Unauthorized/Unauthenticated        → 401 auth.unauthorized
ForbiddenAccessException            → 403 auth.forbidden
WorkspaceAccessDeniedException      → 403 workspace.access_denied
NotFoundException                   → 404 resource.not_found
ConcurrencyException                → 409 concurrency.conflict
IdempotencyConflictException        → 409 idempotency.conflict
EntitlementExceededException        → 403 entitlement.exceeded
RateLimitException                  → 429 rate_limit.exceeded
Unhandled Exception                 → 500 internal_server_error
```

### 12.4 Forbidden error patterns

Do not return:

```csharp
Results.BadRequest(new { errors = result.Errors })
Results.Json(new { error = ... })
new ApiError(...)
new ErrorResponse(...)
```

All errors go through ProblemDetails.

### 12.5 Security rule

Production error responses must never expose:

```txt
- Stack traces.
- SQL queries.
- Connection strings.
- Secrets/tokens/API keys.
- Provider raw errors containing credentials.
- Internal class names unless explicitly safe.
```

---

## 13. Result vs Exception Flow

Application may use typed exceptions, Result<T>, or a hybrid during migration.

API rule:

```txt
Regardless of Application flow, all failures returned to HTTP clients must normalize to ProblemDetails.
```

Preferred target:

```txt
Validation/authorization/not found/conflict exceptions
→ GlobalExceptionHandler
→ ProblemDetails

Application Result<T> failures
→ ResultExtensions.ToHttpResult()
→ ProblemDetails
```

Endpoint must not build ad-hoc error objects.

If `Result<T>` is used:

```csharp
return result.Match(
    success => Results.Ok(Response.From(success)),
    failure => failure.ToProblemHttpResult(httpContext));
```

But the final HTTP error shape must still be ProblemDetails.

---

## 14. Authentication and Authorization

### 14.1 API role

API performs authentication and coarse-grained authorization.

```txt
RequireAuthorization()
RequireAuthorization("SystemAdmin")
RequireAuthorization("InternalService")
RequireAuthorization("WebhookProvider")
```

Application performs fine-grained permission:

```txt
IWorkspaceRequest
IRequirePermission
IRequireEntitlement
IExpectedVersionRequest
```

### 14.2 Forbidden endpoint authorization

Do not write:

```csharp
if (user.Role == "Owner") { ... }
if (claims.Contains("WorkspaceAdmin")) { ... }
```

inside endpoints.

### 14.3 Claims mapping

API may map claims into current user context, but must not decide business permissions.

Claims should support:

```txt
UserId
Email
SessionId
Tenant/workspace hints if available
Security stamp/session version
```

---

## 15. Workspace and Tenant Context

### 15.1 Route binding

Workspace-scoped endpoints should pass `workspaceId` from route into Application command/query.

```csharp
var command = request.ToCommand(workspaceId);
```

Do not read workspace from client body if it is already in route.

### 15.2 Workspace resolution

`WorkspaceResolutionMiddleware` may enrich context, but Application pipeline remains the source of fine-grained workspace membership/permission enforcement.

### 15.3 Fail-closed rule

If a request is workspace-scoped and workspace cannot be resolved/validated:

```txt
Fail closed.
Do not fallback to global query.
Do not silently disable tenant filter.
```

---

## 16. Idempotency

### 16.1 Header

Use:

```txt
Idempotency-Key: <client-generated-key>
```

Required for:

```txt
- Create board
- Create board item
- Import/export job creation
- Billing checkout/session creation
- Webhook processing
- Payment-related commands
- Any command likely to be retried by frontend/network
```

### 16.2 API responsibility

API validates header presence for endpoints that require it.

Application/Infrastructure owns durable idempotency behavior.

### 16.3 Scope

Idempotency key must be scoped by:

```txt
UserId or client/app identity
WorkspaceId when available
Route/use case
Request hash when needed
```

Do not use raw idempotency key globally without scope.

---

## 17. Optimistic Concurrency

Concurrency-sensitive update commands should support one of:

```txt
If-Match: "<version>"
```

or request body field:

```json
{
  "expectedVersion": 12
}
```

Recommended rule:

```txt
- For REST resource update: prefer If-Match.
- For action commands: expectedVersion in body is acceptable.
```

Conflict response:

```txt
409 concurrency.conflict
```

Endpoint should pass expected version to Application command. It must not check version itself.

---

## 18. Pagination, Filtering, Sorting

Large collections must use cursor pagination.

Default pattern:

```txt
GET /api/v1/boards/{boardId}/items?pageSize=50&cursor=...
```

Response:

```json
{
  "items": [],
  "nextCursor": "...",
  "hasMore": true
}
```

Rules:

```txt
- Do not use offset pagination for high-volume mutable tables.
- pageSize must have a server-side maximum.
- Filters must be explicit and documented.
- Sorting must be whitelisted.
- Search query must be length-limited.
```

---

## 19. Read API vs Write API

### 19.1 Write API

Write endpoints map to commands/use cases.

Examples:

```txt
POST  /api/v1/workspaces/{workspaceId}/boards
PATCH /api/v1/boards/{boardId}/items/{itemId}/values/{fieldId}
POST  /api/v1/automations/{automationId}/enable
```

Responses should be small:

```txt
201 Created
200 OK with lightweight result
204 NoContent
202 Accepted for async jobs
```

### 19.2 Read API

Read endpoints may be screen-oriented and composite.

Examples:

```txt
GET /api/v1/boards/{boardId}/schema
GET /api/v1/boards/{boardId}/items?viewId=...&cursor=...
GET /api/v1/workspaces/{workspaceId}/home
GET /api/v1/workspaces/{workspaceId}/search?q=...
```

Read models can include:

```txt
- effectivePermissions
- featureEntitlements
- display metadata
- view config
- compact referenced users
```

This avoids frontend calling 10 endpoints to open one screen.

---

## 20. WorkManagement API Design

### 20.1 Modules

```txt
WorkManagement/
├── Boards
├── BoardItems
├── BoardFields
├── BoardGroups
├── BoardViews
├── BoardSchema
├── Checklists
├── Relations
├── Forms
├── Templates
├── Workload
├── Approvals
├── Rollups
└── BoardSearch
```

Canonical naming:

```txt
Board
BoardItem
BoardField
BoardGroup
BoardView
BoardSchema
```

Do not use legacy names as canonical API concepts:

```txt
Card
List
Column
```

Compatibility aliases may exist temporarily only with deprecation notes.

### 20.2 Board routes

```txt
GET    /api/v1/workspaces/{workspaceId}/boards
POST   /api/v1/workspaces/{workspaceId}/boards
GET    /api/v1/boards/{boardId}
PATCH  /api/v1/boards/{boardId}
POST   /api/v1/boards/{boardId}/archive
POST   /api/v1/boards/{boardId}/restore
POST   /api/v1/boards/{boardId}/duplicate
POST   /api/v1/boards/{boardId}/move
PATCH  /api/v1/boards/{boardId}/visibility
```

Endpoint code:

```txt
Endpoints/WorkManagement/Boards/Commands/CreateBoardEndpoint.cs
Endpoints/WorkManagement/Boards/Commands/RenameBoardEndpoint.cs
Endpoints/WorkManagement/Boards/Commands/ArchiveBoardEndpoint.cs
Endpoints/WorkManagement/Boards/Queries/GetBoardEndpoint.cs
Endpoints/WorkManagement/Boards/Queries/ListWorkspaceBoardsEndpoint.cs
```

### 20.3 Board schema routes

```txt
GET /api/v1/boards/{boardId}/schema
GET /api/v1/boards/{boardId}/schema/views/{viewId}
```

Response should include:

```txt
board
fields
fieldOptions
groups
views
defaultView
effectivePermissions
featureEntitlements
workspaceContext
```

### 20.4 Board item routes

```txt
GET    /api/v1/boards/{boardId}/items
POST   /api/v1/boards/{boardId}/items
GET    /api/v1/boards/{boardId}/items/{itemId}
PATCH  /api/v1/boards/{boardId}/items/{itemId}
DELETE /api/v1/boards/{boardId}/items/{itemId}
PATCH  /api/v1/boards/{boardId}/items/{itemId}/values/{fieldId}
PATCH  /api/v1/boards/{boardId}/items/{itemId}/values
POST   /api/v1/boards/{boardId}/items/{itemId}/move
POST   /api/v1/boards/{boardId}/items/reorder
POST   /api/v1/boards/{boardId}/items/{itemId}/archive
POST   /api/v1/boards/{boardId}/items/{itemId}/restore
POST   /api/v1/boards/{boardId}/items/{itemId}/duplicate
POST   /api/v1/boards/{boardId}/items/{itemId}/assignees
DELETE /api/v1/boards/{boardId}/items/{itemId}/assignees/{userId}
```

Endpoint code must split by use case.

---

## 21. Documents API Design

Documents should be block-oriented.

Modules:

```txt
Documents/
├── Pages
├── Blocks
├── Versions
├── Links
└── Exports
```

Routes:

```txt
GET    /api/v1/workspaces/{workspaceId}/pages
POST   /api/v1/workspaces/{workspaceId}/pages
GET    /api/v1/pages/{pageId}
PATCH  /api/v1/pages/{pageId}
POST   /api/v1/pages/{pageId}/archive
POST   /api/v1/pages/{pageId}/restore
POST   /api/v1/pages/{pageId}/move
GET    /api/v1/pages/{pageId}/blocks
POST   /api/v1/pages/{pageId}/blocks
PATCH  /api/v1/pages/{pageId}/blocks/{blockId}
DELETE /api/v1/pages/{pageId}/blocks/{blockId}
POST   /api/v1/pages/{pageId}/blocks/batch
GET    /api/v1/pages/{pageId}/versions
POST   /api/v1/pages/{pageId}/versions/{versionId}/restore
```

Endpoint code:

```txt
Endpoints/Documents/Blocks/Commands/AppendBlockEndpoint.cs
Endpoints/Documents/Blocks/Commands/UpdateBlockEndpoint.cs
Endpoints/Documents/Blocks/Commands/ReorderBlocksEndpoint.cs
Endpoints/Documents/Blocks/Queries/ListPageBlocksEndpoint.cs
```

---

## 22. Governance API Design

Modules:

```txt
Governance/
├── Permissions
├── Roles
├── ShareLinks
├── AuditLogs
├── SecurityEvents
└── Policies
```

Routes:

```txt
GET    /api/v1/resources/{resourceType}/{resourceId}/permissions
POST   /api/v1/resources/{resourceType}/{resourceId}/permissions
PATCH  /api/v1/resources/{resourceType}/{resourceId}/permissions/{permissionId}
DELETE /api/v1/resources/{resourceType}/{resourceId}/permissions/{permissionId}
GET    /api/v1/resources/{resourceType}/{resourceId}/effective-permissions
GET    /api/v1/resources/{resourceType}/{resourceId}/permission-decision
GET    /api/v1/resources/{resourceType}/{resourceId}/share-links
POST   /api/v1/resources/{resourceType}/{resourceId}/share-links
PATCH  /api/v1/share-links/{shareLinkId}
DELETE /api/v1/share-links/{shareLinkId}
GET    /api/v1/workspaces/{workspaceId}/audit-logs
GET    /api/v1/workspaces/{workspaceId}/security-events
GET    /api/v1/workspaces/{workspaceId}/policies
PATCH  /api/v1/workspaces/{workspaceId}/policies
```

Rules:

```txt
- Effective permissions endpoint is for frontend UX only.
- Backend still enforces permission in Application pipeline.
- Permission management requires ManageResourcePermission.
- Audit logs require ViewAuditLog.
```

---

## 23. Collaboration API Design

Modules:

```txt
Collaboration/
├── Comments
├── Reactions
├── Notifications
├── Activity
├── Attachments
└── Watchers
```

Routes:

```txt
GET    /api/v1/resources/{resourceType}/{resourceId}/comments
POST   /api/v1/resources/{resourceType}/{resourceId}/comments
PATCH  /api/v1/comments/{commentId}
DELETE /api/v1/comments/{commentId}
POST   /api/v1/comments/{commentId}/resolve
POST   /api/v1/comments/{commentId}/reopen
POST   /api/v1/resources/{resourceType}/{resourceId}/reactions
DELETE /api/v1/reactions/{reactionId}
GET    /api/v1/notifications
GET    /api/v1/notifications/unread-count
POST   /api/v1/notifications/{notificationId}/read
POST   /api/v1/notifications/read-all
GET    /api/v1/workspaces/{workspaceId}/activity
GET    /api/v1/resources/{resourceType}/{resourceId}/activity
POST   /api/v1/resources/{resourceType}/{resourceId}/attachments
GET    /api/v1/resources/{resourceType}/{resourceId}/attachments
DELETE /api/v1/attachments/{attachmentId}
```

---

## 24. Automation API Design

Modules:

```txt
Automation/
├── Rules
├── Templates
├── Executions
├── ExecutionLogs
└── DryRuns
```

Routes:

```txt
GET    /api/v1/workspaces/{workspaceId}/automations
POST   /api/v1/workspaces/{workspaceId}/automations
GET    /api/v1/automations/{automationId}
PATCH  /api/v1/automations/{automationId}
DELETE /api/v1/automations/{automationId}
POST   /api/v1/automations/{automationId}/enable
POST   /api/v1/automations/{automationId}/disable
POST   /api/v1/automations/{automationId}/dry-run
GET    /api/v1/automations/{automationId}/executions
GET    /api/v1/automation-executions/{executionId}
GET    /api/v1/workspaces/{workspaceId}/automation-templates
```

Rules:

```txt
- API starts/validates automation rule use cases only.
- Actual automation execution belongs to Application/Infrastructure workers.
- Dry-run must not mutate production state unless explicitly designed.
```

---

## 25. Integrations API Design

Modules:

```txt
Integrations/
├── Catalog
├── Connections
├── OAuth
├── SyncJobs
├── Webhooks
├── WebhookDeliveries
└── ProviderCallbacks
```

Routes:

```txt
GET    /api/v1/workspaces/{workspaceId}/integrations
GET    /api/v1/workspaces/{workspaceId}/integrations/connections
POST   /api/v1/workspaces/{workspaceId}/integrations/{provider}/connect
PATCH  /api/v1/integrations/connections/{connectionId}
DELETE /api/v1/integrations/connections/{connectionId}
GET    /api/v1/integrations/{provider}/callback
POST   /api/v1/integrations/connections/{connectionId}/sync
GET    /api/v1/integrations/connections/{connectionId}/health
GET    /api/v1/workspaces/{workspaceId}/webhooks
POST   /api/v1/workspaces/{workspaceId}/webhooks
PATCH  /api/v1/webhooks/{webhookId}
DELETE /api/v1/webhooks/{webhookId}
GET    /api/v1/webhooks/{webhookId}/deliveries
POST   /api/v1/integrations/webhooks/{provider}
```

Inbound webhook endpoints require:

```txt
- Signature verification.
- Idempotency.
- Raw body support.
- Provider rate limit.
- Replay protection when provider supports timestamp/signature scheme.
```

---

## 26. Billing API Design

Modules:

```txt
Billing/
├── Plans
├── Subscriptions
├── Checkout
├── CustomerPortal
├── Invoices
├── Usage
├── Entitlements
└── Webhooks
```

Routes:

```txt
GET    /api/v1/workspaces/{workspaceId}/billing/plan
GET    /api/v1/workspaces/{workspaceId}/billing/subscription
POST   /api/v1/workspaces/{workspaceId}/billing/checkout-session
POST   /api/v1/workspaces/{workspaceId}/billing/customer-portal
POST   /api/v1/workspaces/{workspaceId}/billing/cancel
GET    /api/v1/workspaces/{workspaceId}/billing/invoices
GET    /api/v1/workspaces/{workspaceId}/billing/usage
GET    /api/v1/workspaces/{workspaceId}/entitlements
POST   /api/v1/billing/webhooks/{provider}
```

Rules:

```txt
- Billing webhook must not require user auth.
- Billing webhook must require provider signature verification.
- Billing commands must be idempotent.
- Entitlement checks happen in Application pipeline, not endpoint.
```

---

## 27. Search, Reporting, Operations API Design

### 27.1 Search

```txt
GET /api/v1/workspaces/{workspaceId}/search?q=...
GET /api/v1/boards/{boardId}/search?q=...
```

Rules:

```txt
- Search must be permission-aware.
- API does not filter private data after the fact.
- Application/ReadModel must return only accessible results.
```

### 27.2 Reporting

```txt
GET    /api/v1/workspaces/{workspaceId}/dashboards
POST   /api/v1/workspaces/{workspaceId}/dashboards
GET    /api/v1/dashboards/{dashboardId}
PATCH  /api/v1/dashboards/{dashboardId}
DELETE /api/v1/dashboards/{dashboardId}
POST   /api/v1/dashboards/{dashboardId}/widgets
PATCH  /api/v1/dashboards/{dashboardId}/widgets/{widgetId}
DELETE /api/v1/dashboards/{dashboardId}/widgets/{widgetId}
```

### 27.3 Operations

```txt
POST /api/v1/workspaces/{workspaceId}/imports
GET  /api/v1/import-jobs/{jobId}
POST /api/v1/workspaces/{workspaceId}/exports
GET  /api/v1/export-jobs/{jobId}
```

Async operation rule:

```txt
POST import/export → 202 Accepted with jobId.
GET job status → current status/result/error.
```

---

## 28. Upload and Download Rules

### 28.1 Uploads

File upload endpoints must define:

```txt
- Max file size.
- Allowed content types.
- Malware scan hook if needed.
- Storage target abstraction.
- Workspace/resource ownership.
- Idempotency if upload finalization can be retried.
```

API may accept multipart data, but must not directly store provider-specific files. It sends command to Application or calls upload abstraction if explicitly designed as API infrastructure concern.

### 28.2 Downloads

Prefer signed/temporary download URLs for large files.

Do not stream large files through API unless there is a clear reason.

---

## 29. Webhook Rules

Inbound webhook endpoint flow:

```txt
1. Capture raw body.
2. Verify provider signature.
3. Parse event.
4. Apply idempotency check.
5. Send Application command.
6. Return provider-compatible success/failure.
```

Do not process provider side effects directly inside endpoint.

Webhook endpoints must be covered by tests for:

```txt
- Invalid signature.
- Duplicate event.
- Unknown event type.
- Malformed payload.
- Valid event accepted.
```

---

## 30. Realtime API Rules

Realtime hub endpoints are not command APIs.

```txt
REST command endpoint → Application use case → event/outbox/realtime publisher
```

Do not mutate business state directly inside SignalR hub methods unless the hub method itself calls an Application command/query.

Routes:

```txt
/hubs/workspace
/hubs/board
/hubs/notifications
```

Realtime authorization must verify workspace/resource access.

---

## 31. OpenAPI Standard

OpenAPI must document:

```txt
- Auth scheme.
- ProblemDetails response format.
- Cursor pagination.
- Idempotency-Key header.
- If-Match / expectedVersion.
- Rate limit headers.
- Workspace-scoped route behavior.
- 403 vs 404 private resource behavior.
- Webhook signature headers.
```

Every endpoint should have:

```txt
- WithName
- WithTags
- WithSummary
- Produces / ProducesProblem
- Request/response examples where useful
```

Do not let OpenAPI infer everything from anonymous objects.

---

## 32. Health Checks

Routes:

```txt
GET /health
GET /health/live
GET /health/ready
```

Readiness should include:

```txt
- Database connectivity.
- Redis/cache connectivity if required.
- Outbox backlog threshold.
- Worker heartbeat.
- External provider optional degraded checks.
```

Liveness should be lightweight.

Do not put sensitive diagnostics in public health response.

---

## 33. Rate Limiting

Rate limits should be policy-based.

Examples:

```txt
AuthenticatedUserPolicy
WorkspacePolicy
PublicApiTokenPolicy
WebhookProviderPolicy
AuthEndpointPolicy
SearchPolicy
```

High-risk endpoints:

```txt
- Login/register/reset password.
- Search.
- File upload.
- Import/export.
- Webhooks.
- Public API endpoints.
```

Rate limit failures return ProblemDetails:

```txt
429 rate_limit.exceeded
```

---

## 34. Security Headers and Request Safety

API should configure:

```txt
- HSTS in production.
- HTTPS redirection.
- Secure cookies.
- CORS allowlist.
- Security headers where applicable.
- Request body limits.
- JSON depth limits.
- Multipart limits.
```

Do not allow wildcard CORS in production.

---

## 35. Observability

Every request should include/log:

```txt
- traceId
- correlationId
- userId when authenticated
- workspaceId when available
- route name
- status code
- errorCode when ProblemDetails
- duration
```

Metrics:

```txt
api_requests_total{route,status}
api_request_duration_ms{route,status}
api_problem_details_total{errorCode,status}
api_rate_limit_rejections_total{policy}
api_idempotency_conflicts_total{route}
api_concurrency_conflicts_total{route}
api_auth_failures_total{reason}
```

Do not log:

```txt
- passwords
- tokens
- authorization headers
- webhook secrets
- raw file contents
- large payloads by default
```

---

## 36. API Architecture Tests

Add tests to prevent drift.

### 36.1 Boundary tests

```txt
- API must not reference Domain aggregate namespaces directly in endpoints/contracts.
- API must not inject DbContext into endpoints.
- API must not inject Infrastructure provider services directly into endpoints.
- API must not return Domain/EF entity types.
- API Application dependency is allowed.
```

### 36.2 Endpoint structure tests

```txt
- Endpoint files must live under Endpoints/{BoundedContext}/{Module}/Commands|Queries.
- Large module endpoint files are not allowed.
- Map{Module}Endpoints.cs contains route composition only.
- Endpoint classes end with Endpoint.
- Map methods start with Map.
```

### 36.3 Error tests

```txt
- Validation error returns application/problem+json.
- NotFound returns ProblemDetails.
- Forbidden returns ProblemDetails.
- Concurrency conflict returns ProblemDetails.
- Unhandled exception returns ProblemDetails without stack trace.
- All error responses include errorCode and traceId.
```

### 36.4 Contract tests

```txt
- Public endpoint response types are API Contracts, not EF/Domain entities.
- ProblemDetails schema is included in OpenAPI.
- Endpoint has WithName and WithTags.
```

---

## 37. Development Rules for New Endpoint

Before adding a new endpoint, answer:

```txt
1. Which bounded context?
2. Which module?
3. Is it a command or query?
4. Which Application use case does it call?
5. Is it workspace-scoped?
6. Which permission/entitlement does Application enforce?
7. Does it require idempotency?
8. Does it require expectedVersion/If-Match?
9. Does it return a stable API contract?
10. What ProblemDetails responses are documented?
11. Is it frontend/public/internal/webhook/health/realtime?
12. Does it require cursor pagination?
13. Does it require rate limiting?
```

If these cannot be answered, the endpoint is not ready.

---

## 38. Definition of Done for Endpoint

```txt
[ ] Endpoint file is under correct bounded context/module/use-case folder.
[ ] Route is resource-oriented and versioned.
[ ] Endpoint calls ISender, not DbContext/provider.
[ ] API request contract exists if endpoint is stable/public/frontend.
[ ] API response contract exists if endpoint returns data.
[ ] Contract mapper exists if mapping is non-trivial.
[ ] ProblemDetails responses are declared.
[ ] Authorization policy is coarse-grained only.
[ ] WorkspaceId is route-bound where appropriate.
[ ] Idempotency-Key is required for retry-sensitive command.
[ ] If-Match/expectedVersion is supported for concurrency-sensitive command.
[ ] Cursor pagination is used for large collections.
[ ] OpenAPI name/tag/summary are defined.
[ ] Tests cover success + at least one failure path.
[ ] No Domain/EF entity is returned.
[ ] No business logic is in endpoint.
```

---

## 39. Agent Guardrails

When an Agent modifies the API layer, it must obey:

```txt
1. Do not create flat BoardEndpoints.cs/BillingEndpoints.cs files for large modules.
2. Do not put all routes in Program.cs.
3. Do not inject DbContext into endpoints.
4. Do not return Domain/EF entities.
5. Do not add custom ErrorResponse envelope.
6. Do not bypass Application CQRS handlers.
7. Do not implement permission logic inside endpoints.
8. Do not add Hellang unless explicitly approved by architecture decision.
9. Do not add public API routes without contracts and OpenAPI docs.
10. Do not create route names without bounded context/module/use case.
11. Do not add new root folders casually.
12. Do not mix legacy Card/List/Column naming with canonical BoardItem/BoardGroup/BoardField naming.
13. Do not process webhook side effects directly inside endpoint.
14. Do not use offset pagination for large mutable collections.
15. Do not use anonymous error responses.
```

If a requested change violates these rules, stop and propose a compliant structure.

---

## 40. Refactor Policy

This API architecture is intended to avoid repeated structural refactors.

Root structure is stable.

Allowed future changes:

```txt
- Add new endpoint use case files.
- Add new contracts under existing bounded context/module.
- Add new module under existing bounded context.
- Add new bounded context only when Domain/Application has one.
- Add OpenAPI filters or setup files under existing OpenApi folder.
- Add middleware only if it is API-level and cross-cutting.
```

Not allowed without ADR:

```txt
- Replacing Minimal API with Controllers globally.
- Replacing ProblemDetails with custom ErrorResponse.
- Moving endpoint organization back to flat files.
- Removing API Contracts from stable endpoints.
- Adding direct DbContext usage in API.
- Introducing GraphQL as primary API surface.
- Adding new public API surface without version/deprecation policy.
```

---

## 41. Implementation Roadmap

### Phase 1 — Structure lock

```txt
- Restructure Endpoints by bounded context/module/use case.
- Add Map{Module}Endpoints.cs files.
- Remove/avoid large flat endpoint files.
- Clean GlobalUsings to avoid Domain aggregate leakage.
```

### Phase 2 — Error standardization

```txt
- Ensure all failures return ProblemDetails.
- Remove active custom ErrorResponse usage.
- Update ResultExtensions to produce ProblemDetails.
- Add validation/problem tests.
```

### Phase 3 — WorkManagement endpoints

```txt
- Boards
- BoardItems
- BoardFields
- BoardGroups
- BoardViews
- BoardSchema
```

Prioritize board schema and item list query APIs because they drive frontend screens.

### Phase 4 — Workspace/Governance/Billing core

```txt
- Workspace management
- Members/invitations
- Permissions/effective permissions
- Billing plan/subscription/entitlements
```

### Phase 5 — Automation/Integrations/Webhooks

```txt
- Automation rules/executions
- Integration connections
- Provider callbacks
- Inbound webhooks
- Webhook deliveries
```

### Phase 6 — Observability/OpenAPI hardening

```txt
- API metrics by route/status/errorCode
- OpenAPI examples
- Health/readiness details
- Rate limit documentation
```

### Phase 7 — Public API readiness, only if needed

```txt
- Token scopes
- Public API versioning
- SDK-safe contracts
- Deprecation policy
- Public docs
```

---

## 42. Final Summary

The Notrelix API layer must be designed as a platform boundary, not a collection of controllers.

Final principles:

```txt
Product primitives define the route.
Application use cases define the endpoint code.
Bounded contexts define the folder boundary.
Read APIs serve screens and optimized read models.
Write APIs serve commands.
ProblemDetails standardizes all errors.
API Contracts protect external stability.
Architecture tests prevent drift.
```

This is the final recommended direction for building the Notrelix API layer at enterprise scale.
