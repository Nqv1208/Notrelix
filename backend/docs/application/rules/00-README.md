# Notrelix Application Layer Documentation Pack

Bộ tài liệu này dùng để khóa quy tắc viết code và triển khai use case trong tầng `Notrelix.Application`.
Mục tiêu là giúp human developer và coding agent không đặt file sai chỗ, không bypass pipeline, không tự commit transaction, không tự build cache key, không bỏ permission, và không làm side effect trước commit.

## Cách dùng

Đọc theo thứ tự:

1. `01-application-layer-rules.md` — luật bắt buộc, ngắn gọn, agent-safe.
2. `02-folder-structure-and-boundaries.md` — cấu trúc thư mục và boundary giữa `Requests`, `Caching`, `Security`, `Tenancy`, `Context`, `Behaviors`.
3. `03-request-contracts-and-pipeline.md` — request markers, classifier, pipeline order.
4. `04-usecase-implementation-guide.md` — cách tạo command/query/handler/validator/result.
5. `05-security-tenancy-and-permissions.md` — workspace/resource/account security, permission, current request context.
6. `06-transactions-concurrency-postcommit.md` — transaction, optimistic concurrency, post-commit.
7. `07-caching-rules.md` — public cache, authorized cache, permissioned cache.
8. `08-events-outbox-and-messaging.md` — integration events, outbox, consumers, idempotency.
9. `09-testing-and-architecture-gates.md` — test bắt buộc và architecture gates.
10. `10-code-review-checklist.md` — checklist review PR.

## Nguyên tắc tổng quát

Tầng Application là nơi orchestration use case diễn ra. Nó không làm HTTP, không làm EF implementation, không publish trực tiếp ra message broker, không chứa infrastructure concrete. Nó gọi Domain để mutate state, dùng abstractions để truy cập dữ liệu và external concerns, và để pipeline xử lý cross-cutting concerns.

Luật nền:

```txt
API = transport only
Application = use case orchestration
Domain = business invariants
Infrastructure = implementation details
```

## Source of truth trong repo

- Application structure: `backend/src/Notrelix.Application/README.md`
- Global rulebook: `backend/RULE.md`
- Pipeline registration: `backend/src/Notrelix.Application/DependencyInjection.cs`
- Request contracts: `backend/src/Notrelix.Application/Common/Requests/**`
- Runtime services: `backend/src/Notrelix.Application/Common/{Caching,Security,Tenancy,Context,...}`
- Behaviors: `backend/src/Notrelix.Application/Common/Behaviors/**`
