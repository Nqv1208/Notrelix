# 01 — Infrastructure Layer Rules

## 1. Nhiệm vụ của Infrastructure

Infrastructure chịu trách nhiệm cho technical capabilities:

- Database / EF Core / PostgreSQL / migrations / seed.
- RLS session application.
- Redis cache implementation.
- MassTransit / RabbitMQ / in-memory messaging.
- Outbox dispatcher.
- Consumer filters / idempotency.
- JWT / cookies / password hashing / current user adapter.
- Email, storage, realtime provider adapters.
- Rate limiting provider.
- Observability, metrics, background jobs.

Infrastructure không chứa use case decision.

## 2. Dependency direction

Allowed:

```txt
Infrastructure -> Application
Infrastructure -> Domain
Infrastructure -> external packages
```

Forbidden:

```txt
Application -> Infrastructure
Domain -> Infrastructure
Domain -> Application
```

Application định nghĩa abstraction; Infrastructure implement.

Ví dụ:

```txt
Application/Common/Caching/IRedisCacheService.cs
Infrastructure/Caching/RedisCacheService.cs
```

## 3. Composition root phải mỏng

`Notrelix.Infrastructure/DependencyInjection.cs` chỉ được delegate tới các registration class.

Đúng:

```csharp
services.AddPersistence(configuration);
services.AddMessaging(configuration);
services.AddCaching(configuration);
```

Sai:

```csharp
// Không nhồi hàng trăm dòng registration vào DependencyInjection.cs
services.AddDbContext(...);
services.AddMassTransit(...);
services.AddSingleton(...);
```

Mỗi capability có registration riêng:

```txt
DependencyInjection/
  PersistenceRegistration.cs
  MessagingRegistration.cs
  CacheRegistration.cs
  AuthRegistration.cs
  SecurityRegistration.cs
  BackgroundJobsRegistration.cs
  RealtimeRegistration.cs
  EmailRegistration.cs
  IntegrationsRegistration.cs
```

## 4. Không duplicate technical mechanism

Mỗi concern chỉ có một cơ chế chính.

Bắt buộc:

- Một cơ chế consumer idempotency.
- Một cơ chế tenant/RLS session application trong consumer path.
- Một outbox dispatcher path.
- Một Redis cache implementation cho `IRedisCacheService`.
- Một source of truth cho `IEventTypeRegistry`.

Không được giữ song song:

```txt
DeduplicationConsumeFilter + ConsumerPipelineExecutor + manual handler dedup
```

trừ khi có ADR nói rõ cơ chế nào active và cơ chế nào legacy/dead code.

## 5. Infrastructure không được bypass Application pipeline

Không được:

- Gọi command/query handler trực tiếp.
- Tự mở use case transaction bên ngoài `DbRequestScopeBehavior`, trừ background worker/consumer path có rule riêng.
- Tự check permission trong endpoint/consumer để bỏ qua Application authorization contract.
- Publish external side effect trực tiếp từ EF interceptor.

## 6. No direct business logic

Infrastructure service chỉ được làm technical work.

Đúng:

```csharp
public sealed class SmtpEmailSender : IEmailSender
{
    public Task SendAsync(EmailMessage message, CancellationToken ct) { ... }
}
```

Sai:

```csharp
public sealed class SmtpEmailSender : IEmailSender
{
    public Task SendWelcomeEmailIfUserIsPremium(User user) { ... } // business decision
}
```

Business decision nằm ở Application handler/domain service. Infrastructure adapter chỉ thực thi.

## 7. Options phải validate on start

Mọi options quan trọng phải:

- Bind từ config section rõ ràng.
- Validate required values.
- `ValidateOnStart()`.

Bắt buộc với:

- Database
- RLS
- JwtSettings
- Messaging/RabbitMQ
- Redis/Cache
- Email
- Storage
- OAuth/provider options
- Rate limiting

Không được fallback im lặng cho production-critical config.

## 8. Secret handling

Không commit secret.

Không lưu plaintext access token/provider refresh token trong DB nếu không bắt buộc.

Dùng:

- Environment variables.
- Secret manager.
- `SecretRef` nếu token cần lưu gián tiếp.
- Hash cho token one-way như refresh token/share link token.

## 9. Logging rule

Log phải đủ debug nhưng không leak secret.

Không log:

- Raw JWT.
- Refresh token.
- OAuth code/verifier/token.
- Email OTP.
- Password hash.
- Raw provider secret.

Được log:

- CorrelationId.
- EventId.
- MessageName.
- ConsumerName.
- WorkspaceId/AccountId nếu không nhạy trong context nội bộ.
- Retry count.
- Error category.

## 10. Test gate

Mọi capability mới phải có ít nhất một trong các test sau:

- Unit test cho service/adapter.
- Infrastructure test cho DB/RLS/converter/outbox.
- Integration test cho runtime path.
- Architecture test cho boundary.

Không merge Infrastructure feature chỉ vì build pass.
