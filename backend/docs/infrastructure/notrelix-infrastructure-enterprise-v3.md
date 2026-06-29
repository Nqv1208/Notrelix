# Notrelix Infrastructure Layer — Final Enterprise Architecture Standard v3

> **Scope:** `backend/Notrelix.Infrastructure` cho Notrelix theo hướng Modular Monolith, DDD, Clean Architecture, PostgreSQL multi-schema, Outbox, Messaging, Redis, Worker, Search, Billing, Integrations.  
> **Mục tiêu:** Chốt kiến trúc Infrastructure rõ ràng, ổn định, có khả năng mở rộng nhưng không refactor cấu trúc layer liên tục.  
> **Trạng thái áp dụng:** Đây là **Final Standard** cho tầng Infrastructure. Các thay đổi sau này phải là mở rộng theo module/feature, không thay đổi lại cấu trúc nền nếu không có Architecture Decision Record rõ ràng.

---

## 0. Executive Decision

### 0.1 Kết luận cuối cùng

Tầng Infrastructure của Notrelix nên đi theo hướng:

```txt
Infrastructure = technical implementation layer
- EF Core / PostgreSQL multi-schema
- Outbox / Messaging / MassTransit
- Redis cache / distributed lock
- Storage / Email / Realtime
- Workers / background jobs
- Search / Reporting projections
- External integrations / billing providers
- Observability / health checks / metrics
```

Infrastructure **không phải nơi chứa business rule**, không quyết định invariant nghiệp vụ, không chứa permission policy lõi, không để Domain phụ thuộc EF/Redis/MassTransit/SignalR.

Dependency bắt buộc:

```txt
API → Application → Domain
API → Infrastructure
Infrastructure → Application → Domain
Domain → không phụ thuộc layer nào
```

### 0.2 Quyết định khóa kiến trúc

Các quyết định sau được xem là **đã chốt**:

```txt
1. Giữ một ApplicationDbContext trong modular monolith.
2. Giữ PostgreSQL multi-schema theo bounded context.
3. EF configurations chia theo schema/context ownership.
4. Outbox đặt trong Infrastructure/Data/Outbox và DB schema ops.
5. Outbox xử lý IntegrationEvent là chính, không persist toàn bộ DomainEvent.
6. IntegrationEvent dùng stable MessageName + SchemaVersion.
7. MassTransit in-memory chỉ là transport hiện tại, không phải reliability guarantee.
8. Redis dùng cho cache/lock/idempotency phụ trợ, DB vẫn là source of truth.
9. DependencyInjection phải chia module registration, không gom một file khổng lồ.
10. Không tạo GenericRepository cho mọi entity.
11. Query phức tạp dùng ReadModels/ReadServices.
12. Tenancy phải fail-closed.
13. Worker phải idempotent, có retry, có lock, có observability.
14. Không refactor folder structure liên tục sau khi áp dụng bản này.
```

---

## 1. Đánh giá khách quan kiến trúc Infrastructure v2 đã đề xuất

### 1.1 Điểm tốt

Kiến trúc v2 đúng ở các hướng chính:

| Điểm tốt | Đánh giá |
|---|---|
| Phân định rõ vai trò Infrastructure | Đúng. Infrastructure triển khai kỹ thuật, không chứa invariant nghiệp vụ. |
| Multi-schema PostgreSQL | Đúng với Modular Monolith và service-ready boundary. |
| EF configurations theo schema ownership | Đúng, giúp mapping rõ và tránh file cấu hình khổng lồ. |
| Outbox / worker / retry | Đúng, cần thiết cho enterprise async reliability. |
| Redis cache + invalidation | Đúng, nhưng phải có rule cache rõ. |
| ReadModels / Query Services | Đúng, cần cho BoardItems, Reporting, Search, Permission-aware queries. |
| Auth/Security/Storage/Email/Realtime tách concern | Đúng. |
| Observability/Health checks | Đúng, là phần bắt buộc cho production. |
| DI registration theo module | Đúng, giúp Infrastructure không thành file DI khổng lồ. |

### 1.2 Điểm chưa tốt của v2

Bản v2 vẫn còn vài điểm dễ khiến dự án tiếp tục refactor quá nhiều:

| Vấn đề | Vì sao chưa tốt | Quyết định chỉnh |
|---|---|---|
| Folder quá rộng, có cả `Persistence`, `Outbox`, `Operations` tách riêng song song với `Data` | Dễ làm trùng trách nhiệm giữa `Data/Outbox` và `Outbox/` | Chốt `Data` là persistence root. Outbox persistence nằm `Data/Outbox`; worker nằm `BackgroundJobs/Outbox`. |
| Đưa quá nhiều provider chưa cần ngay | Dễ tạo skeleton rỗng, làm dự án phình | Chỉ tạo folder/provider khi có implementation hoặc interface Application cần. |
| Có nguy cơ refactor theo “lý tưởng” thay vì theo nhu cầu | Dễ thay đổi cấu trúc liên tục | Chốt final folder structure, các module chưa dùng để `Reserved` trong file chuẩn, không bắt buộc tạo folder rỗng. |
| Outbox flow chưa đủ rõ giữa DomainEvent, DurableDomainEvent, IntegrationEvent | Dễ persist toàn bộ DomainEvent hoặc publish sync sai chỗ | Chốt 3-tier event model và Outbox chỉ persist IntegrationEvent + selected DurableDomainEvent. |
| Observability nêu đúng nhưng chưa có Definition of Done | Dễ bỏ qua khi triển khai | Bổ sung checklist health/metrics/log/backlog bắt buộc. |
| Cache rule chưa gắn với bounded context cụ thể | Dễ cache sai permission, board schema, item list | Bổ sung cache ownership và invalidation matrix. |

### 1.3 Kết luận về bản v2

```txt
Bản v2 đúng hướng Enterprise nhưng còn thiên về target architecture rộng.
Bản v3 này sẽ chốt lại thành kiến trúc ổn định, rõ folder, rõ chức năng, rõ rule, rõ thứ tự triển khai.
```

---

## 2. Current State Audit — Infrastructure hiện tại

### 2.1 Nhận định hiện tại

Infrastructure hiện tại đã có nền quan trọng:

```txt
Notrelix.Infrastructure/
├── BackgroundJobs/
├── Caching/
├── Data/
├── Email/
├── Identity/Services/
├── Jwt/
├── Messaging/
├── Middleware/
├── Otp/
├── RateLimit/
├── Services/
├── DependencyInjection.cs
└── Notrelix.Infrastructure.csproj
```

Đây là nền khá tốt cho giai đoạn Application core, nhưng chưa đạt Enterprise production-ready vì còn thiếu chuẩn hóa ở các phần:

```txt
- Outbox v2 / IntegrationEvent-centric dispatch.
- ProcessedEventStore cho consumer idempotency.
- EventTypeRegistry bằng stable MessageName + SchemaVersion.
- Tenant/RLS fail-closed.
- Modular DI registration.
- ReadModels infrastructure rõ.
- Cache invalidation rõ.
- Observability/health/backlog metrics.
```

### 2.2 Điểm đang tốt

| Khu vực | Điểm tốt |
|---|---|
| `Data/` | Đã có DbContext, configurations, migrations, interceptors, converters. |
| `BackgroundJobs/` | Đã có worker/outbox dispatcher/N8n/queued jobs. |
| `Messaging/` | Đã bắt đầu có MassTransit/integration event bus. |
| `Caching/` | Đã có Redis/caching foundation. |
| `Jwt`, `Email`, `RateLimit`, `Otp` | Đã có các concern hạ tầng cơ bản. |
| `Services/CurrentWorkspace` | Đã có bước đầu cho workspace context. |

### 2.3 Điểm cần chỉnh ngay

| Ưu tiên | Vấn đề | Hướng xử lý |
|---|---|---|
| P0 | Outbox còn domain-event centric | Chuyển sang IntegrationEvent-centric, MessageType/MessageName/SchemaVersion. |
| P0 | Event registry resolve bằng class name | Dùng `[EventName]` + schema version registry. |
| P0 | Consumer idempotency chưa rõ | Tạo `ops.processed_events`, unique `(event_id, consumer_name)`. |
| P0 | Tenant context chưa fail-closed | Workspace middleware + EF filter + optional DB session/RLS. |
| P0 | DependencyInjection quá lớn | Tách `DependencyInjection/*.cs` theo concern. |
| P1 | Read service/projection chưa rõ | Tạo `ReadModels/{Context}` cho query nặng. |
| P1 | Cache invalidation chưa có matrix | Chuẩn hóa `CacheKeys`, `CacheInvalidationService`. |
| P1 | Worker observability thiếu | Health check outbox backlog, worker heartbeat, retry/dead-letter metrics. |

---

## 3. Final Folder Structure — Không refactor tiếp nếu không có ADR

### 3.1 Cấu trúc chuẩn cuối cùng

```txt
Notrelix.Infrastructure/
├── Data/
│   ├── ApplicationDbContext.cs
│   ├── ApplicationDbContextFactory.cs
│   ├── DbSchemas.cs
│   ├── UnitOfWork.cs
│   ├── Configurations/
│   │   ├── Identity/
│   │   ├── Workspace/
│   │   ├── Governance/
│   │   ├── Work/
│   │   ├── Docs/
│   │   ├── Collab/
│   │   ├── Automation/
│   │   ├── Integration/
│   │   ├── Billing/
│   │   ├── Reporting/
│   │   ├── Search/
│   │   └── Ops/
│   ├── Converters/
│   ├── Interceptors/
│   ├── Migrations/
│   ├── Outbox/
│   │   ├── OutboxMessage.cs
│   │   ├── OutboxMessageType.cs
│   │   ├── OutboxMessageStatus.cs
│   │   ├── EventNameAttribute.cs
│   │   ├── EventTypeRegistry.cs
│   │   ├── OutboxSerializer.cs
│   │   └── OutboxOptions.cs
│   └── Ops/
│       ├── ProcessedEvent.cs
│       ├── IdempotencyRecord.cs
│       └── JobLock.cs
│
├── ReadModels/
│   ├── Work/
│   ├── Workspace/
│   ├── Governance/
│   ├── Docs/
│   ├── Collab/
│   ├── Billing/
│   ├── Reporting/
│   └── Search/
│
├── Caching/
│   ├── RedisCacheService.cs
│   ├── CacheKeys.cs
│   ├── CacheInvalidationService.cs
│   ├── PermissionCacheService.cs
│   ├── WorkspaceMembershipCacheService.cs
│   ├── BoardSchemaCacheService.cs
│   └── DistributedLockService.cs
│
├── Identity/
│   └── Services/
│
├── Jwt/
├── Otp/
├── RateLimit/
├── Security/
├── Email/
├── Storage/
├── Realtime/
├── Messaging/
│   ├── IntegrationEventBus.cs
│   ├── Consumers/
│   ├── ConsumerIdempotency/
│   │   └── ProcessedEventStore.cs
│   └── MassTransit/
│
├── BackgroundJobs/
│   ├── Outbox/
│   │   └── OutboxDispatcher.cs
│   ├── Automation/
│   ├── Notifications/
│   ├── Search/
│   ├── Webhooks/
│   ├── ImportExport/
│   └── Cleanup/
│
├── Integrations/
├── Billing/
├── Search/
├── Reporting/
├── Operations/
├── Observability/
│   ├── HealthChecks.cs
│   ├── OpenTelemetrySetup.cs
│   ├── MetricsService.cs
│   ├── LoggingEnricher.cs
│   └── WorkerHeartbeatService.cs
│
├── Middleware/
│   ├── WorkspaceResolutionMiddleware.cs
│   └── CorrelationIdMiddleware.cs
│
├── DependencyInjection/
│   ├── PersistenceRegistration.cs
│   ├── CacheRegistration.cs
│   ├── AuthRegistration.cs
│   ├── SecurityRegistration.cs
│   ├── EmailRegistration.cs
│   ├── StorageRegistration.cs
│   ├── RealtimeRegistration.cs
│   ├── MessagingRegistration.cs
│   ├── BackgroundJobsRegistration.cs
│   ├── IntegrationsRegistration.cs
│   ├── BillingRegistration.cs
│   ├── SearchRegistration.cs
│   ├── ReportingRegistration.cs
│   └── ObservabilityRegistration.cs
│
├── DependencyInjection.cs
└── GlobalUsings.cs
```

### 3.2 Quy tắc không refactor cấu trúc nữa

Sau khi áp dụng structure này:

```txt
Không đổi:
- Data là root của persistence.
- Data/Configurations chia theo schema.
- Data/Outbox chứa persistence model/serializer/registry của outbox.
- BackgroundJobs chứa worker/processors.
- Messaging chứa bus/consumer/idempotency.
- ReadModels chứa query service/projection reader.
- DependencyInjection chia theo concern.
```

Chỉ được thêm file/folder con khi:

```txt
1. Có Application interface cần Infrastructure implement.
2. Có provider thật sự được dùng.
3. Có background processor thật sự chạy.
4. Có read model/query phức tạp thật sự cần tách.
5. Có ADR nếu thay đổi root folder hoặc trách nhiệm layer.
```

---

## 4. Infrastructure Responsibility Rules

### 4.1 Infrastructure được làm

```txt
- Map Domain entity/value object vào database.
- Implement Application abstractions.
- Quản lý technical persistence: EF, transactions, migrations.
- Publish/dispatch IntegrationEvent qua outbox/bus.
- Implement cache, storage, email, realtime, external API client.
- Chạy worker/background jobs.
- Implement read models/query services.
- Implement security primitives: hash, encrypt, token generation, signing.
- Implement observability: health, metrics, logs, tracing.
```

### 4.2 Infrastructure không được làm

```txt
- Không chứa business invariant.
- Không quyết định permission rule lõi.
- Không gọi API controller.
- Không return HTTP response.
- Không để Domain biết EF/MassTransit/Redis/SignalR.
- Không mutate aggregate ngoài Application use case.
- Không dùng Infrastructure service để bypass Application authorization.
- Không publish event trực tiếp từ Domain.
```

---

## 5. Data / Persistence Standard

### 5.1 DbContext

Giữ một `ApplicationDbContext` cho modular monolith.

Lý do:

```txt
- Transaction cross-schema còn cần thiết.
- Domain vẫn chạy như một app modular monolith.
- Service split chưa phải mục tiêu hiện tại.
- Multi-schema đã đủ để phân ownership.
```

Rule:

```txt
- DbContext không chứa business logic.
- DbContext chỉ apply configurations, global filters, interceptors.
- DbContext không gọi external service.
- DbContext không publish event trực tiếp ra bus.
- DbContext không tự authorize.
```

### 5.2 Configurations

EF configurations bắt buộc chia theo schema:

```txt
Data/Configurations/Work/BoardConfiguration.cs
Data/Configurations/Billing/SubscriptionConfiguration.cs
Data/Configurations/Governance/PermissionRuleConfiguration.cs
```

Rule mapping:

```txt
- Table name dùng snake_case.
- Schema lấy từ DbSchemas.
- Enum dùng HasConversion<string>() nếu cần readability.
- JSONB dùng converter rõ ràng.
- ValueObject dùng converter hoặc owned type.
- AggregateRoot.Version là concurrency token.
- Soft delete filter dùng DeletedAt == null.
- IWorkspaceScoped filter phải fail-closed khi request yêu cầu workspace.
```

### 5.3 Migration

Rule migration:

```txt
- Không tạo migration phá toàn bộ DB nếu không cần.
- Không rename/drop destructive khi chưa có data migration.
- Mỗi migration phải có mục tiêu rõ.
- Migration lớn phải có smoke test.
- Schema drift check giữa SQL reference, EF configuration, migration là bắt buộc trước release.
```

---

## 6. Tenancy / Workspace Isolation Standard

### 6.1 Mục tiêu

Tenant/workspace isolation phải có 3 lớp:

```txt
1. Application: Command/Query có WorkspaceId rõ.
2. Infrastructure: EF global query filter cho IWorkspaceScoped.
3. Database: RLS/session variable nếu bật production hardening.
```

### 6.2 WorkspaceResolutionMiddleware

Middleware phải làm:

```txt
1. Đọc workspaceId từ route/header/subdomain tùy API convention.
2. Verify user là active member hoặc có system access.
3. Nếu hợp lệ: set ICurrentWorkspace.
4. Nếu không hợp lệ: trả 403 hoặc 404 theo policy.
5. Không cho client tự set X-Workspace-Id mà không verify.
```

### 6.3 Fail-closed rule

```txt
Nếu request là workspace-scoped mà CurrentWorkspace chưa set:
- Command: fail trước handler.
- Query: fail trước query.
- DbContext filter không được âm thầm bỏ workspace filter.
```

Ngoại lệ:

```txt
- Auth/register/login.
- GetMyWorkspaces.
- System/admin job có explicit system context.
```

---

## 7. Outbox / Messaging Final Standard

### 7.1 Event model chốt

```txt
LocalDomainEvent
- Xử lý local/sync hoặc không xử lý.
- Không persist outbox mặc định.

DurableDomainEvent
- Chỉ dùng khi có internal durable consumer rõ ràng.
- Outbox → MediatR.
- Hiện chưa cần tạo nếu chưa có use case.

IntegrationEvent
- Contract async consumer-driven.
- Luôn persist vào Outbox.
- Outbox → IIntegrationEventBus / MassTransit.
```

### 7.2 Flow chuẩn

```txt
Command Handler
→ Aggregate.DomainMethod()
→ DomainEvent(s)
→ SaveChanges
  → DomainEventInterceptor:
      1. Capture DomainEvents.
      2. Map DomainEvent → 0..N IntegrationEvents.
      3. Persist IntegrationEvents vào OutboxMessage cùng transaction.
      4. Optional persist DurableDomainEvent nếu có.
      5. Clear DomainEvents.
→ Commit DB
→ OutboxDispatcher:
      1. Claim Pending + Failed.
      2. Recover Processing timeout.
      3. Dispatch theo MessageType.
→ Consumers:
      1. Idempotent by (EventId, ConsumerName).
      2. Execute side effect.
```

### 7.3 Không publish LocalDomainEvent trong interceptor

Không làm:

```txt
DomainEventInterceptor → IMediator.Publish(LocalDomainEvent) trước/trong SaveChanges
```

Lý do:

```txt
- Dễ re-enter SaveChanges.
- Handler có thể query DB trước commit.
- Handler mutate thêm entity trong lúc EF đang save.
- Lỗi handler làm transaction flow khó đoán.
```

Nếu cần same-transaction behavior, xử lý trong Application handler hoặc Application service.

### 7.4 OutboxMessage v2

`OutboxMessage` phải có tối thiểu:

```txt
Id
EventId
SourceEventId nullable
MessageType: IntegrationEvent | DurableDomainEvent
MessageName
SchemaVersion
WorkspaceId nullable
ActorUserId nullable
CorrelationId nullable
CausationId nullable
PayloadJson
HeadersJson nullable
Status: Pending | Processing | Processed | Failed | DeadLetter
RetryCount
NextAttemptAt
LockedAt nullable
LockedBy nullable
ProcessedAt nullable
Error nullable
CreatedAt
```

Index bắt buộc:

```txt
(status, next_attempt_at, created_at)
(message_type, status, next_attempt_at)
(event_id)
(workspace_id, created_at)
```

### 7.5 Stable MessageName

Không dùng class `FullName` làm event type.

Dùng:

```txt
MessageName = work.board.created
SchemaVersion = 1
```

Class name có thể là:

```txt
BoardCreatedIntegrationEventV1
```

### 7.6 EventTypeRegistry

Registry resolve bằng:

```txt
(MessageName, SchemaVersion) → .NET Type
```

Không resolve bằng:

```txt
typeof(T).FullName
class Name
namespace scan ending with Event
```

### 7.7 ProcessedEventStore

Table:

```txt
ops.processed_events
- id
- event_id
- message_name
- schema_version
- consumer_name
- processed_at
- checksum nullable
```

Unique:

```txt
(event_id, consumer_name)
```

Rule:

```txt
- Consumer phải insert processed event trước hoặc trong transaction xử lý side effect.
- Nếu unique violation: skip, coi như already processed.
- Không unique chỉ theo event_id vì nhiều consumer cùng xử lý một event.
```

### 7.8 Consumer design

Không bắt buộc 1 IntegrationEvent = 1 Consumer.

Đúng hơn:

```txt
Consumer theo subsystem/side effect:
- NotificationConsumer
- SearchIndexingConsumer
- ActivityProjectionConsumer
- AutomationTriggerConsumer
- PermissionCacheInvalidationConsumer
- BillingProjectionConsumer
```

Một consumer có thể handle nhiều IntegrationEvent.

---

## 8. Dependency Injection Standard

### 8.1 Root file

`DependencyInjection.cs` chỉ còn orchestration:

```csharp
public static IServiceCollection AddInfrastructure(
    this IServiceCollection services,
    IConfiguration configuration)
{
    services.AddPersistence(configuration);
    services.AddCaching(configuration);
    services.AddAuthInfrastructure(configuration);
    services.AddSecurityInfrastructure(configuration);
    services.AddEmailInfrastructure(configuration);
    services.AddStorageInfrastructure(configuration);
    services.AddRealtimeInfrastructure(configuration);
    services.AddMessagingInfrastructure(configuration);
    services.AddBackgroundJobs(configuration);
    services.AddIntegrationsInfrastructure(configuration);
    services.AddBillingInfrastructure(configuration);
    services.AddSearchInfrastructure(configuration);
    services.AddReportingInfrastructure(configuration);
    services.AddObservability(configuration);
    return services;
}
```

### 8.2 Registration files

```txt
DependencyInjection/PersistenceRegistration.cs
DependencyInjection/CacheRegistration.cs
DependencyInjection/MessagingRegistration.cs
DependencyInjection/BackgroundJobsRegistration.cs
...
```

Rule:

```txt
- Không để một file DependencyInjection dài hàng trăm dòng.
- Mỗi concern tự đăng ký options/service/provider của nó.
- HostedService đăng ký trong BackgroundJobsRegistration.
- MassTransit đăng ký trong MessagingRegistration.
- EF/interceptors đăng ký trong PersistenceRegistration.
```

---

## 9. ReadModels / Query Infrastructure Standard

### 9.1 Khi nào dùng ReadModel service

Dùng ReadModel service khi:

```txt
- Query join nhiều bảng/schema.
- Query cần projection DTO lớn.
- Query cần raw SQL hoặc compiled query.
- Query cần cursor pagination.
- Query cần permission-aware filtering.
- Query phục vụ board item list, board schema, reporting, search, activity feed.
```

Không cần ReadModel service cho query đơn giản.

### 9.2 ReadModel ownership

```txt
ReadModels/Work/BoardSchemaReadService.cs
ReadModels/Work/BoardItemReadService.cs
ReadModels/Governance/EffectivePermissionReadService.cs
ReadModels/Search/SearchReadService.cs
ReadModels/Reporting/DashboardReadService.cs
```

Rule:

```txt
- Read service implement interface từ Application.
- Read service không chứa business invariant.
- Read service phải permission-aware hoặc nhận permission filter từ Application.
- Read service không return EF entity.
- Read service dùng AsNoTracking/projection.
```

---

## 10. Cache Standard

### 10.1 Cache được phép

```txt
- Workspace membership.
- Effective permissions.
- Board schema.
- Field options.
- Entitlements.
- Workspace navigation tree.
- Feature flags/config.
```

### 10.2 Cache không nên dùng sớm

```txt
- Board item list cực kỳ mutable nếu chưa có cursor/scope/invalidation.
- Audit logs.
- Search results nếu permission filter chưa chắc.
- Data có secret/token.
```

### 10.3 Invalidation matrix

| Event | Cache invalidation |
|---|---|
| WorkspaceMemberRoleChanged | workspace membership, effective permissions |
| ResourcePermissionGranted/Revoked | effective permissions, search visibility |
| BoardFieldCreated/Updated/Deleted | board schema, board view cache |
| BoardViewConfigChanged | board view cache |
| EntitlementGranted/Expired/Revoked | entitlement cache |
| BoardVisibilityChanged | permission/search visibility cache |

Rule:

```txt
Không cache permission nếu chưa có invalidation rõ.
```

---

## 11. Background Jobs Standard

### 11.1 Worker categories

```txt
BackgroundJobs/Outbox
BackgroundJobs/Automation
BackgroundJobs/Notifications
BackgroundJobs/Search
BackgroundJobs/Webhooks
BackgroundJobs/ImportExport
BackgroundJobs/Cleanup
```

### 11.2 Worker rules

```txt
- Worker phải idempotent.
- Worker phải có retry/backoff.
- Worker phải có lock nếu chạy multi-instance.
- Worker phải ghi heartbeat.
- Worker phải expose metrics/backlog.
- Worker không chứa business invariant.
- Worker gọi Application service/handler nếu cần chạy use case.
```

### 11.3 Outbox dispatcher rules

```txt
- Claim Pending + Failed.
- Recover stuck Processing.
- Mark Processing bằng locked_by + locked_at.
- Mark Processed chỉ sau khi dispatch/handler thành công theo semantics đã chọn.
- Failed tăng retry_count + next_attempt_at.
- Quá retry → DeadLetter.
```

---

## 12. Security / Secrets Standard

```txt
- Không lưu raw refresh token.
- Không lưu raw API token.
- Không lưu webhook secret plain text.
- Không lưu integration credential plain text.
- Token chỉ hiển thị một lần lúc tạo.
- SecretRef trong Domain/Application, secret storage ở Infrastructure.
```

Infrastructure components:

```txt
Security/TokenHasher.cs
Security/SecretEncryptor.cs
Security/WebhookSignatureService.cs
Security/RandomTokenGenerator.cs
```

---

## 13. External Providers Standard

### 13.1 Email

```txt
Application → IEmailService
Infrastructure → SmtpEmailSender / Resend / SendGrid
```

Rule:

```txt
Email quan trọng nên đi qua outbox/job, không gửi trực tiếp trong Domain.
```

### 13.2 Storage

```txt
Application → IStorageService
Infrastructure → Local/S3/AzureBlob provider
```

Rule:

```txt
DB lưu metadata + storage_key, không lưu binary file trong core table.
```

### 13.3 Billing

```txt
Application → IBillingProvider / IFeatureEntitlementService
Infrastructure → Stripe/Mock provider
```

Rule:

```txt
WorkManagement không phụ thuộc Stripe.
Billing webhook phải idempotent.
```

### 13.4 Integrations/Webhooks

```txt
Inbound webhook → verify signature → idempotency → Application use case
Outbound webhook → outbox/job → sign → retry
```

---

## 14. Observability Standard

### 14.1 Health checks bắt buộc

```txt
- Database connectivity.
- Redis connectivity.
- Outbox backlog.
- Failed/dead-letter count.
- Worker heartbeat.
- Email provider optional.
- Storage provider optional.
```

### 14.2 Metrics bắt buộc

```txt
- outbox_pending_count
- outbox_failed_count
- outbox_dead_letter_count
- outbox_dispatch_duration
- worker_heartbeat_age
- cache_hit_rate
- db_query_duration
- external_provider_latency
```

### 14.3 Logging rules

```txt
- Log correlation_id.
- Log workspace_id khi có.
- Log event_id/message_name cho outbox.
- Không log password/token/secret.
- Audit log khác application log.
```

---

## 15. Implementation Roadmap — Không refactor mãi

### Phase 0 — Stop-the-bleeding cleanup

Mục tiêu: ổn định cấu trúc, không thay đổi nghiệp vụ.

```txt
1. Tách DependencyInjection thành registration files.
2. Move/rename folder theo final structure nếu cần.
3. Không đổi business logic.
4. Build phải xanh.
```

### Phase 1 — Outbox v2

```txt
1. OutboxMessage thêm MessageType, MessageName, SchemaVersion, SourceEventId, LockedAt, LockedBy.
2. EventNameAttribute + EventTypeRegistry stable name/version.
3. IIntegrationEvent + IIntegrationEventMapper + Composite mapper ở Application.
4. DomainEventInterceptor map DomainEvent → IntegrationEvent và persist IntegrationEvent.
5. OutboxDispatcher claim Pending + Failed, recover Processing timeout.
6. ProcessedEventStore unique(event_id, consumer_name).
```

### Phase 2 — Tenant fail-closed

```txt
1. WorkspaceResolutionMiddleware.
2. Verify active membership trước khi set CurrentWorkspace.
3. EF filter fail-closed cho workspace-scoped request.
4. Optional PostgreSQL session variable/RLS hook.
5. Integration tests chống cross-workspace leak.
```

### Phase 3 — ReadModels + Cache

```txt
1. BoardSchemaReadService.
2. BoardItemReadService cursor pagination.
3. EffectivePermissionReadService.
4. CacheKeys + CacheInvalidationService.
5. Invalidation theo IntegrationEvent.
```

### Phase 4 — Operations + Observability

```txt
1. Health checks.
2. Worker heartbeat.
3. Outbox backlog metrics.
4. Dead-letter monitoring.
5. Correlation logging.
```

### Phase 5 — Providers khi cần

```txt
1. Storage provider.
2. Realtime provider.
3. Search provider.
4. Billing provider.
5. External integrations.
```

---

## 16. Definition of Done cho Infrastructure feature

Một hạng mục Infrastructure chỉ được xem là xong khi:

```txt
[ ] Implement đúng Application abstraction.
[ ] Không chứa business invariant.
[ ] Có DI registration đúng concern.
[ ] Có options/config binding nếu cần.
[ ] Có logging/correlation nếu là worker/provider.
[ ] Có retry/idempotency nếu gọi external system.
[ ] Có test hoặc smoke test tối thiểu.
[ ] Không leak secret/token vào log.
[ ] Không bypass permission/Application use case.
[ ] Có health/metric nếu là background processor quan trọng.
```

---

## 17. Architecture Tests nên có

```txt
1. Domain không reference Infrastructure.
2. Application không reference Infrastructure.
3. Infrastructure reference Application và Domain là hợp lệ.
4. OutboxMessage không nằm trong Domain.
5. EF configurations nằm trong Infrastructure/Data/Configurations.
6. Consumer phải dùng ProcessedEventStore hoặc base idempotent consumer.
7. IntegrationEvent phải có EventNameAttribute và SchemaVersion.
8. Infrastructure service không được inject Controller/API types.
9. Background worker không được gọi DbContext cross-context tùy tiện nếu đã có Application service.
10. No raw token/secret property names được map plain text nếu không có hash/encryption policy.
```

---

## 18. Final Recommendation

Không cần tiếp tục refactor layer theo hướng đổi cấu trúc liên tục. Hãy chốt như sau:

```txt
Giữ Infrastructure structure v3.
Tập trung hardening Outbox, Tenancy, DI, ReadModels, Observability.
Mọi feature mới phải đặt đúng folder final.
Feature cũ chỉ move khi chạm vào hoặc khi làm Phase 0 cleanup.
Không tạo provider/folder rỗng nếu chưa có use case.
Không đổi root folder nếu không có ADR.
```

### Điểm kỳ vọng sau khi áp dụng v3

| Khu vực | Hiện tại | Sau v3 |
|---|---:|---:|
| Persistence | 7.8 | 8.5 |
| Outbox/Messaging | 6.2 | 8.8 |
| Tenancy | 6.5 | 8.5 |
| DI/Composition | 6.5 | 8.5 |
| ReadModels | 5.5 | 8.0 |
| Observability | 4.5 | 8.0 |
| Enterprise readiness | 6.8 | 8.5 |

---

## 19. Coding Agent Prompt

```txt
Refactor Notrelix.Infrastructure toward the finalized Enterprise Infrastructure v3 standard without changing business behavior.

Goals:
1. Keep Infrastructure as technical implementation layer only.
2. Do not move business rules into Infrastructure.
3. Stabilize folder structure and stop future structural churn.
4. Split DependencyInjection.cs into concern-based registration files.
5. Implement Outbox v2 as IntegrationEvent-centric:
   - MessageType
   - MessageName
   - SchemaVersion
   - SourceEventId
   - LockedAt
   - LockedBy
   - ProcessedEventStore with unique(event_id, consumer_name)
6. EventTypeRegistry must resolve by stable MessageName + SchemaVersion, not class FullName.
7. DomainEventInterceptor must map DomainEvents to IntegrationEvents and persist IntegrationEvents into Outbox. It must not publish LocalDomainEvents during SaveChanges.
8. OutboxDispatcher must claim Pending + Failed messages and recover stuck Processing messages.
9. Add WorkspaceResolutionMiddleware and tenant fail-closed behavior.
10. Add basic observability for outbox backlog and worker heartbeat.

Constraints:
- Do not change Domain entity behavior.
- Do not change Application use case behavior.
- Do not create generic repositories for every entity.
- Do not create empty provider folders unless required by an implemented service.
- Ensure build passes after each phase.
```

---

## 20. Final One-Page Rule

```txt
Infrastructure của Notrelix là layer triển khai kỹ thuật.
Nó ổn định quanh Data, Messaging, BackgroundJobs, Caching, ReadModels, Providers và Observability.
Không refactor lại root structure sau v3.
Mọi mở rộng phải đi vào đúng module đã chốt.
Outbox là nguồn reliable cho IntegrationEvent.
Tenant isolation fail-closed.
Worker idempotent.
Cache có invalidation.
Provider không chứa business rule.
Observability là bắt buộc, không phải trang trí.
```
