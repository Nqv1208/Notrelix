# 02 — Folder Structure and Boundaries

## 1. Cấu trúc Infrastructure hiện tại

Tầng Infrastructure được tổ chức theo capability:

```txt
Notrelix.Infrastructure/
  Auditing/
  Auth/
  BackgroundJobs/
  Billing/
  Caching/
  Data/
  DependencyInjection/
  Email/
  Events/
  Identity/
  Integrations/
  Messaging/
  Observability/
  Operations/
  Ops/
  Options/
  RateLimiting/
  ReadModels/
  Realtime/
  Security/
  Services/
  Storage/
  DependencyInjection.cs
```

Rule:

- Capability runtime adapter nằm ở folder capability.
- EF Core persistence nằm trong `Data/`.
- DI wiring nằm trong `DependencyInjection/`.
- Không tạo folder `Common` trong Infrastructure để chứa mọi thứ lẫn lộn.

## 2. Folder `DependencyInjection/`

Chứa registration theo capability.

Đúng:

```txt
DependencyInjection/PersistenceRegistration.cs
DependencyInjection/MessagingRegistration.cs
DependencyInjection/CacheRegistration.cs
DependencyInjection/AuthRegistration.cs
```

Sai:

```txt
DependencyInjection/EverythingRegistration.cs
```

Mỗi registration chỉ wire capability của nó.

## 3. Folder `Data/`

Chứa EF Core và database-specific implementation:

```txt
Data/
  Abstractions/
  Configurations/
  Converters/
  Events/
  Interceptors/
  Messaging/
  Migrations/
  Rls/
  Seed/
  ApplicationDbContext.cs
  ApplicationDbContext.DbSets.cs
  ApplicationDbContextInitialiser.cs
  DbSchemas.cs
```

Rule:

- Entity configuration đặt trong `Data/Configurations/{BoundedContext}/`.
- Value converter đặt trong `Data/Converters/`.
- RLS session code đặt trong `Data/Rls/`.
- EF interceptors đặt trong `Data/Interceptors/`.
- Outbox/processed event EF entities đặt trong `Data/Messaging/` hoặc `Data/Outbox/` theo naming hiện tại.

## 4. Folder `Messaging/`

Chứa MassTransit, integration event bus, consumer filters và idempotency store.

Allowed:

```txt
Messaging/IntegrationEventBus.cs
Messaging/TenantContextConsumeFilter.cs
Messaging/DeduplicationConsumeFilter.cs
Messaging/MessageDeduplicationStore.cs
```

Không đặt domain mapping hoặc use case business logic trong `Messaging/`.

## 5. Folder `Events/`

Chứa event registry, dispatch policy, integration event mapping implementation nếu thuộc infrastructure concern.

Rule:

- Domain event type registration phải tập trung.
- Event name/message name phải ổn định.
- Không đổi message name tùy tiện vì ảnh hưởng outbox/backward compatibility.

## 6. Folder `Auth/` và `Identity/`

`Auth/` dành cho technical authentication adapter:

```txt
Auth/Jwt
Auth/Cookies
Auth/Passwords
```

`Identity/` dành cho current user/current account/current workspace adapters hoặc identity provider implementation.

Rule:

- Không đặt register/login use case ở Infrastructure.
- Login/OAuth linking decision nằm ở Application.
- Infrastructure chỉ verify token, hash password, issue JWT, read HTTP context, call provider.

## 7. Folder `Security/`

Dành cho technical security services:

- Encryption.
- OTP generation/validation helpers.
- Low-level security provider implementation.

Permission business rule không nằm ở đây nếu nó là Application governance decision.

## 8. Folder `Caching/`

Chứa Redis implementation.

Không chứa request cache marker. Marker nằm ở Application/Common/Requests/Caching.

Đúng:

```txt
Infrastructure/Caching/RedisCacheService.cs
Application/Common/Caching/CacheKeyFactory.cs
Application/Common/Requests/Caching/IAuthorizedCacheableRequest.cs
```

## 9. Folder `BackgroundJobs/`

Chứa hosted services/worker loop:

- Outbox dispatcher.
- queued job worker.
- recurring background jobs.

Rule:

- Background job phải tạo service scope.
- Nếu job truy cập tenant-scoped data, phải set tenant/system context rõ ràng.
- Không dùng `SystemContext` tùy tiện.

## 10. Folder `ReadModels/`

Chứa Infrastructure implementation cho projections/read models.

Rule:

- Read model update phải chạy sau commit hoặc qua outbox/consumer.
- Không update read model trước transaction commit của write model.
