# Notrelix Application Layer Rules

> Mục tiêu: file này là rulebook ngắn, rõ, dùng được cho coding agent. Nếu có xung đột giữa cảm tính và rule này, theo rule này.

## 1. Layer responsibility

### Application được phép làm

- Định nghĩa use case bằng `ICommand<TResponse>` hoặc `IQuery<TResponse>`.
- Điều phối workflow nghiệp vụ.
- Gọi aggregate/domain method.
- Gọi abstraction của DbContext theo bounded context interface.
- Gọi abstraction của service: permission, tenant, cache, idempotency, email, storage, integration, clock.
- Trả về `Result`, DTO, response model.
- Khai báo request marker để pipeline xử lý authorization, transaction, cache, concurrency, realtime, gates.

### Application không được làm

- Không gọi ASP.NET `HttpContext` trực tiếp trong handler.
- Không gọi Infrastructure concrete.
- Không inject `ApplicationDbContext` concrete vào handler.
- Không gọi `SaveChangesAsync` trực tiếp trong handler nếu request là `ITransactionalRequest`.
- Không publish MassTransit/RabbitMQ trực tiếp từ handler.
- Không gửi email/webhook/external side effect trực tiếp từ handler.
- Không tự build raw cache key trong query.
- Không tự set tenant/RLS/system context trong handler.

## 2. Use case placement

Canonical path:

```txt
src/Notrelix.Application/Features/{BoundedContext}/{Module}/Commands/{UseCase}/
src/Notrelix.Application/Features/{BoundedContext}/{Module}/Queries/{UseCase}/
```

Ví dụ đúng:

```txt
Features/WorkManagement/Boards/Commands/CreateBoardInWorkspace/
Features/WorkManagement/Boards/Queries/GetBoard/
Features/Governance/ShareLinks/Commands/CreateShareLink/
```

Không tạo use case mới ở legacy path:

```txt
Features/{Context}/Commands/{Module}/{UseCase}/
Features/{Context}/Queries/{Module}/{UseCase}/
```

## 3. Request contracts

Request contracts nằm trong:

```txt
Common/Requests/**
```

Không tạo marker mới trong `Common/CQRS`.

Request phải khai báo đúng nhu cầu:

```txt
Mutation command                         -> ITransactionalRequest
Workspace/resource/account scoped         -> IWorkspaceRequest/IResourceScopedRequest/IAccountRequest
Cần permission                            -> IRequirePermission
Cần optimistic concurrency                -> IExpectedVersionRequest
Public cache                              -> IPublicCacheableQuery
Private/tenant/user cache                 -> IAuthorizedCacheableRequest
Realtime sau commit                       -> IRealtimeRequest
Feature gate                              -> IRequireFeature
Subscription/plan gate                    -> IRequireSubscription
Anonymous endpoint                        -> IAnonymousRequest
System-only internal flow                 -> ISystemInternalRequest
```

## 4. Security default-deny

- Workspace/resource/account scoped request phải có `IRequirePermission`, trừ khi là `ISystemInternalRequest` đã được whitelist rõ.
- Anonymous request không được đồng thời là tenant-scoped hoặc permissioned.
- Public cache request không được tenant-scoped.
- Global request không được đồng thời là tenant/resource scoped.
- Handler không được tự thay permission result.

## 5. Handler context rule

Handler không inject `ICurrentTenantContext` trực tiếp.

Dùng:

```csharp
ICurrentRequestContext
```

khi handler cần:

```txt
UserId
AccountId
WorkspaceId
```

`ICurrentTenantContext` chỉ dùng trong pipeline behaviors, tenant/RLS services, DbContext/filter, infrastructure runtime, background/consumer scopes.

## 6. Transaction rule

- Mutation command phải implement `ITransactionalRequest`.
- Handler không gọi `SaveChangesAsync`.
- `DbRequestScopeBehavior` mở transaction/RLS, gọi handler, save changes, commit/rollback.
- Side effect durable phải qua outbox hoặc post-commit queue tùy loại.

## 7. Concurrency rule

Request implement `IExpectedVersionRequest` thì:

- `ExpectedVersion` phải positive.
- `ResourceRef` phải supported bởi `IResourceVersionReader`.
- Không được silently skip check.
- Version mismatch trả conflict.
- EF concurrency token vẫn là race protection ở `SaveChanges`.

## 8. Cache rule

- Query không tự tạo raw string cache key.
- Query chỉ khai báo metadata: `CacheScope`, `CacheIdentity`, `CacheTtl`.
- Public cache dùng `IPublicCacheableQuery`.
- Authorized cache dùng `IAuthorizedCacheableRequest`.
- Permissioned cache bắt buộc dùng `IPermissionVersionProvider`.
- Không dùng permission version hardcoded như `default`, `unknown`, `v1`.

## 9. Event/outbox rule

- Handler không publish integration event trực tiếp ra bus.
- Cross-context hoặc external side effect phải qua outbox/integration event.
- Consumer phải idempotent theo `event_id + consumer_name`.
- Không combine nhiều cơ chế idempotency cho cùng consumer path.

## 10. Tests are required

Mỗi use case phải có test phù hợp:

- Validator tests nếu rule validation đáng kể.
- Handler tests cho success/failure chính.
- Architecture tests nếu thêm marker/rule mới.
- Integration tests nếu liên quan tenant/RLS/outbox/concurrency/cache.
