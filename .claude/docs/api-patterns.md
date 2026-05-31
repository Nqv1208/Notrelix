# Common API Patterns

Quick reference for common API patterns in Notrelix.

## Response Format

### Success Response

```json
{
  "success": true,
  "data": {
    "id": "123e4567-e89b-12d3-a456-426614174000",
    "title": "My Board",
    "workspaceId": "workspace-id"
  },
  "error": null
}
```

### Error Response (RFC 7807)

```json
{
  "type": "https://notrelix.com/errors/not-found",
  "title": "Resource not found",
  "status": 404,
  "detail": "Page with id 'xxx' not found",
  "traceId": "00-abc123-def456-00"
}
```

---

## CRUD Patterns

### Create Resource

**Request:**
```http
POST /api/v1/workspaces/{workspaceId}/boards
Content-Type: application/json

{
  "title": "New Board",
  "description": "Project board",
  "color": "#6161ff"
}
```

**Response:**
```http
HTTP/1.1 201 Created
Location: /api/v1/boards/board-id

{
  "success": true,
  "data": {
    "id": "board-id",
    "workspaceId": "workspace-id",
    "title": "New Board",
    "description": "Project board",
    "color": "#6161ff",
    "createdAt": "2026-05-31T10:00:00Z"
  }
}
```

### Get Resource

**Request:**
```http
GET /api/v1/boards/{boardId}
```

**Response:**
```http
HTTP/1.1 200 OK

{
  "success": true,
  "data": {
    "id": "board-id",
    "title": "My Board",
    "workspaceId": "workspace-id"
  }
}
```

### Update Resource (Partial)

**Request:**
```http
PATCH /api/v1/boards/{boardId}
Content-Type: application/json

{
  "title": "Updated Board Title"
}
```

**Response:**
```http
HTTP/1.1 200 OK

{
  "success": true,
  "data": {
    "id": "board-id",
    "title": "Updated Board Title",
    "updatedAt": "2026-05-31T10:05:00Z"
  }
}
```

### Delete Resource (Soft Delete)

**Request:**
```http
DELETE /api/v1/boards/{boardId}
```

**Response:**
```http
HTTP/1.1 204 No Content
```

---

## List/Collection Patterns

### Get Collection

**Request:**
```http
GET /api/v1/workspaces/{workspaceId}/boards
```

**Response:**
```http
HTTP/1.1 200 OK

{
  "success": true,
  "data": [
    {
      "id": "board-1",
      "title": "Board 1"
    },
    {
      "id": "board-2",
      "title": "Board 2"
    }
  ]
}
```

### Pagination (Cursor-based)

**Request:**
```http
GET /api/v1/workspaces/{workspaceId}/boards?limit=20&cursor=abc123
```

**Response:**
```http
HTTP/1.1 200 OK

{
  "success": true,
  "data": {
    "items": [...],
    "nextCursor": "def456",
    "hasMore": true
  }
}
```

### Filtering

**Request:**
```http
GET /api/v1/boards/{boardId}/cards?listId=list-123&dueDate=2026-06-01
```

### Sorting

**Request:**
```http
GET /api/v1/boards?sort=createdAt&order=desc
```

---

## Nested Resource Patterns

### Get Nested Resource

```http
GET /api/v1/boards/{boardId}/lists
GET /api/v1/lists/{listId}/cards
GET /api/v1/pages/{pageId}/blocks
```

### Create Nested Resource

```http
POST /api/v1/boards/{boardId}/lists
POST /api/v1/lists/{listId}/cards
POST /api/v1/pages/{pageId}/blocks
```

---

## Action Patterns (Non-CRUD)

### Move Card

**Request:**
```http
POST /api/v1/cards/{cardId}/move
Content-Type: application/json

{
  "listId": "new-list-id",
  "position": 2.5
}
```

**Response:**
```http
HTTP/1.1 200 OK

{
  "success": true,
  "data": {
    "id": "card-id",
    "listId": "new-list-id",
    "position": 2.5
  }
}
```

### Link Page to Card

**Request:**
```http
POST /api/v1/cards/{cardId}/link-page
Content-Type: application/json

{
  "pageId": "page-id"
}
```

**Response:**
```http
HTTP/1.1 200 OK

{
  "success": true,
  "data": {
    "id": "card-id",
    "linkedPageId": "page-id"
  }
}
```

### Unlink Page from Card

**Request:**
```http
DELETE /api/v1/cards/{cardId}/link-page
```

**Response:**
```http
HTTP/1.1 204 No Content
```

### Batch Reorder

**Request:**
```http
POST /api/v1/blocks/reorder
Content-Type: application/json

{
  "updates": [
    { "id": "block-1", "position": 1.0 },
    { "id": "block-2", "position": 2.0 },
    { "id": "block-3", "position": 3.0 }
  ]
}
```

---

## Full Resource Patterns

### Get Full Board (with relations)

**Request:**
```http
GET /api/v1/boards/{boardId}/full
```

**Response:**
```http
HTTP/1.1 200 OK

{
  "success": true,
  "data": {
    "id": "board-id",
    "title": "My Board",
    "lists": [
      {
        "id": "list-1",
        "title": "To Do",
        "cards": [
          {
            "id": "card-1",
            "title": "Task 1"
          }
        ]
      }
    ],
    "members": [...],
    "labels": [...]
  }
}
```

---

## Search Pattern

**Request:**
```http
GET /api/v1/workspaces/{workspaceId}/search?q=project&type=board,page
```

**Response:**
```http
HTTP/1.1 200 OK

{
  "success": true,
  "data": {
    "boards": [...],
    "pages": [...],
    "cards": [...]
  }
}
```

---

## Error Patterns

### 400 Bad Request (Validation Error)

```json
{
  "type": "https://notrelix.com/errors/validation",
  "title": "Validation failed",
  "status": 400,
  "errors": {
    "title": ["Title is required"],
    "email": ["Invalid email format"]
  },
  "traceId": "00-abc123-def456-00"
}
```

### 401 Unauthorized

```json
{
  "type": "https://notrelix.com/errors/unauthorized",
  "title": "Unauthorized",
  "status": 401,
  "detail": "Access token is missing or invalid",
  "traceId": "00-abc123-def456-00"
}
```

### 403 Forbidden

```json
{
  "type": "https://notrelix.com/errors/forbidden",
  "title": "Forbidden",
  "status": 403,
  "detail": "You don't have permission to access this resource",
  "traceId": "00-abc123-def456-00"
}
```

### 404 Not Found

```json
{
  "type": "https://notrelix.com/errors/not-found",
  "title": "Resource not found",
  "status": 404,
  "detail": "Board with id 'xxx' not found",
  "traceId": "00-abc123-def456-00"
}
```

### 409 Conflict

```json
{
  "type": "https://notrelix.com/errors/conflict",
  "title": "Conflict",
  "status": 409,
  "detail": "A board with this slug already exists",
  "traceId": "00-abc123-def456-00"
}
```

### 500 Internal Server Error

```json
{
  "type": "https://notrelix.com/errors/internal",
  "title": "Internal server error",
  "status": 500,
  "detail": "An unexpected error occurred",
  "traceId": "00-abc123-def456-00"
}
```

---

## Authentication Patterns

### Login

**Request:**
```http
POST /api/v1/auth/login
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "password123"
}
```

**Response:**
```http
HTTP/1.1 200 OK
Set-Cookie: refresh_token=xxx; HttpOnly; Secure; SameSite=Strict

{
  "success": true,
  "data": {
    "accessToken": "eyJhbGciOiJIUzI1NiIs...",
    "user": {
      "id": "user-id",
      "email": "user@example.com",
      "name": "John Doe"
    }
  }
}
```

### Refresh Token

**Request:**
```http
POST /api/v1/auth/refresh
Cookie: refresh_token=xxx
```

**Response:**
```http
HTTP/1.1 200 OK

{
  "success": true,
  "data": {
    "accessToken": "eyJhbGciOiJIUzI1NiIs..."
  }
}
```

### Logout

**Request:**
```http
POST /api/v1/auth/logout
Authorization: Bearer {accessToken}
```

**Response:**
```http
HTTP/1.1 204 No Content
Set-Cookie: refresh_token=; Max-Age=0
```

---

## File Upload Pattern

### Upload Attachment

**Request:**
```http
POST /api/v1/attachments
Content-Type: multipart/form-data
Authorization: Bearer {accessToken}

------WebKitFormBoundary
Content-Disposition: form-data; name="file"; filename="document.pdf"
Content-Type: application/pdf

[binary data]
------WebKitFormBoundary
Content-Disposition: form-data; name="resourceType"

card
------WebKitFormBoundary
Content-Disposition: form-data; name="resourceId"

card-id
------WebKitFormBoundary--
```

**Response:**
```http
HTTP/1.1 201 Created

{
  "success": true,
  "data": {
    "id": "attachment-id",
    "fileName": "document.pdf",
    "fileSize": 1024000,
    "mimeType": "application/pdf",
    "url": "https://cdn.notrelix.com/attachments/xxx.pdf"
  }
}
```

---

## Webhook Pattern

### Webhook Payload

```json
{
  "event": "card.created",
  "timestamp": "2026-05-31T10:00:00Z",
  "workspaceId": "workspace-id",
  "data": {
    "id": "card-id",
    "title": "New Card",
    "listId": "list-id"
  }
}
```

### Webhook Signature

```http
POST https://your-webhook-url.com/webhook
Content-Type: application/json
X-Notrelix-Signature: sha256=abc123...

{
  "event": "card.created",
  ...
}
```

---

## Rate Limiting

**Headers:**
```http
X-RateLimit-Limit: 100
X-RateLimit-Remaining: 95
X-RateLimit-Reset: 1622548800
```

**429 Too Many Requests:**
```json
{
  "type": "https://notrelix.com/errors/rate-limit",
  "title": "Rate limit exceeded",
  "status": 429,
  "detail": "Too many requests. Please try again later.",
  "retryAfter": 60,
  "traceId": "00-abc123-def456-00"
}
```

---

## See Also

- [AGENTS.md](../AGENTS.md) — Section 3.5: API Conventions
- [domains.md](./domains.md) — Domain structure
- [conventions.md](./conventions.md) — Naming conventions
