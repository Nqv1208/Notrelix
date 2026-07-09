0. Chốt hướng phát triển cuối cùng
Hướng bắt buộc

Notrelix sẽ đi theo Modular Monolith trước, service-ready sau.

Không tách microservice lúc này. Database vẫn dùng một PostgreSQL instance, nhiều schema theo bounded context. Cross-schema FK vẫn được giữ trong giai đoạn này để bảo vệ data integrity. Khi nào tách service thật thì mới chuyển cross-context FK thành uuid reference + local projection + integration events.

Canonical schema được giữ

Giữ các schema chính:

identity
workspace
governance
work
docs
collab
automation
integration
billing
reporting
search
ops
authz
events
messaging
notifications
audit
analytics
activity

Nhưng phải làm rõ vai trò:

identity       = user/account/session/security source of truth
workspace      = workspace/member/team/space/invitation source of truth
governance     = permission/role/share-link/policy source of truth
work           = board/item/field/view/form/approval source of truth
docs           = page/block/document source of truth
collab         = comment/reaction/mention/attachment/presence/read-state
automation     = automation rule/execution/schedule/AI agent
integration    = external connection/webhook/calendar sync
billing        = plan/subscription/invoice/entitlement/usage source of truth
reporting      = dashboard/report config
search         = search projection + indexing queue, not Domain aggregate
ops            = idempotency/job-lock/import/export/cleanup infra
authz          = RLS authorization projection
events         = durable business event log
messaging      = outbox/inbox/processed event canonical messaging schema
notifications  = notification center/email delivery canonical schema
audit          = compliance/security audit canonical schema
analytics      = analytical daily projection
activity       = user-facing activity feed projection

File SQL hiện tại cũng đã mô tả Search/Ops/Outbox/JobLocks/ProcessedEvents/SearchIndexJobs là technical/projection storage, không phải core Domain bounded contexts. Vì vậy code Domain không được map các bảng này thành aggregate.

1. Quyết định cleanup bắt buộc

Vì bạn nói chưa xây dựng production data, nên không giữ bảng compatibility/legacy.

Xóa khỏi baseline

Bắt buộc loại khỏi file SQL baseline:

collab.notifications
collab.notification_preferences
collab.notification_deliveries
collab.unread_counters
collab.activity_logs

governance.audit_logs
governance.security_events

automation.outbox_messages

ops.processed_events

Lý do:

Notification canonical là notifications.*.
Activity canonical là activity.workspace_activity_logs.
Audit canonical là audit.*.
Outbox canonical là messaging.outbox_messages.
Processed event canonical là messaging.processed_events.
ops chỉ giữ idempotency/job locks/import/export/cleanup.

Trong SQL hiện tại, chính file cũng đã comment rằng collab.notifications, collab.notification_preferences, collab.notification_deliveries là legacy/deprecated và new code phải dùng notifications.*.

Activity boundary cũng đã được chốt trong file: activity.* sở hữu user-facing activity feed, audit.* sở hữu compliance/security audit, events.* sở hữu raw business facts, còn collab.* chỉ sở hữu collaboration source state.

2. Roadmap tổng thể

Thứ tự triển khai bắt buộc:

Phase 0 — Freeze & Inventory
Phase 1 — Clean DB Baseline
Phase 2 — DB Integrity/RLS/Index Hardening
Phase 3 — Domain Critical Rules
Phase 4 — Domain State Machine & Versioning
Phase 5 — Domain Rules theo từng bounded context
Phase 6 — EF Core Mapping + Migration Strategy
Phase 7 — Application CQRS/Pipeline Contract
Phase 8 — Infrastructure Outbox/RLS/Authz/Projection
Phase 9 — Tests/CI/Verification

Không làm Application/Infrastructure trước Domain. Nếu Domain chưa chặt, Application sẽ phải vá rule ở handler, về sau rất rối.

Phase 0 — Freeze & Inventory
Mục tiêu

Khóa trạng thái hiện tại, tạo checklist chính xác để coding agent không đoán.

Việc phải làm

Tạo file:

docs/backend/hardening/notrelix-backend-hardening-master-plan.md
docs/backend/hardening/notrelix-schema-cleanup-checklist.md
docs/backend/hardening/notrelix-domain-rule-checklist.md

Nội dung phải có:

1. Danh sách schema canonical
2. Danh sách bảng bị xóa khỏi baseline
3. Danh sách bảng giữ lại
4. Danh sách Domain rules phải implement
5. Danh sách Application pipeline phải có
6. Danh sách Infrastructure services phải có
7. Definition of Done cho từng phase
Prompt triển khai Phase 0
Bạn là senior backend architect cho dự án Notrelix.

Nhiệm vụ: tạo bộ tài liệu hardening master plan cho backend, không thay code business logic trong phase này.

Bối cảnh bắt buộc:
- Notrelix đi theo Modular Monolith trước, service-ready sau.
- Một PostgreSQL database, nhiều schema theo bounded context.
- Không giữ compatibility/legacy tables vì dự án chưa có production data.
- Không đưa option cho người dùng chọn.
- Không triển khai frontend.

Tạo các file:
1. docs/backend/hardening/notrelix-backend-hardening-master-plan.md
2. docs/backend/hardening/notrelix-schema-cleanup-checklist.md
3. docs/backend/hardening/notrelix-domain-rule-checklist.md

Nội dung bắt buộc:
- Chốt canonical schema ownership.
- Liệt kê bảng legacy phải loại khỏi baseline:
  collab.notifications,
  collab.notification_preferences,
  collab.notification_deliveries,
  collab.unread_counters,
  collab.activity_logs,
  governance.audit_logs,
  governance.security_events,
  automation.outbox_messages,
  ops.processed_events.
- Chốt canonical alternatives:
  notifications.*,
  activity.workspace_activity_logs,
  audit.*,
  messaging.outbox_messages,
  messaging.processed_events.
- Tạo checklist implementation theo phase:
  DB baseline cleanup,
  DB RLS/index/trigger hardening,
  Domain critical rules,
  Domain state transitions,
  EF Core mapping,
  Application CQRS pipeline,
  Infrastructure outbox/RLS/authz,
  tests/CI.
- Không viết chung chung. Mỗi checklist item phải có file/module cần sửa, rule cần đảm bảo, và verification command.

Sau khi tạo tài liệu:
- Không sửa code.
- Chạy format nếu repo có markdown formatter.
- In ra danh sách file đã tạo và các quyết định kiến trúc đã chốt.
Phase 1 — Clean DB Baseline
Mục tiêu

Biến SQL hiện tại từ dạng “V2 + patches + compatibility” thành baseline sạch duy nhất.

File SQL hiện tại có header table count và nhiều đoạn patch chồng lên nhau, trong khi nội dung đã có nhiều schema enterprise mới. Header mô tả 98 logical tables, 8 partition child tables, 106 physical tables, nhưng file sau đó có thêm nhiều section V4/V5 như notifications/activity/messaging.

Output bắt buộc

Tạo cấu trúc:

backend/database/baseline/
  001_extensions.sql
  002_roles_and_schemas.sql
  003_functions.sql
  004_tables.sql
  005_constraints.sql
  006_indexes.sql
  007_triggers.sql
  008_rls.sql
  009_seed_system_data.sql
  010_comments.sql
  README.md
Quy tắc baseline
1. Không dùng patch-style trong baseline

Không được để:

ALTER TABLE ... ADD COLUMN ...
COMMENT ON TABLE ... DEPRECATED ...
REVOKE app FROM legacy table ...

với bảng legacy đã quyết định xóa.

Baseline sạch phải là:

CREATE TABLE ...
CREATE INDEX ...
CREATE POLICY ...
2. Không giữ legacy table

Xóa toàn bộ định nghĩa, index, trigger, policy, comment liên quan đến các bảng legacy đã chốt.

3. Canonical ownership

Áp dụng đúng:

notifications.*  = notification center
activity.*       = user-facing activity feed
audit.*          = compliance/security
messaging.*      = outbox/inbox/processed events
ops.*            = idempotency/job locks/import/export/cleanup
events.*         = durable event log
4. Header phải tự kiểm chứng được

README phải có SQL verification:

SELECT table_schema, count(*)
FROM information_schema.tables
WHERE table_type = 'BASE TABLE'
  AND table_schema IN (
    'identity','workspace','governance','work','docs','collab',
    'automation','integration','billing','reporting','search','ops',
    'authz','events','messaging','notifications','audit','analytics','activity'
  )
GROUP BY table_schema
ORDER BY table_schema;
Prompt triển khai Phase 1
Bạn là senior PostgreSQL database architect.

Nhiệm vụ: refactor file database hiện tại thành clean baseline SQL nhiều file cho Notrelix.

Hướng bắt buộc:
- Đây là dự án chưa có production data.
- Không giữ compatibility/legacy tables.
- Không tạo migration patch chồng lên patch.
- Không đưa option.
- Không sửa code C# trong phase này.

Input:
- File SQL hiện tại nằm trong repo hoặc docs/database hiện tại. Hãy tìm file schema v5/final hiện có.
- Đọc toàn bộ file trước khi sửa.

Output bắt buộc:
Tạo thư mục:
backend/database/baseline/

Tạo các file:
001_extensions.sql
002_roles_and_schemas.sql
003_functions.sql
004_tables.sql
005_constraints.sql
006_indexes.sql
007_triggers.sql
008_rls.sql
009_seed_system_data.sql
010_comments.sql
README.md

Quyết định bắt buộc:
1. Xóa khỏi baseline toàn bộ:
   - collab.notifications
   - collab.notification_preferences
   - collab.notification_deliveries
   - collab.unread_counters
   - collab.activity_logs
   - governance.audit_logs
   - governance.security_events
   - automation.outbox_messages
   - ops.processed_events

2. Giữ canonical:
   - notifications.notification_items
   - notifications.notification_recipients
   - notifications.notification_preferences
   - notifications.notification_deliveries
   - notifications.notification_counters
   - notifications.email_outbox
   - notifications.email_delivery_attempts
   - activity.workspace_activity_logs
   - activity.activity_read_states
   - audit.audit_logs
   - audit.security_events
   - messaging.outbox_messages
   - messaging.outbox_delivery_attempts
   - messaging.processed_events
   - events.domain_event_logs
   - ops.idempotency_keys
   - ops.job_locks
   - ops.import_jobs
   - ops.export_jobs
   - search.search_documents
   - search.search_index_jobs
   - authz.workspace_access_grants

3. Nếu có reference/policy/index/trigger/comment tới bảng đã xóa, xóa luôn reference đó.

4. File 008_rls.sql phải dùng pattern:
   DROP POLICY IF EXISTS ...
   CREATE POLICY ...

5. Function SECURITY DEFINER phải có SET search_path rõ ràng, không để search_path mặc định.

6. README.md phải ghi:
   - Thứ tự chạy các file SQL.
   - Lệnh psql chạy baseline.
   - SQL verification đếm schema/table.
   - SQL check không còn legacy tables.
   - SQL check RLS table không policy.

Verification bắt buộc:
- Dùng grep hoặc script để chứng minh không còn tên legacy table.
- In ra table count theo schema sau khi cleanup nếu có môi trường database.
- Nếu không có database, in ra static grep result và danh sách file thay đổi.
Phase 2 — DB Integrity/RLS/Index Hardening
Mục tiêu

Sau khi baseline sạch, bổ sung constraint/index/RLS để database thật sự bảo vệ tenant/data integrity.

Việc phải làm
2.1 RLS hardening

Tất cả workspace-scoped business tables phải có:

app select/insert/update policy
worker all policy
support readonly policy

Không cho notrelix_app truy cập internal tables như:

messaging.outbox_messages
messaging.outbox_delivery_attempts
messaging.processed_events
events.domain_event_logs
ops.job_locks
search.search_index_jobs
authz.workspace_access_grants

notrelix_app chỉ đọc/ghi qua API/application, không thao tác trực tiếp internal infrastructure table.

2.2 Security definer function

Mọi function SECURITY DEFINER phải có:

SET search_path = pg_catalog, target_schema, ops, authz, pg_temp

Không để mặc định.

2.3 Trigger updated_at

Thêm trigger cho mọi bảng có updated_at.

Đặc biệt phải có:

work.board_item_connections
notifications.notification_counters
collab.resource_read_states
activity.activity_read_states
2.4 Index soft delete

Mọi business table có deleted_at phải có partial active index theo query chính:

WHERE deleted_at IS NULL
2.5 Composite FK theo workspace

Mọi bảng có workspace_id + child reference phải dùng pattern:

UNIQUE (workspace_id, id)

FOREIGN KEY (workspace_id, child_id)
REFERENCES parent_schema.parent_table(workspace_id, id)

Áp dụng cho các nhóm:

workspace.team_members -> workspace.teams
workspace.spaces.parent_space_id -> workspace.spaces
work.form_questions -> work.forms + work.board_fields
work.approval_steps -> work.approval_requests
integration.calendar_event_links -> integration.calendar_integrations
reporting.dashboard_sources -> reporting.dashboards + work.boards
2.6 Billing hardening

Bổ sung tối thiểu:

billing.invoice_line_items
billing.billing_customers
billing.subscription_items
billing.plan_prices

Không dùng plans.price_cents làm nguồn pricing duy nhất.

billing.subscriptions hiện đang unique theo workspace_id, nghĩa là mỗi workspace chỉ có một subscription active/history hạn chế. Phần invoice/payment/subscription hiện cũng còn đơn giản, ví dụ billing.subscriptions, payment_methods, invoices mới ở mức cơ bản.

Prompt triển khai Phase 2
Bạn là senior PostgreSQL + SaaS multi-tenant security engineer.

Nhiệm vụ: harden DB baseline Notrelix sau Phase 1.

Không thay đổi quyết định canonical schema.
Không thêm lại legacy tables.
Không sửa Domain C# trong phase này.

Yêu cầu bắt buộc:

1. RLS
- Tất cả workspace-scoped business tables phải ENABLE ROW LEVEL SECURITY.
- Tạo policy cho:
  notrelix_app,
  notrelix_worker,
  notrelix_support_readonly.
- Internal infrastructure tables không cho notrelix_app ghi trực tiếp:
  messaging.outbox_messages,
  messaging.outbox_delivery_attempts,
  messaging.processed_events,
  events.domain_event_logs,
  ops.job_locks,
  search.search_index_jobs,
  authz.workspace_access_grants.

2. SECURITY DEFINER
- Tìm tất cả function SECURITY DEFINER.
- Mỗi function phải có SET search_path rõ ràng.
- Pattern:
  SET search_path = pg_catalog, <owning_schema>, ops, authz, pg_temp

3. updated_at triggers
- Tìm mọi bảng có updated_at.
- Bảng nào thiếu trigger thì thêm vào 007_triggers.sql.
- Đảm bảo có trigger cho:
  work.board_item_connections,
  notifications.notification_counters,
  collab.resource_read_states,
  activity.activity_read_states.

4. Soft-delete indexes
- Tìm mọi bảng có deleted_at.
- Mỗi bảng business có deleted_at phải có ít nhất một partial active index WHERE deleted_at IS NULL theo query chính.
- Không thêm index vô nghĩa chỉ trên id nếu không phục vụ query.

5. Composite workspace FK
- Với bảng có workspace_id và reference đến parent cũng workspace-scoped, thêm UNIQUE(workspace_id, id) ở parent nếu thiếu.
- Thêm composite FK để chống reference nhầm workspace.

6. Billing hardening
- Thêm bảng:
  billing.billing_customers
  billing.plan_prices
  billing.subscription_items
  billing.invoice_line_items
- Không xóa plans/subscriptions/invoices hiện có.
- plans giữ metadata sản phẩm.
- plan_prices giữ giá theo interval/currency/provider.
- subscription_items giữ quantity/price mapping.
- invoice_line_items giữ dòng invoice.
- Thêm index cho subscription status, current_period_end, invoice status/due_at, entitlement feature, billing event provider/external id.

Verification:
- Tạo script SQL check:
  a. RLS enabled table without policy = 0
  b. SECURITY DEFINER without SET search_path = 0
  c. updated_at column without trigger = 0
  d. legacy tables exist = 0
  e. soft-delete tables without active partial index = 0
- Nếu có test database, chạy toàn bộ baseline.
- Nếu không có database, tạo file docs/backend/hardening/schema-verification.sql chứa toàn bộ verification queries.
Phase 3 — Domain Critical Rules
Mục tiêu

Siết các lỗi Critical/High trong Domain trước khi làm Application.

Audit hiện tại chỉ rõ Domain có 10 Rules classes rỗng, không gọi Guard.MaxLength, có rule quan trọng nhưng chưa được gọi, và nhiều aggregate thiếu state transition guards.

Rule bắt buộc
3.1 MaxLength

Áp dụng:

Board.Title                         255
Board.Description                   5000
Board.ItemKeyPrefix                 10
BoardItem.Name                      500
BoardField.Name                     100
BoardGroup.Title                    255
BoardView.Name                      255
Workspace.Name                      160
Space.Name                          160
Team.Name                           160
User.Name / DisplayName             100 hoặc 160 theo DB
CustomRole.Name                     100
CustomRole.Description              500
AuditLog.Action                     255
Comment.Content                     10000
Page.Title                          500
Block content text                  theo block type
3.2 Slug

Bắt buộc dùng Slug.Create() trong aggregate/domain factory, không dùng raw:

slug.Trim().ToLowerInvariant()
3.3 Email

WorkspaceInvitation phải dùng Email value object hoặc private normalized email field được tạo từ Email.Create().

Không cho factory nhận raw email rồi tự lowercase.

3.4 Permission grant

ResourcePermission.Grant() không được tạo permission nếu không biết actor/granter level.

Chữ ký bắt buộc theo hướng:

public static ResourcePermission Grant(
    Guid workspaceId,
    ResourceRef resource,
    PermissionSubject subject,
    PermissionLevel requestedLevel,
    PermissionLevel granterLevel,
    Guid grantedByUserId,
    DateTimeOffset now)

Trong method phải gọi:

PermissionRules.EnsureCanGrant(granterLevel, requestedLevel);
PermissionRules.EnsureCanAssignOwner(granterLevel, requestedLevel);
3.5 Email normalization

Chốt chuẩn:

Email.Value = lowercase
User.NormalizedEmail = uppercase
DB normalized_email = uppercase
Lookup dùng NormalizedEmail uppercase
Email value object dùng cho display/canonical email lowercase

Không cần ép cả hai giống nhau. Quan trọng là không so sánh Email.Value với NormalizedEmail.

Prompt triển khai Phase 3
Bạn là senior DDD engineer cho Notrelix Domain layer.

Nhiệm vụ: triển khai Critical Domain Rules, không sửa Application/Infrastructure trừ khi build bắt buộc.

Không đưa option.
Không làm rộng sang feature khác.
Không viết rule ở handler nếu rule thuộc Domain.

Yêu cầu bắt buộc:

1. Guard.MaxLength
- Tìm Guard.MaxLength trong Common/Guard.cs.
- Áp dụng max length ở aggregate/value object/factory/mutation method cho:
  Board.Title = 255
  Board.Description = 5000
  Board.ItemKeyPrefix = 10
  BoardItem.Name = 500
  BoardField.Name = 100
  BoardGroup.Title = 255
  BoardView.Name = 255
  Workspace.Name = 160
  Space.Name = 160
  Team.Name = 160
  User display/name = 100 hoặc 160 theo entity/DB mapping hiện tại
  CustomRole.Name = 100
  CustomRole.Description = 500
  Comment.Content = 10000
  Page.Title = 500
  Block text content theo block type
- Không chỉ thêm DB max length. Domain phải reject trước.

2. Slug
- Workspace.Create và mọi nơi nhận slug phải gọi Slug.Create().
- Không được dùng raw slug.Trim().ToLowerInvariant() trong aggregate.
- Nếu property hiện là string, vẫn normalize bằng Slug.Create(slug).Value.
- Thêm tests invalid slug:
  "hello world",
  "hello_world",
  "Hello",
  "a!",
  empty/null.

3. WorkspaceInvitation email
- Dùng Email.Create(email) để validate và normalize.
- Không accept raw arbitrary string.
- Nếu entity vẫn lưu string/citext, giá trị lưu phải lấy từ Email.Value.
- Thêm tests invalid email.

4. PermissionRules
- ResourcePermission.Grant phải nhận granterLevel.
- Gọi PermissionRules trong Domain.
- Không cho Viewer/Commenter/Editor grant Owner/Manager nếu rule không cho phép.
- PermissionRule.Create cũng phải validate owner-level assignment nếu tạo rule có Owner-level effect.
- Thêm tests permission hierarchy.

5. Email normalization
- Giữ contract:
  Email.Value lowercase.
  User.NormalizedEmail uppercase.
  Login/register query dùng uppercase normalized.
- Không so sánh Email.Value với NormalizedEmail trực tiếp.
- Update tests cho User.Create, User.UpdateEmail, login uniqueness nếu có.

Tests bắt buộc:
- Thêm hoặc cập nhật Domain unit tests cho từng rule.
- Boundary tests: đúng max length pass, vượt 1 ký tự fail.
- Slug/email invalid fail.
- Permission grant invalid fail.

Verification:
- dotnet build backend/backend.slnx
- dotnet test backend/tests/ --filter Domain
- Nếu filter không đúng project hiện tại, chạy toàn bộ dotnet test backend/tests/
Phase 4 — Domain State Machine & Versioning
Mục tiêu

Mọi mutation phải có:

EnsureNotDeleted()
EnsureNotArchived()/EnsureActive() nếu cần
Business rule guard
State transition hợp lệ
IncrementVersion()
Domain event nếu business-significant

Audit đã chỉ ra nhiều mutation thiếu IncrementVersion(), Board archived vẫn cho edit, FieldValueValidator chưa enforce settings, BoardItem.AssignParentItem chưa validate parent cùng board, User.UpdateEmail fire event dù email không đổi, và ResourcePermission.Revoke có nguy cơ triple-event.

Quy tắc bắt buộc
4.1 Versioning

Mọi aggregate mutation đổi state phải gọi IncrementVersion().

Không gọi nếu method no-op early return.

4.2 Archived guard

Các aggregate có trạng thái archived phải có:

private void EnsureNotArchived()
{
    if (IsArchived || Status == Archived) throw ...
}

Áp dụng cho:

Board
Workspace
Space
Team
AutomationRule
Dashboard
Page nếu có archived
4.3 Field validation

FieldValueValidator phải enforce:

Text maxLength
LongText maxLength
Number min/max
Date min/max nếu settings có
Status/Select option tồn tại
MultiSelect options tồn tại
User/People ids không duplicate
4.4 BoardItem parent

AssignParentItem phải nhận parent board id hoặc parent item snapshot, không chỉ parent id.

Chữ ký bắt buộc:

public void AssignParentItem(BoardItem parentItem, DateTimeOffset now)

Rule:

parentItem != null
parentItem.WorkspaceId == WorkspaceId
parentItem.BoardId == BoardId
parentItem.Id != Id
no cycle
4.5 User.UpdateEmail

Nếu email không đổi sau normalize, return sớm và không fire event.

4.6 ResourcePermission.Revoke

Không dùng SoftDelete() nếu nó fire generic event rồi lại fire revoked event.

Chốt hướng:

Revoke = business state transition
Set IsRevoked = true
Set RevokedAt/RevokedBy
Set DeletedAt nếu schema yêu cầu exclude active lookup
Fire exactly one ResourcePermissionRevokedEvent
IncrementVersion once
Prompt triển khai Phase 4
Bạn là senior DDD engineer.

Nhiệm vụ: harden Domain state transitions và optimistic versioning.

Không sửa DB schema trong phase này.
Không chuyển business rules vào Application handlers.
Không thêm feature mới.

Yêu cầu bắt buộc:

1. Mutation contract
- Tìm mọi public method trong AggregateRoot/Entity có thay đổi state.
- Với AggregateRoot mutation, đảm bảo:
  EnsureNotDeleted()
  EnsureNotArchived()/EnsureActive() nếu entity có trạng thái archived/disabled/revoked
  business rule guard
  no-op early return trước khi IncrementVersion và AddDomainEvent
  IncrementVersion() đúng 1 lần khi state thật sự đổi
  AddDomainEvent nếu mutation business-significant

2. Identity versioning
Thêm IncrementVersion cho:
- UserProfile.UpdateTimezone
- UserProfile.UpdateLocale
- UserProfile.UpdateTheme
- UserProfile.UpdatePreferences
- UserSession.Revoke
- UserSession.Expire
- UserMfaMethod.Verify
- UserMfaMethod.SetAsPrimary
- UserMfaMethod.UnsetAsPrimary
- UserMfaMethod.Disable
- UserSecuritySettings.EnableMfa
- UserSecuritySettings.DisableMfa
- UserSecuritySettings.RequirePasswordChangeNow
- UserSecuritySettings.MarkPasswordChanged
- UserSecuritySettings.UpdateSettings

3. Archived guards
- Board archived không cho Rename, UpdateDescription, UpdateBackground, ChangeVisibility, SetDefaultGroup, GenerateNextItemIdentity.
- Workspace archived không cho Rename, UpdateSettings, AssignToAccount hoặc mutation tương đương.
- Space/Team archived không cho rename/update/move membership mutation nếu không hợp lệ.

4. Field value validation
- FieldValueValidator phải enforce FieldSettings:
  Text/LongText maxLength
  Number min/max
  Date min/max nếu có
  Select/Status option id tồn tại
  MultiSelect option ids tồn tại và không duplicate
- BoardField.UpdateFormula phải validate formula expression bằng FormulaExpression/FormulaRules hiện có.
- FieldEngineRules.EnsureValidTypeTransition không được để empty stub.

5. BoardItem parent
- Đổi AssignParentItem để nhận BoardItem parentItem hoặc parent snapshot đủ WorkspaceId/BoardId/Id.
- Validate parent cùng WorkspaceId và BoardId.
- Validate no cycle.
- Update callers/tests tương ứng.

6. User.UpdateEmail
- Nếu email normalize không đổi thì return sớm.
- Không IncrementVersion.
- Không fire event.

7. ResourcePermission.Revoke
- Revoke phải fire đúng một event ResourcePermissionRevokedEvent.
- Không fire cả SoftDeletedEvent lẫn RevokedEvent.
- IncrementVersion đúng một lần.

Tests bắt buộc:
- Mỗi mutation trên phải có at least 1 test success và 1 test invalid transition.
- Test archived board cannot edit.
- Test User.UpdateEmail same email no event/no version increment.
- Test ResourcePermission.Revoke emits exactly one event.

Verification:
- dotnet build backend/backend.slnx
- dotnet test backend/tests/ --filter Domain
Phase 5 — Domain Rules theo từng bounded context
Mục tiêu

Không còn Rules class rỗng. Rule class nào không có logic thật thì xóa; rule nào giữ thì phải được aggregate gọi.

Audit đã nêu các Rules classes rỗng như CommentRules, NotificationRules, AttachmentRules, SubscriptionRules, PlanRules, EntitlementRules, IntegrationRules, CalendarSyncRules, WebhookRules, PageRules, WorkspaceInvitationRules.

Rule bắt buộc theo schema
identity
User:
- email valid
- normalized email uppercase
- display name max length
- suspended/deleted user không login/session active mới

UserSession:
- Revoke chỉ từ Active
- Expire chỉ từ Active
- không revoke/expire lại

MFA:
- verified method mới được primary
- disabled method không được primary
- chỉ một primary method active
workspace
Workspace:
- name max 160
- slug valid
- archived workspace không update
- deleted workspace không restore nếu owner deleted/suspended

WorkspaceMember:
- không remove/downgrade/suspend owner cuối cùng
- invited/pending member không thể active nếu invitation expired
- role transition hợp lệ

WorkspaceInvitation:
- email valid
- role chỉ Admin/Member/Guest
- không duplicate pending invitation cùng workspace/email
- expired invitation không accept
- revoked invitation không accept
governance
Permission:
- actor không grant cao hơn quyền mình
- Owner-level chỉ Owner/Admin rule hợp lệ
- expired/revoked permission không active
- field permission không grant edit nếu không view

CustomRole:
- system role không rename/delete
- permission code phải thuộc registry
- role name unique trong workspace

ShareLink:
- disabled/expired/deleted link không dùng
- public link không được Owner/Manager
work
Board:
- archived không edit
- deleted không edit
- default group không được xóa
- item key prefix max 10, uppercase slug-like

BoardGroup:
- title max 255
- không delete default group
- position valid

BoardField:
- name max 100
- type transition rule rõ
- formula expression valid
- formula circular dependency blocked
- settings valid theo type

BoardItem:
- name max 500
- parent same board/workspace
- no cycle
- deleted parent không assign
- completed item rules nếu có
docs
Page:
- title max 500
- archived/deleted page không edit
- parent page cùng workspace
- page template valid

Block:
- content theo block type
- block parent/page cùng workspace
- position valid
collab
Comment:
- content max 10000
- parent comment cùng target/workspace
- deleted comment không reply/edit
- resolved thread transition hợp lệ

Reaction:
- unique target/user/emoji
- emoji valid non-empty max length

Attachment:
- file size max theo rule
- mime type allowed
- resource ref valid
automation
AutomationRule:
- disabled rule không execute
- trigger/condition/action JSON schema valid
- rule count limit theo entitlement

AutomationExecution:
- Pending -> Running -> Succeeded/Failed/Cancelled
- không transition ngược

AiAgent:
- status transition state machine
- disabled agent không run
- tool permission/scope phải valid
integration
IntegrationConnection:
- inactive/error connection không sync
- secret version required when enabled
- status transition valid

WebhookSubscription:
- enable/disable/rotate secret fire domain events
- disabled webhook không deliver
- URL valid https

CalendarIntegration:
- link event chỉ khi connection active
- external event id unique per connection
billing
Plan:
- code unique
- price không âm
- archived plan không dùng cho subscription mới

Subscription:
- valid status transitions
- cancel subscription triggers entitlement revocation
- expired/cancelled không grant new usage

Entitlement:
- restore phải active lại nếu business rule cho phép
- revoked/disabled entitlement không authorize feature

UsageMetric:
- check limit trước khi fire exceeded event
- increase không âm
reporting/analytics
Dashboard:
- max widget count
- deleted dashboard không update
- widget position valid

ReportingSnapshot:
- workspace/report type required
- snapshot data json not null
Prompt triển khai Phase 5
Bạn là senior DDD domain modeler.

Nhiệm vụ: implement toàn bộ Rules classes còn rỗng hoặc chưa được wire vào aggregate.

Không tạo placeholder.
Không để rule class tồn tại chỉ có comment.
Không đưa business rule vào Application nếu nó là invariant của aggregate.
Không thêm feature ngoài danh sách.

Yêu cầu bắt buộc:

1. Tìm tất cả file *Rules.cs trong Notrelix.Domain.
2. Với mỗi Rules class:
   - Nếu có rule thật: implement method cụ thể và aggregate phải gọi.
   - Nếu không còn cần: xóa class và xóa using/reference.
3. Implement rules theo bounded context:

Identity:
- UserSession transition Active -> Revoked/Expired only.
- Mfa method verified before primary.
- Disabled method cannot be primary.

Workspace:
- WorkspaceRules.ValidateName used by Create/Rename.
- SpaceRules.ValidateName used by Create/Rename.
- TeamRules.ValidateName used by Create/Rename.
- WorkspaceInvitationRules validates email/role/status/expiry/duplicate pending.
- Last owner protection covers remove, downgrade, suspend.

Governance:
- PermissionRules wired into ResourcePermission and PermissionRule.
- CustomRole system protection.
- ShareLink disabled/expired/deleted guards.

Work:
- Board archived/deleted guards.
- BoardGroup cannot delete default group.
- BoardField settings/formula/type transition rules.
- BoardItem parent same board/workspace and no cycle.

Docs:
- Page title max length.
- Parent page same workspace.
- Block content validation by block type.

Collab:
- Comment content max length.
- Parent comment same target/workspace.
- Reaction uniqueness rule method.
- Attachment size/mime/resource validation.

Automation:
- AutomationRule disabled cannot execute.
- AutomationExecution state machine.
- AiAgent status state machine.

Integration:
- IntegrationConnection active requirement.
- WebhookSubscription events for Enable/Disable/RotateSecret.
- CalendarIntegration.LinkEvent requires active connection.

Billing:
- Subscription state machine.
- Cancel subscription must cause entitlement revocation via Domain event or Application event handler, not silent state drift.
- Entitlement Restore must make status consistent.
- UsageMetric checks limit before exceeded event.
- BillingEvent MarkProcessed/MarkFailed/MarkIgnored methods.

Reporting/Analytics:
- Dashboard max widget count.
- Widget update guards.
- ReportingSnapshot validation.

Tests:
- Every implemented rule gets unit tests.
- Empty Rules classes count must be zero.
- Grep check:
  no file containing only "// Rules for"
  no empty EnsureValid method.
  no stub method with empty body.

Verification:
- dotnet build backend/backend.slnx
- dotnet test backend/tests/ --filter Domain
- Print list of Rules classes changed/deleted.
Phase 6 — EF Core Mapping + Migration Strategy
Chốt hướng

Dùng hybrid EF Core:

EF Core owns:
- domain/business tables
- entity mapping
- relationships
- normal indexes
- value conversions
- concurrency tokens

Raw SQL migrations own:
- PostgreSQL roles
- schemas
- extensions
- RLS policies
- SECURITY DEFINER functions
- partition setup
- advanced GIN/trigram/tsvector indexes nếu EF khó quản lý
- cleanup functions

Không dùng pure raw SQL cho toàn bộ vì sẽ làm lệch Domain/EF mapping. Không dùng pure EF vì RLS/policies/functions/partitions sẽ rất khó sạch.

Mapping rule
Domain aggregate -> EF entity config
Projection/infra table -> Infrastructure persistence model, not Domain aggregate

Không tạo Domain entity cho:

search.search_index_jobs
search.search_documents nếu là projection
ops.idempotency_keys
ops.job_locks
ops.import_jobs
ops.export_jobs
messaging.outbox_messages
messaging.processed_events
events.domain_event_logs
authz.workspace_access_grants
notifications.email_outbox nếu là delivery infra
activity.workspace_activity_logs nếu là projection
analytics.*

Có thể có Infrastructure models cho các bảng này, nhưng không đặt trong Domain.

Prompt triển khai Phase 6
Bạn là senior EF Core/PostgreSQL architect cho Notrelix.

Nhiệm vụ: đồng bộ EF Core mapping với clean database baseline.

Hướng bắt buộc:
- Hybrid EF Core + raw SQL migration.
- EF Core quản lý business/domain tables.
- Raw SQL quản lý extensions, schemas, roles, RLS, SECURITY DEFINER functions, partitions, advanced PostgreSQL features.
- Projection/infra tables không được map thành Domain aggregate.

Không đưa option.
Không sửa Domain rule trừ khi mapping bắt buộc phát hiện lỗi.

Yêu cầu:

1. Inventory DbContext hiện tại
- Tìm ApplicationDbContext hoặc các DbContext hiện có.
- Liệt kê DbSet hiện có.
- Xác định DbSet nào là Domain business table, DbSet nào là projection/infra.

2. Remove wrong Domain mapping
Không map các bảng sau thành Domain aggregate:
- search.search_documents
- search.search_index_jobs
- ops.idempotency_keys
- ops.job_locks
- ops.import_jobs
- ops.export_jobs
- messaging.outbox_messages
- messaging.outbox_delivery_attempts
- messaging.processed_events
- events.domain_event_logs
- authz.workspace_access_grants
- notifications.email_outbox
- notifications.email_delivery_attempts
- activity.workspace_activity_logs
- analytics.workspace_usage_daily
- analytics.feature_usage_daily

Nếu cần truy cập các bảng này, tạo Infrastructure persistence model riêng trong Infrastructure/Data hoặc Infrastructure/Messaging/Search/Ops.

3. Business entity mapping
- Mỗi aggregate root phải có:
  schema
  table
  key
  version concurrency token
  deleted_at nếu soft delete
  max length đúng với Domain/DB
  enum conversion rõ ràng
  owned value object conversion nếu có
  indexes/unique constraints tương ứng DB

4. Migration
- Tạo initial baseline migration clean.
- Raw SQL migration phải gọi các file baseline SQL hoặc embed SQL có thứ tự rõ.
- Không tạo migration ALTER TABLE patch lộn xộn nếu chưa có production data.

5. Verification
- dotnet ef migrations add InitialEnterpriseBaseline nếu migrations chưa clean.
- dotnet ef database update trên dev database.
- dotnet build backend/backend.slnx
- dotnet test backend/tests/
- Kiểm tra generated migration không tạo lại legacy tables.
Phase 7 — Application CQRS/Pipeline Contract
Mục tiêu

Application không chứa invariant cốt lõi của Domain, nhưng chịu trách nhiệm:

authorization
workspace context
idempotency
transaction
expectedVersion
entitlement
cache invalidation
outbox enqueue
realtime after commit
activity/notification projection trigger
Marker bắt buộc
ICommand<TResponse>
IQuery<TResponse>
IWorkspaceRequest
IRequirePermission
ITransactionalRequest
IExpectedVersionRequest
IIdempotentRequest
IRequireEntitlement
ICacheableQuery
IInvalidateCacheRequest
IAuditableRequest
IActivityRequest
IRealtimeRequest
Pipeline order bắt buộc
1. Logging
2. Validation
3. WorkspaceContext
4. Authorization
5. Idempotency
6. Entitlement
7. Concurrency/ExpectedVersion
8. Transaction
9. Handler
10. OutboxIntegrationEventMapping
11. CacheInvalidation
12. RealtimeAfterCommit
13. ExceptionMapping
Handler rule

Handler không được gọi SaveChangesAsync lung tung nếu request là transactional. TransactionBehavior là nơi commit.

Chỉ exception:

non-transactional read/query
explicit infrastructure job
streaming/background worker special case
Prompt triển khai Phase 7
Bạn là senior Application Layer architect cho Clean Architecture/CQRS.

Nhiệm vụ: chuẩn hóa Application layer Notrelix theo CQRS pipeline contract.

Không sửa Domain invariant nếu không cần.
Không đưa option.
Không làm frontend.

Yêu cầu:

1. Tạo/chuẩn hóa marker interfaces:
- ICommand<TResponse>
- IQuery<TResponse>
- IWorkspaceRequest
- IRequirePermission
- ITransactionalRequest
- IExpectedVersionRequest
- IIdempotentRequest
- IRequireEntitlement
- ICacheableQuery
- IInvalidateCacheRequest
- IAuditableRequest
- IActivityRequest
- IRealtimeRequest

2. Chuẩn hóa ResourceRef/PermissionRef/FeatureCode/CacheInvalidationKey/RealtimeTopic nếu chưa có.

3. Implement pipeline order:
Logging
Validation
WorkspaceContext
Authorization
Idempotency
Entitlement
Concurrency/ExpectedVersion
Transaction
Handler
OutboxIntegrationEventMapping
CacheInvalidation
RealtimeAfterCommit
ExceptionMapping

4. Transaction rule:
- Command có ITransactionalRequest không tự SaveChangesAsync trong handler.
- TransactionBehavior commit một lần.
- Domain events được collect trong transaction.
- Integration events được map và ghi messaging.outbox_messages trong cùng transaction.
- Realtime/cache invalidation chạy sau commit.

5. Authorization:
- IRequirePermission không tự query lung tung trong handler.
- AuthorizationBehavior gọi IPermissionEvaluator.
- Fine-grained permission dùng governance model.
- RLS chỉ là defense layer, không thay thế permission engine.

6. ExpectedVersion:
- Collaborative mutations trên board/item/field/page phải implement IExpectedVersionRequest.
- Behavior check version trước mutation hoặc repository load by expected version theo pattern hiện tại.

7. Idempotency:
- IIdempotentRequest dùng ops.idempotency_keys.
- Same key + same request fingerprint trả lại response cũ.
- Same key + different fingerprint reject.

8. Verification:
- Không còn SaveChangesAsync trong handler transactional, trừ whitelist có comment.
- dotnet build backend/backend.slnx
- dotnet test backend/tests/
Phase 8 — Infrastructure Outbox/RLS/Authz/Projection
Chốt hướng event
LocalDomainEvent:
- in-process only
- không persist DB
- không outbox

DurableDomainEvent:
- persist events.domain_event_logs nếu cần audit/replay/projection nội bộ
- không publish external bus trực tiếp

IntegrationEvent:
- persist messaging.outbox_messages
- dispatcher publish qua MassTransit/RabbitMQ/in-memory tùy environment
- consumer idempotency dùng messaging.processed_events

File SQL hiện có cũng comment messaging.outbox_messages phải là IntegrationEvent-only theo application convention.

Projection bắt buộc
authz.workspace_access_grants
search.search_documents
activity.workspace_activity_logs
notifications.notification_items / recipients
analytics.workspace_usage_daily

là projection/read model/infra storage, không phải aggregate source of truth.

Prompt triển khai Phase 8
Bạn là senior Infrastructure architect cho .NET, EF Core, PostgreSQL, MassTransit.

Nhiệm vụ: hoàn thiện Infrastructure event/outbox/RLS/authz/projection layer.

Không đưa option.
Không đổi quyết định event model.

Event model bắt buộc:
1. LocalDomainEvent:
   - publish in-process qua MediatR sau SaveChanges hoặc trong transaction tùy current design đã chứng minh an toàn.
   - không ghi outbox.
   - không ghi messaging.

2. DurableDomainEvent:
   - ghi events.domain_event_logs nếu được đánh dấu durable.
   - dùng cho internal audit/replay/projection.
   - không tự động áp dụng cho mọi DomainEvent.

3. IntegrationEvent:
   - map từ DomainEvent bằng IIntegrationEventMapper.
   - ghi vào messaging.outbox_messages trong cùng transaction với aggregate mutation.
   - dispatcher publish qua IIntegrationEventBus/MassTransit.
   - consumer idempotency ghi messaging.processed_events.

Yêu cầu triển khai:

1. Outbox
- Xóa hoặc bỏ sử dụng automation.outbox_messages.
- Chỉ dùng messaging.outbox_messages.
- OutboxMessage có:
  event_id
  message_name
  schema_version
  payload_json
  headers_json
  source_context
  aggregate_type
  aggregate_id
  workspace_id
  actor_user_id
  correlation_id
  causation_id
  status
  retry_count
  next_attempt_at
  locked_by
  locked_until
  occurred_at
  created_at
- Claim pending messages bằng lock timeout an toàn.
- Retry exponential backoff.
- Dead-letter sau max attempts.

2. Processed events
- Chỉ dùng messaging.processed_events.
- Unique(event_id, consumer_name).
- Consumer wrapper check trước khi xử lý.
- Ghi processed trong cùng transaction với side effects của consumer.

3. Authz projection
- Implement consumer/update service cho authz.workspace_access_grants từ:
  WorkspaceMemberCreated/RoleChanged/Suspended/Removed
  CustomRoleChanged
  PermissionChanged nếu có
- Projection fail closed.
- Có reconciliation job rebuild grants theo workspace.

4. RLS context
- Request middleware set:
  app.current_user_id
  app.current_workspace_id
  app.request_scope
- Set bằng SET LOCAL trong transaction/connection scope.
- Không leak context giữa pooled connections.

5. Search projection
- Search indexing jobs đọc search.search_index_jobs.
- search.search_documents là projection.
- Không query source context trực tiếp trong search service nếu không qua event/projection contract.

6. Notifications/activity
- Notification projection tạo notifications.notification_items + recipients.
- Activity projection tạo activity.workspace_activity_logs.
- Không ghi collab.notifications/collab.activity_logs.

Verification:
- Integration test outbox persists IntegrationEvent in same transaction.
- Dispatcher publishes and marks status.
- Duplicate consumer event ignored by messaging.processed_events.
- RLS test: user A không đọc được workspace B.
- Authz projection test: removed member mất access.
- dotnet build backend/backend.slnx
- dotnet test backend/tests/
Phase 9 — Tests/CI/Verification
Test bắt buộc
Domain tests
MaxLength boundary
Slug validation
Email validation
Permission hierarchy
Archived/deleted guards
State transition invalid
Version increment
Domain event count
Field settings validation
BoardItem parent same board
Billing subscription entitlement cascade
Integration active connection guard
Database tests
baseline applies cleanly
legacy tables do not exist
RLS enabled tables have policy
SECURITY DEFINER functions have search_path
updated_at tables have trigger
soft-delete tables have active partial index
workspace composite FK prevents cross-workspace mismatch
Application tests
pipeline order
transaction commits once
handler SaveChangesAsync forbidden for transactional request
idempotency same key same response
idempotency same key different payload reject
expectedVersion conflict
authorization denied
entitlement denied
Infrastructure tests
outbox persisted in same transaction
dispatcher claim/retry/dead-letter
processed event idempotency
authz projection update
RLS context no leak
search/activity/notification projection write correct tables
CI gates bắt buộc
dotnet restore
dotnet build backend/backend.slnx --configuration Release
dotnet test backend/tests/ --configuration Release
database baseline apply test
schema verification SQL
grep no legacy tables
grep no empty Rules classes
grep no transactional handler SaveChangesAsync except whitelist
Prompt triển khai Phase 9
Bạn là senior backend QA/CI engineer.

Nhiệm vụ: thêm test và CI gates cho backend hardening Notrelix.

Không đổi kiến trúc.
Không bỏ qua test bằng Skip trừ khi có issue link và lý do rõ.
Không mock quá mức các rule Domain.

Yêu cầu:

1. Domain tests
Tạo tests cho:
- MaxLength boundary pass/fail.
- Slug invalid.
- WorkspaceInvitation invalid email.
- Permission grant hierarchy.
- Archived board/workspace cannot mutate.
- Identity mutations increment version.
- User.UpdateEmail same email no event.
- ResourcePermission.Revoke exactly one event.
- FieldValueValidator enforces settings.
- BoardItem parent same board/workspace.
- Billing cancellation revokes entitlements.
- Webhook enable/disable/rotate secret events.
- CalendarIntegration link requires active connection.

2. Database tests
- Apply clean baseline to empty PostgreSQL test container.
- Run verification SQL:
  legacy tables = 0
  RLS table without policy = 0
  SECURITY DEFINER without search_path = 0
  updated_at without trigger = 0
  soft-delete without active partial index = 0

3. Application tests
- Pipeline order test.
- TransactionBehavior commits once.
- Transactional handlers do not call SaveChangesAsync.
- Idempotency tests.
- Authorization denied test.
- ExpectedVersion conflict test.

4. Infrastructure tests
- Outbox same transaction.
- Dispatcher retry/dead-letter.
- Processed event duplicate ignored.
- Authz projection grants/revokes access.
- RLS user cannot access other workspace.
- Notification/activity projection writes canonical tables only.

5. CI
Update GitHub Actions:
- restore
- build release
- unit tests
- integration tests with PostgreSQL service
- schema verification SQL
- grep checks:
  no collab.notifications in baseline except forbidden list test
  no automation.outbox_messages
  no ops.processed_events
  no Rules class with only placeholder comment
  no empty EnsureValid/EnsureCan method
  no SaveChangesAsync in transactional handlers except whitelist

Output:
- Updated CI file.
- Test files.
- docs/backend/hardening/verification-report.md summarizing commands and results.


Không chạy một prompt lớn duy nhất. Chạy đúng thứ tự này:

1. Phase 0 prompt
2. Phase 1 prompt
3. Phase 2 prompt
4. Phase 3 prompt
5. Phase 4 prompt
6. Phase 5 prompt
7. Phase 6 prompt
8. Phase 7 prompt
9. Phase 8 prompt
10. Phase 9 prompt

Sau mỗi phase bắt buộc agent phải báo:

Files changed
Rules implemented
Commands run
Build/test result
Known remaining items