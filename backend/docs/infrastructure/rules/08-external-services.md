# 08 — External Services: Auth, Cache, Rate Limit, Email, Storage, Realtime, Integrations

## 1. General adapter rule

External services are adapters. They implement Application abstractions.

They must not contain use case business decisions.

Examples:

```txt
JwtTokenGenerator implements IJwtTokenGenerator
RedisCacheService implements IRedisCacheService
SmtpEmailSender implements IEmailSender
StorageService implements IFileStorageService
RealtimePublisher implements IRealtimePublisher
```

## 2. Auth/JWT/cookies

Infrastructure may:

- hash passwords;
- generate JWT;
- validate JWT;
- manage auth cookies;
- read current user from HTTP context;
- check token blacklist.

Application owns:

- login/register decision;
- session creation decision;
- OAuth account linking decision;
- suspended/inactive user rule;
- refresh token rotation use case.

JWT settings must validate:

- SecretKey required and long enough.
- Issuer required.
- Audience required.
- Expiry positive.
- Refresh token days positive.

Do not log raw tokens.

## 3. Redis cache

Redis implementation must only get/set/remove serialized values.

Cache key construction belongs to Application `CacheKeyFactory` and cache behaviors.

Infrastructure Redis service must not invent keys for request cache.

Allowed:

```csharp
Task<T?> GetAsync<T>(string key)
Task SetAsync<T>(string key, T value, TimeSpan ttl)
Task RemoveAsync(string key)
```

Forbidden:

```csharp
BuildBoardCacheKey(...)
```

## 4. Rate limiting

Infrastructure rate limit service implements token/window algorithms.

Policy decision and endpoint metadata live outside provider implementation.

Rules:

- Partition key must be explicit.
- Do not silently fall back from unimplemented algorithm.
- If `TokenBucket` is listed but not implemented, throw `NotSupportedException` or remove it.
- Return `RetryAfter`, `Remaining`, `ResetAt` consistently.

## 5. Email

Email provider sends messages. It does not decide who should receive which business email.

Allowed:

```txt
Send message with subject/body/to
```

Forbidden:

```txt
If user registered and workspace premium, send X
```

Business email decision belongs in Application consumer/handler, but durable dispatch should be via post-commit/outbox depending on reliability requirement.

Do not log email body if it may contain secret/OTP/token.

## 6. Storage

Storage adapter owns:

- upload/download/delete object;
- signed URL generation;
- content type validation if technical;
- virus scanning integration if technical.

Application owns:

- whether user can upload;
- storage quota;
- attachment ownership;
- resource association;
- lifecycle decision.

Rules:

- Never trust filename as path.
- Generate object key server-side.
- Include account/workspace/resource partition in object key when appropriate.
- Do not expose internal bucket path directly.
- Validate content length/type.

## 7. Realtime

Realtime adapter only publishes event to channel/topic.

Application/post-commit behavior decides what to publish and when.

Rules:

- Publish after commit only.
- Topic must include tenant/workspace dimension.
- Do not include sensitive payload in broad topic.
- Do not publish before authorization/transaction success.

## 8. OAuth/integrations provider clients

Provider clients live in Infrastructure.

Application owns linking/session/use case decision.

Rules:

- Use Authorization Code + PKCE for login providers.
- Validate state/nonce/id_token for OIDC.
- Do not store provider access token unless integration requires it.
- Store token via secret reference/encrypted store, not plaintext.
- Do not auto-link unverified email.

## 9. Observability

Metrics/logging/tracing must be technical.

Include:

- correlation id;
- event id;
- consumer name;
- outbox status;
- retry count;
- duration.

Exclude:

- secrets;
- tokens;
- passwords;
- OTP values;
- private provider payload unless sanitized.

## 10. External service tests

Required tests:

- options validation;
- serialization/deserialization;
- failure handling;
- timeout/cancellation;
- no secret logging where applicable;
- development-only provider cannot run in production.
