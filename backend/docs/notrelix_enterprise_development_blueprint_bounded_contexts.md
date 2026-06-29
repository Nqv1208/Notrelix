# NOTRELIX — Tài liệu kế hoạch phát triển & xây dựng chương trình phần mềm Enterprise

**Phiên bản:** 1.0  
**Ngày:** 2026-06-24  
**Phạm vi:** Backend-first system blueprint, có liên kết frontend/API/infra ở mức kế hoạch triển khai  
**Vai trò biên soạn:** Senior Enterprise Backend / System Design / Project Management  
**Repository tham chiếu:** `https://github.com/Nqv1208/Notrelix`  

---

## 0. Mục tiêu của tài liệu

Tài liệu này không chỉ mô tả Notrelix là hệ thống gì, mà dùng như **blueprint triển khai phần mềm** cho toàn bộ dự án theo hướng enterprise.

Tài liệu trả lời các câu hỏi:

1. Hệ thống Notrelix là gì?
2. Ai sử dụng hệ thống?
3. Hệ thống cần có chức năng nào?
4. Mỗi bounded context chịu trách nhiệm gì?
5. Luồng nghiệp vụ chi tiết của từng bounded context là gì?
6. Khi triển khai một use case cần tạo những file nào?
7. Tên file, namespace, rule coding, rule domain, rule application, rule infrastructure được quy định ra sao?
8. Kế hoạch triển khai theo phase/sprint như thế nào để không refactor lặp lại?

Tài liệu này được viết để dùng cho:

- Lập kế hoạch phát triển phần mềm.
- Giao việc cho coding agent hoặc developer.
- Kiểm soát kiến trúc khi mở rộng tính năng.
- Viết issue/backlog/PR checklist.
- Làm tài liệu đồ án/portfolio theo chuẩn hệ thống lớn.

---

## 1. Tóm tắt hệ thống

### 1.1. Notrelix là hệ thống gì?

**Notrelix** là một nền tảng SaaS workspace dành cho đội nhóm hiện đại, kết hợp:

- Quản lý workspace nhiều tenant.
- Quản lý công việc dạng board, item, field, view, checklist, automation.
- Tài liệu dạng page/block, template, version.
- Cộng tác realtime: comment, mention, notification, activity log, presence.
- Phân quyền, audit, policy, share link.
- Tích hợp lịch và hệ thống ngoài.
- Billing/subscription/entitlement cho SaaS.
- Search, analytics, import/export, background jobs.

Định hướng sản phẩm: **workspace hợp nhất giữa documentation + project execution + collaboration + governance**.

### 1.2. Nhận định kiến trúc hiện tại

Repo hiện tại đã đi theo hướng tốt cho hệ thống lớn:

- Backend chia thành `Notrelix.Domain`, `Notrelix.Application`, `Notrelix.Infrastructure`, `Notrelix.API`.
- Domain chia theo bounded context: `Identity`, `Workspaces`, `WorkManagement`, `Documents`, `Collaboration`, `Governance`, `Integrations`, `Automation`, `Billing`, `Analytics`.
- Application có `Features/{BoundedContext}/{Module}/{Commands|Queries}`.
- Infrastructure có EF Core DbContext, projections, outbox, ops entities, search projection, governance projection.
- API dùng endpoint-based structure theo module.
- Tests đã có nhiều project: Domain, Application, Infrastructure, Integration, API, Architecture.

Tuy nhiên, để phát triển bền vững, cần đóng băng convention và triển khai từng bounded context theo thứ tự phụ thuộc, tránh phát triển dàn trải.

---

## 2. Product vision và scope

### 2.1. Product vision

Notrelix giúp đội nhóm **lên kế hoạch, quản lý công việc, ghi chép tài liệu, cộng tác và kiểm soát quyền truy cập trong một workspace duy nhất**.

Tầm nhìn dài hạn:

> Trở thành nền tảng workspace SaaS có kiến trúc enterprise, hỗ trợ team vận hành công việc, tài liệu, automation, integrations và governance ở quy mô tổ chức.

### 2.2. MVP scope

MVP nên tập trung vào 5 năng lực cốt lõi:

1. **Identity & Auth**  
   Đăng ký, đăng nhập, refresh token, session, profile, OAuth nền tảng.

2. **Workspace & Members**  
   Tạo workspace, mời thành viên, role cơ bản, workspace context.

3. **Work Management Core**  
   Board, group, field, item, status, assignment, due date, view cơ bản.

4. **Collaboration Core**  
   Comment, mention, notification, activity log.

5. **Governance Core**  
   Permission evaluation, RBAC, audit log, tenant isolation.

Các phần như Automation nâng cao, Billing đầy đủ, Analytics nâng cao, external marketplace nên nằm sau MVP.

### 2.3. Out of scope cho MVP

Không nên làm ngay:

- Full AI agent automation.
- Marketplace integrations phức tạp.
- Multi-region deployment.
- Enterprise SSO/SCIM hoàn chỉnh.
- Usage-based billing phức tạp.
- Analytics dashboard nâng cao.
- Custom formula engine quá đầy đủ như Monday/Excel.

---

## 3. Actors và stakeholders

### 3.1. Actors chính

| Actor | Mục tiêu | Quyền chính |
|---|---|---|
| Guest Visitor | Tìm hiểu sản phẩm, đăng ký | Xem landing, đăng ký |
| Registered User | Sử dụng hệ thống cá nhân hoặc tham gia workspace | Login, profile, tham gia workspace |
| Workspace Owner | Quản lý toàn bộ workspace | Billing, members, roles, delete/archive workspace |
| Workspace Admin | Quản trị workspace | Invite member, manage boards/pages/settings theo quyền |
| Workspace Member | Làm việc hằng ngày | Board, item, page, comment, notification |
| Workspace Guest | Truy cập giới hạn | Xem/sửa resource được chia sẻ |
| Billing Admin | Quản lý gói dịch vụ | Subscription, invoice, payment method |
| Integration Admin | Kết nối hệ thống ngoài | OAuth connection, calendar sync, webhook |
| Automation Owner | Tạo automation rule | Trigger/action/schedule automation |
| System Admin | Vận hành hệ thống | Ops jobs, health, audit, support |
| Background Worker | Xử lý async | Outbox, search indexing, notification, sync |

### 3.2. Stakeholders kỹ thuật

| Stakeholder | Quan tâm |
|---|---|
| Product Owner | Scope, roadmap, release plan |
| Backend Engineer | Domain, CQRS, DB, transaction, tests |
| Frontend Engineer | API contract, DTO, realtime events |
| DevOps | Docker, migration, CI/CD, environment |
| Security Reviewer | Auth, permission, tenant isolation, audit |
| QA | Use case, acceptance criteria, regression |
| Project Manager | Milestone, risk, estimation, delivery |

---

## 4. Kiến trúc tổng thể đề xuất

### 4.1. Architectural style

Notrelix nên tiếp tục theo hướng:

> **Modular Monolith + Clean Architecture + DDD Tactical Patterns + CQRS + Event-driven internal integration**

Không nên tách microservices sớm. Microservices chỉ nên xem xét khi:

- Một bounded context có team riêng.
- Có nhu cầu scale riêng biệt.
- Có boundary dữ liệu rõ.
- Có observability, DevOps, CI/CD đủ trưởng thành.

### 4.2. Layer ownership

| Layer | Trách nhiệm | Không được làm |
|---|---|---|
| Domain | Business rules, aggregates, value objects, domain events | Không inject service infra, không gọi DB/API |
| Application | Use case orchestration, CQRS, validation, permission, transaction boundary | Không chứa business invariant sâu thay aggregate |
| Infrastructure | EF Core, Redis, storage, email, outbox, external service, implementations | Không chứa business workflow chính |
| API | HTTP endpoints, request mapping, auth middleware, response mapping | Không query DbContext trực tiếp cho business flow |
| Tests | Bảo vệ rule, use case, integration, architecture | Không chỉ test happy path |

### 4.3. Dependency rule

```text
API  ---> Application ---> Domain
API  ---> Infrastructure ---> Application/Domain
Domain không phụ thuộc layer nào bên ngoài.
```

### 4.4. Cross-cutting concerns bắt buộc

Mọi use case enterprise phải đi qua các concern:

1. Validation
2. Authentication
3. Workspace context resolution
4. Authorization / permission
5. Entitlement/quota nếu có
6. Idempotency với command nguy hiểm hoặc external retry
7. Transaction
8. Domain events
9. Outbox/integration events nếu async
10. Cache invalidation
11. Realtime notification nếu UI cần
12. Audit/activity log
13. Observability/logging
14. Tests

---

## 5. Bounded context map

### 5.1. Danh sách bounded context chuẩn

| Bounded Context | Trạng thái trong repo | Vai trò |
|---|---|---|
| Identity | Có domain + feature + endpoint | Người dùng, auth, session, profile, OAuth, MFA |
| Workspaces | Có domain + feature + endpoint | Tenant, member, invitation, team, space |
| WorkManagement | Có domain + feature + endpoint | Board, item, field, view, checklist, relation, formula |
| Documents | Có domain + feature + endpoint | Page, block, template, version, resource link |
| Collaboration | Có domain + feature + endpoint | Comment, reaction, mention, notification, activity, presence |
| Governance | Có domain + feature + endpoint | RBAC, permission, policy, audit, share link, security event |
| Integrations | Có domain + feature | Calendar, connection, sync, webhook |
| Automation | Có domain + feature + endpoint | Rule, trigger, condition, action, execution, schedule, agent |
| Billing | Có domain + feature | Plan, subscription, payment, entitlement, usage, invoice |
| Analytics | Có domain + feature | Dashboard, widget, snapshot, reporting source |
| Search | Có application feature + infra projection | Search document, index job, query/search API |
| Operations | Có application feature + infra ops entities | Import/export jobs, idempotency, job locks, background ops |

### 5.2. Shared Kernel

`SharedKernel` không phải bounded context nghiệp vụ độc lập. Nó chỉ chứa primitive/value object dùng chung như:

- `JsonValue`
- `FractionalIndex`
- `SecretRef`
- `TokenHash`
- Common enums/value converters

Rule:

- Không đưa business rule của bounded context vào SharedKernel.
- Không đưa service, repository, use case vào SharedKernel.
- Chỉ đưa type thật sự stable và dùng rộng rãi.

---

## 6. Chiến lược triển khai tổng thể

### 6.1. Thứ tự phụ thuộc nên triển khai

```text
Phase 0: Stabilization & Architecture Rules
Phase 1: Identity + Workspace + Governance Core
Phase 2: WorkManagement Core
Phase 3: Documents Core
Phase 4: Collaboration Core
Phase 5: Search + Realtime + Notifications
Phase 6: Integrations + Automation
Phase 7: Billing + Entitlements
Phase 8: Analytics + Operations + Production Hardening
```

Lý do:

- WorkManagement/Documents cần Workspace + Identity + Permission trước.
- Collaboration cần resource model ổn định.
- Search cần các resource đã có lifecycle event.
- Automation cần domain events/use cases ổn định.
- Billing/Entitlement cần feature boundaries rõ.
- Analytics chỉ có ý nghĩa khi có dữ liệu vận hành.

### 6.2. Nguyên tắc không refactor mãi

1. **Chốt convention trước khi thêm feature.**
2. **Mọi use case mới phải theo vertical slice.**
3. **Không để handler tự do bypass pipeline.**
4. **Không tạo module “future-proof” nếu chưa có use case.**
5. **Mỗi bounded context có ownership rõ.**
6. **Mỗi aggregate có invariants và events rõ.**
7. **Mỗi command mutate phải có validator + permission + transaction + test.**
8. **Mỗi query list phải có paging/sorting/filtering chuẩn.**
9. **Mỗi cross-context side effect phải qua event/outbox hoặc application service có boundary rõ.**
10. **Mỗi PR phải pass architecture tests.**

---

# PHẦN A — PHÂN TÍCH CHI TIẾT TỪNG BOUNDED CONTEXT

---

## 7. Identity bounded context

### 7.1. Trách nhiệm

Identity quản lý danh tính người dùng và vòng đời xác thực.

Bao gồm:

- User account
- Email/password authentication
- Session/refresh token
- OAuth account
- Profile
- MFA
- Security settings
- Login attempts
- Verification/reset token
- API token
- SSO/SCIM sau MVP

### 7.2. Module hiện có trong domain

```text
Notrelix.Domain/Identity/
├── Mfa/
├── OAuth/
├── Profiles/
├── Security/
├── Sessions/
├── Tokens/
└── Users/
```

### 7.3. Aggregate candidates

| Aggregate | Vai trò | Ghi chú |
|---|---|---|
| User | Aggregate root chính | Email, normalized email, password hash, status, login |
| UserProfile | Có thể entity/aggregate riêng | Tùy mức độ lifecycle độc lập |
| Session | Aggregate/Entity | Refresh token, device, revoke |
| OAuthAccount | Entity thuộc User hoặc aggregate riêng | Nếu token lifecycle phức tạp thì tách |
| UserMfaMethod | Entity/Aggregate | Cần security lifecycle riêng |
| ApiToken | Aggregate | Token hash, scope, expiry, revoke |

### 7.4. Functional requirements

#### Identity Core

- Đăng ký tài khoản bằng email/password.
- Đăng nhập bằng email/password.
- Refresh access token.
- Logout một session.
- Logout tất cả session.
- Xem/cập nhật profile.
- Đổi email.
- Đổi mật khẩu.
- Quên mật khẩu.
- Xác thực email.
- Ghi nhận login attempt.

#### Identity Advanced

- OAuth Google/GitHub.
- MFA TOTP/email OTP.
- Quản lý security settings.
- API token cho integration.
- SSO provider, SCIM directory sync cho enterprise.

### 7.5. Business rules

1. Email phải được normalize và unique theo `NormalizedEmail`.
2. Password hash không bao giờ trả ra API.
3. User bị suspended không được login.
4. Refresh token phải được hash hoặc lưu dạng secure reference.
5. Logout phải revoke session/token.
6. Đổi email cần xác thực email mới nếu hệ thống bật email verification.
7. Login attempt thất bại nhiều lần phải rate limit/lockout tạm thời.
8. OAuth account không được link trùng provider với provider id khác.
9. MFA enabled thì login cần challenge step thứ hai.

### 7.6. Luồng nghiệp vụ chi tiết

#### UC-ID-01 — Register user

```text
Actor: Guest Visitor
Input: email, name, password

1. API nhận RegisterRequest.
2. Validator kiểm tra email format, password strength, name length.
3. Application kiểm tra NormalizedEmail chưa tồn tại.
4. PasswordHasher tạo password hash.
5. Domain User.Create(email, name, passwordHash, now).
6. Domain emit UserRegisteredDomainEvent.
7. Application lưu User.
8. Nếu email verification bật: tạo EmailVerificationToken.
9. Transaction commit.
10. Outbox/email worker gửi verification email.
11. API trả user id hoặc auth token tùy policy.
```

Files cần triển khai:

```text
Domain:
backend/src/Notrelix.Domain/Identity/Users/User.cs
backend/src/Notrelix.Domain/Identity/Users/Email.cs
backend/src/Notrelix.Domain/Identity/Users/UserStatus.cs
backend/src/Notrelix.Domain/Identity/Users/Events/UserRegisteredDomainEvent.cs
backend/src/Notrelix.Domain/Identity/Tokens/EmailVerificationToken.cs

Application:
backend/src/Notrelix.Application/Features/Identity/Auth/Commands/Register/RegisterCommand.cs
backend/src/Notrelix.Application/Features/Identity/Auth/Commands/Register/RegisterCommandHandler.cs
backend/src/Notrelix.Application/Features/Identity/Auth/Commands/Register/RegisterCommandValidator.cs
backend/src/Notrelix.Application/Features/Identity/Auth/Commands/Register/RegisterResult.cs

API:
backend/src/Notrelix.API/Endpoints/Identity/Auth/RegisterEndpoint.cs

Infrastructure:
backend/src/Notrelix.Infrastructure/Data/Configurations/Identity/UserConfiguration.cs
backend/src/Notrelix.Infrastructure/Data/Configurations/Identity/EmailVerificationTokenConfiguration.cs
backend/src/Notrelix.Infrastructure/Auth/PasswordHasher.cs
backend/src/Notrelix.Infrastructure/Email/VerificationEmailSender.cs

Tests:
backend/tests/Notrelix.Domain.Tests/Identity/UserTests.cs
backend/tests/Notrelix.Application.Tests/Features/Identity/Auth/RegisterCommandHandlerTests.cs
backend/tests/Notrelix.API.Tests/Identity/RegisterEndpointTests.cs
backend/tests/Notrelix.Architecture.Tests/IdentityArchitectureTests.cs
```

#### UC-ID-02 — Login

```text
Actor: Registered User
Input: email, password, device metadata

1. API nhận LoginRequest.
2. Validator kiểm tra email/password không rỗng.
3. Application normalize email.
4. Query User by NormalizedEmail.
5. Nếu không tồn tại: ghi login attempt generic, trả Unauthorized.
6. Nếu user inactive/suspended/deleted: trả Forbidden/Unauthorized theo policy.
7. Verify password hash.
8. Nếu password sai: ghi failed attempt, apply lockout nếu vượt limit.
9. Nếu MFA enabled: tạo challenge và trả MfaRequired.
10. Nếu thành công: User.RecordLogin(now).
11. Tạo Session/RefreshToken.
12. Tạo AccessToken.
13. Commit.
14. Trả token pair + user profile summary.
```

Files:

```text
Application/Features/Identity/Auth/Commands/Login/LoginCommand.cs
Application/Features/Identity/Auth/Commands/Login/LoginCommandHandler.cs
Application/Features/Identity/Auth/Commands/Login/LoginCommandValidator.cs
Application/Features/Identity/Auth/Commands/Login/LoginResult.cs
API/Endpoints/Identity/Auth/LoginEndpoint.cs
Infrastructure/Auth/JwtTokenService.cs
Infrastructure/Auth/RefreshTokenService.cs
Infrastructure/Security/LoginRateLimiter.cs
```

#### UC-ID-03 — Refresh token

```text
1. API nhận refresh token.
2. Hash token, tìm Session/RefreshToken.
3. Kiểm tra token chưa revoke/chưa expire.
4. Rotate refresh token nếu dùng rotation.
5. Cấp access token mới.
6. Ghi security event nếu token reuse/replay.
```

#### UC-ID-04 — Link OAuth account

```text
1. User chọn provider.
2. API redirect OAuth authorization URL.
3. Callback nhận code.
4. Infrastructure đổi code lấy token/profile.
5. Application resolve user hiện tại hoặc tạo user mới.
6. Domain User.LinkOAuthAccount(...).
7. Lưu token bằng SecretRef/secure storage.
8. Commit.
```

### 7.7. Commands/Queries chuẩn

```text
Commands:
- RegisterCommand
- LoginCommand
- LogoutCommand
- RefreshTokenCommand
- VerifyEmailCommand
- RequestPasswordResetCommand
- ResetPasswordCommand
- ChangePasswordCommand
- UpdateProfileCommand
- UpdateEmailCommand
- LinkOAuthAccountCommand
- UnlinkOAuthAccountCommand
- EnableMfaCommand
- DisableMfaCommand

Queries:
- GetCurrentUserQuery
- GetUserProfileQuery
- ListUserSessionsQuery
- ListOAuthAccountsQuery
- GetSecuritySettingsQuery
```

### 7.8. Domain events

```text
UserRegisteredDomainEvent
UserLoggedInDomainEvent
UserEmailChangedDomainEvent
UserPasswordChangedDomainEvent
UserProfileUpdatedDomainEvent
UserSuspendedDomainEvent
UserActivatedDomainEvent
UserDeactivatedDomainEvent
OAuthAccountLinkedDomainEvent
OAuthAccountUnlinkedDomainEvent
MfaEnabledDomainEvent
MfaDisabledDomainEvent
SessionRevokedDomainEvent
```

### 7.9. API endpoints đề xuất

```text
POST   /api/auth/register
POST   /api/auth/login
POST   /api/auth/refresh
POST   /api/auth/logout
POST   /api/auth/logout-all
GET    /api/users/me
PUT    /api/users/me/profile
PUT    /api/users/me/email
PUT    /api/users/me/password
GET    /api/users/me/sessions
DELETE /api/users/me/sessions/{sessionId}
GET    /api/auth/oauth/{provider}/start
GET    /api/auth/oauth/{provider}/callback
```

---

## 8. Workspaces bounded context

### 8.1. Trách nhiệm

Workspaces quản lý tenant boundary của toàn hệ thống.

Bao gồm:

- Workspace
- Workspace member
- Invitation
- Team
- Space
- Workspace settings
- Workspace lifecycle

Đây là context nền tảng vì gần như mọi resource khác đều `WorkspaceScoped`.

### 8.2. Module hiện có trong domain

```text
Notrelix.Domain/Workspaces/
├── Invitations/
├── Members/
├── Rules/
├── Spaces/
├── Teams/
└── Workspaces/
```

### 8.3. Aggregate candidates

| Aggregate | Vai trò |
|---|---|
| Workspace | Tenant root, settings, status |
| WorkspaceMember | Membership lifecycle |
| WorkspaceInvitation | Invite/accept/expire/revoke |
| Team | Group members for permission/assignment |
| Space | Folder/area inside workspace |

### 8.4. Functional requirements

- Tạo workspace.
- Cập nhật workspace name/slug/settings.
- Archive/restore/delete workspace.
- Invite member bằng email.
- Accept/decline invitation.
- Remove member.
- Change member role.
- Create/manage teams.
- Create/manage spaces.
- Resolve current workspace từ route/header/subdomain.

### 8.5. Business rules

1. Workspace slug unique.
2. Workspace owner không thể bị remove nếu chưa transfer ownership.
3. Invitation chỉ accept được nếu chưa expire/revoked/accepted.
4. Một user không được có duplicate active membership trong cùng workspace.
5. Workspace archived không được tạo board/page mới.
6. Workspace soft-deleted không được truy cập bởi member thông thường.
7. Member role thay đổi phải ghi audit.
8. Workspace context phải resolve trước authorization.

### 8.6. Luồng nghiệp vụ chi tiết

#### UC-WS-01 — Create workspace

```text
Actor: Registered User
Input: name, slug, description, isPersonal

1. API nhận CreateWorkspaceRequest.
2. Validator kiểm tra name/slug.
3. Application kiểm tra slug unique.
4. Domain Workspace.Create(ownerId, name, slug, now).
5. Domain WorkspaceMember.CreateOwner(workspaceId, ownerId).
6. Emit WorkspaceCreatedDomainEvent, WorkspaceMemberAddedDomainEvent.
7. Lưu workspace + member.
8. Commit.
9. Trả WorkspaceDto.
```

Files:

```text
Domain:
Notrelix.Domain/Workspaces/Workspaces/Workspace.cs
Notrelix.Domain/Workspaces/Workspaces/WorkspaceSettings.cs
Notrelix.Domain/Workspaces/Workspaces/WorkspaceStatus.cs
Notrelix.Domain/Workspaces/Workspaces/Events/WorkspaceCreatedDomainEvent.cs
Notrelix.Domain/Workspaces/Members/WorkspaceMember.cs

Application:
Notrelix.Application/Features/Workspaces/Workspaces/Commands/CreateWorkspace/CreateWorkspaceCommand.cs
Notrelix.Application/Features/Workspaces/Workspaces/Commands/CreateWorkspace/CreateWorkspaceCommandHandler.cs
Notrelix.Application/Features/Workspaces/Workspaces/Commands/CreateWorkspace/CreateWorkspaceCommandValidator.cs
Notrelix.Application/Features/Workspaces/Workspaces/DTOs/WorkspaceDto.cs

API:
Notrelix.API/Endpoints/Workspaces/Workspaces/CreateWorkspaceEndpoint.cs

Infrastructure:
Notrelix.Infrastructure/Data/Configurations/Workspaces/WorkspaceConfiguration.cs
Notrelix.Infrastructure/Data/Configurations/Workspaces/WorkspaceMemberConfiguration.cs
```

#### UC-WS-02 — Invite member

```text
Actor: Workspace Owner/Admin
Input: workspaceId, email, role

1. Resolve workspace context.
2. Validate email and role.
3. Authorization: ManageMembers.
4. Check workspace active.
5. Check email chưa là active member hoặc pending invitation.
6. Domain WorkspaceInvitation.Create(...).
7. Emit WorkspaceInvitationCreatedDomainEvent.
8. Commit.
9. Email worker gửi invitation.
10. Activity log ghi “invited member”.
```

Commands/Files:

```text
Application/Features/Workspaces/Invitations/Commands/InviteWorkspaceMember/InviteWorkspaceMemberCommand.cs
Application/Features/Workspaces/Invitations/Commands/InviteWorkspaceMember/InviteWorkspaceMemberCommandHandler.cs
Application/Features/Workspaces/Invitations/Commands/InviteWorkspaceMember/InviteWorkspaceMemberCommandValidator.cs
API/Endpoints/Workspaces/Invitations/InviteWorkspaceMemberEndpoint.cs
```

#### UC-WS-03 — Accept invitation

```text
1. User mở invitation link.
2. API nhận token/invitationId.
3. Application kiểm tra invitation tồn tại, chưa expire, chưa accepted/revoked.
4. Nếu user chưa login: yêu cầu login/register.
5. Domain invitation.Accept(userId, now).
6. Domain WorkspaceMember.Create(...).
7. Commit.
8. Realtime notify workspace admins.
```

#### UC-WS-04 — Change member role

```text
1. Owner/Admin gửi request đổi role.
2. Authorization ManageMembers.
3. Không cho hạ quyền owner cuối cùng.
4. Domain member.ChangeRole(newRole).
5. Ghi audit + activity.
```

### 8.7. Commands/Queries chuẩn

```text
Commands:
- CreateWorkspaceCommand
- UpdateWorkspaceSettingsCommand
- RenameWorkspaceCommand
- ArchiveWorkspaceCommand
- RestoreWorkspaceCommand
- InviteWorkspaceMemberCommand
- AcceptWorkspaceInvitationCommand
- RevokeWorkspaceInvitationCommand
- RemoveWorkspaceMemberCommand
- ChangeWorkspaceMemberRoleCommand
- CreateTeamCommand
- AddTeamMemberCommand
- RemoveTeamMemberCommand
- CreateSpaceCommand
- RenameSpaceCommand

Queries:
- ListMyWorkspacesQuery
- GetWorkspaceBySlugQuery
- GetWorkspaceMembersQuery
- ListWorkspaceInvitationsQuery
- ListTeamsQuery
- ListSpacesQuery
```

### 8.8. Domain events

```text
WorkspaceCreatedDomainEvent
WorkspaceRenamedDomainEvent
WorkspaceArchivedDomainEvent
WorkspaceSoftDeletedDomainEvent
WorkspaceRestoredDomainEvent
WorkspaceSettingsUpdatedDomainEvent
WorkspaceMemberAddedDomainEvent
WorkspaceMemberRemovedDomainEvent
WorkspaceMemberRoleChangedDomainEvent
WorkspaceInvitationCreatedDomainEvent
WorkspaceInvitationAcceptedDomainEvent
WorkspaceInvitationRevokedDomainEvent
TeamCreatedDomainEvent
TeamMemberAddedDomainEvent
SpaceCreatedDomainEvent
```

---

## 9. Governance bounded context

### 9.1. Trách nhiệm

Governance quản lý quyền truy cập, policy, audit, security event và chia sẻ resource.

Đây là context cốt lõi để Notrelix đạt chuẩn enterprise.

### 9.2. Module hiện có trong domain

```text
Notrelix.Domain/Governance/
├── Audit/
├── Permissions/
├── Policies/
├── Roles/
├── Security/
├── ShareLinks/
└── Templates/
```

### 9.3. Aggregate candidates

| Aggregate | Vai trò |
|---|---|
| CustomRole | Role tùy chỉnh theo workspace |
| ResourcePermission | Permission cụ thể theo resource |
| PermissionRule | Rule evaluation |
| WorkspacePolicy | Policy bảo mật/cộng tác |
| ShareLink | Public/guest sharing link |
| AuditLog | Immutable audit record, có thể infra-managed |
| SecurityEvent | Security event lifecycle |
| PermissionTemplate | Template quyền |

### 9.4. Functional requirements

- Permission evaluation cho mọi resource.
- Role mặc định: Owner, Admin, Member, Guest.
- Custom role cho workspace enterprise.
- Resource-level permission.
- Field-level permission cho board fields.
- Policy: sharing, guest access, export, retention.
- Audit log immutable.
- Share link tạo/revoke/expire.
- Security event logging.

### 9.5. Business rules

1. Permission evaluation phải deterministic.
2. Deny rule nên thắng allow rule nếu conflict.
3. Owner luôn có full control, trừ system policy đặc biệt.
4. Không được xóa role đang được gán nếu chưa migrate member.
5. Audit log immutable: không update/delete bằng business flow.
6. Share link phải có expiry hoặc scope rõ.
7. Field permission không được override quyền workspace nếu user không có quyền xem board.
8. Permission cache phải invalidated khi role/policy/resource permission thay đổi.

### 9.6. Luồng nghiệp vụ chi tiết

#### UC-GOV-01 — Evaluate permission

```text
Input: actorUserId, workspaceId, action, resourceRef

1. Load workspace membership.
2. Nếu không có membership và resource không public/share link: deny.
3. Load role assignments.
4. Load workspace policies.
5. Load resource permissions nếu resource-level access bật.
6. Load field permissions nếu action áp dụng field.
7. Apply evaluation order:
   a. System deny
   b. Workspace policy deny
   c. Owner allow
   d. Explicit resource deny
   e. Explicit resource allow
   f. Role permission allow
   g. Default deny
8. Trả PermissionDecision.
9. Cache decision ngắn hạn theo key actor/workspace/resource/action/version.
```

Application service:

```text
Application/Common/Security/IPermissionEvaluator.cs
Application/Common/Security/PermissionDecision.cs
Application/Common/Security/PermissionRequirement.cs
Infrastructure/Security/PermissionEvaluator.cs
```

#### UC-GOV-02 — Create custom role

```text
1. Workspace admin gửi name + permission set.
2. Authorization: ManageRoles.
3. Validate permission set hợp lệ.
4. Check role name unique trong workspace.
5. Domain CustomRole.Create(...).
6. Commit.
7. Invalidate permission cache.
8. Audit log.
```

#### UC-GOV-03 — Create share link

```text
1. User muốn share page/board/item.
2. Authorization: ShareResource.
3. Check workspace policy có cho share không.
4. Domain ShareLink.Create(resource, scope, expiry, createdBy).
5. Commit.
6. Return one-time display URL/token.
```

#### UC-GOV-04 — Write audit log

```text
1. Use case quan trọng hoàn tất thành công.
2. Audit behavior hoặc domain event handler tạo AuditLog.
3. AuditLog chứa actor, workspace, action, resource, before/after summary, ip/user-agent/correlation id.
4. Lưu append-only.
5. Không cho business update/delete.
```

### 9.7. Commands/Queries chuẩn

```text
Commands:
- CreateCustomRoleCommand
- UpdateCustomRoleCommand
- DeleteCustomRoleCommand
- AssignRoleToMemberCommand
- GrantResourcePermissionCommand
- RevokeResourcePermissionCommand
- UpdateWorkspacePolicyCommand
- CreateShareLinkCommand
- RevokeShareLinkCommand
- RecordSecurityEventCommand

Queries:
- EvaluatePermissionQuery              # internal/private if needed
- ListCustomRolesQuery
- GetRolePermissionsQuery
- ListResourcePermissionsQuery
- ListAuditLogsQuery
- ListSecurityEventsQuery
- ListShareLinksQuery
```

### 9.8. Files chuẩn

```text
Domain:
Notrelix.Domain/Governance/Roles/CustomRole.cs
Notrelix.Domain/Governance/Roles/CustomRolePermission.cs
Notrelix.Domain/Governance/Permissions/ResourcePermission.cs
Notrelix.Domain/Governance/Permissions/FieldPermission.cs
Notrelix.Domain/Governance/Policies/WorkspacePolicy.cs
Notrelix.Domain/Governance/Audit/AuditLog.cs
Notrelix.Domain/Governance/Security/Events/SecurityEvent.cs
Notrelix.Domain/Governance/ShareLinks/ShareLink.cs

Application:
Notrelix.Application/Features/Governance/Roles/Commands/CreateCustomRole/...
Notrelix.Application/Features/Governance/Permissions/Commands/GrantResourcePermission/...
Notrelix.Application/Features/Governance/Audit/Queries/ListAuditLogs/...

API:
Notrelix.API/Endpoints/Governance/Roles/CreateCustomRoleEndpoint.cs
Notrelix.API/Endpoints/Governance/Permissions/GrantResourcePermissionEndpoint.cs
Notrelix.API/Endpoints/Governance/Audit/ListAuditLogsEndpoint.cs

Infrastructure:
Notrelix.Infrastructure/Security/PermissionEvaluator.cs
Notrelix.Infrastructure/Data/Governance/Projections/PermissionCache.cs
Notrelix.Infrastructure/Data/Configurations/Governance/*.cs
```

---

## 10. WorkManagement bounded context

### 10.1. Trách nhiệm

WorkManagement là context lõi tạo giá trị sản phẩm: quản lý công việc kiểu board/item/field/view.

### 10.2. Module hiện có trong domain

```text
Notrelix.Domain/WorkManagement/
├── Approvals/
├── BoardGroups/
├── Boards/
├── Checklists/
├── Fields/
├── Forms/
├── Formulas/
├── Items/
├── Labels/
├── Relations/
├── Rollups/
├── Rules/
├── Templates/
├── Views/
└── Workload/
```

### 10.3. Aggregate candidates

| Aggregate | Vai trò |
|---|---|
| Board | Root quản lý board lifecycle, visibility, item sequence |
| BoardGroup | Nhóm item trong board |
| BoardField | Field schema của board |
| BoardItem | Task/card/item |
| Checklist | Checklist theo item |
| Label | Label/tag |
| BoardView | View config |
| Form | Form input tạo item |
| ApprovalRequest | Approval workflow |
| BoardRelation | Relation giữa board/item |
| FormulaDependency | Dependency graph cho field formula |
| RollupSnapshot | Snapshot rollup read model |
| WorkloadAllocation | Capacity/workload |
| BoardTemplate | Template board |

### 10.4. Functional requirements

#### Board management

- Create board trong workspace/space.
- Update board: title, description, background, visibility.
- Archive/unarchive board.
- Soft delete/restore board.
- Generate item identity.
- Default groups/fields khi tạo board.

#### Field management

- Add field.
- Update field name/type/settings.
- Reorder field bằng fractional index.
- Hide/show field per view.
- Field type: text, status, person, date, number, formula, relation, rollup.

#### Item management

- Create item.
- Update item title/values.
- Move item between groups.
- Assign member.
- Add label.
- Set due date/timeline.
- Complete/reopen item.
- Archive/delete item.

#### View management

- Create kanban/list/calendar/timeline view.
- Save filter/sort/grouping.
- Pin view.
- User view preferences.

#### Advanced

- Checklists.
- Dependencies.
- Relation/mirror field.
- Formula engine.
- Rollups.
- Forms.
- Approvals.
- Workload.
- Templates.

### 10.5. Business rules

1. Board phải thuộc một workspace hợp lệ.
2. Board archived không cho mutate item/field/view, trừ unarchive/restore.
3. Board title không rỗng.
4. Board visibility phải hợp lệ và permission-aware.
5. Item key sequence do Board quản lý để đảm bảo unique trong board.
6. Field system không được xóa nếu là core field bắt buộc.
7. Field type sau khi có data không được đổi tùy tiện nếu gây mất dữ liệu.
8. BoardItem value phải tương thích với FieldType.
9. Move item phải preserve ordering/fractional index.
10. Relation/rollup/formula không được tạo cycle không kiểm soát.
11. Mọi mutation phải emit domain event và invalidate search/read model.
12. Mọi command mutate phải có workspace permission.

### 10.6. Luồng nghiệp vụ chi tiết

#### UC-WM-01 — Create board in workspace

```text
Actor: Workspace Member có quyền CreateBoard
Input: workspaceId, title, description, background, visibility

1. API nhận request.
2. Validator kiểm tra title, visibility enum, background JSON hoặc token hợp lệ.
3. WorkspaceContextBehavior set workspaceId.
4. AuthorizationBehavior kiểm tra PermissionAction.CreateBoard trên workspace.
5. EntitlementBehavior kiểm tra quota số board nếu gói giới hạn.
6. Handler kiểm tra workspace active.
7. Domain Board.Create(...).
8. Domain tạo board với audit create và BoardCreatedDomainEvent.
9. Handler tạo default fields: Title, Status, Assignee, Due Date.
10. Handler tạo default group nếu rule yêu cầu.
11. SaveChanges trong TransactionBehavior.
12. CacheInvalidationBehavior invalidate board list/workspace dashboard.
13. RealtimeBehavior publish workspace board-created event sau handler thành công.
14. Outbox/search handler tạo SearchIndexJob.
15. API trả board id.
```

Files bắt buộc:

```text
Domain:
backend/src/Notrelix.Domain/WorkManagement/Boards/Board.cs
backend/src/Notrelix.Domain/WorkManagement/Boards/BoardVisibility.cs
backend/src/Notrelix.Domain/WorkManagement/Boards/BoardType.cs
backend/src/Notrelix.Domain/WorkManagement/Boards/BoardFamily.cs
backend/src/Notrelix.Domain/WorkManagement/Boards/Events/BoardCreatedDomainEvent.cs
backend/src/Notrelix.Domain/WorkManagement/Fields/BoardField.cs
backend/src/Notrelix.Domain/WorkManagement/Fields/FieldType.cs
backend/src/Notrelix.Domain/WorkManagement/Fields/FieldSettings.cs

Application:
backend/src/Notrelix.Application/Features/WorkManagement/Boards/Commands/CreateBoardInWorkspace/CreateBoardInWorkspaceCommand.cs
backend/src/Notrelix.Application/Features/WorkManagement/Boards/Commands/CreateBoardInWorkspace/CreateBoardInWorkspaceCommandHandler.cs
backend/src/Notrelix.Application/Features/WorkManagement/Boards/Commands/CreateBoardInWorkspace/CreateBoardInWorkspaceCommandValidator.cs
backend/src/Notrelix.Application/Features/WorkManagement/Boards/Commands/CreateBoardInWorkspace/CreateBoardInWorkspaceResult.cs
backend/src/Notrelix.Application/Features/WorkManagement/Boards/DTOs/BoardDto.cs

API:
backend/src/Notrelix.API/Endpoints/WorkManagement/Boards/CreateBoardInWorkspaceEndpoint.cs

Infrastructure:
backend/src/Notrelix.Infrastructure/Data/Configurations/WorkManagement/BoardConfiguration.cs
backend/src/Notrelix.Infrastructure/Data/Configurations/WorkManagement/BoardFieldConfiguration.cs

Tests:
backend/tests/Notrelix.Domain.Tests/WorkManagement/BoardTests.cs
backend/tests/Notrelix.Application.Tests/Features/WorkManagement/Boards/CreateBoardInWorkspaceCommandHandlerTests.cs
backend/tests/Notrelix.Integration.Tests/WorkManagement/CreateBoardFlowTests.cs
```

#### UC-WM-02 — Update board

```text
Actor: Member có quyền ManageBoard
Input: boardId, title?, description?, background?, visibility?, expectedVersion?

1. Validator kiểm tra ít nhất một field được gửi.
2. Validator kiểm tra visibility enum bằng TryParse, không parse trong handler.
3. Handler load board theo boardId.
4. Authorization theo ResourceRef(Board, boardId) hoặc sau khi load board thì board.WorkspaceId.
5. Nếu expectedVersion có, kiểm tra optimistic concurrency.
6. Domain gọi Rename/UpdateDescription/UpdateBackground/ChangeVisibility.
7. Domain increment version và emit events tương ứng.
8. Commit.
9. Invalidate cache board detail/list.
10. Publish realtime board-updated.
11. Search index update nếu title/description thay đổi.
```

Rule quan trọng:

- Không dùng `Enum.Parse` trực tiếp trong handler.
- Command phải implement `IRequirePermission`, không tự gọi permission service trong handler trừ case đặc biệt có ghi chú.
- Nếu update nhiều field, audit nên gom summary hoặc domain events riêng từng thay đổi.

#### UC-WM-03 — Create item

```text
1. Request gồm boardId, groupId?, title, fieldValues.
2. Validate title và field value schema.
3. Authorization CreateItem trên board.
4. Load board active.
5. Load group hoặc default group.
6. Board.GenerateNextItemIdentity(actor, now).
7. BoardItem.Create(workspaceId, boardId, groupId, sequence/key, title, actor, now).
8. Validate từng field value phù hợp FieldType.
9. Persist BoardItem + BoardItemValues.
10. Commit.
11. Emit BoardItemCreatedDomainEvent.
12. Create activity log, notification for watchers/assignees.
13. Create search index job.
```

Files:

```text
Application/Features/WorkManagement/Items/Commands/CreateBoardItem/CreateBoardItemCommand.cs
Application/Features/WorkManagement/Items/Commands/CreateBoardItem/CreateBoardItemCommandHandler.cs
Application/Features/WorkManagement/Items/Commands/CreateBoardItem/CreateBoardItemCommandValidator.cs
Domain/WorkManagement/Items/BoardItem.cs
Domain/WorkManagement/Items/BoardItemValue.cs
API/Endpoints/WorkManagement/Items/CreateBoardItemEndpoint.cs
```

#### UC-WM-04 — Update item field value

```text
1. Request itemId, fieldId, value, expectedVersion.
2. Validate field exists in same board.
3. Validate value type theo FieldType.
4. Permission UpdateItem/UpdateFieldValue.
5. Domain BoardItem.SetFieldValue(field, value).
6. Emit BoardItemValueChangedDomainEvent.
7. If field is formula dependency source: schedule formula recalculation.
8. If field affects rollup/relation: schedule rollup refresh.
9. Commit.
10. Realtime item updated.
```

#### UC-WM-05 — Move item

```text
1. Request itemId, targetGroupId, targetIndexBefore/After.
2. Validate target group belongs same board.
3. Permission MoveItem.
4. Compute FractionalIndex.
5. Domain BoardItem.MoveToGroup(...).
6. Commit.
7. Realtime board lane update.
```

#### UC-WM-06 — Create field

```text
1. Request boardId, name, type, settings.
2. Validate field type allowed by plan/entitlement.
3. Permission ManageFields.
4. Validate settings schema for field type.
5. Compute field order.
6. Domain BoardField.Create(...).
7. Commit.
8. Realtime schema changed.
9. Existing items may get null/default value lazily.
```

#### UC-WM-07 — Formula recalculation

```text
1. Field value changed or formula field updated.
2. Create FormulaRecalculationJob or SearchIndexJob-like background job.
3. Worker loads dependency graph.
4. Detect cycle.
5. Recalculate impacted items.
6. Persist calculated values/snapshots.
7. Publish realtime update.
```

### 10.7. Commands/Queries chuẩn

```text
Commands:
- CreateBoardInWorkspaceCommand
- UpdateBoardCommand
- ArchiveBoardCommand
- UnarchiveBoardCommand
- DeleteBoardCommand
- RestoreBoardCommand
- CreateBoardGroupCommand
- RenameBoardGroupCommand
- ReorderBoardGroupCommand
- CreateBoardFieldCommand
- UpdateBoardFieldCommand
- DeleteBoardFieldCommand
- ReorderBoardFieldCommand
- CreateBoardItemCommand
- UpdateBoardItemCommand
- MoveBoardItemCommand
- ArchiveBoardItemCommand
- DeleteBoardItemCommand
- AssignBoardItemMemberCommand
- AddBoardItemLabelCommand
- CreateChecklistCommand
- AddChecklistItemCommand
- CompleteChecklistItemCommand
- CreateBoardViewCommand
- UpdateBoardViewCommand
- PinBoardViewCommand
- CreateFormCommand
- SubmitFormCommand
- CreateApprovalRequestCommand

Queries:
- GetBoardQuery
- ListWorkspaceBoardsQuery
- GetBoardSchemaQuery
- ListBoardItemsQuery
- GetBoardItemQuery
- ListBoardViewsQuery
- GetBoardActivityQuery
- GetWorkloadQuery
```

### 10.8. API endpoints đề xuất

```text
POST   /api/workspaces/{workspaceId}/boards
GET    /api/workspaces/{workspaceId}/boards
GET    /api/boards/{boardId}
PATCH  /api/boards/{boardId}
POST   /api/boards/{boardId}/archive
POST   /api/boards/{boardId}/unarchive
DELETE /api/boards/{boardId}

POST   /api/boards/{boardId}/groups
PATCH  /api/board-groups/{groupId}
POST   /api/boards/{boardId}/fields
PATCH  /api/board-fields/{fieldId}
DELETE /api/board-fields/{fieldId}

POST   /api/boards/{boardId}/items
GET    /api/boards/{boardId}/items
GET    /api/items/{itemId}
PATCH  /api/items/{itemId}
POST   /api/items/{itemId}/move
DELETE /api/items/{itemId}

POST   /api/items/{itemId}/comments
POST   /api/items/{itemId}/labels/{labelId}
POST   /api/items/{itemId}/members/{userId}
```

---

## 11. Documents bounded context

### 11.1. Trách nhiệm

Documents quản lý tài liệu kiểu page/block, version history, template và liên kết resource.

### 11.2. Module hiện có trong domain

```text
Notrelix.Domain/Documents/
├── Blocks/
├── Pages/
├── ResourceLinks/
├── Rules/
├── Templates/
└── Versions/
```

### 11.3. Aggregate candidates

| Aggregate | Vai trò |
|---|---|
| Page | Root cho document/page lifecycle |
| Block | Entity/block tree, có thể aggregate nếu CRDT/collab phức tạp |
| DocumentVersion | Version snapshot |
| PageTemplate | Template tạo page |
| ResourceLink | Link giữa page và board/item/resource |

### 11.4. Functional requirements

- Create page trong workspace/space/parent page.
- Rename/update page icon/cover.
- Move page.
- Archive/delete/restore page.
- Create/update/delete block.
- Reorder block.
- Version snapshot.
- Restore version.
- Create/use template.
- Link page với board item/card.

### 11.5. Business rules

1. Page phải thuộc workspace.
2. Page parent nếu có phải cùng workspace.
3. Không tạo cyclic parent page.
4. Block phải thuộc page.
5. Block order dùng fractional index hoặc tree position.
6. Restore version phải tạo version mới hoặc audit rõ.
7. Page visibility phải theo governance/share link.
8. Mention trong page tạo collaboration event.
9. Nội dung page thay đổi phải update search document.

### 11.6. Luồng nghiệp vụ chi tiết

#### UC-DOC-01 — Create page

```text
1. Request workspaceId, title, parentPageId?, spaceId?, templateId?.
2. Validate title.
3. Permission CreatePage trên workspace/space/parent.
4. Nếu parentPageId có: load parent, kiểm tra cùng workspace và không deleted.
5. Nếu templateId có: load template và materialize blocks.
6. Domain Page.Create(...).
7. Tạo initial block nếu cần.
8. Commit.
9. Emit PageCreatedDomainEvent.
10. Search index job tạo document.
11. Realtime notify workspace/page tree.
```

Files:

```text
Domain/Documents/Pages/Page.cs
Domain/Documents/Pages/Events/PageCreatedDomainEvent.cs
Domain/Documents/Blocks/Block.cs
Application/Features/Documents/Pages/Commands/CreatePage/CreatePageCommand.cs
Application/Features/Documents/Pages/Commands/CreatePage/CreatePageCommandHandler.cs
Application/Features/Documents/Pages/Commands/CreatePage/CreatePageCommandValidator.cs
API/Endpoints/Documents/Pages/CreatePageEndpoint.cs
Infrastructure/Data/Configurations/Documents/PageConfiguration.cs
```

#### UC-DOC-02 — Update block

```text
1. Request blockId, content, metadata, expectedVersion.
2. Load block + page.
3. Permission EditPage.
4. Validate block content theo block type.
5. Domain Block.UpdateContent(...).
6. Create DocumentVersion snapshot policy:
   - immediate for major change, or
   - debounced background snapshot.
7. Commit.
8. Search index update.
9. Realtime block updated.
```

#### UC-DOC-03 — Move page

```text
1. Request pageId, newParentId?, newSpaceId?, position.
2. Validate target cùng workspace.
3. Detect cycle.
4. Permission MovePage.
5. Domain Page.Move(...).
6. Commit.
7. Realtime page tree changed.
```

#### UC-DOC-04 — Link page to board item

```text
1. Request pageId, targetResourceType, targetResourceId.
2. Permission EditPage + View target resource.
3. Validate target exists and same workspace.
4. Domain ResourceLink.Create(...).
5. Commit.
6. Activity log both resources.
```

### 11.7. Commands/Queries chuẩn

```text
Commands:
- CreatePageCommand
- RenamePageCommand
- UpdatePageMetadataCommand
- MovePageCommand
- ArchivePageCommand
- RestorePageCommand
- DeletePageCommand
- CreateBlockCommand
- UpdateBlockCommand
- DeleteBlockCommand
- ReorderBlockCommand
- CreatePageTemplateCommand
- ApplyPageTemplateCommand
- CreateDocumentVersionCommand
- RestoreDocumentVersionCommand
- LinkResourceToPageCommand
- UnlinkResourceFromPageCommand

Queries:
- GetPageQuery
- ListWorkspacePagesQuery
- GetPageTreeQuery
- ListPageBlocksQuery
- ListDocumentVersionsQuery
- ListPageTemplatesQuery
- ListResourceLinksQuery
```

---

## 12. Collaboration bounded context

### 12.1. Trách nhiệm

Collaboration quản lý tương tác giữa người dùng trên resource.

### 12.2. Module hiện có trong domain

```text
Notrelix.Domain/Collaboration/
├── Activity/
├── Attachments/
├── Comments/
├── Mentions/
├── Notifications/
├── Presence/
├── Reactions/
├── Rules/
└── Watchers/
```

### 12.3. Aggregate candidates

| Aggregate | Vai trò |
|---|---|
| Comment | Thread/reply/comment lifecycle |
| Reaction | Reaction to comment/resource |
| Attachment | File metadata attached to resource |
| Notification | User notification lifecycle |
| NotificationPreference | Preferences |
| ActivityLog | Activity feed entry |
| ResourceWatcher | Watch/subscription |
| PresenceSession | Online/collab presence |
| Mention | Mention event/entity |

### 12.4. Functional requirements

- Comment on page/board/item.
- Reply thread.
- Edit/delete comment.
- Add/remove reaction.
- Mention users/teams.
- Attach files.
- Watch/unwatch resource.
- Notification generation/read/archive.
- Activity log per resource/workspace.
- Presence session for realtime.

### 12.5. Business rules

1. User phải có quyền view resource mới được comment/read activity.
2. User phải có quyền comment mới được comment.
3. Mention chỉ notify user có quyền xem resource.
4. Notification phải idempotent để tránh spam.
5. Attachment phải pass file policy/security scan nếu bật.
6. ActivityLog nên append-only.
7. Presence có TTL, không nên lưu như transaction-critical business data.

### 12.6. Luồng nghiệp vụ chi tiết

#### UC-COL-01 — Add comment

```text
1. Request resourceType, resourceId, content, parentCommentId?.
2. Validate content length/safety.
3. Permission CommentResource.
4. Validate resource exists qua ResourceResolver.
5. Domain Comment.Create(...).
6. Extract mentions từ content.
7. Create Mention entities.
8. Add ResourceWatcher nếu auto-watch enabled.
9. Commit.
10. Domain event CommentCreatedDomainEvent.
11. Notification handler notify mentioned users/watchers.
12. Activity handler append activity log.
13. Realtime publish to resource topic.
```

Files:

```text
Domain/Collaboration/Comments/Comment.cs
Domain/Collaboration/Mentions/Mention.cs
Domain/Collaboration/Notifications/Notification.cs
Application/Features/Collaboration/Comments/Commands/AddComment/AddCommentCommand.cs
Application/Features/Collaboration/Comments/Commands/AddComment/AddCommentCommandHandler.cs
Application/Features/Collaboration/Comments/Commands/AddComment/AddCommentCommandValidator.cs
API/Endpoints/Collaboration/Comments/AddCommentEndpoint.cs
```

#### UC-COL-02 — Generate notification from domain event

```text
1. Domain event: ItemAssigned, CommentMentioned, PageShared, InvitationCreated.
2. Notification handler checks user preferences.
3. Check recipient can view target resource.
4. Deduplicate by eventId + recipient + type.
5. Create Notification.
6. Create NotificationDelivery if email/push needed.
7. Realtime send if user online.
```

#### UC-COL-03 — Mark notifications as read

```text
1. User sends notification ids or mark all.
2. Validate ownership: notification.UserId == current user.
3. Domain Notification.MarkAsRead(now).
4. Commit.
5. Update unread counter projection.
```

#### UC-COL-04 — Upload attachment

```text
1. Request upload intent for resource.
2. Permission AttachFile.
3. Infrastructure creates presigned upload URL.
4. Client uploads file to object storage.
5. Confirm upload command creates Attachment metadata.
6. Optional virus scan/background check.
7. Activity + notification if needed.
```

### 12.7. Commands/Queries chuẩn

```text
Commands:
- AddCommentCommand
- EditCommentCommand
- DeleteCommentCommand
- AddReactionCommand
- RemoveReactionCommand
- CreateAttachmentUploadIntentCommand
- ConfirmAttachmentUploadCommand
- DeleteAttachmentCommand
- WatchResourceCommand
- UnwatchResourceCommand
- MarkNotificationAsReadCommand
- MarkAllNotificationsAsReadCommand
- UpdateNotificationPreferencesCommand
- StartPresenceSessionCommand
- HeartbeatPresenceCommand

Queries:
- ListResourceCommentsQuery
- ListNotificationsQuery
- GetUnreadNotificationCountQuery
- ListActivityLogsQuery
- ListResourceWatchersQuery
- GetPresenceQuery
```

---

## 13. Integrations bounded context

### 13.1. Trách nhiệm

Integrations quản lý kết nối hệ thống ngoài, calendar sync, webhooks, secret versions và sync cursor.

### 13.2. Module hiện có trong domain

```text
Notrelix.Domain/Integrations/
├── Calendar/
├── Connections/
├── Rules/
├── Sync/
└── Webhooks/
```

### 13.3. Aggregate candidates

| Aggregate | Vai trò |
|---|---|
| IntegrationConnection | Kết nối provider/account |
| IntegrationSecretVersion | Secret/token version |
| CalendarIntegration | Calendar provider connection |
| CalendarEvent | External/internal event mapping |
| IntegrationSyncCursor | Cursor state |
| WebhookSubscription | Outbound webhook config |
| WebhookDelivery | Delivery attempt |
| InboundWebhookEvent | Received webhook event |

### 13.4. Functional requirements

- Connect Google Calendar.
- Disconnect integration.
- Manage scopes.
- Sync calendar events two-way.
- Store sync cursor.
- Receive inbound webhook.
- Send outbound webhook.
- Retry failed webhook deliveries.

### 13.5. Business rules

1. Secret/token không lưu plain text trong domain.
2. Token reference dùng `SecretRef`, secret material ở secure storage.
3. Sync phải idempotent theo external event id/version.
4. Webhook delivery phải retry với backoff.
5. Inbound webhook phải verify signature.
6. Integration phải thuộc workspace hoặc user rõ ràng.
7. Disconnect không được xóa audit/sync history quan trọng.

### 13.6. Luồng nghiệp vụ chi tiết

#### UC-INT-01 — Connect calendar provider

```text
1. User chọn Connect Google Calendar.
2. Authorization ManageIntegrations.
3. API tạo OAuth authorization URL với state.
4. Callback nhận code/state.
5. Verify state.
6. Exchange code for tokens.
7. Store tokens securely, get SecretRef.
8. Domain CalendarIntegration.Create(...).
9. Domain IntegrationConnection.Create(...).
10. Commit.
11. Schedule initial sync job.
```

#### UC-INT-02 — Calendar sync external -> Notrelix

```text
1. Background job chạy theo schedule/webhook.
2. Load CalendarIntegration + sync cursor.
3. Fetch external events changed since cursor.
4. For each event:
   a. Map external event to CalendarEvent.
   b. Check idempotency by provider event id + etag/version.
   c. Upsert CalendarEvent.
   d. Link with board item due date if configured.
5. Update sync cursor.
6. Commit.
7. Emit CalendarEventSyncedDomainEvent.
```

#### UC-INT-03 — Notrelix item due date -> calendar

```text
1. BoardItem due date changed.
2. Integration event handler checks linked calendar config.
3. Create/Update external calendar event.
4. Store external id/link.
5. Retry on transient failure.
6. Record sync status.
```

#### UC-INT-04 — Webhook outbound delivery

```text
1. Domain/IntegrationEvent xảy ra: board.created, item.updated, page.published.
2. Find active webhook subscriptions matching event type.
3. Create WebhookDelivery records.
4. Worker signs payload.
5. Send HTTP request.
6. If success: mark delivered.
7. If fail: retry with backoff, max attempts, dead-letter.
```

### 13.7. Commands/Queries chuẩn

```text
Commands:
- StartIntegrationConnectionCommand
- CompleteIntegrationConnectionCommand
- DisconnectIntegrationCommand
- RefreshIntegrationTokenCommand
- TriggerCalendarSyncCommand
- CreateWebhookSubscriptionCommand
- UpdateWebhookSubscriptionCommand
- DeleteWebhookSubscriptionCommand
- ProcessInboundWebhookCommand
- RetryWebhookDeliveryCommand

Queries:
- ListIntegrationConnectionsQuery
- GetIntegrationConnectionQuery
- ListCalendarEventsQuery
- ListWebhookSubscriptionsQuery
- ListWebhookDeliveriesQuery
```

---

## 14. Automation bounded context

### 14.1. Trách nhiệm

Automation cho phép người dùng tạo rule tự động dựa trên trigger/condition/action.

### 14.2. Module hiện có trong domain

```text
Notrelix.Domain/Automation/
├── Actions/
├── Agents/
├── Conditions/
├── Executions/
├── Rules/
├── RulesEngine/
├── Scheduled/
├── Templates/
└── Triggers/
```

### 14.3. Aggregate candidates

| Aggregate | Vai trò |
|---|---|
| AutomationRule | Rule root |
| AutomationExecution | Execution history |
| ScheduledJob | Schedule trigger state |
| AutomationTemplate | Template rule |
| AiAgent | Agent config |
| AiAgentRun | Agent run history |

### 14.4. Functional requirements

- Create automation rule.
- Enable/disable rule.
- Define trigger, condition, action.
- Execute rule on domain event.
- Execute scheduled rule.
- Record execution result.
- Retry failed action nếu idempotent.
- Provide templates.

### 14.5. Business rules

1. Automation rule phải thuộc workspace.
2. Rule disabled không được execute.
3. Trigger/action type phải nằm trong allowlist.
4. Condition evaluation phải deterministic.
5. Action phải permission-aware: rule chạy dưới actor/system context rõ.
6. Execution phải idempotent theo trigger event id + rule id.
7. Không cho automation tạo vòng lặp vô hạn.
8. Usage/quota automation phải qua Billing/Entitlement.

### 14.6. Luồng nghiệp vụ chi tiết

#### UC-AUTO-01 — Create automation rule

```text
1. User chọn trigger/action từ UI.
2. Validator kiểm tra schema trigger/condition/action.
3. Permission ManageAutomation.
4. Entitlement check automation enabled + quota.
5. Domain AutomationRule.Create(...).
6. Commit.
7. Audit log.
```

#### UC-AUTO-02 — Execute rule from domain event

```text
1. Domain event được mapped thành automation trigger candidate.
2. Automation dispatcher tìm active rules matching event type/resource/workspace.
3. For each rule:
   a. Check idempotency by ruleId + eventId.
   b. Evaluate conditions.
   c. If pass, create AutomationExecution.
   d. Execute actions qua application command hoặc integration service.
   e. Mark execution success/failure.
4. Prevent loop bằng causation/correlation id và max depth.
```

#### UC-AUTO-03 — Scheduled automation

```text
1. ScheduledJob worker tick.
2. Find due jobs with lock.
3. Create AutomationExecution.
4. Execute actions.
5. Compute next run time.
6. Release lock.
```

### 14.7. Commands/Queries chuẩn

```text
Commands:
- CreateAutomationRuleCommand
- UpdateAutomationRuleCommand
- EnableAutomationRuleCommand
- DisableAutomationRuleCommand
- DeleteAutomationRuleCommand
- ExecuteAutomationRuleCommand
- RetryAutomationExecutionCommand
- CreateAutomationTemplateCommand

Queries:
- ListAutomationRulesQuery
- GetAutomationRuleQuery
- ListAutomationExecutionsQuery
- ListAutomationTemplatesQuery
```

---

## 15. Billing bounded context

### 15.1. Trách nhiệm

Billing quản lý gói dịch vụ, subscription, payment, invoice, entitlement và usage.

### 15.2. Module hiện có trong domain

```text
Notrelix.Domain/Billing/
├── BillingEvents/
├── Entitlements/
├── Payments/
├── Plans/
├── Rules/
├── Subscriptions/
└── Usage/
```

### 15.3. Aggregate candidates

| Aggregate | Vai trò |
|---|---|
| Plan | Product plan |
| PlanLimit | Feature/limit config |
| Subscription | Workspace subscription lifecycle |
| PaymentMethod | Payment method reference |
| Invoice | Billing invoice |
| BillingEvent | Provider billing event |
| Entitlement | Effective feature access |
| UsageMetric | Usage definition |
| WorkspaceFeatureUsage | Current usage |
| FeatureUsageLedger | Immutable usage ledger |

### 15.4. Functional requirements

- Create/manage plans.
- Start trial/subscription.
- Change plan.
- Cancel subscription.
- Payment method management.
- Invoice listing.
- Entitlement check.
- Usage tracking.
- Billing provider webhook processing.

### 15.5. Business rules

1. Subscription thuộc workspace.
2. Workspace chỉ có một active subscription chính.
3. Entitlement là effective access, không nhất thiết là billing source duy nhất.
4. Usage ledger append-only.
5. Plan limit thay đổi không phá dữ liệu hiện có; chỉ chặn tạo mới nếu vượt quota.
6. Billing webhook phải idempotent theo provider event id.
7. Payment secrets không lưu plain text.
8. Cancel có thể immediate hoặc end-of-period, phải rõ policy.

### 15.6. Luồng nghiệp vụ chi tiết

#### UC-BILL-01 — Check entitlement before creating board

```text
1. CreateBoardCommand implement IRequireEntitlement.
2. EntitlementBehavior nhận FeatureCode.Boards.
3. Load effective entitlement for workspace.
4. Load current usage WorkspaceFeatureUsage.
5. If feature disabled: return Forbidden/PaymentRequired.
6. If usage >= limit: return QuotaExceeded.
7. Handler tiếp tục tạo board.
8. Sau commit, usage ledger ghi +1 và update current usage.
```

#### UC-BILL-02 — Start subscription

```text
1. Owner chọn plan.
2. Permission ManageBilling.
3. Create checkout/payment session qua provider.
4. Billing provider webhook payment succeeded.
5. ProcessBillingWebhookCommand idempotently.
6. Domain Subscription.Activate(...).
7. Create Entitlements from PlanLimits.
8. Commit.
9. Notify owner.
```

#### UC-BILL-03 — Cancel subscription

```text
1. Owner request cancel.
2. Permission ManageBilling.
3. Validate active subscription.
4. Domain Subscription.ScheduleCancellation(endOfPeriod).
5. Provider update if needed.
6. Commit.
7. Entitlements remain active until effective end date.
```

### 15.7. Commands/Queries chuẩn

```text
Commands:
- CreatePlanCommand
- UpdatePlanCommand
- StartSubscriptionCommand
- ChangeSubscriptionPlanCommand
- CancelSubscriptionCommand
- ProcessBillingWebhookCommand
- CreatePaymentMethodCommand
- RemovePaymentMethodCommand
- RecordFeatureUsageCommand
- RecalculateEntitlementsCommand

Queries:
- ListPlansQuery
- GetCurrentSubscriptionQuery
- ListInvoicesQuery
- GetWorkspaceEntitlementsQuery
- GetUsageSummaryQuery
```

---

## 16. Search bounded context / projection service

### 16.1. Trách nhiệm

Search không nên là domain aggregate chính. Đây là projection/read-model context dùng để tìm kiếm tài nguyên.

Repo hiện có infra projection:

```text
Infrastructure/Data/Projections/Search/SearchDocuments
Infrastructure/Data/Projections/Search/SearchIndexJobs
```

### 16.2. Functional requirements

- Index page, board, item, comment/file metadata nếu cần.
- Search theo workspace.
- Filter theo resource type.
- Permission-aware search result.
- Reindex resource khi thay đổi.
- Background search index job.

### 16.3. Business rules

1. SearchDocument phải workspace-scoped.
2. Không trả resource user không có quyền xem.
3. Indexing phải idempotent theo resource id + version.
4. Delete/archive resource phải remove/deactivate search document.
5. Search không là source of truth.

### 16.4. Luồng nghiệp vụ

#### UC-SEARCH-01 — Index resource after domain event

```text
1. Resource changed event xảy ra: PageUpdated, BoardCreated, ItemUpdated.
2. Event handler tạo SearchIndexJob(resourceType, resourceId, workspaceId, reason).
3. Worker claim pending jobs.
4. Worker load source resource.
5. Build searchable text + metadata.
6. Upsert SearchDocument.
7. Mark job completed.
8. Retry on failure.
```

#### UC-SEARCH-02 — Search workspace

```text
1. Request query, workspaceId, filters, paging.
2. Validate query length.
3. Permission: user phải là workspace member.
4. Query SearchDocuments by workspace + text index.
5. For each result, apply permission check or pre-filter by visibility ACL.
6. Return SearchResultDto.
```

### 16.5. Files chuẩn

```text
Application/Features/Search/Queries/SearchWorkspace/SearchWorkspaceQuery.cs
Application/Features/Search/Queries/SearchWorkspace/SearchWorkspaceQueryHandler.cs
Application/Features/Search/Queries/SearchWorkspace/SearchWorkspaceQueryValidator.cs
Application/Features/Search/DTOs/SearchResultDto.cs
Application/Features/Search/Commands/ReindexResource/ReindexResourceCommand.cs
Infrastructure/Data/Projections/Search/SearchDocument.cs
Infrastructure/Data/Projections/Search/SearchIndexJob.cs
Infrastructure/Search/SearchIndexerWorker.cs
API/Endpoints/Search/SearchWorkspaceEndpoint.cs
```

---

## 17. Analytics bounded context

### 17.1. Trách nhiệm

Analytics cung cấp dashboard/reporting cho workspace.

### 17.2. Module hiện có trong domain

```text
Notrelix.Domain/Analytics/
├── Dashboards/
├── Rules/
├── Snapshots/
└── Widgets/
```

### 17.3. Aggregate candidates

| Aggregate | Vai trò |
|---|---|
| Dashboard | Dashboard config |
| DashboardWidget | Widget config |
| DashboardSource | Data source definition |
| ReportingSnapshot | Precomputed snapshot |

### 17.4. Functional requirements

- Create dashboard.
- Add/update/remove widget.
- Widget source: board, item, workload, usage.
- Generate reporting snapshots.
- Query metrics.
- Permission-aware dashboard access.

### 17.5. Business rules

1. Dashboard thuộc workspace.
2. Widget source phải thuộc workspace.
3. User xem dashboard phải có quyền xem underlying data.
4. Snapshot không là source of truth.
5. Analytics query phải giới hạn thời gian/paging để tránh heavy query.

### 17.6. Luồng nghiệp vụ

#### UC-AN-01 — Create dashboard

```text
1. User request dashboard name.
2. Permission ManageDashboards.
3. Domain Dashboard.Create(...).
4. Commit.
5. Activity log.
```

#### UC-AN-02 — Refresh reporting snapshot

```text
1. Scheduled/reporting job due.
2. Load dashboard/widget source.
3. Run aggregate queries/read model.
4. Save ReportingSnapshot.
5. Mark generatedAt.
```

### 17.7. Commands/Queries

```text
Commands:
- CreateDashboardCommand
- UpdateDashboardCommand
- DeleteDashboardCommand
- AddDashboardWidgetCommand
- UpdateDashboardWidgetCommand
- RemoveDashboardWidgetCommand
- RefreshReportingSnapshotCommand

Queries:
- ListDashboardsQuery
- GetDashboardQuery
- GetWidgetDataQuery
- ListReportingSnapshotsQuery
```

---

## 18. Operations bounded context / ops capability

### 18.1. Trách nhiệm

Operations không phải product domain cho end user thông thường. Nó quản lý công việc vận hành hệ thống:

- Idempotency keys
- Import jobs
- Export jobs
- Job locks
- Background jobs status
- Health/readiness
- Admin operations

### 18.2. Functional requirements

- Track idempotent command.
- Import workspace data.
- Export workspace data.
- Lock background job processing.
- Expose health/status.
- Admin retry/inspect failed jobs.

### 18.3. Business rules

1. Idempotency key scoped theo user/workspace/command.
2. Import/export phải permission-aware.
3. Export cần audit vì có thể chứa dữ liệu nhạy cảm.
4. Job lock phải expire để tránh deadlock.
5. Operations endpoint chỉ system/admin được truy cập.

### 18.4. Luồng nghiệp vụ

#### UC-OPS-01 — Idempotent command processing

```text
1. Request có Idempotency-Key header.
2. IdempotencyBehavior check key.
3. Nếu key completed: trả cached response.
4. Nếu key processing: trả conflict/retry later.
5. Nếu key mới: create IdempotencyKey processing.
6. Execute handler.
7. Save result metadata.
8. Commit.
```

#### UC-OPS-02 — Export workspace

```text
1. Owner/Admin request export.
2. Permission ExportWorkspace.
3. Create ExportJob.
4. Worker claim job.
5. Stream data by bounded context.
6. Write file to storage.
7. Mark completed with download reference.
8. Audit log.
```

### 18.5. Files chuẩn

```text
Application/Features/Operations/Exports/Commands/CreateWorkspaceExport/CreateWorkspaceExportCommand.cs
Application/Features/Operations/Exports/Queries/GetExportJob/GetExportJobQuery.cs
Infrastructure/Data/Ops/Entities/ExportJob.cs
Infrastructure/Ops/ExportWorker.cs
API/Endpoints/Operations/Exports/CreateWorkspaceExportEndpoint.cs
```

---

# PHẦN B — LUỒNG NGHIỆP VỤ CROSS-CONTEXT

---

## 19. Luồng onboarding workspace

```text
Guest/Register -> Identity.UserCreated
Identity -> Workspaces.CreatePersonalWorkspace or CreateWorkspace
Workspaces -> WorkspaceMember.OwnerCreated
Governance -> Default roles/policies initialized
Billing -> Free plan entitlements initialized
Collaboration -> ActivityLog: workspace created
Search -> No index required or index workspace metadata
API -> Return workspace switch context
```

File/use case chain:

```text
Identity/Auth/Register
Workspaces/Workspaces/CreateWorkspace
Governance/Policies/InitializeWorkspacePolicy
Billing/Entitlements/InitializeFreeEntitlements
Collaboration/Activity/RecordActivity
```

Rule:

- Register không nên trực tiếp tạo quá nhiều side effect trong handler nếu vượt một transaction phức tạp.
- Có thể dùng application orchestration `RegisterAndCreateWorkspaceCommand` cho onboarding MVP.
- Với production, side effect như email/activity/analytics nên qua domain event/outbox sau commit.

---

## 20. Luồng tạo board hoàn chỉnh

```text
1. User ở workspace active.
2. Permission CreateBoard.
3. Entitlement check board quota.
4. Board.Create.
5. Default fields/groups created.
6. Commit transaction.
7. Domain events -> activity log.
8. SearchIndexJob created.
9. Realtime workspace topic receives board created.
10. Frontend updates board list.
```

Cross-context involvement:

| Context | Vai trò |
|---|---|
| Identity | current user |
| Workspaces | workspace active/member |
| Governance | permission |
| Billing | entitlement/quota |
| WorkManagement | board/fields/groups |
| Collaboration | activity/notification |
| Search | index board |
| Operations | idempotency optionally |

---

## 21. Luồng comment mention notification

```text
1. User comments on item/page.
2. Collaboration validates resource permission via Governance.
3. Comment created.
4. Mention parser extracts @users.
5. Only users with ViewResource permission receive mentions.
6. Notification created.
7. Activity log appended.
8. Realtime pushes to resource topic and user notification topic.
```

Rule:

- Mention không được leak tên/resource cho user không có quyền.
- Notification phải kiểm tra permission ở thời điểm tạo và nên kiểm tra lại khi click mở.

---

## 22. Luồng automation khi item đổi status

```text
1. WorkManagement emits BoardItemStatusChangedDomainEvent.
2. Automation dispatcher receives candidate event.
3. Find active automation rules for workspace.
4. Check rule trigger = item status changed.
5. Evaluate conditions.
6. Execute actions: assign user, send notification, create calendar event, move group.
7. Record AutomationExecution.
8. Prevent loops using correlation/causation id.
```

Rule:

- Automation action nên gọi application command thay vì mutate DbContext trực tiếp.
- Automation cần actor context: `SystemAutomationActor` + original actor/correlation.

---

## 23. Luồng calendar sync với due date

```text
1. Item due date changed.
2. WorkManagement event created.
3. Integration handler checks calendar mapping.
4. If mapping active, create/update external calendar event.
5. Store CalendarEventLink.
6. On external calendar update, sync worker updates Notrelix item or creates conflict.
7. Conflict resolution follows policy: Notrelix wins / external wins / manual.
```

---

# PHẦN C — QUY ĐỊNH FILE, TÊN FILE, CODING RULE

---

## 24. Cấu trúc file chuẩn cho một use case

### 24.1. Command use case

Ví dụ: `CreateBoardInWorkspace`

```text
backend/src/Notrelix.Application/Features/{BoundedContext}/{Module}/Commands/{UseCase}/
├── {UseCase}Command.cs
├── {UseCase}CommandHandler.cs
├── {UseCase}CommandValidator.cs
├── {UseCase}Result.cs              # nếu response phức tạp
└── {UseCase}Mapping.cs             # optional nếu mapping riêng
```

Nếu use case rất nhỏ, có thể gộp command + handler trong một file `{UseCase}.cs`, nhưng enterprise rule khuyến nghị tách file khi:

- Handler dài hơn 80 dòng.
- Có nhiều dependency.
- Có Result riêng.
- Có mapping phức tạp.
- Có test cần đọc rõ.

### 24.2. Query use case

```text
backend/src/Notrelix.Application/Features/{BoundedContext}/{Module}/Queries/{UseCase}/
├── {UseCase}Query.cs
├── {UseCase}QueryHandler.cs
├── {UseCase}QueryValidator.cs
├── {UseCase}Result.cs
└── {UseCase}QueryMapping.cs         # optional
```

### 24.3. DTOs

```text
backend/src/Notrelix.Application/Features/{BoundedContext}/{Module}/DTOs/
├── {Resource}Dto.cs
├── {Resource}SummaryDto.cs
├── {Resource}DetailDto.cs
└── {Resource}ListItemDto.cs
```

Rule:

- DTO không chứa domain entity.
- DTO không expose internal fields như password hash, secret ref raw, deleted metadata nếu không cần.
- DTO phải ổn định với API contract.

### 24.4. Domain files

```text
backend/src/Notrelix.Domain/{BoundedContext}/{Module}/
├── {Aggregate}.cs
├── {Entity}.cs
├── {ValueObject}.cs
├── {Enum}.cs
├── Events/
│   ├── {Aggregate}{Action}DomainEvent.cs
│   └── ...
└── Rules/
    ├── {BusinessRule}.cs
    └── ...
```

### 24.5. Infrastructure files

```text
backend/src/Notrelix.Infrastructure/Data/Configurations/{BoundedContext}/
├── {Entity}Configuration.cs
└── ...

backend/src/Notrelix.Infrastructure/{Capability}/
├── {Service}.cs
├── {Options}.cs
└── ...
```

### 24.6. API endpoint files

```text
backend/src/Notrelix.API/Endpoints/{BoundedContext}/{Module}/
├── {UseCase}Endpoint.cs
└── {Module}EndpointGroup.cs         # optional group registration
```

Endpoint rule:

- Endpoint chỉ map HTTP request -> command/query.
- Endpoint không query DbContext.
- Endpoint không chứa business rule.
- Endpoint trả response chuẩn: `Results.Ok`, `Results.Created`, `Results.NoContent`, problem details.

### 24.7. Test files

```text
backend/tests/Notrelix.Domain.Tests/{BoundedContext}/{Aggregate}Tests.cs
backend/tests/Notrelix.Application.Tests/Features/{BoundedContext}/{Module}/{UseCase}Tests.cs
backend/tests/Notrelix.API.Tests/{BoundedContext}/{Module}/{UseCase}EndpointTests.cs
backend/tests/Notrelix.Integration.Tests/{BoundedContext}/{Flow}FlowTests.cs
backend/tests/Notrelix.Architecture.Tests/{RuleName}Tests.cs
```

---

## 25. Naming conventions

### 25.1. Bounded context

```text
Identity
Workspaces
WorkManagement
Documents
Collaboration
Governance
Integrations
Automation
Billing
Analytics
Search
Operations
```

Không dùng tên mơ hồ như `CommonBusiness`, `Core`, `Managers`, `Services` cho bounded context.

### 25.2. Use case naming

Tên use case phải là động từ + danh từ rõ nghĩa:

```text
CreateBoardInWorkspace
UpdateBoard
ArchiveBoard
CreateBoardItem
MoveBoardItem
InviteWorkspaceMember
AcceptWorkspaceInvitation
CreatePage
UpdateBlock
AddComment
MarkNotificationAsRead
CreateAutomationRule
ProcessBillingWebhook
```

Không dùng tên chung:

```text
HandleBoard
ManageItem
ProcessData
UpdateInfo
DoAction
```

### 25.3. Command/Query naming

```text
{UseCase}Command
{UseCase}CommandHandler
{UseCase}CommandValidator
{UseCase}Result

{UseCase}Query
{UseCase}QueryHandler
{UseCase}QueryValidator
{UseCase}Result
```

### 25.4. Domain event naming

```text
{Aggregate}{PastTenseAction}DomainEvent
```

Ví dụ:

```text
BoardCreatedDomainEvent
BoardRenamedDomainEvent
BoardArchivedDomainEvent
BoardItemCreatedDomainEvent
WorkspaceInvitationAcceptedDomainEvent
UserPasswordChangedDomainEvent
```

Rule:

- Domain event dùng past tense.
- Domain event là sự kiện đã xảy ra, không phải command.
- Event phải có metadata: EventId, OccurredAt, WorkspaceId?, ActorUserId?, CorrelationId?, CausationId?, EventVersion.

### 25.5. Integration event naming

```text
{bounded_context}.{resource}.{event}.v{version}
```

Ví dụ:

```text
work_management.board.created.v1
work_management.item.updated.v1
documents.page.updated.v1
billing.subscription.changed.v1
```

---

## 26. CQRS rules

### 26.1. Command rules

Command dùng để thay đổi state.

Mọi command mutate phải:

- Implement `ICommand<Result>` hoặc `ICommand<Result<T>>`.
- Implement `ITransactionalRequest` nếu ghi DB.
- Implement `IWorkspaceRequest` nếu workspace-scoped.
- Implement `IRequirePermission` nếu cần permission.
- Implement `IRequireEntitlement` nếu bị giới hạn plan/quota.
- Implement `IIdempotentRequest` nếu request có thể retry hoặc external callback.
- Có validator thật.
- Có tests.

### 26.2. Query rules

Query dùng để đọc state.

Mọi query list phải:

- Có paging.
- Có max page size.
- Có sorting/filtering explicit.
- Không load toàn bộ entity graph.
- Không trả deleted resource trừ khi query restore/admin.
- Apply permission/tenant filtering.
- Có cache nếu read-heavy và safe.

### 26.3. Handler rules

Handler được phép:

- Orchestrate use case.
- Load aggregate/entity cần thiết.
- Gọi domain methods.
- Gọi application abstractions.
- Tạo DTO/result.

Handler không được:

- Chứa business invariant đáng lẽ thuộc aggregate.
- Parse enum nguy hiểm bằng `Enum.Parse` không validate.
- Bypass permission.
- Gọi external HTTP trực tiếp.
- Publish realtime/email trực tiếp trước commit.
- Gọi `SaveChangesAsync` thủ công nếu đã dùng `TransactionBehavior`, trừ exception có ghi chú.

---

## 27. Validation rules

### 27.1. Validator bắt buộc

Mỗi public command/query phải có validator, trừ query không input như `GetCurrentUserQuery`, nhưng vẫn nên có file rỗng có comment nếu cần.

Validator không được rỗng nếu input có:

- string
- enum dạng string
- paging
- sorting
- id
- json/settings
- date range
- file upload metadata

### 27.2. Validation examples

```text
Title:
- Not empty
- Trimmed length 1..200

Description:
- Max length 5000

Visibility:
- Must be valid BoardVisibility
- Use Enum.TryParse in validator

PageSize:
- 1..100

DateRange:
- from <= to
- max range depending query
```

### 27.3. Không để validation trong domain thay thế toàn bộ validator

Domain guard bảo vệ invariant cuối cùng. Validator bảo vệ API boundary và trả lỗi thân thiện.

Cả hai đều cần:

- Validator: input contract.
- Domain guard: business invariant.

---

## 28. Authorization & permission rules

### 28.1. Rule chính

Mọi command/query truy cập resource workspace-scoped phải có permission requirement rõ.

```text
IRequirePermission
- PermissionAction Action
- ResourceRef Resource
```

Ví dụ:

```csharp
public PermissionAction Action => PermissionAction.CreateBoard;
public ResourceRef Resource => ResourceRef.Create(ResourceType.Workspace, WorkspaceId);
```

### 28.2. Không dùng hai style authorization lẫn lộn

Không nên để một số command dùng pipeline, một số command tự gọi `_permissions.Ensure...` trong handler.

Target standard:

- Default: authorization qua pipeline.
- Exception: handler cần load resource trước mới biết workspace/resource, khi đó phải ghi comment và có test.

### 28.3. Permission actions đề xuất

```text
Workspace:
- ViewWorkspace
- ManageWorkspace
- ManageMembers
- ManageRoles
- ManageBilling
- ManageIntegrations
- ExportWorkspace

Board:
- ViewBoard
- CreateBoard
- ManageBoard
- DeleteBoard
- CreateItem
- UpdateItem
- DeleteItem
- ManageFields
- ManageViews

Documents:
- ViewPage
- CreatePage
- EditPage
- DeletePage
- SharePage

Collaboration:
- CommentResource
- AttachFile
- ManageComments

Automation:
- ViewAutomation
- ManageAutomation

Governance:
- ViewAudit
- ManagePolicies
- ManageResourcePermissions
```

---

## 29. Tenancy rules

### 29.1. Workspace scoped entity

Entity thuộc tenant phải implement `IWorkspaceScoped` và có `WorkspaceId`.

```text
Board
BoardItem
Page
Comment
Notification
AutomationRule
Subscription
Entitlement
SearchDocument
Dashboard
```

### 29.2. Query filter

DbContext có thể dùng global query filter theo workspace, nhưng không được chỉ dựa vào query filter.

Bắt buộc có test:

- User workspace A không đọc được data workspace B.
- User workspace A không update được data workspace B.
- Background/system context phải explicit.

### 29.3. API route rule

Ưu tiên route chứa workspace nếu resource tạo từ workspace:

```text
POST /api/workspaces/{workspaceId}/boards
GET  /api/workspaces/{workspaceId}/boards
```

Resource detail có thể dùng id trực tiếp nhưng phải permission check:

```text
GET /api/boards/{boardId}
PATCH /api/boards/{boardId}
```

---

## 30. Transaction, events, outbox rules

### 30.1. Transaction rule

- Command mutate dùng `ITransactionalRequest`.
- `TransactionBehavior` chịu trách nhiệm SaveChanges/Commit.
- Handler không tự commit transaction.

### 30.2. Domain event rule

Aggregate method thay đổi state nên emit domain event nếu:

- Cần activity/audit.
- Cần cache/search/realtime update.
- Cần automation/integration.
- Là lifecycle change quan trọng.

### 30.3. Outbox rule

External side effects không publish trực tiếp trong transaction.

Các side effect nên qua outbox/background worker:

- Email
- Webhook
- Integration event
- External API call
- Search indexing nếu không cần sync immediate
- Notification delivery ngoài DB

### 30.4. Realtime rule

Realtime event nên publish sau commit thành công.

Không để client nhận event cho dữ liệu rollback.

---

## 31. Data & migration rules

### 31.1. Schema ownership

Đề xuất schema theo context:

```text
identity.*
workspaces.*
work.*
documents.*
collab.*
governance.*
integrations.*
automation.*
billing.*
analytics.*
search.*
ops.*
```

### 31.2. Table conventions

- Table dùng snake_case.
- Primary key: `id uuid`.
- Workspace-scoped table có `workspace_id uuid not null`.
- Audit columns: `created_at`, `created_by`, `updated_at`, `updated_by`.
- Soft delete: `deleted_at`, `deleted_by`, `delete_reason`.
- Concurrency: `version bigint not null` cho aggregate root.
- Enum lưu string với max length hợp lý.
- JSON settings dùng `jsonb`.

### 31.3. Index conventions

Bắt buộc index:

```text
workspace_id
workspace_id + created_at
workspace_id + deleted_at where deleted_at is null
foreign keys
normalized_email unique
slug unique
outbox status/available_at
search workspace_id + resource_type
```

### 31.4. Migration rules

- Không sửa migration đã apply shared environment.
- Với dev chưa production, có thể squash/clean migration nhưng phải làm có chủ đích.
- Mỗi migration phải có tên nghiệp vụ rõ.
- Không dùng raw SQL nếu EF config làm được, trừ index/filter/extension đặc biệt.
- Projection table vẫn nên được quản lý bởi EF nếu nằm trong cùng deployment.

---

## 32. Testing strategy

### 32.1. Test pyramid

| Loại test | Mục tiêu |
|---|---|
| Domain unit tests | Business invariants, events, version |
| Application tests | Handler, validation, permission, transaction behavior |
| Integration tests | EF/PostgreSQL, API flow, outbox, tenant isolation |
| Architecture tests | Enforce dependency/naming/rules |
| Contract tests | API DTO compatibility |
| Security tests | Cross-tenant access, auth edge cases |

### 32.2. Test bắt buộc cho mỗi command mutate

- Validator rejects invalid input.
- Unauthorized user bị deny.
- Cross-workspace access bị deny.
- Happy path persists state.
- Domain event emitted nếu cần.
- Version increment nếu aggregate changed.
- Cache/realtime/outbox behavior nếu command có marker.

### 32.3. Architecture tests đề xuất

```text
- Domain must not reference Application/Infrastructure/API.
- Application must not reference Infrastructure/API.
- Commands ending with Command must implement ICommand.
- Mutating commands under workspace modules must implement IRequirePermission.
- Public commands/queries must have validators.
- Entities implementing IWorkspaceScoped must have WorkspaceId.
- AggregateRoot public mutation methods must call IncrementVersion when state changes.
- Domain events must end with DomainEvent.
- API endpoints must not inject DbContext directly.
```

---

# PHẦN D — KẾ HOẠCH TRIỂN KHAI THEO PHASE

---

## 33. Phase 0 — Stabilization & convention lock

### 33.1. Mục tiêu

Đưa repo về trạng thái có thể phát triển bền vững.

### 33.2. Tasks

```text
P0-01: Chốt runtime .NET version và đồng bộ README/global.json/csproj/Dockerfile/CI.
P0-02: Chốt Application folder convention.
P0-03: Chốt Domain event metadata convention.
P0-04: Chốt command/query/validator file naming.
P0-05: Thêm architecture tests cho dependency và naming.
P0-06: Audit validators rỗng và bổ sung validator thật.
P0-07: Chuẩn hóa authorization qua IRequirePermission.
P0-08: Chạy build/test baseline.
P0-09: Dọn legacy scaffolding không dùng hoặc đánh dấu rõ.
P0-10: Cập nhật AGENTS.md/RULE.md/CLAUDE.md/GEMINI.md theo rule mới.
```

### 33.3. Acceptance criteria

- `dotnet build` pass.
- `dotnet test` pass hoặc có danh sách failing tests rõ.
- README khớp runtime thật.
- Use case mới có template rõ.
- Architecture tests fail nếu vi phạm rule chính.

---

## 34. Phase 1 — Identity + Workspace + Governance Core

### 34.1. Mục tiêu

Có nền auth, tenant, membership, permission.

### 34.2. Scope

```text
Identity:
- Register
- Login
- Refresh token
- Logout
- Current user
- Update profile

Workspaces:
- Create workspace
- List my workspaces
- Get workspace
- Invite member
- Accept invitation
- Change member role
- Remove member

Governance:
- Default roles
- Permission evaluator
- ResourceRef/PermissionAction matrix
- Audit log base
```

### 34.3. Acceptance criteria

- User đăng ký/đăng nhập được.
- Tạo workspace có owner membership.
- Member được mời và accept được.
- User không truy cập workspace khác.
- Permission evaluator có tests.
- Audit log ghi được các action quan trọng.

---

## 35. Phase 2 — WorkManagement Core

### 35.1. Scope

```text
Boards:
- Create board
- Update board
- Archive/unarchive/delete/restore board
- List workspace boards
- Get board detail

Board schema:
- Default fields
- Create/update/delete field
- List board fields

Items:
- Create item
- Update item
- Move item
- Delete/archive item
- List items

Views:
- List view basic
- Kanban/list view config
```

### 35.2. Acceptance criteria

- Workspace member có quyền tạo board.
- Board có default fields/groups.
- Item tạo ra có key sequence đúng.
- Update item validate field value theo type.
- Cross-workspace board/item access bị deny.
- Realtime/cache/search hooks có skeleton đúng.

---

## 36. Phase 3 — Documents Core

### 36.1. Scope

```text
Pages:
- Create page
- Rename page
- Move page
- Archive/delete/restore
- Page tree

Blocks:
- Create/update/delete/reorder block

Versions:
- Snapshot version
- List versions
- Restore version

Resource links:
- Link page to board/item
```

### 36.2. Acceptance criteria

- Page tree không có cycle.
- Block update tạo search index job.
- Permission edit/view page hoạt động.
- Page có thể link với board item.

---

## 37. Phase 4 — Collaboration Core

### 37.1. Scope

```text
Comments:
- Add/edit/delete comment
- Thread reply

Mentions:
- Parse @mention
- Notify mentioned users

Notifications:
- List notifications
- Mark read
- Unread count

Activity:
- Resource activity logs

Attachments:
- Upload intent + confirm metadata
```

### 37.2. Acceptance criteria

- User chỉ comment resource có quyền.
- Mention không leak cho user không có quyền.
- Notification idempotent.
- Activity log append-only.

---

## 38. Phase 5 — Search + Realtime + Background Jobs

### 38.1. Scope

```text
Search:
- SearchDocuments projection
- SearchIndexJobs worker
- Search workspace endpoint

Realtime:
- Topic convention
- Board/page/comment update events

Background:
- Outbox dispatcher
- Job lock
- Retry/backoff
```

### 38.2. Acceptance criteria

- Resource update tạo search index job.
- Worker xử lý idempotent.
- Search result permission-aware.
- Realtime publish sau commit.

---

## 39. Phase 6 — Integrations + Automation

### 39.1. Scope

```text
Integrations:
- Connect/disconnect calendar
- Calendar sync cursor
- Webhook subscription/delivery

Automation:
- Create rule
- Enable/disable rule
- Execute rule from domain event
- Execution history
```

### 39.2. Acceptance criteria

- Integration secret không lưu plain text.
- Webhook delivery retry được.
- Automation không tạo vòng lặp.
- Execution idempotent theo eventId + ruleId.

---

## 40. Phase 7 — Billing + Entitlements

### 40.1. Scope

```text
Plans:
- List plans
- Manage plan limits

Subscriptions:
- Current subscription
- Start/change/cancel subscription

Entitlements:
- Effective entitlement check
- Feature quota behavior

Usage:
- Usage ledger
- Current usage summary
```

### 40.2. Acceptance criteria

- Create board/page/item có thể bị chặn bởi entitlement.
- Usage ledger append-only.
- Billing webhook idempotent.

---

## 41. Phase 8 — Analytics + Production Hardening

### 41.1. Scope

```text
Analytics:
- Dashboard
- Widget
- Snapshot

Production:
- CI/CD
- Observability
- Health/readiness
- Rate limiting
- Backup/restore
- Security review
```

### 41.2. Acceptance criteria

- Dashboard query không quá nặng.
- Metrics/log/trace có correlation id.
- Deployment pipeline repeatable.
- Security checklist pass.

---

# PHẦN E — DEFINITION OF READY / DONE

---

## 42. Definition of Ready cho một use case

Một use case chỉ được đưa vào triển khai khi có:

```text
[ ] Actor rõ
[ ] Bounded context rõ
[ ] Module rõ
[ ] Input/output rõ
[ ] Permission action rõ
[ ] Workspace scope rõ
[ ] Business rules rõ
[ ] Domain aggregate bị ảnh hưởng rõ
[ ] Domain events cần emit rõ
[ ] Cache/search/realtime side effects rõ
[ ] API endpoint contract rõ
[ ] Acceptance criteria rõ
[ ] Test cases tối thiểu rõ
```

---

## 43. Definition of Done cho một command mutate

```text
[ ] Command file đúng folder convention
[ ] Handler không chứa business invariant sai layer
[ ] Validator thật, không rỗng
[ ] Permission marker hoặc documented exception
[ ] Workspace scope đúng
[ ] Transaction marker nếu ghi DB
[ ] Entitlement marker nếu bị giới hạn plan
[ ] Idempotency nếu command có retry/external callback
[ ] Domain method được gọi thay vì set property trực tiếp
[ ] Aggregate increment version nếu state thay đổi
[ ] Domain event emit nếu lifecycle/business event quan trọng
[ ] Cache invalidation nếu query cache bị ảnh hưởng
[ ] Realtime topic nếu UI cần realtime
[ ] Search index job nếu searchable content thay đổi
[ ] Audit/activity nếu action quan trọng
[ ] Unit tests
[ ] Application tests
[ ] Integration/API tests nếu endpoint public
[ ] Architecture tests pass
[ ] Documentation/update API contract
```

---

## 44. Definition of Done cho một query

```text
[ ] Query file đúng convention
[ ] Validator cho filter/paging/sort
[ ] Workspace/permission filtering
[ ] Paging bắt buộc với list query
[ ] Max page size
[ ] Projection DTO, không trả entity trực tiếp
[ ] Không N+1 query
[ ] Index phù hợp
[ ] Cache nếu read-heavy
[ ] Tests cho permission/cross-tenant
```

---

# PHẦN F — ISSUE TEMPLATE & PR CHECKLIST

---

## 45. Issue template cho use case

```md
# [BC][Module] Use case name

## Business goal

## Actor

## Input

## Output

## Business rules

## Permission

## Workspace scope

## Domain aggregates/entities

## Domain events

## Commands/Queries

## API endpoints

## Files to implement

## Acceptance criteria

## Test cases

## Risks / notes
```

---

## 46. PR checklist

```md
## Scope
- [ ] PR chỉ xử lý đúng bounded context/use case đã ghi
- [ ] Không refactor lan rộng không liên quan

## Architecture
- [ ] Đúng layer dependency
- [ ] Đúng folder convention
- [ ] Không bypass pipeline

## Application
- [ ] Command/query có validator
- [ ] Permission marker đúng
- [ ] Transaction/idempotency/entitlement marker đúng nếu cần

## Domain
- [ ] Business rule nằm trong aggregate/domain service đúng chỗ
- [ ] IncrementVersion khi state thay đổi
- [ ] Domain event phù hợp

## Infrastructure
- [ ] EF config đầy đủ
- [ ] Index/migration phù hợp
- [ ] No secret plain text

## Tests
- [ ] Domain tests
- [ ] Application tests
- [ ] Integration/API tests nếu cần
- [ ] Architecture tests pass

## Security
- [ ] Cross-tenant access checked
- [ ] Permission checked
- [ ] Sensitive data không leak

## Observability
- [ ] Logs/metrics/tracing nếu flow quan trọng
```

---

# PHẦN G — RỦI RO VÀ KIỂM SOÁT

---

## 47. Risk register

| Risk | Mức độ | Dấu hiệu | Cách kiểm soát |
|---|---:|---|---|
| Architecture drift | Cao | README/code/version lệch | Phase 0 stabilization, architecture tests |
| Over-engineering | Cao | Tạo quá nhiều module chưa có use case | MVP scope, DoR bắt buộc |
| Tenant data leak | Rất cao | Query thiếu workspace/permission | Cross-tenant tests P0 |
| Authorization inconsistent | Cao | Handler tự check quyền tùy ý | IRequirePermission + architecture test |
| Validator rỗng | Trung bình/Cao | Input lỗi vào handler/domain | Validator rule + tests |
| Outbox/realtime trước commit | Cao | Client thấy data rollback | Publish after commit/outbox |
| Billing entitlement sai | Cao | User vượt quota hoặc bị chặn sai | Entitlement tests |
| Search leak data | Rất cao | Search trả resource không có quyền | Permission-aware search tests |
| Automation loop | Cao | Rule trigger lặp vô hạn | causation id, max depth, idempotency |
| Performance query list | Trung bình | No paging/N+1 | Query rules + profiling |

---

# PHẦN H — KẾT LUẬN TRIỂN KHAI

---

## 48. Hướng triển khai tốt nhất

Hướng tốt nhất cho Notrelix không phải thêm thật nhiều tính năng ngay, mà là:

```text
1. Stabilize architecture.
2. Lock conventions.
3. Build Identity + Workspace + Governance first.
4. Build WorkManagement as main product core.
5. Add Documents and Collaboration.
6. Then add Search/Reatime/Automation/Integrations/Billing.
7. Enforce every rule bằng tests và PR checklist.
```

### 48.1. Nguyên tắc quyết định khi phân vân

- Nếu liên quan business invariant → Domain.
- Nếu liên quan use case orchestration → Application.
- Nếu liên quan DB/external service/cache/storage → Infrastructure.
- Nếu liên quan HTTP contract → API.
- Nếu là read optimization → Projection/Search/ReadModel, không biến thành aggregate.
- Nếu là side effect external → Outbox/background worker.
- Nếu là reusable primitive thật sự stable → SharedKernel.

### 48.2. Ưu tiên gần nhất

```text
P0:
- Đồng bộ runtime/docs/build.
- Chuẩn hóa validator.
- Chuẩn hóa authorization marker.
- Thêm architecture tests.
- Tenant isolation tests.

P1:
- Hoàn thiện Identity/Workspace/Governance.
- Hoàn thiện WorkManagement board/item/field core.

P2:
- Documents, Collaboration, Search, Realtime.

P3:
- Automation, Integrations, Billing, Analytics.
```

---

## 49. Appendix — Bảng file skeleton khi thêm use case mới

Ví dụ tạo use case: `CreateXCommand`

```text
1. Domain
   - {Aggregate}.cs nếu chưa có
   - Events/{Aggregate}CreatedDomainEvent.cs
   - Rules/{RuleName}.cs nếu rule phức tạp

2. Application
   - Features/{BC}/{Module}/Commands/{UseCase}/{UseCase}Command.cs
   - Features/{BC}/{Module}/Commands/{UseCase}/{UseCase}CommandHandler.cs
   - Features/{BC}/{Module}/Commands/{UseCase}/{UseCase}CommandValidator.cs
   - Features/{BC}/{Module}/Commands/{UseCase}/{UseCase}Result.cs
   - Features/{BC}/{Module}/DTOs/{Resource}Dto.cs nếu cần

3. API
   - Endpoints/{BC}/{Module}/{UseCase}Endpoint.cs

4. Infrastructure
   - Data/Configurations/{BC}/{Entity}Configuration.cs nếu entity mới
   - Services/{Service}.cs nếu cần external/infrastructure implementation

5. Tests
   - Domain.Tests/{BC}/{Aggregate}Tests.cs
   - Application.Tests/Features/{BC}/{Module}/{UseCase}Tests.cs
   - API.Tests/{BC}/{Module}/{UseCase}EndpointTests.cs
   - Integration.Tests/{BC}/{UseCase}FlowTests.cs nếu flow quan trọng
```

---

## 50. Appendix — Rule ngắn để đưa vào `AGENTS.md` / `RULE.md`

```md
# Notrelix Enterprise Development Rules

1. Backend follows Modular Monolith + Clean Architecture + DDD + CQRS.
2. Domain contains business invariants and emits domain events.
3. Application contains use cases only; no direct external side effects.
4. Infrastructure implements persistence, cache, storage, messaging, external services.
5. API maps HTTP to commands/queries only.
6. Every mutating workspace command must implement IWorkspaceRequest, IRequirePermission, and ITransactionalRequest unless explicitly documented.
7. Every public command/query must have a real validator.
8. Every workspace-scoped entity must implement IWorkspaceScoped and have WorkspaceId.
9. Never publish external side effects before DB commit; use outbox/background workers.
10. Every aggregate state change increments Version and emits a domain event if it affects other contexts, audit, search, realtime, or automation.
11. Queries must be permission-aware, tenant-aware, paginated, and DTO-based.
12. No handler should call DbContext SaveChanges if TransactionBehavior owns the transaction.
13. No API endpoint should inject DbContext directly for business flow.
14. Tests must cover validation, authorization, tenant isolation, and business invariants.
15. Architecture tests are mandatory and must pass before merge.
```

---

**End of document.**
