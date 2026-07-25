# Workspace Module — Phase 0 Foundation Design

## Principles

- Domain changes allowed for real business behavior (e.g., Workspace.UpdateDescription)
- No technical workaround tables (no WorkspaceSlugReservation)
- No ResourceRef modification (Domain doesn't know "current account")
- No IWorkspaceSlugAllocator
- No handler SaveChangesAsync
- Slug unique per account
- AccountId từ tenant context, không từ client

## Micro-slices

| Slice | Focus | Files |
|-------|-------|-------|
| W0A | Account permission contract | IAccountRequest, IRequirePermission, AuthorizationBehavior |
| W0B | Workspace.UpdateDescription + event | Workspace.cs, new DomainEvent |
| W0C | Slug index + conflict mapping | Migration, EF config, exception mapping |
| W0D | CreateWorkspace hardening | CreateWorkspaceCommand, handler, PermissionAction |
| W0E | UpdateWorkspaceProfile split | Delete old, create new command |
| W0F | Handler/marker cleanup | 15 handlers, GetUserWorkspaces fix |

## Key decisions

- IAccountRequest: metadata-only, no AccountId property
- IRequirePermission.Resource: nullable for account-scoped
- AuthorizationBehavior: resolve account resource from tenant context
- WorkspaceDescriptionUpdatedDomainEvent inherits WorkspaceRootDomainEvent
- WorkspaceFactory.CreateWithOwner signature unchanged (isPersonal before description)
- Unique violation check: DbUpdateException.InnerException, constraint name match
- Slug-based mutations: not half-secured in Phase 0
