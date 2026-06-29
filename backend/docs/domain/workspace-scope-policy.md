# Workspace Scope Policy

Tenant isolation is a Domain rule. `IWorkspaceScoped` is the marker for models
whose business state belongs to one workspace.

## Scope Types

| Scope type | Meaning | Examples |
|---|---|---|
| Global aggregate | Exists outside any workspace. | `User`, global `Plan`, some identity security models. |
| Workspace root | Defines tenant boundary and is not scoped by itself. | `Workspace`. |
| Workspace-scoped aggregate/entity | Carries `WorkspaceId` and must implement `IWorkspaceScoped`. | `Board`, `BoardItem`, `Page`, `Comment`, `Subscription`. |
| System-owned model | Created by system/catalog and may have no workspace. | global templates, catalog plans. |
| Projection/ops record | May carry workspace metadata but is not rich Domain. | search documents, outbox, idempotency. |

## Workspace Root Rules

`Workspace` is the root of tenant scope and must not implement
`IWorkspaceScoped`. It owns workspace metadata and lifecycle only.

Workspace creation must create owner membership in the same use case or factory
contract. Domain already exposes `WorkspaceFactory.CreateWithOwner`; Application
must not bypass this rule for normal workspace creation.

## Workspace-Scoped Resource Rules

Every Domain type with a required `WorkspaceId` must implement
`IWorkspaceScoped`, except explicitly classified global/system/projection/ops
models.

Workspace-scoped aggregates must:

- validate non-empty `WorkspaceId` on create;
- include `WorkspaceId` in workspace-scoped domain events;
- reject cross-workspace reference objects;
- never infer workspace from current HTTP/user context;
- leave parent workspace archive/delete checks to Application unless parent
  state is passed as a pure policy input.

## System Context

System actions are allowed only when the method documents system behavior.

Examples:

- expiration jobs may pass null actor for `Expired` events and audit updates;
- billing provider facts must be converted by Application into domain method
  calls, not provider SDK objects;
- system restore/delete use cases must still pass timestamps supplied by
  Application.

Domain must not call `DateTime.UtcNow` or `DateTimeOffset.UtcNow`.

## Archived Workspace Behavior

Archived workspace policy:

- read operations are allowed;
- normal content creation/mutation is denied;
- workspace settings/name updates are denied;
- billing/admin/system operations may continue when explicitly documented;
- export may be allowed by Application policy;
- delete/restore is admin/system controlled.

Current Domain examples:

- `Workspace.Rename` rejects archived state.
- `Workspace.UpdateSettings` rejects archived state.

Child aggregates such as boards/pages do not know parent workspace state. New
Application use cases must check workspace state before creating or mutating
workspace-scoped resources.

## Soft-Deleted Workspace Behavior

Soft-deleted workspace policy:

- no normal user operations;
- dependent resources remain scoped but inaccessible;
- restore is admin/system only;
- purge/export policies must be explicit;
- child resources should not individually invent tenant deletion semantics.

If a child restore is requested while workspace is deleted or archived,
Application must reject it or pass a pure domain policy input that rejects it.
