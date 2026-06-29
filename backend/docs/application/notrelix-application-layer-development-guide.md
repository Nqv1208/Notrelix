# Notrelix Application Layer Enterprise Architecture v3

> Phiên bản chuẩn hóa lại theo hướng **Enterprise Application Layer**, **Pragmatic CQRS**, **Module-first Vertical Slice**, phù hợp với Domain/Bouded Context hiện tại của Notrelix.  
> Mục tiêu chính: Application không còn tổ chức kiểu `Feature/Commands/Module` nữa, mà chuyển dần sang `Feature/Module/Commands|Queries/UseCase` để dễ scale khi hệ thống lớn.

---

## 0. Tóm tắt quyết định kiến trúc

### 0.1. Quyết định chính

Notrelix nên xây Application theo hướng:

```txt
Modular Monolith + Clean Architecture + DDD Domain + Pragmatic CQRS + Module-first Vertical Slice
```

Không nên làm:

```txt
- CRUD service layer theo bảng.
- Controller gọi thẳng DbContext.
- Application gom toàn bộ Commands/Queries ở cấp bounded context quá lâu.
- Full CQRS tách read/write database ngay từ đầu.
- Event Sourcing toàn hệ thống quá sớm.
```

### 0.2. Cấu trúc đúng cần hướng tới

Target structure:

```txt
Notrelix.Application/
  Common/
  Features/
    WorkManagement/
      Boards/
        Commands/
          CreateBoard/
          RenameBoard/
          ArchiveBoard/
        Queries/
          GetBoard/
          GetWorkspaceBoards/
        DTOs/
        Services/
        Mapping/
        Cache/
        Permissions/

      BoardItems/
        Commands/
          CreateBoardItem/
          UpdateBoardItemFieldValue/
          MoveBoardItem/
        Queries/
          GetBoardItem/
          GetBoardItemsByView/
        DTOs/
        ReadModels/
        Services/
```

Không nên giữ lâu dài:

```txt
Notrelix.Application/
  Features/
    WorkManagement/
      Commands/
        Boards/
        BoardItems/
      Queries/
        Boards/
        BoardItems/
      DTOs/
```

Cấu trúc hiện tại chạy được, nhưng khi WorkManagement lớn lên, nó sẽ làm module bị tách đôi giữa `Commands`, `Queries`, `DTOs`, gây khó maintain.

### 0.3. Nguyên tắc cốt lõi

```txt
Domain         = nghiệp vụ lõi, invariant, state transition, domain event.
Application    = orchestration use case, CQRS, validation, authorization, transaction, idempotency, entitlement, audit, read model.
Infrastructure = EF Core, PostgreSQL, Redis, Email, Storage, Outbox worker, SignalR, external provider.
API            = HTTP transport, request binding, response mapping, auth entrypoint.
```

Application không được biến thành Domain thứ hai. Application điều phối, không sở hữu invariant lõi.

---

## 1. Vì sao cần Module-first Vertical Slice?

### 1.1. Vấn đề của cấu trúc `Commands/Queries` ở cấp bounded context

Ví dụ hiện tại có thể đang gần dạng:

```txt
Features/
  WorkManagement/
    Commands/
      Boards/
      BoardItems/
      BoardFields/
    Queries/
      Boards/
      BoardItems/
    DTOs/
```

Cấu trúc này ổn ở giai đoạn đầu, nhưng khi hệ thống lớn sẽ có vấn đề:

```txt
- Muốn sửa Boards phải mở nhiều nơi: Commands/Boards, Queries/Boards, DTOs, Services.
- DTO dùng chung dễ bị lạm dụng giữa module.
- WorkManagement/Commands phình rất nhanh.
- Review PR khó vì use case của một module nằm rải rác.
- Khó tách service/module về sau.
```

### 1.2. Lợi ích của Module-first

Module-first đặt nghiệp vụ làm trung tâm:

```txt
WorkManagement/
  Boards/
    Commands/
    Queries/
    DTOs/
    Services/
    Mapping/

  BoardItems/
    Commands/
    Queries/
    DTOs/
    Services/
    ReadModels/
```

Lợi ích:

```txt
- Dễ tìm toàn bộ use case của một module.
- Dễ review theo module nghiệp vụ.
- Dễ tách service sau này.
- Dễ đặt rule riêng cho module.
- Dễ viết test mirror cấu trúc Application.
- Giảm DTO/service dùng sai ngữ cảnh.
```

### 1.3. Quy tắc chọn cấu trúc

```txt
Bounded context nhỏ:
  Có thể dùng BoundedContext/Commands + Queries trong giai đoạn đầu.

Bounded context lớn:
  Bắt buộc dùng BoundedContext/Module/Commands + Queries.
```

Áp dụng cho Notrelix:

```txt
WorkManagement  → bắt buộc module-first.
Governance      → nên module-first.
Automation      → nên module-first.
Billing         → nên module-first.
Collaboration   → nên module-first.
Documents       → nên module-first.
Identity        → module-first theo Auth/Users/Profiles/Sessions.
Workspaces      → module-first theo Workspaces/Members/Invitations/Spaces/Teams.
Integrations    → module-first theo Connections/Webhooks/Providers/Sync.
Analytics       → module-first theo Dashboards/Widgets/Snapshots.
```

---

## 2. Application Layer chịu trách nhiệm gì?

Application trả lời các câu hỏi:

```txt
- Use case là gì?
- Thuộc bounded context/module nào?
- Là command hay query?
- Cần validate input gì?
- Cần workspace context không?
- Cần permission action nào?
- Cần entitlement/quota không?
- Cần transaction không?
- Cần idempotency không?
- Cần expected version/concurrency không?
- Cần audit/activity/outbox/cache invalidation/realtime không?
- DTO trả ra là gì?
```

Application không làm:

```txt
- Không chứa invariant lõi của Domain.
- Không gọi Redis/S3/SMTP/SignalR trực tiếp.
- Không return EF entity hoặc Domain entity ra API.
- Không check role thủ công trong từng handler.
- Không query dữ liệu private rồi để frontend tự lọc.
- Không copy từng Domain class thành folder Application.
- Không tạo service khổng lồ kiểu BoardService/ItemService chứa mọi use case.
```

Ví dụ đúng:

```txt
UpdateBoardItemFieldValueCommandHandler
  - nhận command.
  - permission đã được behavior check.
  - load BoardItem + BoardField.
  - kiểm tra cùng workspace/board nếu cần ở Application boundary.
  - gọi Domain method BoardItem.UpdateFieldValue(...).
  - upsert read/typed value nếu kiến trúc đang dùng dual storage.
  - SaveChanges.
  - domain event/outbox lo side effect.
  - return DTO/result.
```

Ví dụ sai:

```txt
UpdateBoardItemFieldValueCommandHandler
  - tự check role bằng if/else.
  - tự validate toàn bộ field type bằng string rời rạc.
  - tự gọi SignalR.
  - tự gửi email.
  - tự gọi Redis.
  - return BoardItem entity.
```

---

## 3. Target folder structure tổng thể

```txt
Notrelix.Application/
├── Common/
│   ├── Abstractions/
│   ├── CQRS/
│   ├── Behaviors/
│   ├── Security/
│   ├── Tenancy/
│   ├── Transactions/
│   ├── Idempotency/
│   ├── Entitlements/
│   ├── Auditing/
│   ├── Activity/
│   ├── Caching/
│   ├── Events/
│   ├── ReadModels/
│   ├── Mapping/
│   ├── Validation/
│   ├── Models/
│   ├── Exceptions/
│   └── Extensions/
│
├── Features/
│   ├── Identity/
│   ├── Workspaces/
│   ├── Governance/
│   ├── WorkManagement/
│   ├── Documents/
│   ├── Collaboration/
│   ├── Automation/
│   ├── Integrations/
│   ├── Billing/
│   ├── Analytics/
│   ├── Reporting/
│   ├── Search/
│   └── Operations/
│
└── DependencyInjection.cs
```

Nếu một bounded context chưa có trong Application nhưng có trong Domain/DB, vẫn nên thiết kế target để tránh thiếu không gian mở rộng.

---

## 4. Common layer chuẩn Enterprise

### 4.1. Common/CQRS

```txt
Common/CQRS/
├── ICommand.cs
├── ICommandHandler.cs
├── IQuery.cs
├── IQueryHandler.cs
├── ITransactionalRequest.cs
├── IIdempotentRequest.cs
├── IWorkspaceRequest.cs
├── IRequirePermission.cs
├── IRequireEntitlement.cs
├── IAuditableRequest.cs
├── IActivityRequest.cs
├── ICacheableQuery.cs
├── IInvalidateCacheRequest.cs
├── IExpectedVersionRequest.cs
└── IRealtimeRequest.cs
```

Suggested contracts:

```csharp
public interface ICommand<out TResponse> : IRequest<TResponse> { }
public interface ICommand : IRequest<Unit> { }
public interface IQuery<out TResponse> : IRequest<TResponse> { }

public interface IWorkspaceRequest
{
    Guid WorkspaceId { get; }
}

public interface IRequirePermission
{
    PermissionAction Action { get; }
    ResourceRef Resource { get; }
}

public interface ITransactionalRequest { }

public interface IIdempotentRequest
{
    string IdempotencyKey { get; }
}

public interface IRequireEntitlement
{
    FeatureCode Feature { get; }
    int Amount { get; }
}

public interface IExpectedVersionRequest
{
    long ExpectedVersion { get; }
}

public interface ICacheableQuery
{
    string CacheKey { get; }
    TimeSpan? Ttl { get; }
}

public interface IInvalidateCacheRequest
{
    IReadOnlyCollection<CacheInvalidationKey> GetInvalidationKeys();
}
```

Rule:

```txt
- Command mutate state phải implement ICommand.
- Query không mutate state phải implement IQuery.
- Request thuộc workspace phải implement IWorkspaceRequest.
- Request cần permission phải implement IRequirePermission.
- Command mutate aggregate quan trọng nên implement IExpectedVersionRequest.
- Command external/retryable nên implement IIdempotentRequest.
```

### 4.2. Common/Abstractions

```txt
Common/Abstractions/
├── IApplicationDbContext.cs
├── IReadOnlyApplicationDbContext.cs
├── IUnitOfWork.cs
├── ICurrentUser.cs
├── ICurrentWorkspace.cs
├── IDateTimeProvider.cs
├── IPermissionEvaluator.cs
├── IEntitlementChecker.cs
├── IAuditWriter.cs
├── IActivityWriter.cs
├── IOutboxWriter.cs
├── ICacheService.cs
├── ICacheInvalidationService.cs
├── IRealtimePublisher.cs
├── IEmailSender.cs
├── IStorageService.cs
├── ISearchIndexer.cs
├── IBackgroundJobScheduler.cs
└── IIdempotencyService.cs
```

Rule:

```txt
- Application định nghĩa interface.
- Infrastructure implement interface.
- Application không biết Redis/S3/SMTP/SignalR/Npgsql implementation.
- IApplicationDbContext chỉ dùng cho command hoặc query đơn giản.
- Query phức tạp nên dùng ReadService riêng.
```

### 4.3. Common/Behaviors

```txt
Common/Behaviors/
├── LoggingBehavior.cs
├── ValidationBehavior.cs
├── WorkspaceContextBehavior.cs
├── AuthorizationBehavior.cs
├── IdempotencyBehavior.cs
├── EntitlementBehavior.cs
├── TransactionBehavior.cs
├── ConcurrencyBehavior.cs
├── AuditBehavior.cs
├── ActivityBehavior.cs
├── CacheBehavior.cs
├── CacheInvalidationBehavior.cs
├── ExceptionMappingBehavior.cs
└── PerformanceBehavior.cs
```

#### Command pipeline

```txt
Command Request
  ↓
LoggingBehavior
  ↓
ValidationBehavior
  ↓
WorkspaceContextBehavior
  ↓
AuthorizationBehavior
  ↓
IdempotencyBehavior
  ↓
EntitlementBehavior
  ↓
TransactionBehavior
  ↓
Handler
  ↓
AuditBehavior / ActivityBehavior
  ↓
CacheInvalidationBehavior
  ↓
Response
```

#### Query pipeline

```txt
Query Request
  ↓
LoggingBehavior
  ↓
ValidationBehavior
  ↓
WorkspaceContextBehavior
  ↓
AuthorizationBehavior
  ↓
CacheBehavior
  ↓
Handler
  ↓
Response
```

Rule:

```txt
- Authorization trước handler.
- Transaction chỉ áp dụng command mutate state.
- Query không transaction mặc định.
- Idempotency chỉ áp dụng command có side effect.
- Entitlement check trước khi tạo tài nguyên/consume quota.
- Cache invalidation không nằm rải rác trong handler nếu có thể biểu diễn bằng marker/request/event.
```

### 4.4. Common/Security

```txt
Common/Security/
├── PermissionContext.cs
├── PermissionDecision.cs
├── PermissionRequirement.cs
├── PermissionErrorCodes.cs
├── PermissionPolicyNames.cs
├── EffectivePermissionsDto.cs
├── PermissionActionMapper.cs
├── ResourceAccessScope.cs
└── PermissionDeniedException.cs
```

Rule:

```txt
- Application handler không tự check role thủ công.
- Mọi command/query nhạy cảm phải đi qua IPermissionEvaluator.
- Query cũng phải permission-aware.
- Private resource nếu user không có quyền View nên trả NotFound ở API layer để tránh resource probing.
- Permission evaluator deny-by-default cho action nhạy cảm.
```

Suggested API:

```csharp
public interface IPermissionEvaluator
{
    Task<PermissionDecision> EvaluateAsync(PermissionContext context, CancellationToken ct = default);
    Task EnsureAllowedAsync(PermissionContext context, CancellationToken ct = default);
    Task<IReadOnlyCollection<PermissionDecision>> EvaluateManyAsync(
        IReadOnlyCollection<PermissionContext> contexts,
        CancellationToken ct = default);
}
```

### 4.5. Common/Tenancy

```txt
Common/Tenancy/
├── IWorkspaceRequest.cs
├── WorkspaceContext.cs
├── WorkspaceResolutionResult.cs
├── WorkspaceRequiredException.cs
└── CrossWorkspaceAccessException.cs
```

Rule:

```txt
- Request thuộc workspace bắt buộc có WorkspaceId.
- WorkspaceId từ route/header chỉ là requested workspace, chưa đáng tin.
- Middleware/Behavior phải verify user là active member trước khi set CurrentWorkspace.
- EF global filter cho IWorkspaceScoped là safety net.
- Nếu request workspace-scoped thiếu CurrentWorkspace → fail closed.
```

### 4.6. Common/ReadModels

```txt
Common/ReadModels/
├── IBoardSchemaReadService.cs
├── IBoardItemReadService.cs
├── IWorkspaceNavigationReadService.cs
├── IPermissionReadService.cs
├── IActivityFeedReadService.cs
├── ISearchReadService.cs
├── IReportingReadService.cs
└── IReadModelDbConnectionFactory.cs
```

Rule:

```txt
- Query lớn không load aggregate.
- Query dùng AsNoTracking/projection/read service.
- Board item list bắt buộc cursor pagination.
- Search/reporting/activity feed phải permission-aware.
```

---

## 5. Naming conventions

### 5.1. Command

```txt
{Verb}{Resource}Command
{Verb}{Resource}CommandHandler
{Verb}{Resource}CommandValidator
{Verb}{Resource}Result
```

Examples:

```txt
CreateBoardCommand
RenameBoardCommand
ArchiveBoardCommand
UpdateBoardItemFieldValueCommand
MoveBoardItemCommand
GrantResourcePermissionCommand
CreateAutomationRuleCommand
```

### 5.2. Query

```txt
Get{Resource}Query
List{Resource}Query
Search{Resource}Query
Get{Resource}OverviewQuery
```

Examples:

```txt
GetBoardQuery
GetWorkspaceBoardsQuery
GetBoardItemsByViewQuery
GetBoardSchemaQuery
GetEffectivePermissionsQuery
```

### 5.3. Result/DTO

```txt
{UseCase}Result        // for command result
{Resource}Dto          // for reusable read DTO
{Resource}DetailDto    // for detail page
{Resource}SummaryDto   // for list item
{Resource}PageDto      // for paginated response
```

### 5.4. Folder rule

```txt
Feature/Module/Commands/UseCase/*.cs
Feature/Module/Queries/UseCase/*.cs
Feature/Module/DTOs/*.cs
Feature/Module/Services/*.cs
Feature/Module/ReadModels/*.cs
```

Không đặt tất cả DTO vào `Feature/DTOs` nếu DTO chỉ thuộc một module.

---

## 6. Feature structure template

Mỗi module lớn nên theo template:

```txt
Features/{BoundedContext}/{Module}/
├── Commands/
│   └── {UseCase}/
│       ├── {UseCase}Command.cs
│       ├── {UseCase}CommandHandler.cs
│       ├── {UseCase}CommandValidator.cs
│       └── {UseCase}Result.cs
│
├── Queries/
│   └── {UseCase}/
│       ├── {UseCase}Query.cs
│       ├── {UseCase}QueryHandler.cs
│       ├── {UseCase}QueryValidator.cs
│       └── {UseCase}Result.cs
│
├── DTOs/
├── Services/
├── ReadModels/
├── Mapping/
├── Cache/
├── Permissions/
└── README.md
```

`README.md` module nên ghi:

```txt
- Module này xử lý nghiệp vụ gì.
- Domain aggregate/entity liên quan.
- Commands/Queries chính.
- Permission actions.
- Entitlements nếu có.
- Cache keys.
- Outbox events liên quan.
- Test checklist.
```

---

## 7. Bounded Context: Identity

### 7.1. Mục tiêu

Identity xử lý danh tính người dùng, xác thực, phiên đăng nhập, credential, OAuth, MFA, profile. Identity không sở hữu workspace role/permission.

### 7.2. Target structure

```txt
Features/Identity/
├── Auth/
│   ├── Commands/
│   │   ├── Register/
│   │   ├── Login/
│   │   ├── RefreshToken/
│   │   ├── Logout/
│   │   ├── ForgotPassword/
│   │   ├── ResetPassword/
│   │   └── VerifyEmail/
│   ├── Queries/
│   │   └── GetAuthSession/
│   └── DTOs/
│
├── Users/
│   ├── Commands/
│   │   ├── ChangeEmail/
│   │   ├── ChangeUserStatus/
│   │   └── DeleteUser/
│   ├── Queries/
│   │   ├── GetCurrentUser/
│   │   ├── GetUser/
│   │   └── SearchUsers/
│   └── DTOs/
│
├── Profiles/
│   ├── Commands/
│   │   ├── UpdateProfile/
│   │   ├── UpdateAvatar/
│   │   └── UpdatePreferences/
│   ├── Queries/
│   │   └── GetMyProfile/
│   └── DTOs/
│
├── Sessions/
│   ├── Commands/
│   │   ├── RevokeSession/
│   │   └── RevokeAllSessions/
│   ├── Queries/
│   │   └── ListMySessions/
│   └── DTOs/
│
├── OAuth/
│   ├── Commands/
│   │   ├── LinkOAuthAccount/
│   │   └── UnlinkOAuthAccount/
│   └── Queries/
│       └── GetLinkedAccounts/
│
└── Security/
    ├── Commands/
    │   ├── EnableMfa/
    │   ├── DisableMfa/
    │   ├── VerifyMfa/
    │   └── RotateRecoveryCodes/
    ├── Queries/
    │   └── GetSecuritySettings/
    └── DTOs/
```

### 7.3. Rules

```txt
- Auth commands không cần WorkspaceId.
- Workspace role không nằm trong Identity.
- Password hashing/token generation nằm Infrastructure.
- Login/Register không return Domain User entity.
- Auth response chỉ trả token/session DTO an toàn.
- SearchUsers trong workspace phải đi qua Workspaces/Users read service hoặc permission-aware filter.
- Security-sensitive commands phải audit.
```

### 7.4. Critical flows

#### Register

```txt
Validate email/password
Check unique email
Create User domain aggregate
Create credential/profile nếu cần
SaveChanges
Outbox: UserRegisteredIntegrationEvent nếu cần email verify
Return RegisterResult
```

#### Login

```txt
Validate input
Find user credential
Verify password via Infrastructure service
Check user status/MFA
Create session/refresh token
Return AuthTokenDto
```

---

## 8. Bounded Context: Workspaces

### 8.1. Mục tiêu

Workspaces quản lý workspace, member, invitation, team, space, setting. Đây là tenant boundary chính của hệ thống.

### 8.2. Target structure

```txt
Features/Workspaces/
├── Workspaces/
│   ├── Commands/
│   │   ├── CreateWorkspace/
│   │   ├── UpdateWorkspace/
│   │   ├── ArchiveWorkspace/
│   │   ├── DeleteWorkspace/
│   │   └── TransferWorkspaceOwnership/
│   ├── Queries/
│   │   ├── GetWorkspace/
│   │   ├── GetMyWorkspaces/
│   │   └── GetWorkspaceOverview/
│   └── DTOs/
│
├── Members/
│   ├── Commands/
│   │   ├── AddWorkspaceMember/
│   │   ├── RemoveWorkspaceMember/
│   │   ├── ChangeWorkspaceMemberRole/
│   │   ├── SuspendWorkspaceMember/
│   │   └── ReactivateWorkspaceMember/
│   ├── Queries/
│   │   ├── GetWorkspaceMembers/
│   │   └── SearchWorkspaceMembers/
│   └── DTOs/
│
├── Invitations/
│   ├── Commands/
│   │   ├── InviteWorkspaceMember/
│   │   ├── AcceptWorkspaceInvitation/
│   │   ├── RevokeWorkspaceInvitation/
│   │   └── ResendWorkspaceInvitation/
│   ├── Queries/
│   │   └── ValidateInvitationToken/
│   └── DTOs/
│
├── Spaces/
│   ├── Commands/
│   │   ├── CreateSpace/
│   │   ├── RenameSpace/
│   │   ├── MoveSpace/
│   │   ├── ArchiveSpace/
│   │   └── RestoreSpace/
│   ├── Queries/
│   │   ├── GetSpaceTree/
│   │   └── GetSpaceBoards/
│   └── DTOs/
│
├── Teams/
│   ├── Commands/
│   │   ├── CreateTeam/
│   │   ├── RenameTeam/
│   │   ├── AddTeamMember/
│   │   └── RemoveTeamMember/
│   ├── Queries/
│   │   └── GetTeams/
│   └── DTOs/
│
├── Settings/
│   ├── Commands/
│   │   ├── UpdateWorkspaceSettings/
│   │   └── UpdateWorkspaceBranding/
│   ├── Queries/
│   │   └── GetWorkspaceSettings/
│   └── DTOs/
│
└── WorkspaceHome/
    ├── Queries/
    │   ├── GetWorkspaceHome/
    │   ├── GetRecentBoards/
    │   ├── GetRecentPages/
    │   └── GetMyWorkspaceActivity/
    └── DTOs/
```

### 8.3. Rules

```txt
- Không remove owner cuối cùng.
- Không hạ quyền owner cuối cùng.
- Invitation phải idempotent theo email/workspace nếu còn pending.
- AcceptInvitation phải verify token + expiration + email target.
- Guest không được thấy toàn workspace mặc định.
- Team thuộc Workspaces; Governance chỉ dùng Team làm permission subject.
- Workspace query phải filter theo membership.
- WorkspaceId sau khi verify mới set vào CurrentWorkspace.
```

### 8.4. Permission actions

```txt
WorkspaceCreate
WorkspaceView
WorkspaceUpdate
WorkspaceDelete
WorkspaceTransferOwnership
WorkspaceMemberInvite
WorkspaceMemberManage
WorkspaceSettingsManage
SpaceCreate
SpaceManage
TeamManage
```

---

## 9. Bounded Context: Governance

### 9.1. Mục tiêu

Governance quản lý permission rules, resource ACL/projection, share link, custom role, workspace policies, audit/security events.

### 9.2. Target structure

```txt
Features/Governance/
├── Permissions/
│   ├── Commands/
│   │   ├── CreatePermissionRule/
│   │   ├── UpdatePermissionRule/
│   │   ├── DisablePermissionRule/
│   │   ├── GrantResourcePermission/
│   │   ├── RevokeResourcePermission/
│   │   └── BulkGrantResourcePermissions/
│   ├── Queries/
│   │   ├── GetResourcePermissions/
│   │   ├── GetEffectivePermissions/
│   │   └── ExplainPermissionDecision/
│   ├── DTOs/
│   ├── Services/
│   │   ├── PermissionResolver.cs
│   │   ├── PermissionMatrix.cs
│   │   ├── PermissionInheritanceResolver.cs
│   │   └── FieldPermissionResolver.cs
│   └── Cache/
│
├── ShareLinks/
│   ├── Commands/
│   │   ├── CreateShareLink/
│   │   ├── DisableShareLink/
│   │   ├── RotateShareLink/
│   │   └── UpdateShareLinkExpiration/
│   ├── Queries/
│   │   ├── GetResourceShareLinks/
│   │   └── ResolveShareLink/
│   └── DTOs/
│
├── Roles/
│   ├── Commands/
│   │   ├── CreateCustomRole/
│   │   ├── UpdateCustomRole/
│   │   ├── DeleteCustomRole/
│   │   └── AssignCustomRoleToMember/
│   ├── Queries/
│   │   └── GetCustomRoles/
│   └── DTOs/
│
├── Policies/
│   ├── Commands/
│   │   ├── UpdateWorkspacePolicy/
│   │   └── UpdateGuestAccessPolicy/
│   ├── Queries/
│   │   └── GetWorkspacePolicies/
│   └── DTOs/
│
├── AuditLogs/
│   ├── Queries/
│   │   ├── GetWorkspaceAuditLogs/
│   │   ├── GetResourceAuditLogs/
│   │   └── ExportAuditLogs/
│   └── DTOs/
│
└── SecurityEvents/
    ├── Queries/
    │   └── GetSecurityEvents/
    └── DTOs/
```

### 9.3. Rules

```txt
- IPermissionEvaluator là source chính trong Application.
- Handler không query PermissionRule thủ công để quyết định allow/deny.
- PermissionRule là source of truth dài hạn.
- ResourcePermission nếu còn dùng thì là legacy/projection/fallback.
- Permission cache phải invalidated khi rule/role/member/share link thay đổi.
- Private resource không có View permission trả NotFound ở API boundary.
- Grant/Revoke/Role changes phải audit.
- Query như GetEffectivePermissions cũng phải check quyền quản lý resource hoặc self-view rule.
```

### 9.4. Critical flow: EvaluatePermission

```txt
Build PermissionContext
Check workspace membership active
Check owner/admin override nếu policy cho phép
Resolve custom roles/team membership
Resolve permission rules by scope/resource/subject/action
Apply deny-overrides-allow hoặc policy đã chọn
Fallback ResourcePermission nếu cần legacy
Return PermissionDecision with matched rules
```

---

## 10. Bounded Context: WorkManagement

### 10.1. Mục tiêu

WorkManagement là core lớn nhất: boards, board schema, fields, groups, items, views, forms, relations, formulas, rollups, workload, approvals, templates, search trong board.

### 10.2. Target structure tổng thể

```txt
Features/WorkManagement/
├── Boards/
├── BoardSchema/
├── BoardFields/
├── BoardGroups/
├── BoardItems/
├── BoardViews/
├── Checklists/
├── Labels/
├── Forms/
├── Relations/
├── Formulas/
├── Rollups/
├── Approvals/
├── Workload/
├── Templates/
├── MyWork/
├── BoardSearch/
└── Common/
```

### 10.3. WorkManagement/Common

```txt
WorkManagement/Common/
├── DTOs/
│   ├── FieldValueDto.cs
│   ├── PositionDto.cs
│   ├── BoardResourceRefDto.cs
│   └── FieldOptionDto.cs
├── Services/
│   ├── FieldValueApplicationValidator.cs
│   ├── BoardAccessResolver.cs
│   ├── BoardSchemaCacheKeyFactory.cs
│   └── BoardViewQueryCompiler.cs
├── Permissions/
│   └── WorkManagementPermissionActions.cs
└── Cache/
    └── WorkManagementCacheKeys.cs
```

Only truly shared WorkManagement artifacts go here.

---

### 10.4. Module: Boards

```txt
Features/WorkManagement/Boards/
├── Commands/
│   ├── CreateBoard/
│   ├── RenameBoard/
│   ├── UpdateBoardDescription/
│   ├── ChangeBoardVisibility/
│   ├── ArchiveBoard/
│   ├── RestoreBoard/
│   ├── DeleteBoard/
│   ├── DuplicateBoard/
│   └── MoveBoardToSpace/
├── Queries/
│   ├── GetBoard/
│   ├── GetWorkspaceBoards/
│   ├── GetBoardOverview/
│   └── GetRecentBoards/
├── DTOs/
├── Mapping/
├── Permissions/
└── Cache/
```

Rules:

```txt
- CreateBoard check CreateBoard permission + board quota entitlement.
- Board visibility change phải invalidate permission/cache/search if needed.
- Archive/Restore/Delete phải audit/activity.
- DuplicateBoard nên async nếu board lớn.
- Board DTO không include toàn bộ items.
```

CreateBoard flow:

```txt
Authorization: CreateBoard on workspace/space
Entitlement: Boards limit
Transaction
Create Board aggregate
Create default groups/fields/views nếu nghiệp vụ yêu cầu
Grant owner/manager permission nếu cần
SaveChanges
Outbox: BoardCreatedIntegrationEvent
Invalidate workspace boards cache
Return CreateBoardResult
```

---

### 10.5. Module: BoardSchema

```txt
Features/WorkManagement/BoardSchema/
├── Queries/
│   ├── GetBoardSchema/
│   ├── GetBoardSchemaForView/
│   └── GetBoardSchemaVersion/
├── DTOs/
│   ├── BoardSchemaDto.cs
│   ├── BoardFieldDto.cs
│   ├── BoardGroupDto.cs
│   ├── BoardViewDto.cs
│   └── EffectiveBoardPermissionsDto.cs
├── Services/
│   ├── BoardSchemaAssembler.cs
│   └── BoardSchemaCachePolicy.cs
└── Cache/
```

Rules:

```txt
- BoardSchema là query/read model module, không phải Domain aggregate riêng.
- BoardSchema response gồm board summary, fields, options, groups, views, effective permission, feature flags.
- Cache key phải có boardId + userId/permission version.
- Invalidate khi field/group/view/permission/visibility thay đổi.
```

---

### 10.6. Module: BoardFields

```txt
Features/WorkManagement/BoardFields/
├── Commands/
│   ├── CreateBoardField/
│   ├── RenameBoardField/
│   ├── UpdateBoardFieldSettings/
│   ├── UpdateFieldOptions/
│   ├── DeleteBoardField/
│   ├── RestoreBoardField/
│   └── ReorderBoardFields/
├── Queries/
│   ├── GetBoardFields/
│   └── GetBoardFieldUsage/
├── DTOs/
├── Services/
│   ├── FieldSettingsApplicationValidator.cs
│   ├── FieldOptionUsageChecker.cs
│   └── FieldTypeCompatibilityChecker.cs
└── Cache/
```

Rules:

```txt
- FieldType là enum cứng.
- Status/priority/options là data trong field options, không hard-code trong BoardItem.
- Delete field phải check usage nếu policy không cho delete field đang dùng.
- Update settings phải validate backward compatibility.
- Reorder fields dùng fractional index hoặc ordered position strategy.
```

---

### 10.7. Module: BoardGroups

```txt
Features/WorkManagement/BoardGroups/
├── Commands/
│   ├── CreateBoardGroup/
│   ├── RenameBoardGroup/
│   ├── ArchiveBoardGroup/
│   ├── RestoreBoardGroup/
│   ├── DeleteBoardGroup/
│   └── ReorderBoardGroups/
├── Queries/
│   └── GetBoardGroups/
├── DTOs/
└── Cache/
```

Rules:

```txt
- Không delete default group nếu còn item hoặc nếu board policy cấm.
- Move item giữa group thuộc BoardItems module, không nằm trong BoardGroups.
- Kanban column không nên nhầm với BoardGroup; Kanban column đến từ board view config + status/select field.
```

---

### 10.8. Module: BoardItems

```txt
Features/WorkManagement/BoardItems/
├── Commands/
│   ├── CreateBoardItem/
│   ├── UpdateBoardItemName/
│   ├── UpdateBoardItemDescription/
│   ├── UpdateBoardItemFieldValue/
│   ├── BatchUpdateBoardItemFieldValues/
│   ├── MoveBoardItem/
│   ├── ReorderBoardItems/
│   ├── AssignBoardItem/
│   ├── UnassignBoardItem/
│   ├── ArchiveBoardItem/
│   ├── RestoreBoardItem/
│   ├── DeleteBoardItem/
│   └── DuplicateBoardItem/
├── Queries/
│   ├── GetBoardItem/
│   ├── GetBoardItemDetail/
│   ├── GetBoardItems/
│   ├── GetBoardItemsByView/
│   └── GetMyAssignedItems/
├── DTOs/
├── ReadModels/
│   ├── BoardItemListReadModel.cs
│   └── BoardItemDetailReadModel.cs
├── Services/
│   ├── BoardItemHierarchyService.cs
│   ├── BoardItemFieldValueWriter.cs
│   └── BoardItemViewQueryBuilder.cs
└── Cache/
```

Rules:

```txt
- Board item list bắt buộc cursor pagination.
- Query list không load aggregate full.
- UpdateFieldValue phải validate BoardItem + BoardField cùng workspace/board.
- Update People/Relation field phải validate target user/item permission.
- AssignParentItem phải chống cycle bằng Application Domain Service/read query.
- Batch update phải atomic hoặc có policy partial failure rõ.
- Commands mutate item phải dùng ExpectedVersion nếu UI collaborative.
```

UpdateBoardItemFieldValue flow:

```txt
Authorization: UpdateItem on board/item
Entitlement: optional if field type/feature gated
Transaction
Load item + field
Validate workspace/board match
Validate field settings/value type/target permission
Domain: item.UpdateFieldValue(...)
Persist typed value/read value if needed
SaveChanges
Outbox: BoardItemFieldValueChanged
Invalidate item/detail/view/search/reporting cache
Return updated cell/item patch DTO
```

MoveBoardItem flow:

```txt
Authorization: UpdateItem
Load item + target group/view context
If table group move:
  update groupId + position
If Kanban move:
  compile view config
  update field value representing target column
SaveChanges
Outbox: BoardItemMoved/BoardItemFieldValueChanged if Kanban changes field value
```

---

### 10.9. Module: BoardViews

```txt
Features/WorkManagement/BoardViews/
├── Commands/
│   ├── CreateBoardView/
│   ├── RenameBoardView/
│   ├── UpdateBoardViewConfig/
│   ├── ChangeBoardViewVisibility/
│   ├── DeleteBoardView/
│   ├── DuplicateBoardView/
│   ├── SetDefaultBoardView/
│   └── ReorderBoardViews/
├── Queries/
│   ├── GetBoardViews/
│   └── GetBoardView/
├── DTOs/
├── Services/
│   ├── BoardViewConfigValidator.cs
│   ├── BoardViewQueryCompiler.cs
│   └── BoardViewPermissionFilter.cs
└── Cache/
```

Rules:

```txt
- BoardView chỉ lưu config, không lưu item data riêng.
- Calendar/Timeline/Kanban/Gantt config phải validate field tồn tại và đúng type.
- View query phải permission-aware.
- Update view config invalidate board schema/view cache.
```

---

### 10.10. Module: Checklists

```txt
Features/WorkManagement/Checklists/
├── Commands/
│   ├── CreateChecklist/
│   ├── RenameChecklist/
│   ├── AddChecklistItem/
│   ├── CompleteChecklistItem/
│   ├── ReopenChecklistItem/
│   ├── ReorderChecklistItems/
│   └── DeleteChecklist/
├── Queries/
│   └── GetItemChecklists/
└── DTOs/
```

Rules:

```txt
- Checklist permission follows parent item permission.
- Completing checklist item can emit activity but should not over-trigger automation unless configured.
```

---

### 10.11. Module: Labels

```txt
Features/WorkManagement/Labels/
├── Commands/
│   ├── CreateLabel/
│   ├── RenameLabel/
│   ├── UpdateLabelColor/
│   ├── DeleteLabel/
│   └── AssignLabelToItem/
├── Queries/
│   └── GetBoardLabels/
└── DTOs/
```

Rules:

```txt
- Label thuộc board/workspace.
- Deleting label should handle existing assignments by policy: prevent/delete assignment/soft delete.
```

---

### 10.12. Module: Forms

```txt
Features/WorkManagement/Forms/
├── Commands/
│   ├── CreateForm/
│   ├── UpdateFormSettings/
│   ├── PublishForm/
│   ├── CloseForm/
│   ├── AddFormQuestion/
│   ├── UpdateFormQuestion/
│   ├── ReorderFormQuestions/
│   └── SubmitForm/
├── Queries/
│   ├── GetForm/
│   ├── GetPublicForm/
│   └── GetFormSubmissions/
└── DTOs/
```

Rules:

```txt
- Public form read does not use normal workspace permission but must validate token/public settings.
- SubmitForm creates BoardItem or FormSubmission according to workflow.
- Form question field mapping must validate board field compatibility.
- Anti-spam/rate limit belongs API/Infrastructure but Application defines behavior need.
```

---

### 10.13. Module: Relations

```txt
Features/WorkManagement/Relations/
├── Commands/
│   ├── CreateBoardRelation/
│   ├── UpdateBoardRelation/
│   ├── DeleteBoardRelation/
│   ├── LinkItems/
│   └── UnlinkItems/
├── Queries/
│   ├── GetItemRelations/
│   └── GetBoardRelations/
└── DTOs/
```

Rules:

```txt
- Relation target board/item must be permission-aware.
- Cross-board relation requires both source and target access.
- Mirror/Rollup should be projection/read model, not direct synchronous heavy recompute for every query.
```

---

### 10.14. Module: Formulas

```txt
Features/WorkManagement/Formulas/
├── Commands/
│   ├── CreateFormulaField/
│   ├── UpdateFormulaExpression/
│   └── RecalculateFormulaField/
├── Queries/
│   ├── ValidateFormulaExpression/
│   └── GetFormulaDependencies/
└── DTOs/
```

Rules:

```txt
- Formula expression validation belongs Application service + parser, not handler if complex.
- Formula dependencies should be stored/projection for recalculation.
- Recalculate can be async job for large boards.
```

---

### 10.15. Module: Rollups

```txt
Features/WorkManagement/Rollups/
├── Commands/
│   ├── CreateRollupField/
│   ├── UpdateRollupConfig/
│   └── RecalculateRollup/
├── Queries/
│   └── GetRollupPreview/
└── DTOs/
```

Rules:

```txt
- Rollup requires relation source.
- Rollup must be permission-aware when aggregating related data.
- Large recalculation should run via background job/outbox.
```

---

### 10.16. Module: Workload

```txt
Features/WorkManagement/Workload/
├── Queries/
│   ├── GetTeamWorkload/
│   ├── GetUserWorkload/
│   └── GetWorkloadByBoard/
└── DTOs/
```

Rules:

```txt
- Workload is read/reporting style; use projection or optimized query.
- Must respect permission: user should not infer hidden private items.
```

---

### 10.17. Module: MyWork

```txt
Features/WorkManagement/MyWork/
├── Queries/
│   ├── GetMyTasks/
│   ├── GetMyUpcomingItems/
│   └── GetMyOverdueItems/
└── DTOs/
```

Rules:

```txt
- MyWork query must filter by assigned user + permission.
- Cursor pagination mandatory.
```

---

### 10.18. Module: BoardSearch

```txt
Features/WorkManagement/BoardSearch/
├── Queries/
│   ├── SearchBoardItems/
│   └── SearchBoardFields/
└── DTOs/
```

Rules:

```txt
- Search result must be permission-aware.
- Full-text indexing should be async via Search bounded context/outbox.
```

---

## 11. Bounded Context: Documents

### 11.1. Target structure

```txt
Features/Documents/
├── Pages/
│   ├── Commands/
│   │   ├── CreatePage/
│   │   ├── RenamePage/
│   │   ├── MovePage/
│   │   ├── ArchivePage/
│   │   ├── RestorePage/
│   │   └── DeletePage/
│   ├── Queries/
│   │   ├── GetPage/
│   │   ├── GetPageTree/
│   │   └── GetRecentPages/
│   └── DTOs/
│
├── Blocks/
│   ├── Commands/
│   │   ├── AddBlock/
│   │   ├── UpdateBlock/
│   │   ├── MoveBlock/
│   │   ├── DeleteBlock/
│   │   └── RestoreBlock/
│   ├── Queries/
│   │   └── GetPageBlocks/
│   └── DTOs/
│
├── Versions/
│   ├── Commands/
│   │   └── RestorePageVersion/
│   ├── Queries/
│   │   ├── GetPageVersions/
│   │   └── ComparePageVersions/
│   └── DTOs/
│
├── ResourceLinks/
│   ├── Commands/
│   │   ├── LinkPageToResource/
│   │   └── UnlinkPageFromResource/
│   ├── Queries/
│   │   └── GetPageResourceLinks/
│   └── DTOs/
│
├── Templates/
│   ├── Commands/
│   │   ├── CreatePageTemplate/
│   │   └── ApplyPageTemplate/
│   ├── Queries/
│   │   └── GetPageTemplates/
│   └── DTOs/
│
└── Export/
    ├── Commands/
    │   ├── ExportPageToPdf/
    │   └── ExportPageToMarkdown/
    ├── Queries/
    │   └── GetExportJob/
    └── DTOs/
```

### 11.2. Rules

```txt
- Page tree must not cycle.
- Block tree must not cycle.
- UpdateBlock requires UpdatePage permission.
- Resource links must filter target resource permission.
- Export should be async if heavy.
- Docs should not become Google Docs clone in early phase; keep bounded scope.
```

---

## 12. Bounded Context: Collaboration

### 12.1. Target structure

```txt
Features/Collaboration/
├── Comments/
│   ├── Commands/
│   │   ├── CreateComment/
│   │   ├── EditComment/
│   │   ├── DeleteComment/
│   │   └── ResolveCommentThread/
│   ├── Queries/
│   │   └── GetResourceComments/
│   └── DTOs/
│
├── Reactions/
│   ├── Commands/
│   │   ├── AddReaction/
│   │   └── RemoveReaction/
│   └── Queries/
│       └── GetResourceReactions/
│
├── Mentions/
│   ├── Queries/
│   │   └── SearchMentionableUsers/
│   └── Services/
│       └── MentionExtractor.cs
│
├── Notifications/
│   ├── Commands/
│   │   ├── MarkNotificationRead/
│   │   └── MarkAllNotificationsRead/
│   ├── Queries/
│   │   ├── GetMyNotifications/
│   │   └── GetUnreadNotificationCount/
│   └── DTOs/
│
├── Activity/
│   ├── Queries/
│   │   ├── GetResourceActivity/
│   │   └── GetWorkspaceActivity/
│   └── DTOs/
│
├── Attachments/
│   ├── Commands/
│   │   ├── CreateAttachmentUploadUrl/
│   │   ├── ConfirmAttachmentUpload/
│   │   └── DeleteAttachment/
│   ├── Queries/
│   │   └── GetResourceAttachments/
│   └── DTOs/
│
├── Watchers/
│   ├── Commands/
│   │   ├── WatchResource/
│   │   └── UnwatchResource/
│   ├── Queries/
│   │   └── GetResourceWatchers/
│   └── DTOs/
│
└── Presence/
    ├── Commands/
    │   └── UpdatePresence/
    └── Queries/
        └── GetResourcePresence/
```

### 12.2. Rules

```txt
- CreateComment requires View resource + Comment permission.
- Activity feed must be permission-aware.
- Notification delivery should be async worker/outbox.
- Attachment stores metadata/storage key only; binary upload via Infrastructure.
- Presence is ephemeral; do not force it into Domain aggregate if not needed.
- Mentions should verify mentioned users are visible/valid in workspace.
```

---

## 13. Bounded Context: Automation

### 13.1. Target structure

```txt
Features/Automation/
├── Rules/
│   ├── Commands/
│   │   ├── CreateAutomationRule/
│   │   ├── UpdateAutomationRule/
│   │   ├── EnableAutomationRule/
│   │   ├── DisableAutomationRule/
│   │   ├── DeleteAutomationRule/
│   │   └── TestAutomationRule/
│   ├── Queries/
│   │   ├── GetAutomationRule/
│   │   └── GetAutomationRules/
│   ├── DTOs/
│   └── Services/
│       └── AutomationDefinitionValidator.cs
│
├── Engine/
│   ├── Commands/
│   │   ├── TriggerAutomation/
│   │   └── ExecuteAutomationAction/
│   ├── Services/
│   │   ├── IAutomationRuleMatcher.cs
│   │   ├── IAutomationConditionEvaluator.cs
│   │   ├── IAutomationActionExecutor.cs
│   │   └── IAutomationExecutionService.cs
│   └── DTOs/
│
├── Executions/
│   ├── Commands/
│   │   ├── RetryAutomationExecution/
│   │   └── CancelAutomationExecution/
│   ├── Queries/
│   │   ├── GetAutomationExecution/
│   │   └── GetAutomationExecutions/
│   └── DTOs/
│
├── Scheduled/
│   ├── Commands/
│   │   ├── ScheduleAutomationJob/
│   │   └── CancelScheduledJob/
│   ├── Queries/
│   │   └── GetScheduledJobs/
│   └── DTOs/
│
└── Templates/
    ├── Queries/
    │   └── GetAutomationTemplates/
    └── DTOs/
```

### 13.2. Rules

```txt
- Automation does not execute external side effects in Domain.
- Automation worker calls Application Engine.
- Execution must be idempotent by trigger event id + rule id.
- Rule matcher must be permission/entitlement-aware.
- Automation cannot bypass permission.
- Automation should not modify permissions in MVP unless explicit policy exists.
- Trigger/action definitions must be typed/config-validated, not raw uncontrolled string.
```

### 13.3. Critical flow: Outbox event to automation

```txt
Domain event occurs
Outbox stores integration/event contract
Outbox worker publishes event
Automation trigger listener receives event
AutomationRuleMatcher finds enabled rules
ConditionEvaluator validates conditions
ExecutionService creates execution record idempotently
ActionExecutor executes allowed action through Application command
Result stored and retried if transient failure
```

---

## 14. Bounded Context: Integrations

### 14.1. Target structure

```txt
Features/Integrations/
├── Connections/
│   ├── Commands/
│   │   ├── CreateIntegrationConnection/
│   │   ├── UpdateIntegrationConnection/
│   │   ├── DisableIntegrationConnection/
│   │   ├── RefreshIntegrationToken/
│   │   └── DeleteIntegrationConnection/
│   ├── Queries/
│   │   ├── GetIntegrationConnection/
│   │   └── GetIntegrationConnections/
│   └── DTOs/
│
├── Webhooks/
│   ├── Commands/
│   │   ├── CreateWebhookSubscription/
│   │   ├── UpdateWebhookSubscription/
│   │   ├── DisableWebhookSubscription/
│   │   ├── RotateWebhookSecret/
│   │   └── DispatchWebhookDelivery/
│   ├── Queries/
│   │   ├── GetWebhookSubscriptions/
│   │   └── GetWebhookDeliveries/
│   └── DTOs/
│
├── Providers/
│   ├── Queries/
│   │   └── GetAvailableIntegrationProviders/
│   └── DTOs/
│
├── Sync/
│   ├── Commands/
│   │   ├── StartIntegrationSync/
│   │   ├── CancelIntegrationSync/
│   │   └── RetryIntegrationSync/
│   ├── Queries/
│   │   └── GetIntegrationSyncStatus/
│   └── DTOs/
│
└── Inbound/
    ├── Commands/
    │   └── ProcessInboundWebhook/
    └── DTOs/
```

### 14.2. Rules

```txt
- Credentials use SecretRef; never plain text.
- Webhook delivery must be signed.
- Webhook delivery retry via worker/outbox.
- Inbound webhook must be idempotent.
- Provider-specific SDK stays Infrastructure.
- Application defines workflow and interfaces.
```

---

## 15. Bounded Context: Billing

### 15.1. Target structure

```txt
Features/Billing/
├── Plans/
│   ├── Commands/
│   │   ├── CreatePlan/
│   │   ├── UpdatePlan/
│   │   └── ArchivePlan/
│   ├── Queries/
│   │   └── GetPlans/
│   └── DTOs/
│
├── Subscriptions/
│   ├── Commands/
│   │   ├── CreateSubscription/
│   │   ├── ChangePlan/
│   │   ├── CancelSubscription/
│   │   └── ReactivateSubscription/
│   ├── Queries/
│   │   ├── GetWorkspaceSubscription/
│   │   └── GetSubscriptionHistory/
│   └── DTOs/
│
├── Entitlements/
│   ├── Commands/
│   │   ├── GrantEntitlement/
│   │   ├── RevokeEntitlement/
│   │   └── RecalculateEntitlements/
│   ├── Queries/
│   │   ├── GetWorkspaceEntitlements/
│   │   └── CheckFeatureEntitlement/
│   └── DTOs/
│
├── Usage/
│   ├── Commands/
│   │   ├── ConsumeFeatureUsage/
│   │   ├── ReleaseFeatureUsage/
│   │   └── ReconcileWorkspaceUsage/
│   ├── Queries/
│   │   ├── GetWorkspaceUsage/
│   │   └── GetUsageLedger/
│   └── DTOs/
│
├── Invoices/
│   ├── Queries/
│   │   ├── GetInvoices/
│   │   └── GetInvoiceDetail/
│   └── DTOs/
│
├── Payments/
│   ├── Commands/
│   │   └── RetryPayment/
│   ├── Queries/
│   │   └── GetPaymentMethods/
│   └── DTOs/
│
└── Webhooks/
    ├── Commands/
    │   └── ProcessBillingWebhook/
    └── DTOs/
```

### 15.2. Rules

```txt
- Core modules ask IEntitlementChecker; they do not know billing provider.
- Billing webhook must be idempotent.
- Usage writes should be append-friendly where possible.
- Quota check must happen before creating resource.
- Reconciliation jobs compare actual counts vs usage ledger/snapshot.
```

### 15.3. EntitlementBehavior

Example:

```txt
CreateBoardCommand implements IRequireEntitlement(FeatureCode.Boards, 1)
EntitlementBehavior checks plan limit and current usage
Handler creates board
After successful save, usage is consumed or outbox schedules usage update depending policy
```

---

## 16. Bounded Context: Analytics / Reporting

### 16.1. Target structure

```txt
Features/Analytics/
├── Dashboards/
│   ├── Commands/
│   │   ├── CreateDashboard/
│   │   ├── UpdateDashboard/
│   │   ├── DeleteDashboard/
│   │   └── ShareDashboard/
│   ├── Queries/
│   │   ├── GetDashboard/
│   │   └── GetDashboards/
│   └── DTOs/
│
├── Widgets/
│   ├── Commands/
│   │   ├── AddDashboardWidget/
│   │   ├── UpdateDashboardWidget/
│   │   ├── RemoveDashboardWidget/
│   │   └── ReorderDashboardWidgets/
│   ├── Queries/
│   │   ├── GetWidgetData/
│   │   └── PreviewWidgetQuery/
│   └── DTOs/
│
├── Snapshots/
│   ├── Commands/
│   │   ├── CreateReportingSnapshot/
│   │   └── RebuildReportingSnapshot/
│   ├── Queries/
│   │   └── GetReportingSnapshotStatus/
│   └── DTOs/
│
└── Metrics/
    ├── Queries/
    │   ├── GetWorkspaceMetrics/
    │   └── GetBoardMetrics/
    └── DTOs/
```

### 16.2. Rules

```txt
- Reporting query must not scan huge board_item_values repeatedly for dashboard refresh.
- Use snapshots/projections for expensive aggregation.
- Reporting must be permission-aware.
- Widget config validator should validate source board/view/field permission.
```

---

## 17. Bounded Context: Search

### 17.1. Target structure

```txt
Features/Search/
├── GlobalSearch/
│   ├── Queries/
│   │   └── SearchGlobal/
│   └── DTOs/
│
├── BoardSearch/
│   ├── Queries/
│   │   └── SearchBoard/
│   └── DTOs/
│
├── Indexing/
│   ├── Commands/
│   │   ├── RequestSearchReindex/
│   │   ├── ReindexResource/
│   │   └── RebuildWorkspaceSearchIndex/
│   ├── Queries/
│   │   └── GetSearchIndexJobStatus/
│   └── DTOs/
│
└── Permissions/
    ├── Services/
    │   └── SearchPermissionFilter.cs
    └── DTOs/
```

### 17.2. Rules

```txt
- Search result must be permission-filtered before returning.
- Indexing should be async via outbox/search jobs.
- Do not expose hidden resource title/snippet through search.
- Global search can use provider abstraction; Application should not know Elastic/Meilisearch/Postgres implementation.
```

---

## 18. Bounded Context: Operations

### 18.1. Target structure

```txt
Features/Operations/
├── ImportExport/
│   ├── Commands/
│   │   ├── ImportBoardFromCsv/
│   │   ├── ExportBoardToCsv/
│   │   ├── ExportWorkspaceData/
│   │   └── CancelExportJob/
│   ├── Queries/
│   │   └── GetImportExportJobStatus/
│   └── DTOs/
│
├── Jobs/
│   ├── Commands/
│   │   ├── RetryJob/
│   │   └── CancelJob/
│   ├── Queries/
│   │   ├── GetJob/
│   │   └── GetJobs/
│   └── DTOs/
│
├── Idempotency/
│   ├── Queries/
│   │   └── GetIdempotencyRecord/
│   └── DTOs/
│
└── Admin/
    ├── Queries/
    │   ├── GetSystemHealth/
    │   └── GetOutboxStatus/
    └── DTOs/
```

### 18.2. Rules

```txt
- Heavy import/export should be async.
- User-triggered export must respect permission and data boundary.
- Admin queries require system/admin permission.
- Idempotency records should not expose sensitive payload.
```

---

## 19. Transaction rules

```txt
- Command that mutates Domain state uses transaction.
- Query normally no transaction.
- TransactionBehavior wraps handler and SaveChanges.
- Domain event to outbox should be persisted in same transaction.
- External side effects must not run inside DB transaction.
- If external side effect needed, emit outbox/integration event.
```

Example command transaction:

```txt
CreateBoardCommand
  Validate
  Authorize
  Check entitlement
  Open transaction
  Create board/domain state
  SaveChanges + outbox messages
  Commit
  Return result
```

---

## 20. Permission rules in Application

### 20.1. Every request declares permission

```csharp
public sealed record CreateBoardCommand(
    Guid WorkspaceId,
    Guid? SpaceId,
    string Name)
    : ICommand<CreateBoardResult>,
      IWorkspaceRequest,
      IRequirePermission,
      ITransactionalRequest,
      IRequireEntitlement
{
    public PermissionAction Action => PermissionAction.BoardCreate;
    public ResourceRef Resource => ResourceRef.Workspace(WorkspaceId);
    public FeatureCode Feature => FeatureCode.Boards;
    public int Amount => 1;
}
```

### 20.2. Handler does not check role

Wrong:

```csharp
if (member.Role != WorkspaceRole.Admin) throw new ForbiddenException();
```

Right:

```txt
AuthorizationBehavior → IPermissionEvaluator.EnsureAllowedAsync(...)
```

### 20.3. Query must check permission

```txt
GetBoard             → ViewBoard
GetBoardItemsByView  → ViewBoard
GetPage              → ViewPage
GetAuditLogs         → ViewAuditLog
SearchGlobal         → Permission-aware filtering
```

---

## 21. Tenant/workspace rules

```txt
- WorkspaceId is mandatory for workspace-scoped requests.
- Never trust WorkspaceId from client until membership verified.
- API/Middleware resolves workspace, Application behavior verifies if needed.
- DbContext global query filter is not enough alone.
- Permission evaluator must verify membership status.
- Cross-workspace resource reference is invalid unless explicitly supported.
```

Fail-closed rule:

```txt
If request implements IWorkspaceRequest and CurrentWorkspace is not set or mismatched → reject.
```

---

## 22. Read model and query rules

```txt
- Query handler should return DTO, not entity.
- Use AsNoTracking for EF queries.
- Use projection select, not Include everything.
- Use read service for complex query.
- Cursor pagination for high-cardinality data.
- Search/reporting/activity should use projection/index when expensive.
```

High-cardinality endpoints:

```txt
GetBoardItems
GetBoardItemsByView
GetActivityFeed
SearchGlobal
GetAuditLogs
GetNotifications
```

must not use offset pagination as primary strategy at scale.

---

## 23. Cache rules

```txt
- Cache query results, not command results.
- Cache key must include workspaceId and user/permission dimension if permission affects output.
- Board schema cache must include boardId + userId/effective permission version.
- Invalidate on schema, permission, visibility, view config changes.
- Do not cache sensitive private data without permission-aware key.
```

Example keys:

```txt
board-schema:{workspaceId}:{boardId}:{userId}:{permissionVersion}
board-items-view:{workspaceId}:{boardId}:{viewId}:{userId}:{cursorHash}
workspace-home:{workspaceId}:{userId}
permission-effective:{workspaceId}:{resourceType}:{resourceId}:{userId}
```

---

## 24. Outbox/event rules

```txt
- DomainEvent belongs Domain.
- IntegrationEvent/Outbox contract belongs Application/Contracts.
- Not every DomainEvent must be public integration event.
- Cross-context/external side effects go through outbox.
- Email, webhook, search indexing, notification delivery, automation trigger must not run directly in handler.
- Outbox handler must be idempotent by EventId.
```

Recommended mapping:

```txt
BoardCreatedDomainEvent
  → BoardCreatedIntegrationEventV1
  → SearchReindexRequestedEventV1
  → ActivityLogRequestedEventV1
  → AutomationTriggerRequestedEventV1
```

Do not expose raw domain event shape as long-term external contract if it may change.

---

## 25. Idempotency rules

Use idempotency for:

```txt
- Create payment/subscription.
- Process billing webhook.
- Process inbound webhook.
- Create board/item from retryable client request if UI can retry.
- Execute automation action.
- Import/export job creation.
```

Rule:

```txt
Same idempotency key + same request fingerprint → return previous result.
Same key + different fingerprint → conflict.
```

---

## 26. Entitlement/quota rules

```txt
- Resource creation must check entitlement before mutation.
- Boards/items/members/storage/automations/integrations/AI usage should be feature-gated.
- Domain should not know Stripe/provider.
- Application uses IEntitlementChecker.
- Billing owns plan/usage/entitlement source.
```

Examples:

```txt
CreateBoard               → FeatureCode.Boards
InviteWorkspaceMember     → FeatureCode.Members
CreateAutomationRule      → FeatureCode.Automations
UploadAttachment          → FeatureCode.Storage
CreateIntegrationConnection → FeatureCode.Integrations
```

---

## 27. Audit and activity rules

Audit:

```txt
- Security/compliance/action record.
- Used for admin/security/history.
- Immutable or append-only.
```

Activity:

```txt
- User-facing feed.
- Permission-aware.
- Can be hidden from UI but not physically deleted if compliance requires history.
```

Rules:

```txt
- Permission change must audit.
- Billing/subscription change must audit.
- Workspace member role change must audit.
- Board/item visible changes can produce activity.
- Do not write activity manually in every handler if domain event/outbox projection can do it.
```

---

## 28. Validation rules

```txt
- Request shape validation in FluentValidation.
- Domain invariant validation in Domain methods.
- Cross-aggregate validation in Application service/domain service.
- External/provider validation in Infrastructure implementation.
```

Examples:

```txt
CreateBoardCommandValidator:
  name required, max length.

Board.Create:
  normalized name, valid board type/visibility.

CreateBoardHandler:
  workspace exists, user has permission, entitlement available.

Infrastructure:
  storage provider accepts file type/size if upload.
```

---

## 29. Concurrency rules

```txt
- Important commands should carry ExpectedVersion.
- EF uses aggregate Version as concurrency token.
- On concurrency conflict, return conflict error with current version if safe.
- UI collaborative edit should handle retry/refresh.
```

Examples:

```txt
RenameBoardCommand(ExpectedVersion)
UpdateBoardViewConfigCommand(ExpectedVersion)
UpdateBoardItemFieldValueCommand(ExpectedVersion optional depending cell-level strategy)
UpdatePageBlockCommand(ExpectedVersion)
```

---

## 30. Testing strategy

### 30.1. Test structure mirror Application

```txt
tests/Notrelix.Application.Tests/
  Features/
    WorkManagement/
      Boards/
        Commands/
          CreateBoardTests.cs
        Queries/
          GetBoardTests.cs
      BoardItems/
        Commands/
          UpdateBoardItemFieldValueTests.cs
```

### 30.2. Minimum tests per command

```txt
[ ] Validator rejects invalid input.
[ ] Permission denied case.
[ ] Workspace mismatch case.
[ ] Entitlement denied case if applicable.
[ ] Happy path mutates correct aggregate.
[ ] Domain event/outbox expected.
[ ] Cache invalidation expected if applicable.
[ ] Concurrency conflict if expected version used.
```

### 30.3. Minimum tests per query

```txt
[ ] Permission denied/private resource not leaked.
[ ] Workspace filter enforced.
[ ] Pagination works.
[ ] DTO shape correct.
[ ] Does not return soft-deleted resource.
[ ] Cache behavior if applicable.
```

---

## 31. Migration plan từ cấu trúc hiện tại sang module-first

### Phase 1: Foundation

```txt
- Add Common/CQRS contracts.
- Add marker interfaces.
- Add missing behaviors: Transaction, Idempotency, Entitlement, Cache, Exception mapping.
- Standardize IPermissionEvaluator API.
```

### Phase 2: Refactor WorkManagement first

Move:

```txt
Features/WorkManagement/Commands/Boards/*
Features/WorkManagement/Queries/Boards/*
```

to:

```txt
Features/WorkManagement/Boards/Commands/*
Features/WorkManagement/Boards/Queries/*
```

Repeat for:

```txt
BoardItems
BoardFields
BoardGroups
BoardViews
Checklists
Labels
```

### Phase 3: Split DTOs

```txt
WorkManagement/DTOs/BoardDto.cs      → WorkManagement/Boards/DTOs/BoardDto.cs
WorkManagement/DTOs/BoardItemDto.cs  → WorkManagement/BoardItems/DTOs/BoardItemDto.cs
Shared DTOs                          → WorkManagement/Common/DTOs
```

### Phase 4: Apply module-first to other bounded contexts

```txt
Governance: Permissions, ShareLinks, Roles, Policies
Automation: Rules, Engine, Executions, Scheduled, Templates
Billing: Plans, Subscriptions, Entitlements, Usage, Webhooks
Collaboration: Comments, Notifications, Activity, Attachments
Documents: Pages, Blocks, Versions, Export
```

### Phase 5: Add tests and guardrails

```txt
- Add architecture tests to forbid new files under Feature/Commands at context level for large BCs.
- Add handler tests.
- Add permission/tenant integration tests.
- Add outbox/idempotency tests.
```

---

## 32. Architecture tests nên có

Use ArchUnitNET or custom tests:

```txt
- Application must not reference Infrastructure.
- Application handlers must not return Domain entities.
- Commands must be under Features/{BC}/{Module}/Commands/{UseCase}.
- Queries must be under Features/{BC}/{Module}/Queries/{UseCase}.
- Command handlers must not call external provider interfaces directly unless through Application abstraction.
- Command handlers requiring workspace must implement IWorkspaceRequest.
- Handlers should not directly use role enum checks for authorization.
```

---

## 33. Definition of Done cho mỗi use case

```txt
[ ] Đặt đúng folder Feature/Module/Commands|Queries/UseCase.
[ ] Có Command hoặc Query rõ ràng.
[ ] Có Validator.
[ ] Có DTO/Result riêng, không return entity.
[ ] Có permission action.
[ ] Có workspace context nếu workspace-scoped.
[ ] Có entitlement nếu tạo/consume feature gated resource.
[ ] Có transaction nếu command mutate state.
[ ] Có idempotency nếu retryable/external.
[ ] Có expected version nếu update aggregate quan trọng.
[ ] Không chứa invariant lõi trong handler.
[ ] Không gọi Redis/S3/SMTP/SignalR trực tiếp.
[ ] Có outbox nếu side effect cross-context/external.
[ ] Có cache invalidation nếu thay đổi schema/view/permission/list.
[ ] Có audit/activity nếu nghiệp vụ yêu cầu.
[ ] Có tests permission/tenant/happy path/error path.
```

---

## 34. Coding agent prompt chuẩn để refactor

```txt
Refactor Notrelix.Application to Enterprise module-first vertical slice architecture.

Target pattern:
Features/{BoundedContext}/{Module}/Commands/{UseCase}
Features/{BoundedContext}/{Module}/Queries/{UseCase}
Features/{BoundedContext}/{Module}/DTOs
Features/{BoundedContext}/{Module}/Services
Features/{BoundedContext}/{Module}/ReadModels
Features/{BoundedContext}/Common for truly shared artifacts only.

Do not use:
Features/{BoundedContext}/Commands/{Module}
Features/{BoundedContext}/Queries/{Module}
for large bounded contexts such as WorkManagement, Governance, Automation, Billing, Collaboration, Documents.

Start with WorkManagement modules:
- Boards
- BoardItems
- BoardFields
- BoardGroups
- BoardViews
- Checklists
- Labels

Move files only first.
Do not change business logic during structural refactor.
Update namespaces and using statements.
Keep shared DTOs in WorkManagement/Common/DTOs only when used by more than one module.
Ensure the solution builds.
After structure is stable, add CQRS marker interfaces and pipeline behaviors.
```

---

## 35. Final recommendation

Notrelix should move from:

```txt
Feature → Commands/Queries → Module
```

to:

```txt
Feature → Module → Commands/Queries → UseCase
```

This is the right shape for a large SaaS product because bounded contexts like WorkManagement, Governance, Automation, Billing, and Collaboration will grow quickly. Module-first vertical slice keeps each business capability cohesive, makes reviews easier, prevents service sprawl, and prepares the modular monolith for future service extraction.

