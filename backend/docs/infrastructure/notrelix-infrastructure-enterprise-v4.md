# Notrelix Infrastructure Layer — Final Enterprise Standard v4

> Mục tiêu của tài liệu này là **chốt kiến trúc Infrastructure ổn định** cho Notrelix để refactor một lần theo đúng hướng, sau đó tập trung phát triển tính năng.  
> Không tiếp tục đổi root folder theo cảm tính. Mọi thay đổi kiến trúc lớn sau tài liệu này phải có ADR.

---

## 0. Kết luận kiến trúc cần chốt

Infrastructure của Notrelix nên được hiểu là tầng **technical implementation**:

```text
Domain         = nghiệp vụ, invariant, aggregate, domain event
Application    = use case, CQRS, validation, permission, transaction orchestration
Infrastructure = EF Core, PostgreSQL, Redis, Outbox, MassTransit, workers, storage, email, realtime, integrations, billing provider, search/reporting provider
API            = HTTP entrypoint, routing, request binding, auth middleware composition
```

Dependency rule bắt buộc:

```text
API → Application → Domain
API → Infrastructure → Application → Domain
Infrastructure → Application → Domain
Domain không reference Application/Infrastructure
Application không reference Infrastructure
```

Infrastructure **không được chứa business invariant**. Infrastructure chỉ implement interface do Application định nghĩa hoặc mapping/persistence kỹ thuật.

---

## 1. Cấu trúc root folder cuối cùng

Chốt root folder như sau:

```text
Notrelix.Infrastructure/
├── Data/
├── ReadModels/
├── Messaging/
├── BackgroundJobs/
├── Caching/
├── Identity/
├── Auth/
├── Security/
├── Storage/
├── Email/
├── Realtime/
├── Integrations/
├── Billing/
├── Search/
├── Reporting/
├── Operations/
├── Middleware/
├── Observability/
├── DependencyInjection/
└── DependencyInjection.cs
```

### Rule không đổi root folder

Sau khi refactor theo cấu trúc trên:

```text
- Không đổi tên root folder.
- Không tạo root folder mới nếu chưa có ADR.
- Không tạo folder chung chung như Services, Helpers, Providers ở root.
- Chỉ thêm subfolder/file bên trong root folder đã chốt.
```

Nếu thật sự cần thêm root folder mới, phải tạo ADR:

```text
docs/adr/xxxx-add-infrastructure-root-folder.md
```

ADR phải trả lời:

```text
1. Trách nhiệm kỹ thuật của folder mới là gì?
2. Có trùng với root folder hiện tại không?
3. Vì sao không thể là subfolder?
4. Có ảnh hưởng dependency rule không?
5. Có cần migration cấu trúc lớn không?
```

---

## 2. Data — EF Core, PostgreSQL, mapping, migration, outbox persistence

`Data/` là nơi duy nhất chứa EF Core DbContext, database mapping, migrations, converters, interceptors, seed và persistence model kỹ thuật như Outbox.

```text
Data/
├── ApplicationDbContext.cs
├── ApplicationDbContextFactory.cs
├── DbSchemas.cs
├── Configurations/
│   ├── Identity/
│   ├── Workspace/
│   ├── Governance/
│   ├── Work/
│   ├── Docs/
│   ├── Collab/
│   ├── Automation/
│   ├── Integration/
│   ├── Billing/
│   ├── Reporting/
│   ├── Search/
│   └── Ops/
├── Converters/
├── Interceptors/
│   ├── AuditableEntityInterceptor.cs
│   ├── DomainEventToOutboxInterceptor.cs
│   ├── TenantSessionInterceptor.cs
│   └── SoftDeleteInterceptor.cs
├── Outbox/
│   ├── OutboxMessage.cs
│   ├── OutboxMessageStatus.cs
│   ├── OutboxMessageType.cs
│   ├── OutboxMessageConfiguration.cs
│   ├── EventTypeRegistry.cs
│   └── OutboxSerializer.cs
├── Seed/
└── Migrations/
```

### 2.1. DbSchemas

```csharp
public static class DbSchemas
{
    public const string Identity = "identity";
    public const string Workspace = "workspace";
    public const string Governance = "governance";
    public const string Work = "work";
    public const string Docs = "docs";
    public const string Collab = "collab";
    public const string Automation = "automation";
    public const string Integration = "integration";
    public const string Billing = "billing";
    public const string Reporting = "reporting";
    public const string Search = "search";
    public const string Ops = "ops";
}
```

Rule:

```text
- Không hard-code schema string trong từng configuration.
- Luôn dùng DbSchemas.
- Schema DB phản ánh bounded context ownership.
```

### 2.2. EF Configuration rule

Configuration phải chia theo schema ownership:

```text
Data/Configurations/Work/BoardConfiguration.cs
Data/Configurations/Work/BoardItemConfiguration.cs
Data/Configurations/Governance/PermissionRuleConfiguration.cs
Data/Configurations/Billing/SubscriptionConfiguration.cs
```

Rule mapping:

```text
- Domain không dùng data annotation kỹ thuật dày đặc.
- Enum map bằng HasConversion<string>().
- Value Object map qua converter/owned type rõ ràng.
- JSONB map bằng converter riêng, không serialize thủ công trong handler.
- Soft delete dùng deleted_at; IsDeleted trong Domain là computed nếu có.
- AggregateRoot.Version map concurrency token.
- Id do Domain/Application sinh, EF ValueGeneratedNever().
- Index/filter index đặt trong configuration/migration.
```

Ví dụ:

```csharp
public sealed class BoardConfiguration : IEntityTypeConfiguration<Board>
{
    public void Configure(EntityTypeBuilder<Board> builder)
    {
        builder.ToTable("boards", DbSchemas.Work);
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(x => x.WorkspaceId)
            .HasColumnName("workspace_id")
            .IsRequired();

        builder.Property(x => x.Name)
            .HasColumnName("name")
            .HasMaxLength(180)
            .IsRequired();

        builder.Property(x => x.Visibility)
            .HasColumnName("visibility")
            .HasConversion<string>()
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(x => x.Version)
            .HasColumnName("version")
            .IsConcurrencyToken();

        builder.HasIndex(x => new { x.WorkspaceId, x.Name })
            .HasDatabaseName("ix_boards_workspace_name_active")
            .HasFilter("deleted_at IS NULL");
    }
}
```

### 2.3. ApplicationDbContext rule

`ApplicationDbContext` có thể là một DbContext duy nhất trong modular monolith.

Rule:

```text
- Một DbContext duy nhất được chấp nhận trong modular monolith.
- Không dùng nhiều DbContext quá sớm nếu chưa tách service thật.
- Application không được query cross-context tùy tiện dù DbContext expose nhiều DbSet.
- Query phức tạp nên đi qua ReadModels service.
```

Workspace filter:

```text
- Entity implement IWorkspaceScoped phải được filter theo CurrentWorkspace.
- Request cần workspace mà CurrentWorkspace chưa set phải fail-closed.
- Không fallback về query toàn bộ workspace.
```

### 2.4. Migration rule

Không tạo migration phá toàn bộ DB khi chưa cần.

Rule:

```text
- Migration phải nhỏ, review được.
- Mỗi migration nên gắn với một concern rõ: outbox, permissions, billing, work schema...
- Migration thay đổi dữ liệu phải có backfill script.
- Migration destructive phải có compatibility window hoặc rollback plan.
- Không đổi tên bảng/cột lớn nếu không có lý do rõ.
```

Smoke test sau migration:

```text
[ ] Register/Login chạy.
[ ] Get workspace chạy.
[ ] Get board schema chạy.
[ ] Create board item chạy.
[ ] Update field value chạy.
[ ] Permission check chạy.
[ ] Outbox dispatch chạy.
[ ] Worker không lỗi startup.
```

---

## 3. ReadModels — optimized query implementation

`ReadModels/` implement các read service do Application định nghĩa. Đây là nơi chứa query phức tạp, projection, raw SQL có kiểm soát, query tối ưu cho UI.

```text
ReadModels/
├── WorkManagement/
│   ├── BoardSchemaReadService.cs
│   ├── BoardItemReadService.cs
│   ├── BoardViewReadService.cs
│   └── MyWorkReadService.cs
├── Workspaces/
│   ├── WorkspaceNavigationReadService.cs
│   └── WorkspaceMemberReadService.cs
├── Governance/
│   ├── PermissionReadService.cs
│   └── EffectivePermissionReadService.cs
├── Documents/
│   ├── PageTreeReadService.cs
│   └── PageDetailReadService.cs
├── Collaboration/
│   ├── ActivityFeedReadService.cs
│   └── NotificationReadService.cs
├── Billing/
│   └── EntitlementReadService.cs
├── Reporting/
│   └── DashboardReadService.cs
└── Search/
    └── SearchReadService.cs
```

### Khi nào tạo ReadModel service?

Tạo khi:

```text
- Query cần join nhiều bảng.
- Query cần cursor pagination.
- Query cần raw SQL hoặc projection tối ưu.
- Query phục vụ dashboard/reporting/search/activity feed.
- Query cần permission-aware filter phức tạp.
```

Không cần tạo nếu:

```text
- Query đơn giản, một bảng, AsNoTracking, map DTO nhỏ.
- Handler vẫn rõ ràng và không cross-context.
```

### Rule ReadModels

```text
- ReadModels không mutate Domain aggregate.
- ReadModels return DTO/read model, không return EF entity tracked.
- Query phải AsNoTracking mặc định.
- Query list lớn phải dùng cursor pagination.
- Search/reporting/activity feed phải permission-aware.
- Không để frontend tự lọc dữ liệu nhạy cảm.
```

---

## 4. Messaging — bus, integration event publishing, consumers, idempotency

`Messaging/` là runtime dispatch/consume message. Không chứa persistence model Outbox; Outbox nằm trong `Data/Outbox`.

```text
Messaging/
├── IntegrationEvents/
│   ├── IIntegrationEventBus.cs
│   ├── MassTransitIntegrationEventBus.cs
│   └── IntegrationEventHeaders.cs
├── Bus/
│   ├── MassTransitRegistration.cs
│   └── BusOptions.cs
├── Consumers/
│   ├── ActivityProjectionConsumer.cs
│   ├── NotificationConsumer.cs
│   ├── SearchIndexingConsumer.cs
│   ├── AutomationTriggerConsumer.cs
│   ├── BillingProjectionConsumer.cs
│   └── PermissionCacheInvalidationConsumer.cs
├── Serialization/
│   ├── IntegrationEventSerializer.cs
│   └── EventNameResolver.cs
└── Idempotency/
    ├── ProcessedEvent.cs
    ├── ProcessedEventStore.cs
    └── ProcessedEventConfiguration.cs
```

### 4.1. IntegrationEvent rule

```text
- IntegrationEvent là contract async nội bộ hoặc external-ready.
- DomainEvent không mặc định là IntegrationEvent.
- IntegrationEvent được tạo theo consumer-driven criteria.
- Mọi IntegrationEvent phải persist Outbox trước khi publish.
- IntegrationEvent phải versioned.
```

Naming:

```text
Class:        BoardCreatedIntegrationEventV1
MessageName: work.board.created
Version:     1
```

Không dùng:

```text
MessageName = work.board.created.v1
```

nếu đã có `SchemaVersion = 1`.

### 4.2. Consumer rule

Consumer không nhất thiết 1-1 với event. Consumer nên đại diện cho side effect/subsystem.

Tốt:

```text
NotificationConsumer
SearchIndexingConsumer
ActivityProjectionConsumer
AutomationTriggerConsumer
PermissionCacheInvalidationConsumer
```

Không bắt buộc:

```text
BoardCreatedConsumer
BoardItemAssignedConsumer
BoardItemCompletedConsumer
```

Rule:

```text
- Consumer phải idempotent.
- Unique key: (event_id, consumer_name).
- Consumer không chứa business invariant lõi.
- Consumer xử lý side effect, projection, notification, cache invalidation, automation trigger.
- Consumer không gọi API controller.
```

### 4.3. Processed events

Schema khuyến nghị:

```text
ops.processed_events
- id
- event_id
- message_name
- schema_version
- consumer_name
- processed_at
- checksum nullable
- error nullable
```

Unique:

```text
UNIQUE(event_id, consumer_name)
```

---

## 5. Outbox final standard

Outbox là durable queue trong DB cho IntegrationEvent và DurableDomainEvent hiếm khi cần.

### 5.1. Không persist toàn bộ DomainEvent

Chốt 3 loại event:

```text
LocalDomainEvent
- Xử lý local/sync hoặc trong Application use case
- Không outbox

DurableDomainEvent
- Hiếm
- Chỉ dùng nếu internal domain/application handler cần reliable async
- Có thể outbox → MediatR

IntegrationEvent
- Consumer-driven
- Luôn outbox
- Dispatch qua IntegrationEventBus/MassTransit hoặc broker sau này
```

### 5.2. OutboxMessage fields

```text
ops.outbox_messages
- id
- event_id
- source_event_id nullable
- message_type
- message_name
- schema_version
- workspace_id nullable
- actor_user_id nullable
- correlation_id nullable
- causation_id nullable
- payload_json
- headers_json nullable
- status
- retry_count
- next_attempt_at
- locked_at nullable
- locked_by nullable
- processed_at nullable
- error nullable
- created_at
```

`message_type`:

```text
IntegrationEvent
DurableDomainEvent
```

Không dùng `DomainEvent` chung chung nếu không cần.

### 5.3. Outbox write flow

```text
Command Handler
  → Aggregate.DomainMethod()
  → DomainEvent(s)

SaveChanges Interceptor
  → Capture DomainEvents
  → IIntegrationEventMapper.Map(domainEvent) → 0..N IntegrationEvents
  → Persist IntegrationEvents as OutboxMessage
  → Optional persist DurableDomainEvents
  → Clear DomainEvents

Commit
```

Interceptor không publish LocalDomainEvent sync trong SaveChanges lifecycle.

### 5.4. Outbox dispatch flow

```text
OutboxDispatcher
  → Claim Pending + Failed due messages
  → Recover stuck Processing
  → Deserialize by MessageName + SchemaVersion
  → Dispatch IntegrationEvent → IIntegrationEventBus.PublishAsync
  → Mark Processed / Failed / DeadLetter
```

Claim SQL pattern:

```sql
UPDATE ops.outbox_messages
SET status = 'Processing',
    locked_at = now(),
    locked_by = @workerId
WHERE id IN (
    SELECT id
    FROM ops.outbox_messages
    WHERE status IN ('Pending', 'Failed')
      AND next_attempt_at <= now()
    ORDER BY created_at
    LIMIT @batchSize
    FOR UPDATE SKIP LOCKED
)
RETURNING *;
```

Recover stuck:

```sql
UPDATE ops.outbox_messages
SET status = 'Failed',
    next_attempt_at = now(),
    error = 'Recovered stuck processing message'
WHERE status = 'Processing'
  AND locked_at < now() - interval '5 minutes';
```

---

## 6. BackgroundJobs — workers, processors, schedulers, locks

```text
BackgroundJobs/
├── Dispatchers/
│   ├── OutboxDispatcher.cs
│   └── WebhookDeliveryDispatcher.cs
├── Processors/
│   ├── NotificationDeliveryProcessor.cs
│   ├── SearchIndexProcessor.cs
│   ├── AutomationProcessor.cs
│   ├── ImportJobProcessor.cs
│   ├── ExportJobProcessor.cs
│   └── CleanupProcessor.cs
├── Schedulers/
│   ├── ScheduledAutomationJobScheduler.cs
│   └── BillingReconciliationScheduler.cs
└── Locks/
    ├── JobLockService.cs
    └── DistributedJobLock.cs
```

### Worker rule

```text
- Worker phải idempotent.
- Worker phải có lock hoặc claim atomic.
- Worker phải có retry/backoff.
- Worker phải có dead-letter hoặc failed terminal state.
- Worker phải emit metric backlog/failure/latency.
- Worker không chứa business invariant lõi.
```

---

## 7. Caching — Redis, invalidation, distributed locks

```text
Caching/
├── RedisCacheService.cs
├── CacheKeys.cs
├── CacheInvalidationService.cs
├── PermissionCacheService.cs
├── BoardSchemaCacheService.cs
├── WorkspaceMembershipCacheService.cs
├── EntitlementCacheService.cs
└── DistributedLockService.cs
```

### Cache keys

```text
workspace-member:{workspaceId}:{userId}
effective-permissions:{workspaceId}:{userId}:{resourceType}:{resourceId}
board-schema:{boardId}:{userId}
board-view:{viewId}:{userId}
field-options:{fieldId}
entitlements:{workspaceId}
current-user:{userId}
```

### Cache invalidation rules

```text
Change member role          → clear workspace-member + permission cache
Grant/revoke permission     → clear effective-permissions cache
Change board visibility     → clear board permission/schema cache
Create/update/delete field  → clear board-schema cache
Update view config          → clear board-view cache
Update entitlement          → clear entitlements cache
```

Không cache nếu:

```text
- Không có invalidation rõ.
- Dữ liệu cực kỳ nhạy cảm.
- List thay đổi liên tục mà không có cursor/scope.
- Audit log/history cần tính chính xác cao theo thời gian.
```

---

## 8. Identity, Auth, Security

### 8.1. Identity

`Identity/` chứa infrastructure implementation liên quan identity user/session/profile nếu đang có sẵn trong repo.

```text
Identity/
└── Services/
    ├── CurrentUser.cs
    ├── UserClaimsService.cs
    └── UserSessionService.cs
```

### 8.2. Auth

```text
Auth/
├── Jwt/
│   ├── JwtTokenService.cs
│   └── JwtOptions.cs
├── Cookies/
│   └── CookieAuthOptions.cs
├── Passwords/
│   └── PasswordHasher.cs
└── Sessions/
    └── RefreshTokenService.cs
```

Rule:

```text
- Application không biết ClaimsPrincipal.
- CurrentUser implement ICurrentUser.
- Refresh token lưu hash, không raw token.
- JWT/cookie config compose ở API/Infrastructure registration.
```

### 8.3. Security

```text
Security/
├── Hashing/
│   └── TokenHasher.cs
├── Encryption/
│   ├── SecretEncryptor.cs
│   └── DataProtectionService.cs
├── Tokens/
│   └── RandomTokenGenerator.cs
├── RateLimiting/
│   └── RateLimitService.cs
└── Webhooks/
    └── WebhookSignatureService.cs
```

Rule:

```text
- Không lưu token/secret plain text.
- Không log token/password/secret.
- Webhook phải ký signature.
- Integration credentials dùng SecretRef/encrypted reference.
```

---

## 9. Storage

```text
Storage/
├── StorageService.cs
├── FileUrlSigner.cs
├── StorageOptions.cs
└── Providers/
    ├── LocalStorageProvider.cs
    ├── S3StorageProvider.cs
    └── AzureBlobStorageProvider.cs
```

Rule:

```text
- DB chỉ lưu metadata + storage_key.
- File lớn dùng pre-signed URL.
- Attachment metadata thuộc Collaboration/collab schema.
- Storage provider thay đổi không làm Application đổi.
```

---

## 10. Email

```text
Email/
├── EmailService.cs
├── EmailTemplateRenderer.cs
├── EmailOptions.cs
├── Providers/
│   ├── SmtpEmailSender.cs
│   └── SendGridEmailSender.cs
└── Templates/
    ├── WorkspaceInvitationTemplate.cshtml
    ├── PasswordResetTemplate.cshtml
    ├── EmailVerificationTemplate.cshtml
    └── NotificationTemplate.cshtml
```

Rule:

```text
- Domain không gửi email.
- Application không gọi provider cụ thể.
- Email quan trọng nên qua outbox/job.
- Email phải idempotent theo notification/message id.
```

---

## 11. Realtime

```text
Realtime/
├── Hubs/
│   ├── WorkspaceHub.cs
│   ├── BoardHub.cs
│   ├── PageHub.cs
│   └── NotificationHub.cs
├── Publishers/
│   └── SignalRRealtimePublisher.cs
└── RealtimeChannelResolver.cs
```

Channels:

```text
workspace:{workspaceId}
board:{boardId}
item:{itemId}
page:{pageId}
user:{userId}:notifications
```

Rule:

```text
- Realtime event là patch nhỏ, không phải DTO full.
- Không broadcast toàn workspace nếu chỉ một user cần nhận.
- Realtime không thay thế query chính.
- Realtime side effect nên đi từ consumer/outbox nếu cần reliable.
```

---

## 12. Integrations

```text
Integrations/
├── Connections/
│   └── IntegrationCredentialStore.cs
├── Webhooks/
│   ├── WebhookDispatcher.cs
│   ├── WebhookSigner.cs
│   └── WebhookRetryPolicy.cs
├── Providers/
│   ├── GoogleProviderClient.cs
│   ├── SlackProviderClient.cs
│   └── ProviderFactory.cs
└── Sync/
    └── IntegrationSyncService.cs
```

Rule:

```text
- Credentials không plain text.
- Outbound webhook phải sign.
- Webhook delivery retry qua worker.
- Inbound webhook phải idempotent.
- Provider-specific SDK chỉ nằm Infrastructure.
```

---

## 13. Billing

```text
Billing/
├── Providers/
│   ├── StripeBillingProvider.cs
│   └── MockBillingProvider.cs
├── Webhooks/
│   ├── BillingWebhookHandler.cs
│   └── BillingWebhookVerifier.cs
└── Entitlements/
    └── EntitlementCacheService.cs
```

Rule:

```text
- WorkManagement không biết Stripe.
- Application chỉ biết IBillingProvider/IFeatureEntitlementService.
- Billing webhook phải verify signature.
- Billing webhook phải idempotent.
- Entitlement cache phải invalidated khi subscription/plan/quota thay đổi.
```

---

## 14. Search

```text
Search/
├── SearchQueryService.cs
├── SearchIndexer.cs
├── SearchDocumentMapper.cs
├── PermissionAwareSearchFilter.cs
├── Indexing/
│   └── SearchIndexJobProcessor.cs
└── Providers/
    ├── PostgresSearchProvider.cs
    ├── OpenSearchProvider.cs
    └── MeilisearchProvider.cs
```

Rule:

```text
- Search result phải permission-aware.
- Search indexing nên async qua outbox/job.
- Không trả resource user không có View permission.
- Provider thay đổi không làm Application đổi.
```

---

## 15. Reporting

```text
Reporting/
├── DashboardQueryService.cs
├── ReportingSnapshotWriter.cs
├── Snapshots/
│   └── ReportingSnapshotProcessor.cs
└── Providers/
    ├── BoardChartWidgetProvider.cs
    ├── WorkloadWidgetProvider.cs
    └── NumberWidgetProvider.cs
```

Rule:

```text
- Dashboard không scan core work tables nặng liên tục.
- Ưu tiên reporting snapshots/projections.
- Reporting query phải permission-aware.
- Widget provider chỉ xử lý data technical/query, không chứa invariant Domain.
```

---

## 16. Operations

```text
Operations/
├── Idempotency/
│   ├── IdempotencyService.cs
│   └── IdempotencyKeyConfiguration.cs
├── ImportExport/
│   ├── ImportJobService.cs
│   ├── ExportJobService.cs
│   ├── CsvBoardImporter.cs
│   ├── ExcelBoardExporter.cs
│   └── MarkdownPageExporter.cs
├── JobLocks/
│   ├── JobLockService.cs
│   └── JobLockConfiguration.cs
├── Cleanup/
│   ├── OutboxCleanupJob.cs
│   ├── ExpiredTokenCleanupJob.cs
│   └── TemporaryFileCleanupJob.cs
└── Health/
    └── WorkerHeartbeatStore.cs
```

Rule:

```text
- Idempotency và JobLock là Infrastructure/Ops concern.
- Không đặt JobLock trong Domain.
- Import/export nặng phải async job.
- Cleanup job phải an toàn, batch nhỏ, có metric.
```

---

## 17. Middleware

```text
Middleware/
├── WorkspaceResolutionMiddleware.cs
├── CorrelationIdMiddleware.cs
├── ExceptionHandlingMiddleware.cs
└── RequestLoggingMiddleware.cs
```

### WorkspaceResolutionMiddleware

Flow:

```text
1. Đọc workspaceId từ route/header/subdomain.
2. Kiểm tra user authenticated nếu route yêu cầu.
3. Verify user là member active hoặc có quyền truy cập workspace.
4. Set ICurrentWorkspace.
5. Set PostgreSQL tenant/RLS session variable nếu dùng RLS.
6. Nếu không hợp lệ, return 403/404, không để query chạy.
```

Rule:

```text
- Không tin X-Workspace-Id nếu chưa verify membership.
- Workspace-scoped request thiếu workspace context phải fail-closed.
```

---

## 18. Observability

```text
Observability/
├── Logging/
│   └── LoggingEnricher.cs
├── Metrics/
│   └── MetricsService.cs
├── Tracing/
│   └── OpenTelemetrySetup.cs
└── HealthChecks/
    ├── DatabaseHealthCheck.cs
    ├── RedisHealthCheck.cs
    ├── OutboxBacklogHealthCheck.cs
    ├── WorkerHeartbeatHealthCheck.cs
    └── StorageHealthCheck.cs
```

Metrics bắt buộc:

```text
- outbox.pending.count
- outbox.failed.count
- outbox.dispatch.latency
- worker.heartbeat.age
- cache.hit_rate
- permission.evaluate.duration
- db.query.duration
- webhook.delivery.failed
```

Rule:

```text
- Log correlation_id.
- Log workspace_id nếu có.
- Không log secret/token/password.
- Audit log khác application log.
- Worker backlog phải có health check.
```

---

## 19. DependencyInjection

Root `DependencyInjection.cs` chỉ orchestration:

```csharp
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddPersistence(configuration);
        services.AddReadModels(configuration);
        services.AddMessaging(configuration);
        services.AddBackgroundJobs(configuration);
        services.AddCaching(configuration);
        services.AddAuthInfrastructure(configuration);
        services.AddSecurityInfrastructure(configuration);
        services.AddStorage(configuration);
        services.AddEmail(configuration);
        services.AddRealtime(configuration);
        services.AddIntegrations(configuration);
        services.AddBilling(configuration);
        services.AddSearch(configuration);
        services.AddReporting(configuration);
        services.AddOperations(configuration);
        services.AddObservability(configuration);

        return services;
    }
}
```

Chi tiết nằm trong:

```text
DependencyInjection/
├── PersistenceRegistration.cs
├── ReadModelsRegistration.cs
├── MessagingRegistration.cs
├── BackgroundJobsRegistration.cs
├── CacheRegistration.cs
├── AuthRegistration.cs
├── SecurityRegistration.cs
├── StorageRegistration.cs
├── EmailRegistration.cs
├── RealtimeRegistration.cs
├── IntegrationsRegistration.cs
├── BillingRegistration.cs
├── SearchRegistration.cs
├── ReportingRegistration.cs
├── OperationsRegistration.cs
└── ObservabilityRegistration.cs
```

Rule:

```text
- Không để DependencyInjection.cs thành file lớn hàng trăm dòng.
- Mỗi technical capability có registration riêng.
- Options bind/validate trong registration tương ứng.
```

---

## 20. Infrastructure Do / Don’t

### Do

```text
- Implement Application interfaces.
- EF configuration theo schema ownership.
- Use Outbox for IntegrationEvents.
- Worker idempotent + retry + lock.
- Cache có invalidation rõ.
- Query phức tạp qua ReadModels.
- Provider-specific SDK nằm Infrastructure.
- Secret/token hash/encrypt.
- Observability đầy đủ cho outbox/worker/cache/db.
```

### Don’t

```text
- Không để Domain reference EF/Redis/SignalR/MassTransit.
- Không chứa business invariant trong Infrastructure.
- Không gọi API controller từ Infrastructure.
- Không tạo GenericRepository cho mọi entity.
- Không đặt OutboxMessage trong Domain.
- Không persist toàn bộ DomainEvent.
- Không cache permission nếu không có invalidation.
- Không lưu token/secret plain text.
- Không tạo folder Services/Helpers chung chung ở root.
```

---

## 21. Refactor plan một lần duy nhất

### Phase 1 — Chốt root structure

```text
[ ] Tạo root folders final.
[ ] Di chuyển file hiện có vào đúng root.
[ ] Xóa/gộp folder Services chung chung nếu có.
[ ] DependencyInjection.cs chỉ còn orchestration.
```

### Phase 2 — Data hardening

```text
[ ] Data/Configurations chia theo schema.
[ ] Data/Outbox chứa OutboxMessage persistence.
[ ] ApplicationDbContext workspace filter fail-closed.
[ ] Interceptors rõ trách nhiệm.
```

### Phase 3 — Messaging/Outbox standard

```text
[ ] OutboxMessage thêm MessageType, MessageName, SchemaVersion, SourceEventId.
[ ] EventTypeRegistry resolve by MessageName + SchemaVersion.
[ ] IntegrationEventMapper tạo IntegrationEvents.
[ ] OutboxDispatcher claim Pending + Failed, recover Processing.
[ ] ProcessedEventStore unique(event_id, consumer_name).
```

### Phase 4 — Tenant/workspace security

```text
[ ] WorkspaceResolutionMiddleware verify membership.
[ ] ICurrentWorkspace set sau khi verify.
[ ] DB/RLS session nếu dùng RLS.
[ ] Integration test cross-workspace isolation.
```

### Phase 5 — ReadModels + Cache + Observability

```text
[ ] BoardSchemaReadService.
[ ] BoardItemReadService.
[ ] PermissionReadService.
[ ] Cache invalidation service.
[ ] Outbox/worker health checks.
[ ] Metrics/logging/tracing baseline.
```

---

## 22. Architecture tests bắt buộc

Nên thêm test kiến trúc để tránh phá cấu trúc.

```text
[ ] Domain không reference Infrastructure.
[ ] Application không reference Infrastructure.
[ ] Infrastructure reference Application + Domain là hợp lệ.
[ ] Không có GenericRepository<T> toàn cục.
[ ] Không có root folder Services/Helpers mới.
[ ] OutboxMessage chỉ nằm trong Infrastructure/Data/Outbox.
[ ] Consumer phải dùng ProcessedEventStore hoặc idempotency mechanism.
[ ] EF configurations nằm trong Data/Configurations.
[ ] Infrastructure không chứa Controller.
[ ] Provider-specific SDK không xuất hiện trong Application/Domain.
```

---

## 23. Definition of Done cho Infrastructure capability

Một technical capability chỉ được coi là hoàn thành khi:

```text
[ ] Có interface ở Application nếu cần.
[ ] Infrastructure implement interface.
[ ] Options được bind/validate.
[ ] DI registration riêng.
[ ] Logging có correlation id.
[ ] Error handling rõ.
[ ] Test unit/integration tối thiểu.
[ ] Không leak provider-specific type ra Application.
[ ] Có health check nếu là external dependency.
[ ] Có retry/idempotency nếu là worker/message/external side effect.
```

---

## 24. Kết luận final

Đây là cấu trúc Infrastructure cuối cùng nên chốt cho Notrelix:

```text
Data              = EF Core + mapping + migrations + outbox persistence
ReadModels        = optimized query implementations
Messaging         = IntegrationEvent bus, consumers, idempotency
BackgroundJobs    = workers, processors, schedulers, locks
Caching           = Redis/cache/invalidation/distributed lock
Identity/Auth     = current user, JWT, cookie, password, sessions
Security          = hashing, encryption, token, rate limit, webhook signature
Storage           = file/object storage providers
Email             = email providers/templates
Realtime          = SignalR/publishers/channels
Integrations      = external provider connections/webhooks/sync
Billing           = billing providers/webhooks/entitlement infra
Search            = search provider/indexing/query
Reporting         = reporting snapshots/widget data providers
Operations        = idempotency/import/export/job locks/cleanup
Middleware        = workspace/correlation/error/request middleware
Observability     = logs/metrics/tracing/health checks
DependencyInjection = registration orchestration
```

Sau khi refactor theo cấu trúc này, **không tiếp tục đổi root folder**. Từ đây trở đi chỉ phát triển capability bên trong folder đã chốt.

