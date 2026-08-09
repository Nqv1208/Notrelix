# Backend Contract Gaps

This document tracks endpoints needed by the frontend application that are either unverified, missing, or require specification alignment with the backend API.

## Pending Workspace Endpoints

### 1. Workspace Members CRUD

- **Status**: Pending Verification
- **Required Operations**:
  - `GET /workspaces/{workspaceId}/members` - Retrieve members of a workspace.
  - `PUT /workspaces/{workspaceId}/members/{userId}/role` - Update a member's role (e.g., Owner, Admin, Member).
  - `DELETE /workspaces/{workspaceId}/members/{userId}` - Remove a member from a workspace.
- **Frontend Reference**: `@notrelix/features-workspace` -> `members.service.ts`

### 2. Workspace Custom Views (Persisted)

- **Status**: Pending Verification
- **Required Operations**:
  - `GET /workspaces/{workspaceId}/views` - Get persisted custom workspace views.
  - `POST /workspaces/{workspaceId}/views` - Create a custom view.
  - `PATCH /workspaces/{workspaceId}/views/{viewId}` - Update custom view configuration.
  - `DELETE /workspaces/{workspaceId}/views/{viewId}` - Delete custom view.
  - `POST /workspaces/{workspaceId}/views/reorder` - Reorder custom views.
- **Frontend Reference**: `@notrelix/features-workspace` -> `views.service.ts`

### 3. Workspace Activity Log

- **Status**: Pending Verification
- **Required Operations**:
  - `GET /workspaces/{workspaceId}/activity` - Retrieve workspace-scoped audit/activity log.
- **Frontend Reference**: `@notrelix/features-workspace` -> `activity.service.ts`

---

## Pending Notifications Endpoints

### 1. Unread Count, Archive & Preferences

- **Status**: Pending Verification
- **Required Operations**:
  - `GET /notifications/unread-count` - Get count of unread notifications.
  - `POST /notifications/{notificationId}/archive` - Archive a notification.
  - `GET /notifications/preferences` - Get notification email/push preferences.
  - `PATCH /notifications/preferences` - Update notification preferences.
- **Frontend Reference**: `@notrelix/features-notifications` -> `notifications.service.ts`

---

## Pending Account / User Endpoints

### 1. Preferences & Security Settings

- **Status**: Pending Verification
- **Required Operations**:
  - `GET /users/preferences` - Get user interface preferences (theme, sidebar state).
  - `PATCH /users/preferences` - Update user preferences.
  - `GET /users/security` - Get user security profile (2FA status, session count).
- **Frontend Reference**: `@notrelix/features-account` -> `account.service.ts`

---

## Guidelines for Resolving Gaps

1. **Verify with Backend**: Check Swagger UI or Backend endpoints code first.
2. **If Exist**: Move to Verified, update `@notrelix/contracts/endpoints` and remove the stub warning from the frontend service.
3. **If Missing**:
   - Keep the frontend service stubbed out.
   - Comment code with `// PENDING BACKEND: <url>` and point to this file.
   - Do not predict backend naming patterns.
