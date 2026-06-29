# Notrelix Application Layer Enterprise Rules v4

> Phiên bản này bổ sung các rule triển khai thực tế cho tầng **Application** của Notrelix theo hướng **Enterprise-ready**, **Module-first Vertical Slice**, **Pragmatic CQRS**, và **đầy đủ interface ngay từ đầu**.
>
> Mục tiêu: Application layer có cấu trúc rõ, dễ mở rộng, dễ test, không bị phình thành service layer khổng lồ, không phá boundary của Domain/Infrastructure, và đủ nền để sau này tách service/projection/worker khi hệ thống lớn.

---

## 0. Quyết định kiến trúc chính

### 0.1. Application layer dùng Pragmatic CQRS

Notrelix nên dùng **Pragmatic CQRS**:

```txt
Command = thay đổi state
Query   = đọc dữ liệu / projection / DTO
```

Không dùng full CQRS/Event Sourcing ngay từ đầu.

```txt
Nên:
- Tách command/query ở code level.
- Command đi qua Domain aggregate.
- Query ưu tiên projection/read model.
- Dùng chung database ở giai đoạn modular monolith.
- Dùng outbox cho side effect cross-context/external.

Không nên:
- Tách read/write database quá sớm.
- Event sourcing toàn bộ hệ thống.
- Dùng message bus cho mọi operation nội bộ.
- Biến query thành load aggregate lớn rồi map DTO.
```

### 0.2. Application layer dùng Module-first Vertical Slice

Cấu trúc target:

```txt
Features/{BoundedContext}/{Module}/Commands/{UseCase}
Features/{BoundedContext}/{Module}/Queries/{UseCase}
Features/{BoundedContext}/{Module}/DTOs
Features/{BoundedContext}/{Module}/Services
Features/{BoundedContext}/{Module}/Mappings
```

Không dùng cấu trúc cũ làm target lâu dài:

```txt
Features/{BoundedContext}/Commands/{Module}/{UseCase}
Features/{BoundedContext}/Queries/{Module}/{UseCase}
Features/{BoundedContext}/DTOs
```

Cấu trúc cũ chấp nhận tạm thời trong giai đoạn refactor, nhưng use case mới nên theo module-first.

---

## 1. Vai trò chính xác của Application Layer

Application layer trả lời:

```txt
Người dùng muốn làm gì?
Use case thuộc bounded context nào?
Cần command hay query?
Cần validate gì?
Cần permission gì?
Cần workspace/tenant context không?
Cần transaction không?
Cần optimistic concurrency không?
Cần idempotency không?
Cần entitlement/quota không?
Cần audit/activity/cache/outbox/realtime không?
DTO trả ra API là gì?
```

Application layer không làm:

```txt
- Không chứa invariant lõi của Domain.
- Không copy Domain entity thành Application model.
- Không CRUD máy móc theo bảng.
- Không return EF entity/Domain aggregate.
- Không gọi Redis/S3/SMTP/SignalR/Stripe trực tiếp.
- Không tự viết SQL update bừa bãi cho aggregate mutable.
- Không để frontend tự lọc dữ liệu nhạy cảm.
- Không bypass PermissionEvaluator.
- Không bypass WorkspaceContext.
```

Layer boundary:

```txt
Domain
  = nghiệp vụ lõi, aggregate, entity, value object, invariant, domain event

Application
  = use case orchestration, CQRS, validation, authorization, transaction, DTO, read model contract

Infrastructure
  = EF Core, PostgreSQL, Redis, Email, Storage, Realtime, Outbox worker, external provider

API
  = HTTP endpoint, request binding, authentication entrypoint, response mapping
```

---

## 2. Interface triển khai đầy đủ ngay từ đầu

Bạn muốn triển khai interface đầy đủ ngay. Điều này được chấp nhận, nhưng phải có rule để tránh dùng sai.

### 2.1. CQRS base interfaces

```csharp
public interface ICommand<out TResponse> : IRequest<TResponse> { }

public interface ICommand : IRequest<Unit> { }

public interface IQuery<out TResponse> : IRequest<TResponse> { }
```

Rule:

```txt
- Mọi command phải implement ICommand hoặc ICommand<TResponse>.
- Mọi query phải implement IQuery<TResponse>.
- Không dùng IRequest trực tiếp trong feature use case.
- IRequest chỉ được dùng ở Common nếu cần infrastructure/pipeline abstraction.
```

### 2.2. Workspace/Tenant marker

```csharp
public interface IWorkspaceRequest
{
    Guid WorkspaceId { get; }
}
```

Rule:

```txt
- Mọi command/query thao tác dữ liệu workspace-scoped phải implement IWorkspaceRequest.
- Nếu request có BoardId/ItemId/PageId nhưng không có WorkspaceId thì không đạt chuẩn.
- Application phải fail-closed nếu request cần workspace mà workspace context chưa set/không hợp lệ.
```

### 2.3. Permission marker

```csharp
public interface IRequirePermission
{
    PermissionAction Action { get; }
    ResourceRef Resource { get; }
}
```

Rule:

```txt
- Command nhạy cảm phải implement IRequirePermission.
- Query đọc dữ liệu private cũng phải implement IRequirePermission.
- Handler không được tự check role bằng if/else trừ contextual permission sau khi đã load dữ liệu.
- Không gọi trực tiếp bảng permission trong handler.
```

### 2.4. Transaction marker

```csharp
public interface ITransactionalRequest { }
```

Rule:

```txt
- Command mutate state phải implement ITransactionalRequest.
- Query không implement ITransactionalRequest.
- Command chỉ đọc dữ liệu không cần transaction thì phải có comment lý do.
```

### 2.5. Expected version marker

```csharp
public interface IExpectedVersionRequest
{
    long ExpectedVersion { get; }
}
```

Rule:

```txt
- Update/Delete/Archive/Restore aggregate chính nên có ExpectedVersion.
- Nếu command update Board, BoardItem, BoardField, Page, AutomationRule, Entitlement thì nên dùng ExpectedVersion.
- Không dùng ExpectedVersion cho append-only log/projection.
```

### 2.6. Idempotency marker

```csharp
public interface IIdempotentRequest
{
    string IdempotencyKey { get; }
}
```

Rule:

```txt
Bắt buộc cho:
- Payment/Billing webhook
- External integration callback
- Import/export job
- Bulk operation
- Command có thể retry từ client
- Automation execution action

Không bắt buộc cho:
- Query
- Local UI command đơn giản nếu frontend không retry tự động
```

### 2.7. Entitlement marker

```csharp
public interface IRequireEntitlement
{
    FeatureCode Feature { get; }
    int Amount { get; }
}
```

Rule:

```txt
Bắt buộc cho command tạo/tiêu thụ quota:
- CreateBoard
- CreateBoardItem nếu plan giới hạn item
- InviteWorkspaceMember
- CreateAutomationRule
- ExecuteAutomation
- UploadAttachment
- CreateIntegrationConnection
- CreateDashboard/Report
```

### 2.8. Cache marker

```csharp
public interface ICacheableQuery<TResponse> : IQuery<TResponse>
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
- Query cache chỉ dùng cho dữ liệu ít thay đổi hoặc có invalidation rõ.
- Không cache query chứa permission cá nhân nếu cache key không có userId/effective permission version.
- Board schema cache key phải bao gồm boardId + userId hoặc permission version.
```

### 2.9. Audit/Activity marker

```csharp
public interface IAuditableRequest
{
    string AuditAction { get; }
    ResourceRef Resource { get; }
}

public interface IActivityRequest
{
    string ActivityType { get; }
    ResourceRef Resource { get; }
}
```

Rule:

```txt
- Audit dành cho bảo mật/quản trị/compliance.
- Activity dành cho user-facing feed.
- Không mọi command đều phải activity.
- Không tạo activity cho internal technical mutation.
```

### 2.10. Realtime marker

```csharp
public interface IRealtimeRequest
{
    RealtimeTopic Topic { get; }
}
```

Rule:

```txt
- Realtime không được gọi trực tiếp từ handler.
- Realtime nên đi qua outbox/event handler.
- Marker chỉ dùng để declare intent nếu thật sự cần.
```

---

## 3. Pipeline Behaviors chuẩn Enterprise

### 3.1. Command pipeline

Thứ tự đề xuất:

```txt
Command
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
ConcurrencyBehavior
  ↓
TransactionBehavior
  ↓
Handler
  ↓
CacheInvalidationBehavior
  ↓
ExceptionMappingBehavior
  ↓
Response
```

Rule:

```txt
- Validation chạy trước authorization.
- Workspace context phải được resolve trước authorization.
- Authorization chạy trước transaction để tránh mở transaction không cần thiết.
- Idempotency nên chạy trước transaction với command external retry.
- Entitlement nên chạy trước handler, nhưng consume usage phải nằm trong transaction.
- Transaction chỉ áp dụng command mutate state.
- Cache invalidation sau handler/save thành công.
```

### 3.2. Query pipeline

```txt
Query
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
ExceptionMappingBehavior
  ↓
Response
```

Rule:

```txt
- Query không dùng transaction mặc định.
- Query phải permission-aware.
- Query workspace-scoped thiếu workspace context phải fail-closed.
- Query cache phải gắn với permission/user/workspace phù hợp.
```

---

## 4. Rule tránh over-engineering sai chỗ

### 4.1. Được triển khai interface đầy đủ, nhưng không bắt ép dùng tất cả

Rule:

```txt
- Interface có thể tồn tại sẵn trong Common.
- Use case chỉ implement marker thật sự cần.
- Không ép mọi command phải IAuditableRequest/IActivityRequest/IRealtimeRequest.
- Không ép mọi query phải ICacheableQuery.
- Không ép mọi command phải IIdempotentRequest.
```

### 4.2. Không tạo abstraction nếu chưa có use case thật

Dù interface nền được tạo sẵn, các service/read service cụ thể chỉ nên tạo khi có nhu cầu.

```txt
Tạo IBoardItemReadService khi:
- Query phức tạp
- Cần cursor pagination
- Cần raw SQL/projection
- Cần permission-aware filtering
- Query dùng lại nhiều nơi

Không tạo read service chỉ vì “cho đủ kiến trúc”.
```

### 4.3. Không biến Application thành “interface zoo”

Không nên tạo quá nhiều interface cùng ý nghĩa:

```txt
Không nên:
- IPermissionService
- IPermissionEvaluator
- IWorkspacePermissionService
- IBoardPermissionService
đều tự quyết định quyền riêng.

Nên:
- IPermissionEvaluator là source chính.
- Service khác nếu cần chỉ là wrapper hoặc read helper.
```

---

## 5. Rule Outbox và Event trong Application

### 5.1. Chốt trách nhiệm Outbox

Notrelix nên dùng quy tắc:

```txt
Domain aggregate phát DomainEvent.
Infrastructure DomainEventInterceptor collect DomainEvent.
Nếu event cần async/cross-context/external:
  map thành OutboxMessage.
OutboxDispatcher xử lý async.
Application handler không tự ghi outbox thủ công trừ use case đặc biệt.
```

### 5.2. Không dùng OutboxBehavior nếu đã có DomainEventInterceptor làm nguồn chính

Rule:

```txt
- Không để cả OutboxBehavior và DomainEventInterceptor cùng ghi outbox cho một event.
- Nếu giữ OutboxBehavior, nó chỉ dùng cho application-level integration request đặc biệt.
- Side effect nghiệp vụ chính nên đi qua DomainEvent + Outbox.
```

### 5.3. Phân loại event

```txt
DomainEvent:
- Xảy ra trong Domain.
- Tên: {Aggregate}{ActionPastTense}DomainEvent.
- Ví dụ: BoardCreatedDomainEvent.

IntegrationEvent:
- Contract public/cross-context/external.
- Có version.
- Ví dụ: BoardCreatedIntegrationEventV1 hoặc work.board.created.v1.

OutboxMessage:
- Bản ghi lưu event cần xử lý async.
- Không phải domain concept.
```

### 5.4. Mapping DomainEvent sang Outbox

Rule:

```txt
- Không nhất thiết mọi DomainEvent đều outbox.
- DomainEvent internal same-context có thể xử lý sync nếu nhẹ.
- Cross-context/external side effect phải outbox.
- Activity, Notification, Search, Automation, Webhook nên outbox.
```

### 5.5. Outbox idempotency

```txt
- Outbox handler phải idempotent theo EventId hoặc idempotency key.
- Webhook delivery phải retry được.
- Automation action phải chống chạy trùng.
- Billing event tuyệt đối phải idempotent.
```

---

## 6. Rule Authorization

### 6.1. Coarse-grained authorization trong Behavior

`AuthorizationBehavior` xử lý quyền cơ bản dựa trên marker `IRequirePermission`.

Ví dụ:

```txt
CreateBoard → CreateBoard trên Workspace
UpdateBoardItemFieldValue → UpdateItem trên Board
GetBoardItemsByView → ViewBoard trên Board
GetAuditLogs → ViewAuditLog trên Workspace
```

### 6.2. Contextual authorization trong Handler/Application Service

Một số quyền cần dữ liệu sau khi load.

Ví dụ:

```txt
Field-level permission:
- Behavior check UpdateItem trên Board.
- Handler load BoardField.
- Handler check user có được update field này không.

Private page/resource:
- Behavior check ViewPage nếu ResourceRef đủ.
- Handler vẫn phải verify page thuộc workspace và không bị deleted.
```

Rule:

```txt
- Contextual authorization không thay thế AuthorizationBehavior.
- Handler được check quyền bổ sung khi cần dữ liệu đã load.
- Không được dùng contextual check để bypass permission evaluator.
```

### 6.3. Deny by default

```txt
- Action không map được permission → deny.
- Resource không xác định → deny.
- Workspace context thiếu → deny.
- Membership inactive → deny.
- Permission conflict → Deny thắng Allow nếu rule engine hỗ trợ deny.
```

### 6.4. Query cũng phải permission-aware

```txt
- GetBoardSchema phải ViewBoard.
- GetBoardItemsByView phải ViewBoard và filter field nếu có field permission.
- Search phải filter theo View permission.
- Activity feed phải không lộ resource private.
- Reporting phải không aggregate dữ liệu user không được xem nếu report scoped theo permission.
```

---

## 7. Rule Workspace/Tenant Context

### 7.1. Workspace context là bắt buộc cho workspace-scoped request

```txt
- Request implement IWorkspaceRequest phải có WorkspaceId hợp lệ.
- WorkspaceId từ route/header/body phải nhất quán.
- Middleware/Behavior phải verify user là member hoặc có quyền truy cập workspace.
```

### 7.2. Fail-closed

```txt
Không được:
- Nếu CurrentWorkspace null thì query toàn hệ thống.
- Nếu header thiếu thì tự fallback workspace đầu tiên.
- Nếu user không là member thì vẫn set context.

Phải:
- Trả 403 hoặc 404 tùy security policy.
```

### 7.3. DbContext filter là safety net, không thay thế permission

```txt
EF global filter theo IWorkspaceScoped giúp chống query thiếu filter.
Nhưng Application vẫn phải:
- validate workspace,
- authorize user,
- check resource ownership.
```

### 7.4. Background job context

Background job không có HTTP context.

Rule:

```txt
- Outbox message phải chứa WorkspaceId nếu event workspace-scoped.
- Worker phải set workspace context từ message trước khi chạy handler nếu handler query workspace-scoped data.
- System job có thể ActorUserId null, nhưng WorkspaceId không được null nếu thao tác resource workspace.
```

---

## 8. Rule Transaction và Unit of Work

### 8.1. Command mutate state phải transaction

```txt
- Command create/update/delete/archive/restore aggregate phải ITransactionalRequest.
- Multiple aggregate changes trong một use case phải cùng transaction nếu cần atomic.
- Outbox message phải persist cùng transaction với aggregate change.
```

### 8.2. Không transaction cho query mặc định

```txt
- Query dùng AsNoTracking.
- Query nặng dùng read service/projection.
- Query cần consistency đặc biệt mới cân nhắc transaction read-only.
```

### 8.3. SaveChanges

Rule:

```txt
- Handler không gọi SaveChanges nhiều lần trong một command trừ khi có lý do rõ.
- TransactionBehavior hoặc UnitOfWork chịu trách nhiệm commit.
- Nếu handler cần flush giữa chừng phải comment lý do.
```

---

## 9. Rule Optimistic Concurrency

### 9.1. Aggregate mutable nên dùng ExpectedVersion

Bắt buộc hoặc rất nên dùng cho:

```txt
- Board
- BoardItem
- BoardField
- BoardView
- Page
- Block nếu là aggregate mutable
- AutomationRule
- IntegrationConnection
- Entitlement
- Subscription
```

### 9.2. Handler flow

```txt
1. Load aggregate.
2. Check aggregate.Version == request.ExpectedVersion.
3. Nếu mismatch → throw ConcurrencyConflictException.
4. Gọi Domain method.
5. SaveChanges.
```

### 9.3. Không dùng cho append-only

Không cần ExpectedVersion cho:

```txt
- ActivityLog
- SecurityEvent
- AuditLog
- OutboxMessage
- FeatureUsageLedger nếu append-only
```

---

## 10. Rule Validation

### 10.1. Validator không thay Domain invariant

Validator kiểm tra:

```txt
- Required field.
- Length.
- Format.
- Basic enum.
- Id không empty.
- Page size giới hạn.
- Request shape.
```

Domain kiểm tra:

```txt
- Invariant nghiệp vụ.
- Same workspace/board/resource.
- Transition hợp lệ.
- Không update deleted entity.
- Field type value validation nếu thuộc Domain.
```

### 10.2. Application validation có thể check existence nhẹ

Có thể check:

```txt
- BoardId tồn tại không.
- FieldId thuộc Board không.
- User có trong workspace không.
```

Nhưng nếu check cần nhiều query/context, nên để handler/application service.

---

## 11. Rule Read Model và Query

### 11.1. Query không load aggregate lớn

Không nên:

```txt
GetBoardItemsByView:
- load Board aggregate
- load Items aggregate
- load Fields
- map thủ công trong memory
```

Nên:

```txt
- AsNoTracking
- Projection DTO trực tiếp
- Cursor pagination
- Filter/sort ở database
- Permission-aware filter
```

### 11.2. Simple query vs complex query

```txt
Simple query:
- dùng IApplicationDbContext
- AsNoTracking
- Select DTO

Complex query:
- dùng read service
- raw SQL/projection/compiled query nếu cần
```

Complex query gồm:

```txt
- GetBoardItemsByView
- GetBoardSchema
- GlobalSearch
- ActivityFeed
- ReportingDashboard
- PermissionEffectiveMatrix
- WorkspaceHome
```

### 11.3. Cursor pagination

Bắt buộc cho:

```txt
- Board items
- Activity feed
- Audit logs
- Notifications
- Search results
- Users/members large list
```

Không dùng offset pagination cho board item lớn nếu có thể tránh.

### 11.4. Query DTO không return Domain entity

```txt
- Không return Board, BoardItem, Page, User entity.
- Return BoardDto, BoardItemRowDto, BoardSchemaDto, PageDetailDto.
- DTO phải chứa đúng dữ liệu màn hình cần, không dư private data.
```

---

## 12. Rule DTO

### 12.1. DTO đặt gần module

```txt
Features/WorkManagement/Boards/DTOs
Features/WorkManagement/BoardItems/DTOs
Features/Governance/Permissions/DTOs
```

### 12.2. DTO dùng chung thật sự mới đưa lên Common

```txt
Features/WorkManagement/Common/DTOs
Application/Common/Models
```

Rule:

```txt
- Không gom tất cả DTO vào BoundedContext/DTOs nếu module đã lớn.
- Không tạo DTO riêng cho mọi use case nếu giống nhau 95%.
- Không dùng một DTO khổng lồ cho mọi màn hình.
```

### 12.3. DTO không chứa secret/private fields

Không trả:

```txt
- password hash
- token hash
- secret ref raw
- internal metadata private
- deleted data nếu user không có quyền
- permission rules internal nếu API không cần
```

---

## 13. Rule Cache

### 13.1. Cache query phải permission-aware

Cache key nên chứa:

```txt
workspaceId
resourceId
userId hoặc permissionVersion
query params
locale/timezone nếu ảnh hưởng response
```

Ví dụ:

```txt
board-schema:{workspaceId}:{boardId}:{userId}:{permissionVersion}
board-items-view:{workspaceId}:{boardId}:{viewId}:{filterHash}:{cursor}
```

### 13.2. Invalidation rõ ràng

Board schema invalidated khi:

```txt
- Board renamed/visibility changed
- Field created/updated/deleted/restored/reordered
- Field options changed
- Group created/updated/deleted/reordered
- View created/updated/deleted/default changed
- Permission changed
```

Board item view invalidated khi:

```txt
- Item created/moved/deleted/restored
- Field value changed
- View config changed
- Permission changed
```

### 13.3. Không cache dữ liệu nhạy cảm nếu invalidation chưa chắc

```txt
- Search private resource
- Audit logs
- Permission matrix
- Security events
```

Chỉ cache khi có version/invalidation rõ.

---

## 14. Rule Activity, Audit, Notification, Realtime

### 14.1. Audit

Audit dành cho:

```txt
- Login/security change
- Permission change
- Billing change
- Workspace/member change
- Integration secret change
- Data export/import
- Admin action
```

Audit phải immutable.

### 14.2. Activity

Activity dành cho user-facing feed:

```txt
- Board created
- Item moved
- Field value changed
- Comment created
- Page updated
```

Không activity cho:

```txt
- internal sequence increment
- cache invalidation
- outbox retry
- technical projection update
```

### 14.3. Notification

Notification nên async qua outbox/worker.

Không gửi notification trực tiếp trong handler.

### 14.4. Realtime

Realtime nên publish từ event handler/outbox worker.

Không gọi SignalR trực tiếp từ command handler.

---

## 15. Rule Entitlement/Billing

### 15.1. Application chỉ hỏi entitlement, không biết provider

WorkManagement không biết Stripe/Paddle.

```txt
CreateBoard → IEntitlementChecker.Check(BoardFeature)
CreateAutomationRule → IEntitlementChecker.Check(AutomationFeature)
UploadAttachment → IEntitlementChecker.Check(StorageFeature)
```

### 15.2. Consume usage trong transaction

Nếu command tạo resource có quota:

```txt
1. Check entitlement.
2. Tạo resource.
3. Ghi usage ledger hoặc update usage.
4. Save cùng transaction.
```

### 15.3. Billing webhook idempotent

```txt
- Mọi billing webhook phải IIdempotentRequest.
- Không xử lý trùng event provider.
- Event provider raw payload nên lưu để audit/debug.
```

---

## 16. Rule Idempotency

### 16.1. Bắt buộc cho external/retry use case

```txt
- Payment webhook
- Integration inbound webhook
- Import job
- Export job
- Bulk operation
- Automation execution
- File upload finalize
```

### 16.2. Idempotency response

Rule:

```txt
- Same key + same payload → return same result.
- Same key + different payload → conflict.
- Key phải scoped theo user/workspace/provider.
```

---

## 17. Rule Module-first folder structure

### 17.1. Target structure

```txt
Features/
  WorkManagement/
    Boards/
      Commands/
        CreateBoard/
        RenameBoard/
      Queries/
        GetBoard/
      DTOs/
      Services/

    BoardItems/
      Commands/
      Queries/
      DTOs/
      Services/

    Common/
      DTOs/
      Services/
      Mappings/
```

### 17.2. Khi nào tạo folder use case riêng?

Tạo folder riêng nếu use case có từ 2 file trở lên:

```txt
- Command/Query
- Handler
- Validator
- Result/Dto
- Mapper
```

Nếu chỉ có query nhỏ, vẫn nên tạo folder riêng để nhất quán trong hệ thống lớn.

### 17.3. Không tạo service khổng lồ

Không nên:

```txt
BoardService
BoardItemService
WorkManagementService
```

nếu service đó chứa nhiều use case.

Có thể tạo domain/application service nhỏ:

```txt
BoardSchemaBuilder
BoardViewQueryCompiler
BoardItemFieldValueApplicationValidator
BoardItemHierarchyChecker
PermissionMatrixBuilder
```

Service phải có trách nhiệm hẹp.

---

## 18. Rule chi tiết theo Bounded Context

---

# 18.1. Identity

## Modules

```txt
Identity/
  Auth/
  Users/
  Profiles/
  Sessions/
  Credentials/
  OAuth/
  Security/
  SSO/
  ApiTokens/
```

## Rules

```txt
- Identity quản lý user/account/security credential.
- Không chứa workspace role.
- Không chứa board permission.
- Auth command không cần WorkspaceId trừ flow workspace invitation.
- Password hashing/JWT/Email provider nằm Infrastructure.
- Application chỉ gọi interface.
```

## Commands

```txt
Auth/Register
Auth/Login
Auth/RefreshToken
Auth/Logout
Auth/ForgotPassword
Auth/ResetPassword
Auth/VerifyEmail

Users/ChangeEmail
Users/UpdateUserStatus
Users/DeactivateUser

Profiles/UpdateProfile
Profiles/UpdateAvatar
Profiles/UpdatePreferences

Sessions/RevokeSession
Security/EnableMfa
Security/DisableMfa
ApiTokens/CreateApiToken
ApiTokens/RevokeApiToken
```

## Queries

```txt
Users/GetCurrentUser
Users/SearchUsers
Profiles/GetMyProfile
Sessions/ListMySessions
Security/GetSecuritySettings
ApiTokens/GetMyApiTokens
```

## Enterprise notes

```txt
- Login phải audit security event.
- Token/API token không trả hash.
- Mfa secret không expose raw sau setup.
- SearchUsers phải permission-aware nếu trong workspace.
```

---

# 18.2. Workspaces

## Modules

```txt
Workspaces/
  Workspaces/
  Members/
  Invitations/
  Spaces/
  Teams/
  Settings/
  WorkspaceHome/
```

## Rules

```txt
- Workspace là tenant boundary chính.
- Mọi module sau khi workspace-scoped phải có WorkspaceId.
- Không xóa/hạ quyền owner cuối cùng.
- Guest không được thấy toàn workspace mặc định.
- Team thuộc Workspaces; Governance chỉ dùng Team làm permission subject.
```

## Commands

```txt
Workspaces/CreateWorkspace
Workspaces/UpdateWorkspace
Workspaces/ArchiveWorkspace
Workspaces/RestoreWorkspace
Workspaces/TransferWorkspaceOwnership

Members/AddWorkspaceMember
Members/RemoveWorkspaceMember
Members/ChangeWorkspaceMemberRole
Members/SuspendWorkspaceMember
Members/ReactivateWorkspaceMember

Invitations/InviteWorkspaceMember
Invitations/AcceptWorkspaceInvitation
Invitations/RevokeWorkspaceInvitation
Invitations/ResendWorkspaceInvitation

Spaces/CreateSpace
Spaces/RenameSpace
Spaces/MoveSpace
Spaces/ArchiveSpace

Teams/CreateTeam
Teams/RenameTeam
Teams/AddTeamMember
Teams/RemoveTeamMember

Settings/UpdateWorkspaceSettings
Settings/UpdateWorkspaceBranding
```

## Queries

```txt
Workspaces/GetWorkspace
Workspaces/GetMyWorkspaces
Workspaces/GetWorkspaceOverview
Members/GetWorkspaceMembers
Members/SearchWorkspaceMembers
Spaces/GetSpaceTree
Teams/GetTeams
Settings/GetWorkspaceSettings
WorkspaceHome/GetWorkspaceHome
WorkspaceHome/GetRecentBoards
WorkspaceHome/GetRecentPages
WorkspaceHome/GetMyWorkspaceActivity
```

---

# 18.3. Governance

## Modules

```txt
Governance/
  Permissions/
  PermissionRules/
  ResourcePermissions/
  ShareLinks/
  Roles/
  Policies/
  AuditLogs/
  SecurityEvents/
```

## Rules

```txt
- IPermissionEvaluator là source chính.
- PermissionRule là source of truth dài hạn.
- ResourcePermission có thể là legacy/projection/ACL compatibility.
- Permission change phải audit.
- Permission cache phải invalidated.
- Private resource không có View permission nên trả 404 hoặc 403 theo policy.
```

## Commands

```txt
PermissionRules/CreatePermissionRule
PermissionRules/UpdatePermissionRule
PermissionRules/EnablePermissionRule
PermissionRules/DisablePermissionRule
PermissionRules/DeletePermissionRule

ResourcePermissions/GrantResourcePermission
ResourcePermissions/RevokeResourcePermission
ResourcePermissions/BulkGrantResourcePermissions

ShareLinks/CreateShareLink
ShareLinks/DisableShareLink
ShareLinks/RotateShareLink
ShareLinks/UpdateShareLinkExpiration

Roles/CreateCustomRole
Roles/UpdateCustomRole
Roles/DeleteCustomRole
Roles/AssignCustomRoleToMember

Policies/UpdateWorkspacePolicy
Policies/UpdateGuestAccessPolicy
```

## Queries

```txt
Permissions/EvaluatePermission
Permissions/GetEffectivePermissions
Permissions/ExplainPermissionDecision
ResourcePermissions/GetResourcePermissions
ShareLinks/GetResourceShareLinks
Roles/GetCustomRoles
Policies/GetWorkspacePolicies
AuditLogs/GetWorkspaceAuditLogs
AuditLogs/GetResourceAuditLogs
SecurityEvents/GetSecurityEvents
```

## Enterprise notes

```txt
- ExplainPermissionDecision chỉ cho admin/security role.
- AuditLogs dùng cursor pagination.
- Permission evaluator nên có cache nhưng invalidation phải chính xác.
```

---

# 18.4. WorkManagement

## Modules

```txt
WorkManagement/
  Boards/
  BoardSchema/
  BoardFields/
  BoardGroups/
  BoardItems/
  BoardViews/
  FieldOptions/
  Checklists/
  Labels/
  ItemLinks/
  Relations/
  Formulas/
  Rollups/
  Forms/
  Templates/
  Approvals/
  Workload/
  MyWork/
  BoardSearch/
```

## General rules

```txt
- WorkManagement là bounded context lớn nhất, bắt buộc module-first.
- Không gom tất cả Commands/Queries ở cấp WorkManagement lâu dài.
- Board item list bắt buộc cursor pagination.
- Board schema nên cache permission-aware.
- Field type validation phải thống nhất Domain/Application.
- Không để frontend tự filter item/field không có quyền.
```

## Boards

Commands:

```txt
Boards/CreateBoard
Boards/RenameBoard
Boards/UpdateBoardDescription
Boards/ArchiveBoard
Boards/RestoreBoard
Boards/DeleteBoard
Boards/DuplicateBoard
Boards/MoveBoardToSpace
Boards/ChangeBoardVisibility
Boards/SetDefaultBoardView
```

Queries:

```txt
Boards/GetBoard
Boards/GetWorkspaceBoards
Boards/GetBoardOverview
Boards/GetBoardSettings
```

Rules:

```txt
- CreateBoard phải check entitlement board quota.
- CreateBoard có thể tạo default group/view/field trong cùng transaction.
- Rename/Archive/Restore nên dùng ExpectedVersion.
- DuplicateBoard nên async nếu board lớn.
```

## BoardSchema

Queries:

```txt
BoardSchema/GetBoardSchema
BoardSchema/GetBoardSchemaForView
BoardSchema/GetBoardPermissions
```

Rules:

```txt
- BoardSchema là module riêng vì gom Board + Fields + Groups + Views + Permissions.
- Không nên load aggregate để build schema nếu query phức tạp.
- Cache key phải permission-aware.
```

## BoardFields

Commands:

```txt
BoardFields/CreateBoardField
BoardFields/RenameBoardField
BoardFields/UpdateBoardFieldSettings
BoardFields/DeleteBoardField
BoardFields/RestoreBoardField
BoardFields/ReorderBoardFields
BoardFields/UpdateFieldOptions
```

Queries:

```txt
BoardFields/GetBoardFields
BoardFields/GetBoardField
BoardFields/GetFieldOptions
```

Rules:

```txt
- FieldType là enum cứng.
- Status/priority/options là data trong field options.
- Update settings phải validate schema.
- Delete field phải kiểm tra usage hoặc soft delete.
```

## BoardItems

Commands:

```txt
BoardItems/CreateBoardItem
BoardItems/UpdateBoardItemName
BoardItems/UpdateBoardItemDescription
BoardItems/UpdateBoardItemFieldValue
BoardItems/BatchUpdateBoardItemFieldValues
BoardItems/MoveBoardItem
BoardItems/ReorderBoardItems
BoardItems/ArchiveBoardItem
BoardItems/RestoreBoardItem
BoardItems/DeleteBoardItem
BoardItems/AssignBoardItem
BoardItems/UnassignBoardItem
BoardItems/DuplicateBoardItem
BoardItems/AssignParentItem
BoardItems/SetTimeline
BoardItems/CompleteBoardItem
```

Queries:

```txt
BoardItems/GetBoardItem
BoardItems/GetBoardItemDetail
BoardItems/GetBoardItemsByView
BoardItems/GetMyAssignedItems
```

Rules:

```txt
- Update field value phải validate item/field cùng board/workspace.
- People field phải validate user thuộc workspace.
- Relation field phải validate target resource permission.
- Move item trong Kanban không đồng nghĩa move BoardGroup.
- AssignParentItem phải chống cycle bằng Application service.
- Batch update phải idempotent nếu client retry.
```

Critical flow:

```txt
UpdateBoardItemFieldValue:
1. Validate request.
2. Resolve workspace.
3. Check UpdateItem permission.
4. Load item + field.
5. Validate same workspace/board.
6. Validate field type/settings.
7. Contextual permission nếu field-level permission.
8. Domain item.UpdateFieldValue(...).
9. Upsert typed board_item_values nếu cần.
10. Save transaction.
11. Outbox: activity/search/automation/realtime.
12. Invalidate board item/view/schema cache nếu cần.
```

## BoardViews

Commands:

```txt
BoardViews/CreateBoardView
BoardViews/RenameBoardView
BoardViews/UpdateBoardViewConfig
BoardViews/DeleteBoardView
BoardViews/DuplicateBoardView
BoardViews/SetDefaultBoardView
BoardViews/ReorderBoardViews
BoardViews/ChangeBoardViewVisibility
```

Queries:

```txt
BoardViews/GetBoardViews
BoardViews/GetBoardView
```

Rules:

```txt
- BoardView chỉ lưu config.
- Không lưu item data riêng.
- Kanban/Calendar/Timeline phải validate field tồn tại và đúng type.
- View query compiler phải permission-aware.
```

## Forms

Commands:

```txt
Forms/CreateForm
Forms/UpdateForm
Forms/PublishForm
Forms/CloseForm
Forms/SubmitForm
```

Queries:

```txt
Forms/GetForm
Forms/GetPublicForm
Forms/GetFormSubmissions
```

Rules:

```txt
- Public form không được lộ board schema private.
- Submit form phải validate form status/published.
- Submit form có thể tạo BoardItem trong transaction.
```

---

# 18.5. Documents

## Modules

```txt
Documents/
  Pages/
  Blocks/
  Versions/
  ResourceLinks/
  Templates/
  Export/
```

## Rules

```txt
- Page tree không cycle.
- Block tree không cycle.
- UpdateBlock check UpdatePage permission.
- ResourceLinks phải permission-aware.
- Export nên async nếu nặng.
```

## Commands

```txt
Pages/CreatePage
Pages/RenamePage
Pages/MovePage
Pages/ArchivePage
Pages/RestorePage
Blocks/CreateBlock
Blocks/UpdateBlock
Blocks/MoveBlock
Blocks/DeleteBlock
ResourceLinks/CreateResourceLink
Templates/CreatePageTemplate
Export/RequestPageExport
```

## Queries

```txt
Pages/GetPage
Pages/GetPageTree
Blocks/GetPageBlocks
Versions/GetPageVersions
ResourceLinks/GetPageResourceLinks
Templates/GetPageTemplates
Export/GetExportJobStatus
```

---

# 18.6. Collaboration

## Modules

```txt
Collaboration/
  Comments/
  Reactions/
  Mentions/
  Notifications/
  Activity/
  Attachments/
  Watchers/
  Presence/
```

## Rules

```txt
- Comment requires View resource + Comment permission.
- Activity feed permission-aware.
- Notifications async.
- Attachments store metadata/storage key only.
- Presence is infrastructure/realtime concern, not Domain core.
```

## Commands

```txt
Comments/CreateComment
Comments/EditComment
Comments/DeleteComment
Reactions/AddReaction
Reactions/RemoveReaction
Attachments/UploadAttachmentMetadata
Watchers/WatchResource
Watchers/UnwatchResource
Notifications/MarkNotificationRead
```

## Queries

```txt
Comments/GetResourceComments
Activity/GetResourceActivity
Activity/GetWorkspaceActivity
Notifications/GetMyNotifications
Attachments/GetResourceAttachments
Watchers/GetResourceWatchers
```

---

# 18.7. Automation

## Modules

```txt
Automation/
  Rules/
  Executions/
  Engine/
  Scheduled/
  Templates/
  Triggers/
  Actions/
```

## Rules

```txt
- AutomationRule config phải typed hoặc JsonValue có schema.
- Automation không chạy external side effect trong Domain.
- Worker gọi Application Engine.
- Execution idempotent.
- Không cho automation thay permission trong MVP.
- Automation trigger từ outbox event.
```

## Commands

```txt
Rules/CreateAutomationRule
Rules/UpdateAutomationRule
Rules/EnableAutomationRule
Rules/DisableAutomationRule
Rules/DeleteAutomationRule
Executions/ExecuteAutomationRule
Executions/CancelAutomationExecution
Scheduled/CreateScheduledJob
Scheduled/DisableScheduledJob
```

## Queries

```txt
Rules/GetAutomationRules
Rules/GetAutomationRule
Executions/GetAutomationExecutions
Executions/GetAutomationExecutionDetail
Templates/GetAutomationTemplates
```

## Engine services

```txt
IAutomationRuleMatcher
IAutomationConditionEvaluator
IAutomationActionExecutor
IAutomationExecutionService
IAutomationContextBuilder
```

---

# 18.8. Integrations

## Modules

```txt
Integrations/
  Connections/
  Providers/
  Webhooks/
  Calendar/
  Sync/
  Secrets/
```

## Rules

```txt
- Credentials dùng SecretRef, không plain text.
- Webhook phải sign.
- Webhook delivery retry qua worker.
- Inbound webhook phải idempotent.
- External provider SDK nằm Infrastructure.
```

## Commands

```txt
Connections/CreateIntegrationConnection
Connections/RefreshIntegrationConnection
Connections/DisableIntegrationConnection
Connections/DeleteIntegrationConnection
Webhooks/CreateWebhookSubscription
Webhooks/DisableWebhookSubscription
Webhooks/HandleInboundWebhook
Sync/StartIntegrationSync
Sync/CancelIntegrationSync
```

## Queries

```txt
Connections/GetIntegrationConnections
Providers/GetIntegrationProviders
Webhooks/GetWebhookSubscriptions
Sync/GetSyncStatus
```

---

# 18.9. Billing

## Modules

```txt
Billing/
  Plans/
  Subscriptions/
  Entitlements/
  Usage/
  Invoices/
  Payments/
  Webhooks/
```

## Rules

```txt
- Billing provider không lộ vào WorkManagement.
- WorkManagement chỉ gọi entitlement checker.
- Billing webhook idempotent.
- Usage ledger nên append-only.
- Entitlement snapshot có thể cache nhưng invalidation phải rõ.
```

## Commands

```txt
Subscriptions/CreateSubscription
Subscriptions/CancelSubscription
Subscriptions/ChangePlan
Entitlements/GrantEntitlement
Entitlements/RevokeEntitlement
Usage/ConsumeFeatureUsage
Usage/ReconcileUsage
Webhooks/HandleBillingWebhook
```

## Queries

```txt
Plans/GetPlans
Subscriptions/GetCurrentSubscription
Entitlements/GetWorkspaceEntitlements
Usage/GetWorkspaceUsage
Invoices/GetInvoices
```

---

# 18.10. Reporting / Analytics

## Modules

```txt
Reporting/
  Dashboards/
  Widgets/
  Snapshots/
  Metrics/
  Exports/
```

## Rules

```txt
- Reporting không nên scan board_item_values liên tục.
- Dùng projection/snapshot cho dashboard nặng.
- Reporting phải permission-aware.
- Export report nên async.
```

## Commands

```txt
Dashboards/CreateDashboard
Dashboards/UpdateDashboard
Widgets/AddWidget
Widgets/UpdateWidget
Snapshots/RefreshSnapshot
Exports/RequestReportExport
```

## Queries

```txt
Dashboards/GetDashboard
Widgets/GetWidgetData
Metrics/GetWorkspaceMetrics
Snapshots/GetSnapshotStatus
Exports/GetReportExportStatus
```

---

# 18.11. Search

## Modules

```txt
Search/
  GlobalSearch/
  BoardSearch/
  Indexing/
  Permissions/
```

## Rules

```txt
- Search result phải permission-aware.
- Search indexing async qua outbox/job.
- Không trả resource user không được View.
- Search query phải giới hạn page size.
```

## Commands

```txt
Indexing/RequestReindexResource
Indexing/RebuildWorkspaceIndex
```

## Queries

```txt
GlobalSearch/SearchWorkspace
BoardSearch/SearchBoardItems
```

---

# 18.12. Operations

## Modules

```txt
Operations/
  ImportExport/
  Idempotency/
  Jobs/
  Health/
  AdminTools/
```

## Rules

```txt
- Operations không nhất thiết là Domain core.
- Import/export async nếu nặng.
- Admin tools phải audit.
- Job retry/idempotency do Infrastructure lưu trữ.
```

---

## 19. Architecture Tests bắt buộc

Nếu không có architecture tests, guide sẽ bị phá rất nhanh.

### 19.1. Dependency rules

```txt
- Application không reference Infrastructure.
- Domain không reference Application/Infrastructure.
- Infrastructure có thể reference Application + Domain.
- API có thể reference Application.
```

### 19.2. CQRS rules

```txt
- Class tên *Command phải implement ICommand hoặc ICommand<T>.
- Class tên *Query phải implement IQuery<T>.
- Handler command không return EF entity.
- Handler query không return Domain aggregate.
```

### 19.3. Folder rules

```txt
- Features/{BoundedContext}/{Module}/Commands/{UseCase}
- Features/{BoundedContext}/{Module}/Queries/{UseCase}
- Không thêm use case mới vào Features/{BoundedContext}/Commands trực tiếp.
```

### 19.4. Security rules

```txt
- Workspace-scoped command/query phải implement IWorkspaceRequest.
- Sensitive command/query phải implement IRequirePermission.
- Billing/external webhook phải implement IIdempotentRequest.
```

### 19.5. Infrastructure leakage rules

```txt
Handler không inject:
- DbContext concrete
- Redis concrete
- S3 client
- SMTP client
- SignalR HubContext
- Stripe client
- HttpClient provider cụ thể

Handler chỉ dùng Application abstractions.
```

---

## 20. Definition of Done cho mỗi use case

```txt
[ ] Đặt đúng bounded context/module.
[ ] Có Command hoặc Query rõ.
[ ] Implement đúng ICommand/IQuery.
[ ] Nếu workspace-scoped: implement IWorkspaceRequest.
[ ] Nếu cần permission: implement IRequirePermission.
[ ] Nếu mutate state: implement ITransactionalRequest.
[ ] Nếu update aggregate: có ExpectedVersion nếu phù hợp.
[ ] Nếu external/retry: implement IIdempotentRequest.
[ ] Nếu cần quota: implement IRequireEntitlement.
[ ] Có Validator.
[ ] Handler ngắn, chỉ orchestration.
[ ] Không chứa invariant lõi của Domain.
[ ] Không gọi Infrastructure concrete.
[ ] Không return EF/Domain entity.
[ ] Query dùng AsNoTracking/projection.
[ ] Query private permission-aware.
[ ] Có cache/invalidation nếu cần.
[ ] Có audit/activity/outbox nếu cần.
[ ] Có unit/integration test.
[ ] Có permission test.
[ ] Có tenant isolation test nếu workspace-scoped.
[ ] Có concurrency test nếu ExpectedVersion.
```

---

## 21. Migration plan từ cấu trúc hiện tại sang Module-first

### Phase 1: WorkManagement trước

Chuyển:

```txt
Features/WorkManagement/Commands/Boards/*
Features/WorkManagement/Queries/Boards/*
```

sang:

```txt
Features/WorkManagement/Boards/Commands/*
Features/WorkManagement/Boards/Queries/*
```

Làm trước cho:

```txt
- Boards
- BoardItems
- BoardFields
- BoardGroups
- BoardViews
- BoardSchema
```

### Phase 2: Governance

```txt
Governance/Permissions
Governance/PermissionRules
Governance/ShareLinks
Governance/Roles
Governance/AuditLogs
```

### Phase 3: Automation + Billing

```txt
Automation/Rules
Automation/Executions
Automation/Engine

Billing/Subscriptions
Billing/Entitlements
Billing/Usage
Billing/Webhooks
```

### Phase 4: Documents + Collaboration + Integrations

```txt
Documents/Pages
Documents/Blocks
Collaboration/Comments
Collaboration/Activity
Integrations/Connections
Integrations/Webhooks
```

---

## 22. Prompt cho coding agent

```txt
Refactor Notrelix.Application to Enterprise module-first vertical slice architecture.

Rules:
1. Use target structure:
   Features/{BoundedContext}/{Module}/Commands/{UseCase}
   Features/{BoundedContext}/{Module}/Queries/{UseCase}
   Features/{BoundedContext}/{Module}/DTOs

2. Do not place new use cases directly under:
   Features/{BoundedContext}/Commands
   Features/{BoundedContext}/Queries

3. Preserve business logic during refactor.
4. Update namespaces and using statements.
5. Do not change Domain logic.
6. Do not introduce Infrastructure dependency into Application.
7. Commands must implement ICommand/ICommand<T>.
8. Queries must implement IQuery<T>.
9. Workspace-scoped use cases must implement IWorkspaceRequest.
10. Sensitive use cases must implement IRequirePermission.
11. Mutating commands must implement ITransactionalRequest.
12. External/retry commands must implement IIdempotentRequest.
13. Keep DTOs close to module.
14. Shared DTOs only go to module Common or bounded-context Common when truly shared.
15. Ensure solution builds after each module refactor.
16. Add/update architecture tests.
```

---

## 23. Kết luận

Bản v4 này giữ quyết định của bạn: **triển khai đầy đủ interface ngay từ đầu**.

Tuy nhiên, để hệ thống không bị rối, các rule quan trọng là:

```txt
- Có interface đầy đủ nhưng use case chỉ implement marker cần thiết.
- Module-first là chuẩn target.
- Permission qua IPermissionEvaluator.
- Workspace fail-closed.
- Query permission-aware.
- Outbox không ghi trùng giữa behavior và interceptor.
- Handler không gọi Infrastructure concrete.
- Read service chỉ tạo khi query đủ phức tạp.
- Architecture tests bắt buộc để giữ chuẩn.
```

Target cuối cùng:

```txt
Application layer của Notrelix phải là use-case orchestration layer,
không phải CRUD service layer,
không phải nơi chứa business invariant,
không phải nơi gọi hạ tầng trực tiếp,
và không phải nơi return raw data thiếu kiểm soát.
```
