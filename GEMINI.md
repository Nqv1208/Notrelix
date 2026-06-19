# Notrelix Project Rules (GEMINI.md)

> **Mục đích:** File này định nghĩa toàn bộ conventions, rules và hướng dẫn cho AI agents khi làm việc với dự án Notrelix. Dự án là một SaaS workspace kết hợp Notion-like docs và Trello-like boards với two-way calendar sync.

## 1. Tổng quan dự án & Stack
- **Backend:** .NET 8 / ASP.NET Core / Entity Framework Core / PostgreSQL / Redis 7 / S3
- **Frontend:** Next.js 16 App Router / TypeScript / shadcn/ui / TanStack Query
- **Architecture:** Clean Architecture + CQRS (Backend) / Feature-Sliced Design (Frontend)

## 2. Domain Architecture (7 Domains)
Mọi tính năng phải thuộc về 1 trong 7 domains, KHÔNG cross-domain trực tiếp (dùng domain events hoặc shared/polymorphic structures):
1. **Identity & Auth**: users, user_profiles, sessions, oauth_accounts
2. **Workspace**: workspaces, workspace_members, workspace_invitations
3. **Document (Notion)**: pages, blocks
4. **Board (Trello)**: boards, board_members, lists, labels, cards, checklists...
5. **Calendar Sync**: calendar_integrations, calendar_events
6. **Shared/Cross**: comments, page_mentions, attachments, reactions, permissions, notifications, activity_logs
7. **Extensibility**: webhooks, automations, audit_snapshots

## 3. Backend Rules (.NET 8)
- **Dependency Rule:** API → Application → Domain. Infrastructure → Application → Domain.
- **Entities:** Phải kế thừa `BaseEntity` hoặc `AuditableEntity`. Không override `CreatedBy/UpdatedBy`.
- **Soft Delete:** Sử dụng cờ `IsDeleted` (boolean) và `DeletedAt` (timestamp).
- **Fractional Indexing:** Cột `Position` luôn là `double` (`FLOAT8` trong DB).
- **CQRS:** Sử dụng MediatR. Command/Query + Handler + Validator (FluentValidation) nằm chung thư mục.
- **Background Jobs:** Tác vụ như Calendar sync, gửi email PHẢI chạy async qua Redis queue (không block request cycle).
- **DB Configuration:** Định nghĩa Entity configuration bằng `IEntityTypeConfiguration<T>`. Tên cột snake_case.

## 4. Frontend Rules (Next.js 16)
- **Kiến trúc thư mục:**
  - `app/`: CHỈ UI và routing.
  - `features/`: Logic thuần (hooks, schemas, types, utils) - KHÔNG UI.
  - `components/`: UI dùng chung (>= 2 nơi).
- **Quy tắc Import:** `app` → `features` → `lib`. Không làm ngược lại. `features` không import lẫn nhau.
- **Data Fetching (React Query):**
  - Query keys PHẢI lấy từ `lib/query/query-keys.ts`. KHÔNG hardcode mảng string.
  - Mutation nên đi kèm optimistic updates.
- **Next.js 16:** PHẢI `await params` trong dynamic routes.
- **Auth:** `access_token` lưu in-memory. `refresh_token` lưu trong httpOnly cookie.

## 5. Quy tắc chung
- **Permission Check:** Ưu tiên kiểm tra quyền trên application layer (không query chéo DB). Sử dụng Redis cache (`perm:{userId}:{resourceType}:{resourceId}`).
- **Activity Logging:** Mọi thao tác write quan trọng phải log vào `activity_logs`.
- **Git Commit:** Tuân thủ Conventional Commits (`feat`, `fix`, `refactor`, `chore`, `style`). Kèm scope domain nếu có (ví dụ: `feat(board): ...`). KHÔNG tự tiện auto commit khi chưa có yêu cầu hoặc xác nhận rõ ràng của người dùng. TUYỆT ĐỐI KHÔNG tự động thêm dòng `Co-Authored-By` vào commit message.
- **Không bao giờ làm:**
  - Lưu file binary (ảnh, pdf) vào DB. Dùng S3/R2 presigned URL.
  - Dùng `int` cho Position.
  - Gọi DB trực tiếp trong Middleware.
  - Thêm `Co-Authored-By` vào git commit.