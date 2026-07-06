using Notrelix.API.Security;

namespace Notrelix.API.Endpoints;

public static class EndpointMappingExtensions
{
    // ── Public ──────────────────────────────────────────────
    public static RouteHandlerBuilder MapPublicGet(
        this IEndpointRouteBuilder endpoints,
        string pattern,
        Delegate handler)
        => endpoints.MapGet(pattern, handler).AsPublicEndpoint();

    public static RouteHandlerBuilder MapPublicPost(
        this IEndpointRouteBuilder endpoints,
        string pattern,
        Delegate handler)
        => endpoints.MapPost(pattern, handler).AsPublicEndpoint();

    public static RouteHandlerBuilder MapPublicPut(
        this IEndpointRouteBuilder endpoints,
        string pattern,
        Delegate handler)
        => endpoints.MapPut(pattern, handler).AsPublicEndpoint();

    public static RouteHandlerBuilder MapPublicPatch(
        this IEndpointRouteBuilder endpoints,
        string pattern,
        Delegate handler)
        => endpoints.MapPatch(pattern, handler).AsPublicEndpoint();

    public static RouteHandlerBuilder MapPublicDelete(
        this IEndpointRouteBuilder endpoints,
        string pattern,
        Delegate handler)
        => endpoints.MapDelete(pattern, handler).AsPublicEndpoint();

    // ── Authenticated ───────────────────────────────────────
    public static RouteHandlerBuilder MapAuthenticatedGet(
        this IEndpointRouteBuilder endpoints,
        string pattern,
        Delegate handler)
        => endpoints.MapGet(pattern, handler).AsAuthenticatedEndpoint();

    public static RouteHandlerBuilder MapAuthenticatedPost(
        this IEndpointRouteBuilder endpoints,
        string pattern,
        Delegate handler)
        => endpoints.MapPost(pattern, handler).AsAuthenticatedEndpoint();

    public static RouteHandlerBuilder MapAuthenticatedPut(
        this IEndpointRouteBuilder endpoints,
        string pattern,
        Delegate handler)
        => endpoints.MapPut(pattern, handler).AsAuthenticatedEndpoint();

    public static RouteHandlerBuilder MapAuthenticatedPatch(
        this IEndpointRouteBuilder endpoints,
        string pattern,
        Delegate handler)
        => endpoints.MapPatch(pattern, handler).AsAuthenticatedEndpoint();

    public static RouteHandlerBuilder MapAuthenticatedDelete(
        this IEndpointRouteBuilder endpoints,
        string pattern,
        Delegate handler)
        => endpoints.MapDelete(pattern, handler).AsAuthenticatedEndpoint();

    // ── Account ─────────────────────────────────────────────
    public static RouteHandlerBuilder MapAccountGet(
        this IEndpointRouteBuilder endpoints,
        string pattern,
        Delegate handler)
        => endpoints.MapGet(pattern, handler).AsAccountEndpoint();

    public static RouteHandlerBuilder MapAccountPost(
        this IEndpointRouteBuilder endpoints,
        string pattern,
        Delegate handler)
        => endpoints.MapPost(pattern, handler).AsAccountEndpoint();

    public static RouteHandlerBuilder MapAccountPut(
        this IEndpointRouteBuilder endpoints,
        string pattern,
        Delegate handler)
        => endpoints.MapPut(pattern, handler).AsAccountEndpoint();

    public static RouteHandlerBuilder MapAccountPatch(
        this IEndpointRouteBuilder endpoints,
        string pattern,
        Delegate handler)
        => endpoints.MapPatch(pattern, handler).AsAccountEndpoint();

    public static RouteHandlerBuilder MapAccountDelete(
        this IEndpointRouteBuilder endpoints,
        string pattern,
        Delegate handler)
        => endpoints.MapDelete(pattern, handler).AsAccountEndpoint();

    // ── Workspace ───────────────────────────────────────────
    public static RouteHandlerBuilder MapWorkspaceGet(
        this IEndpointRouteBuilder endpoints,
        string pattern,
        Delegate handler)
        => endpoints.MapGet(pattern, handler).AsWorkspaceEndpoint();

    public static RouteHandlerBuilder MapWorkspacePost(
        this IEndpointRouteBuilder endpoints,
        string pattern,
        Delegate handler)
        => endpoints.MapPost(pattern, handler).AsWorkspaceEndpoint();

    public static RouteHandlerBuilder MapWorkspacePut(
        this IEndpointRouteBuilder endpoints,
        string pattern,
        Delegate handler)
        => endpoints.MapPut(pattern, handler).AsWorkspaceEndpoint();

    public static RouteHandlerBuilder MapWorkspacePatch(
        this IEndpointRouteBuilder endpoints,
        string pattern,
        Delegate handler)
        => endpoints.MapPatch(pattern, handler).AsWorkspaceEndpoint();

    public static RouteHandlerBuilder MapWorkspaceDelete(
        this IEndpointRouteBuilder endpoints,
        string pattern,
        Delegate handler)
        => endpoints.MapDelete(pattern, handler).AsWorkspaceEndpoint();

    // ── Resource ────────────────────────────────────────────
    public static RouteHandlerBuilder MapResourceGet(
        this IEndpointRouteBuilder endpoints,
        string pattern,
        Delegate handler)
        => endpoints.MapGet(pattern, handler).AsResourceEndpoint();

    public static RouteHandlerBuilder MapResourcePost(
        this IEndpointRouteBuilder endpoints,
        string pattern,
        Delegate handler)
        => endpoints.MapPost(pattern, handler).AsResourceEndpoint();

    public static RouteHandlerBuilder MapResourcePut(
        this IEndpointRouteBuilder endpoints,
        string pattern,
        Delegate handler)
        => endpoints.MapPut(pattern, handler).AsResourceEndpoint();

    public static RouteHandlerBuilder MapResourcePatch(
        this IEndpointRouteBuilder endpoints,
        string pattern,
        Delegate handler)
        => endpoints.MapPatch(pattern, handler).AsResourceEndpoint();

    public static RouteHandlerBuilder MapResourceDelete(
        this IEndpointRouteBuilder endpoints,
        string pattern,
        Delegate handler)
        => endpoints.MapDelete(pattern, handler).AsResourceEndpoint();

    // ── Admin ───────────────────────────────────────────────
    public static RouteHandlerBuilder MapAdminGet(
        this IEndpointRouteBuilder endpoints,
        string pattern,
        Delegate handler)
        => endpoints.MapGet(pattern, handler).AsAdminEndpoint();

    public static RouteHandlerBuilder MapAdminPost(
        this IEndpointRouteBuilder endpoints,
        string pattern,
        Delegate handler)
        => endpoints.MapPost(pattern, handler).AsAdminEndpoint();

    public static RouteHandlerBuilder MapAdminPut(
        this IEndpointRouteBuilder endpoints,
        string pattern,
        Delegate handler)
        => endpoints.MapPut(pattern, handler).AsAdminEndpoint();

    public static RouteHandlerBuilder MapAdminPatch(
        this IEndpointRouteBuilder endpoints,
        string pattern,
        Delegate handler)
        => endpoints.MapPatch(pattern, handler).AsAdminEndpoint();

    public static RouteHandlerBuilder MapAdminDelete(
        this IEndpointRouteBuilder endpoints,
        string pattern,
        Delegate handler)
        => endpoints.MapDelete(pattern, handler).AsAdminEndpoint();

    // ── Internal ────────────────────────────────────────────
    public static RouteHandlerBuilder MapInternalGet(
        this IEndpointRouteBuilder endpoints,
        string pattern,
        Delegate handler)
        => endpoints.MapGet(pattern, handler).AsInternalEndpoint();

    public static RouteHandlerBuilder MapInternalPost(
        this IEndpointRouteBuilder endpoints,
        string pattern,
        Delegate handler)
        => endpoints.MapPost(pattern, handler).AsInternalEndpoint();

    public static RouteHandlerBuilder MapInternalPut(
        this IEndpointRouteBuilder endpoints,
        string pattern,
        Delegate handler)
        => endpoints.MapPut(pattern, handler).AsInternalEndpoint();

    public static RouteHandlerBuilder MapInternalPatch(
        this IEndpointRouteBuilder endpoints,
        string pattern,
        Delegate handler)
        => endpoints.MapPatch(pattern, handler).AsInternalEndpoint();

    public static RouteHandlerBuilder MapInternalDelete(
        this IEndpointRouteBuilder endpoints,
        string pattern,
        Delegate handler)
        => endpoints.MapDelete(pattern, handler).AsInternalEndpoint();

    // ── Private helpers ─────────────────────────────────────
    private static RouteHandlerBuilder AsPublicEndpoint(this RouteHandlerBuilder builder)
        => builder
            .AllowAnonymous()
            .WithMetadata(new PublicEndpointAttribute());

    private static RouteHandlerBuilder AsAuthenticatedEndpoint(this RouteHandlerBuilder builder)
        => builder
            .RequireAuthorization()
            .WithMetadata(new AuthenticatedEndpointAttribute());

    private static RouteHandlerBuilder AsAccountEndpoint(this RouteHandlerBuilder builder)
        => builder
            .RequireAuthorization()
            .WithMetadata(new AccountScopedEndpointAttribute());

    private static RouteHandlerBuilder AsWorkspaceEndpoint(this RouteHandlerBuilder builder)
        => builder
            .RequireAuthorization()
            .WithMetadata(new WorkspaceScopedEndpointAttribute());

    private static RouteHandlerBuilder AsResourceEndpoint(this RouteHandlerBuilder builder)
        => builder
            .RequireAuthorization()
            .WithMetadata(new ResourceScopedEndpointAttribute());

    private static RouteHandlerBuilder AsAdminEndpoint(this RouteHandlerBuilder builder)
        => builder
            .RequireAuthorization("SystemAdmin")
            .WithMetadata(new AdminEndpointAttribute());

    private static RouteHandlerBuilder AsInternalEndpoint(this RouteHandlerBuilder builder)
        => builder
            .RequireAuthorization("InternalService")
            .WithMetadata(new InternalEndpointAttribute());
}
