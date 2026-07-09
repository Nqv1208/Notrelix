# 03 — Dependency Injection and Options Rules

## 1. `DependencyInjection.cs` chỉ là orchestrator

`AddInfrastructure(...)` phải delegate tới registration classes.

Allowed:

```csharp
services.AddPersistence(configuration);
services.AddMessaging(configuration);
services.AddBackgroundJobs(configuration);
services.AddCaching(configuration);
services.AddAuthInfrastructure(configuration);
```

Forbidden:

```csharp
// Không add DbContext/MassTransit/Redis trực tiếp ở root DependencyInjection.cs
services.AddDbContext(...);
services.AddMassTransit(...);
```

## 2. Registration class naming

Pattern:

```txt
{Capability}Registration.cs
```

Examples:

```txt
PersistenceRegistration.cs
MessagingRegistration.cs
CacheRegistration.cs
AuthRegistration.cs
SecurityRegistration.cs
BackgroundJobsRegistration.cs
```

## 3. Registration ownership

Mỗi registration chỉ đăng ký capability của nó.

- `PersistenceRegistration`: EF Core, DbContext, interceptors, RLS, persistence services.
- `MessagingRegistration`: MassTransit, integration bus, consumer filters, dedup store.
- `CacheRegistration`: Redis connection/cache implementation.
- `AuthRegistration`: JWT/cookies/password/current user context.
- `SecurityRegistration`: encryption/OTP/security provider.
- `BackgroundJobsRegistration`: hosted workers.

Không đăng ký cross-capability tùy tiện.

## 4. Mapping Application abstraction to Infrastructure implementation

Rule:

```txt
Application defines interface.
Infrastructure registers implementation.
```

Example:

```csharp
services.AddScoped<IRedisCacheService, RedisCacheService>();
services.AddScoped<IPermissionVersionProvider, PermissionVersionProvider>();
services.AddScoped<IIntegrationEventBus, IntegrationEventBus>();
```

Application handler không được inject Infrastructure concrete.

## 5. Lifetime rules

### Scoped

Use scoped for:

- DbContext.
- Services using DbContext.
- Current tenant/user/request context adapters.
- RLS session context.
- Message dedup store.
- Permission version provider if it touches DbContext.

### Singleton

Use singleton for:

- Stateless registries.
- Options validators.
- Redis connection multiplexer.
- Hosted background queue object if thread-safe.

Singleton must not capture scoped services.

### Transient

Use transient for lightweight stateless helpers when needed.

## 6. Options rules

Every option class must:

- Live in Infrastructure/Options or capability folder if capability-specific.
- Have clear section name.
- Validate required values.
- Use `ValidateOnStart()` for production-critical config.

Example:

```csharp
services.AddOptions<RabbitMqOptions>()
    .Bind(configuration.GetSection("Messaging:RabbitMQ"))
    .ValidateDataAnnotations()
    .ValidateOnStart();
```

## 7. Fail-fast config

Fail at startup for missing critical config:

- Database connection string.
- Redis connection string if caching enabled.
- JWT secret/issuer/audience.
- RabbitMQ config when RabbitMQ transport selected.
- Email provider credentials when email provider enabled.
- Storage provider config when storage provider enabled.

Do not silently fallback in staging/production.

## 8. Development-only fallback

Fallback such as `Messaging:Transport=None` is allowed only in Development.

Rule:

```txt
If transport/provider is None/DevNull, code must explicitly check environment and throw outside Development.
```

## 9. Options validation tests

Add tests for:

- Missing required values.
- Invalid numeric values.
- Invalid enum values.
- Production disallowing dev-null provider.
- Startup validation failure.

## 10. Agent checklist

Before adding DI registration, answer:

- Which Application abstraction am I implementing?
- Is this registration in the correct capability registration file?
- Is lifetime correct?
- Does it capture scoped service from singleton?
- Does it require options validation?
- Does it introduce duplicate mechanism?
