# Routing & Authorization Specification

> **TanStack Router Tree, Typed Context, Route Guards & Entitlement Boundaries**

---

## 1. Route Hierarchy

- `_public`: Unauthenticated routes (Sign In, Sign Up, Accept Invite).
- `_authenticated`: Authenticated app shell requiring valid auth session.
  - `workspaces/$workspaceId`: Workspace scope requiring active membership.

---

## 2. Guard Separation

- **Authentication Guard (`requireAuth`):** Redirects unauthenticated users to `/sign-in?returnUrl=...`.
- **Membership Guard (`requireWorkspaceMembership`):** Validates user membership for `$workspaceId`.
- **Permission Guard (`requirePermission`):** Evaluates fine-grained resource permissions (`can('board.update')`).
- **Entitlement Guard (`requireEntitlement`):** Evaluates subscription plan capability (`hasEntitlement('workManagement.timeline')`).
- **Feature Flag Guard (`requireFeatureFlag`):** Controls rollout of experimental features.
