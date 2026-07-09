# Notrelix Infrastructure Layer Rulebook

Bộ tài liệu này là rulebook dành cho Coding Agent khi chỉnh sửa tầng `Notrelix.Infrastructure`.

Mục tiêu của tầng Infrastructure:

- Implement các abstraction được Application định nghĩa.
- Kết nối database, Redis, email, storage, realtime, message bus, background jobs, observability và provider bên ngoài.
- Không chứa business decision của use case.
- Không bypass request pipeline, tenancy, RLS, authorization, outbox hoặc idempotency.

## Cách dùng

1. Đọc `01-infrastructure-layer-rules.md` trước khi sửa code.
2. Đọc file theo capability tương ứng:
   - Persistence/RLS: `04-persistence-dbcontext-rls.md`
   - EF config/migration: `05-ef-configurations-migrations-converters.md`
   - Messaging/outbox: `06-outbox-events-messaging-consumers.md`
   - Background jobs: `07-background-jobs-ops.md`
   - External services: `08-external-services.md`
3. Khi tạo PR, dùng `10-code-review-checklist.md` để tự review.
4. Merge nội dung trong `RULE-infrastructure-layer-patch.md` vào `backend/RULE.md`.
5. Các điểm còn tồn tại và hướng siết chặt nằm trong `11-infrastructure-hardening-plan.md`.

## Nguyên tắc nền

Infrastructure không quyết định nghiệp vụ. Infrastructure chỉ hiện thực hóa kỹ thuật.

Ví dụ:

- Được: `RedisCacheService` implement `IRedisCacheService`.
- Được: `ApplicationDbContext` implement bounded-context DbContext interfaces.
- Được: `OutboxDispatcher` publish message đã được persist sau commit.
- Không được: Infrastructure tự quyết định user có được tạo board không.
- Không được: Infrastructure handler tự gọi permission service để bypass Application pipeline.
- Không được: Consumer tự dedup bằng tay nếu đã có filter/executor chuẩn.
