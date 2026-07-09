Tôi sẽ đọc trực tiếp file SQL bạn vừa gửi, thống kê schema/table/policy/index/trigger, rồi đánh giá theo từng schema: mức hoàn thiện, điểm mạnh, điểm thiếu và rủi ro khi triển khai thật.

Tôi đã thống kê được file SQL: có 19 schema, 138 bảng unique, 220 index, RLS bật trên tất cả 138 bảng, và khoảng 627 policy còn hiệu lực sau khi tính các lệnh DROP/CREATE theo thứ tự. Điểm đáng chú ý đầu tiên: phần header nói 106 physical tables nhưng nội dung thực tế là 138, nên bản “final” này chưa nhất quán metadata.

Thought for 6m 15s

Chưa hoàn thiện. File này là một baseline database rất mạnh về ý tưởng Enterprise, nhưng chưa thể gọi là “final/production-ready” vì còn 3 vấn đề lớn: boundary bị trùng, migration chưa sạch, và một số schema chưa đủ rule/index/operational contract.

Tôi đọc file notrelix-enterprise-schema-v5-0-final(1).sql. Thống kê nhanh:

Hạng mục	Kết quả
Schema	19
Bảng logical/parent	138
Partition child tables	8
Tổng table objects thực tế	~146
Index explicit	220
RLS enabled	138 bảng
Policy còn hiệu lực sau DROP/CREATE	~627
FK references	~432
Cross-schema FK	~292

Điểm đáng chú ý: phần header nói 106 physical CREATE TABLEs, nhưng nội dung thực tế có khoảng 146 table objects nếu tính cả partition children. Đây là dấu hiệu file đang là base + nhiều patch chồng lên nhau, chưa phải một baseline sạch.

Kết luận tổng thể
Mức hoàn thiện hiện tại

Tôi đánh giá khoảng 70–75% về thiết kế database Enterprise, nhưng chỉ khoảng 55–60% nếu xét khả năng dùng làm baseline sạch để bắt đầu phát triển lâu dài.

Lý do:

Tư duy bounded context tốt.
Có RLS, authz projection, outbox, processed events, job locks, import/export jobs.
Có soft delete, version, audit/activity/search/notification/event boundary.
Nhưng còn quá nhiều bảng legacy/canonical cùng tồn tại.
Một số schema bị trùng trách nhiệm.
Một số bảng có updated_at nhưng thiếu trigger.
Một số bảng soft-delete thiếu partial index active rows.
Billing, notification, integration, automation vẫn thiếu vài cấu trúc production quan trọng.
Cross-schema FK quá nhiều, nên “service-ready” mới là định hướng, chưa thật sự dễ tách service.

Nếu bạn chưa deploy production / chưa có dữ liệu thật, tôi khuyên không nên giữ kiểu compatibility patch. Nên clean lại thành một schema baseline duy nhất, bỏ legacy tables, rồi mới map EF Core.

Vấn đề toàn cục cần sửa trước
1. File chưa phải “final baseline” sạch

File có nhiều section kiểu:

Base domain schema V2
Soft delete patch
V4 Monday-style hardening
V4.6 event/messaging/notification
V4.7 RLS
V4.9 boundary correction
V5.0 activity finalization

Cách này hợp lý nếu đây là migration history, nhưng không hợp lý nếu bạn muốn một baseline sạch để bắt đầu dự án.

Nếu chưa có dữ liệu production, nên gộp thành:

001_extensions.sql
002_schemas.sql
003_functions.sql
004_tables.sql
005_indexes.sql
006_rls.sql
007_comments.sql

Không nên để file “final” chứa cả legacy correction.

2. Boundary bị trùng khá nhiều

Các cụm bị trùng rõ:

Cụm	Bảng cũ / trùng	Bảng canonical nên giữ
Notification	collab.notifications, collab.notification_preferences, collab.notification_deliveries, collab.unread_counters	notifications.*
Activity	collab.activity_logs, audit.activity_logs, activity.workspace_activity_logs	activity.workspace_activity_logs cho user feed, audit.audit_logs cho compliance
Audit/Security	governance.audit_logs, governance.security_events, audit.audit_logs, audit.security_events	audit.*
Outbox	automation.outbox_messages, messaging.outbox_messages	messaging.outbox_messages
Processed events	ops.processed_events, messaging.processed_events	chọn 1, tôi nghiêng về messaging.processed_events
Usage	billing.workspace_feature_usage, analytics.feature_usage_daily, analytics.workspace_usage_daily	billing = source of truth, analytics = aggregate projection

Nếu bạn bắt đầu mới, nên xóa bảng legacy, không giữ “compatibility” làm gì. Giữ lại sẽ làm EF mapping, repository, event handler, query service bị rối.

3. RLS tốt nhưng cần siết lại

Điểm tốt: RLS được bật cho tất cả bảng logical, có authz.workspace_access_grants làm local authorization projection. Đây là hướng đúng cho Enterprise SaaS vì không buộc mọi schema join trực tiếp sang workspace_members.

Nhưng có rủi ro:

Một số function dùng SECURITY DEFINER nhưng chưa thấy SET search_path cố định. Nên thêm SET search_path = pg_catalog, ops, authz để tránh search_path hijacking.
Support role có nhiều policy USING (true). Nếu giữ, phải có audit/break-glass logging bắt buộc.
RLS cần integration test thật với SET LOCAL app.current_user_id, app.current_workspace_id, app.request_scope.
Còn sót broad policy p_workspace_isolation trên một số bảng governance legacy như governance.audit_logs, governance.security_events.
4. Cross-schema FK quá nhiều

File có khoảng 292 cross-schema FK. Với modular monolith thì ổn. Nhưng nếu gọi là “service-ready” thì chưa thật sự sạch, vì khi tách service/database, các FK này sẽ phải bị gỡ.

Ví dụ:

work FK rất nhiều sang identity và workspace.
governance FK sang identity, workspace.
billing FK sang workspace.
reporting FK sang work.

Khuyến nghị: hiện tại vẫn có thể giữ FK cho modular monolith, nhưng nên phân loại:

Giai đoạn hiện tại: giữ FK để bảo vệ dữ liệu.
Giai đoạn service split: chuyển FK cross-context thành uuid reference only + local projection + event sync.
5. Migration chưa idempotent hoàn toàn

Nhiều đoạn dùng:

CREATE POLICY ...
CREATE TRIGGER ...

Nếu chạy lại trên database đã tồn tại policy/trigger, có thể fail. PostgreSQL không có CREATE POLICY IF NOT EXISTS.

Cần chuẩn hóa:

DROP POLICY IF EXISTS policy_name ON schema.table;
CREATE POLICY policy_name ON schema.table ...

và trigger cũng nên:

DROP TRIGGER IF EXISTS trigger_name ON schema.table;
CREATE TRIGGER ...

Nếu đây là baseline chạy một lần thì ít vấn đề hơn. Nhưng nếu bạn dùng như migration script lặp lại trong dev/CI, nó chưa đủ an toàn.

Đánh giá từng schema
1. identity — khá tốt, nhưng còn thiếu hardening bảo mật

Vai trò: người dùng, session, OAuth, MFA, token, SSO, SCIM.

Điểm mạnh:

Có citext cho email.
Có normalized_email.
Có session, login attempts, MFA methods, security settings.
Có SSO provider, API token, SCIM sync cho Enterprise.
Có RLS phân biệt self/auth/worker/support.

Điểm yếu:

email và normalized_email đều là citext UNIQUE, hơi dư thừa. Nếu đã có citext, normalized email nên là contract nghiệp vụ, không cần cả hai cùng unique nếu không có lý do rõ.
refresh_token_hash trong user_sessions nên có unique index hoặc cơ chế chống reuse rõ hơn.
email_verification_tokens và password_reset_tokens thiếu index tốt cho cleanup theo expires_at, consumed_at.
sso_providers, api_tokens, scim_directory_syncs có workspace_id, tức Identity đang vừa global vừa workspace-scoped. Không sai, nhưng phải phân rõ: user identity global, enterprise identity config workspace-scoped.
Nếu sau này tách Identity Service riêng, FK từ identity sang workspace sẽ gây khó.

Kết luận: dùng được cho modular monolith. Cần bổ sung token/session cleanup, refresh-token uniqueness, và tách rõ identity global vs workspace identity config.

2. workspace — nền tảng ổn, nhưng thiếu vài invariant quan trọng

Vai trò: workspace, member, invitation, space, team.

Điểm mạnh:

Có workspaces, workspace_members, workspace_invitations, spaces, teams, team_members.
Có unique membership theo (workspace_id, user_id).
Có soft delete/version cho workspace, space, team.
Có RLS khá đầy đủ.

Điểm yếu:

workspace_invitations chưa có unique pending invitation theo (workspace_id, email) với điều kiện status = 'Pending'.
team_members có workspace_id và team_id, nhưng cần composite FK để bảo đảm team_id thuộc đúng workspace_id.
spaces.parent_space_id nên có composite FK hoặc constraint để parent space cùng workspace.
DB không thể bảo vệ rule “không được xóa/downgrade owner cuối cùng”; phần này phải nằm ở Domain/Application, nhưng nên có transaction-level test.
workspaces.slug có unique global. Nếu slug là global URL thì ổn. Nếu slug chỉ unique trong account/tenant thì cần thiết kế lại.

Kết luận: schema workspace khá ổn, nhưng cần siết consistency giữa workspace_id và các child reference.

3. governance — mạnh về ý tưởng, nhưng đang bị lẫn với audit/authz

Vai trò: permission, role, policy, share link, governance audit legacy.

Điểm mạnh:

Có resource_permissions, field_permissions, custom_roles, permission_rules, permission_templates.
Có inheritance cache cho permission.
Có share links.
Có policy table cho workspace settings.
Đây là hướng tốt cho hệ thống kiểu Monday/ClickUp.

Điểm yếu:

resource_permissions và permission_rules có thể trùng trách nhiệm. Cần định nghĩa rõ:
resource_permissions = grant cụ thể?
permission_rules = rule engine/evaluation?
custom_role_permissions = role permission template?
resource_type/resource_id là polymorphic reference, không có FK thật. Chấp nhận được, nhưng bắt buộc cần ResourceRegistry ở Application/Domain để validate.
governance.audit_logs và governance.security_events nên bỏ nếu đã có audit.audit_logs và audit.security_events.
field_permissions, custom_role_permissions, workspace_member_role_assignments thiếu index explicit cho các query hay dùng.
Permission model đang rất mạnh nhưng có nguy cơ over-engineering nếu Application chưa có evaluator tương ứng.

Kết luận: ý tưởng Enterprise tốt, nhưng cần clean boundary. Governance không nên chứa audit/security event canonical nữa.

4. work — schema giàu nhất, đúng hướng SaaS work management, nhưng quá lớn

Vai trò: board, item, field, view, group, relation, form, approval, checklist, template, workload.

Điểm mạnh:

Bao phủ nhiều use case kiểu Monday/ClickUp:
Boards
Board groups
Fields/options
Items/values
Views
Checklists
Relations/mirror/rollup
Forms/submissions
Approval
Time tracking
Workload
Templates
Có nhiều composite FK để đảm bảo item/field/board cùng workspace.
board_item_values có cả value_json và typed columns (value_text, value_number, value_date, etc.) — tốt cho query/filter.
Có GIN index cho JSON/search một số bảng.
Có active indexes cho nhiều bảng soft-delete.

Điểm yếu:

Schema này rất lớn: 33 bảng. Về database thì được, nhưng về Domain code cần chia module con rất rõ: Boards, Items, Fields, Views, Forms, Relations, Approvals.
Một số bảng link/projection chưa có index explicit đủ tốt, ví dụ approval, saved filters, board item labels/members phụ thuộc nhiều vào unique/PK implicit.
work.board_item_connections có updated_at nhưng thiếu trigger update timestamp.
Một số child reference cần rà lại composite FK để không mismatch workspace/board.
board_relations, forms, item_dependencies, time_tracking_entries có soft delete nhưng thiếu partial active index theo thống kê.
field_options dùng is_archived nhưng không có deleted_at, cần quyết định archive-only hay soft-delete.
Rule field/formula/relation không thể chỉ dựa DB, cần Domain rule chặt: type transition, formula validation, circular dependency, relation consistency.

Kết luận: đây là schema tốt nhất về độ bao phủ sản phẩm, nhưng cũng là nơi dễ nợ kỹ thuật nhất. Không nên thêm feature mới trước khi siết rule và index cho WorkManagement.

5. docs — ổn cho tài liệu/page, nhưng còn đơn giản

Vai trò: pages, blocks, document versions, resource links, page templates.

Điểm mạnh:

Có page/block kiểu Notion-lite.
Có version snapshot.
Có resource links để liên kết docs với board/item/resource khác.
Có page templates.
Có GIN index cho blocks.content_json.

Điểm yếu:

resource_links dùng polymorphic resource pair, cần registry validation.
document_versions chưa thấy index cleanup/lookup ngoài unique (page_id, version_number).
Block content JSON mạnh nhưng dễ thành “schema-less dump” nếu không có block type contract ở Domain.
Nếu docs cần realtime/collaborative editing sau này, schema này chưa đủ cho CRDT/operation log.

Kết luận: đủ cho docs cơ bản. Chưa đủ cho collaborative document editor phức tạp.

6. collab — đang bị legacy notification/activity làm bẩn boundary

Vai trò đúng nên là: comments, reactions, mentions, attachments, watchers, presence, read state.

Điểm mạnh:

Có comments, reactions, mentions.
Có attachments.
Có resource watchers.
Có presence sessions.
Có resource read states.
Có uniqueness cho reaction/watchers.

Điểm yếu:

collab.notifications, collab.notification_preferences, collab.notification_deliveries, collab.unread_counters đã được comment là legacy, nhưng vẫn tồn tại.
collab.activity_logs cũng trùng với audit.activity_logs và activity.workspace_activity_logs.
mentions thiếu index rõ cho mentioned_user_id/workspace để lấy mention inbox.
presence_sessions thiếu TTL/expiry/status index rõ ràng.
resource_read_states có updated_at nhưng thiếu trigger.
Nếu giữ cả collab notification legacy và notifications canonical, Application rất dễ ghi nhầm bảng.

Kết luận: cần clean mạnh. Nếu bắt đầu mới, bỏ toàn bộ notification legacy khỏi collab.

7. automation — có nền tảng, nhưng nên bỏ outbox ở đây

Vai trò: automation rules, executions, scheduled jobs, templates, AI agents.

Điểm mạnh:

Có automation rules với trigger/conditions/actions JSON.
Có execution log.
Có scheduled jobs.
Có AI agent và AI agent runs.
Có template.

Điểm yếu:

automation.outbox_messages trùng với messaging.outbox_messages. Nên bỏ automation.outbox_messages.
automation_executions thiếu index theo automation_rule_id, status, created_at.
Trigger/conditions/actions đều là JSON, cần schema validation ở Application/Domain.
AI agent cần thêm safety/audit policy: tool permission, data scope, execution budget, approval mode.
ai_agents có soft delete nhưng thiếu active partial index theo workspace/name.

Kết luận: dùng được làm nền automation v1, nhưng phải chuyển outbox sang messaging và bổ sung execution/index/safety model.

8. integration — tương đối tốt, nhưng thiếu vận hành webhook/sync sâu

Vai trò: external connections, scopes, secrets, webhooks, calendar sync.

Điểm mạnh:

Có integration_connections.
Có scope và secret version.
Có webhook subscription/delivery/inbound event.
Có calendar integration/event link/sync cursor.
Secret lưu dạng reference, không lưu plain secret trực tiếp — đúng hướng.

Điểm yếu:

webhook_deliveries thiếu index mạnh cho retry/status/next attempt.
inbound_webhook_events có unique external event nhưng thiếu index xử lý theo status/received time.
integration_sync_cursors thiếu index cho sync worker.
calendar_event_links nên có index theo resource để tìm ngược từ Notrelix resource ra external event.
Cần rõ boundary giữa integration event ingest và messaging outbox.

Kết luận: đủ nền cho integration cơ bản. Chưa đủ mạnh cho production-scale webhook retry/sync observability.

9. billing — schema yếu nhất nếu muốn Enterprise SaaS thật

Vai trò: plans, subscriptions, payment methods, invoices, entitlements, usage.

Điểm mạnh:

Có plan, plan limit.
Có subscription theo workspace.
Có payment method, invoice, billing event.
Có entitlement và usage metric.
Có feature usage ledger, workspace feature usage.

Điểm yếu lớn:

Billing index quá ít so với mức cần thiết. 11 bảng billing nhưng chỉ có rất ít explicit index.
subscriptions chỉ cho workspace_id UNIQUE, nghĩa là mỗi workspace chỉ có một subscription. Có thể đủ giai đoạn đầu, nhưng Enterprise sau này thường cần history/multiple provider records.
Chưa có invoice line items.
Chưa có price/version model riêng. plans.price_cents quá đơn giản nếu có monthly/yearly/seat-based/usage-based pricing.
Chưa có coupon/discount/tax/refund/credit note.
billing_events thiếu processed status/index vận hành tốt.
entitlements chưa có status mạnh như Active/Revoked/Expired/Overridden nếu Domain của bạn đang có status.
payment_methods thiếu status, billing customer mapping rõ.
Nếu dùng Stripe, nên có bảng mapping provider customer/subscription/item/price IDs đầy đủ hơn.

Kết luận: billing hiện tại đủ MVP/SaaS nhỏ. Chưa đạt Enterprise billing. Cần harden trước khi triển khai subscription thật.

10. reporting — ổn cho dashboard config, chưa phải analytics engine

Vai trò: dashboards, widgets, sources, reporting snapshots.

Điểm mạnh:

Có dashboard và widget.
Có dashboard source.
Có snapshot.
Có soft delete/version cho dashboard/widget.
Có source resource type cho widget.

Điểm yếu:

reporting_snapshots khá đơn giản, chưa có retention/partition nếu dữ liệu lớn.
Widget source dùng polymorphic resource, cần Application validate.
Cần giới hạn số widget/dashboard ở Domain/Billing entitlement.
Nếu dashboard query realtime nhiều, schema này chỉ là config layer, chưa phải reporting warehouse.

Kết luận: ổn cho dashboard config. Không nên xem đây là full analytics/reporting platform.

11. search — đúng vai trò projection, khá sạch

Vai trò: search documents và search index jobs.

Điểm mạnh:

Được tách khỏi Domain core.
Có search_documents với search_vector, trigram title/content, tags.
Có search_index_jobs cho async indexing.
Có retry/lock fields.
Có resource uniqueness (workspace_id, resource_type, resource_id).

Điểm yếu:

resource_type polymorphic cần registry.
Nếu sau này dùng Elasticsearch/OpenSearch/Meilisearch, schema này sẽ thành local projection/index queue, không phải search engine chính.
Cần retention/cleanup cho completed index jobs.
Cần đảm bảo RLS không làm worker indexing bị block.

Kết luận: tốt cho PostgreSQL search projection. Đúng hướng nếu chưa cần search service riêng.

12. ops — khá tốt, nhưng nên giảm trùng với messaging

Vai trò: idempotency, processed events, import/export jobs, job locks.

Điểm mạnh:

idempotency_keys đầy đủ scope/key/status/response/expiry.
Có import/export jobs.
Có job locks với fencing token.
Có processed events.
Có indexes cho expiry/status/worker.

Điểm yếu:

ops.processed_events trùng với messaging.processed_events.
processed_events nên thuộc messaging/inbox nếu nó phục vụ event consumer idempotency.
ops nên giữ các technical jobs chung: idempotency, job locks, import/export.
Nếu giữ ops.processed_events, phải định nghĩa rõ khác gì với messaging.processed_events.

Kết luận: ops tốt, nhưng nên bỏ duplicate processed events hoặc đổi tên mục đích rõ hơn.

13. events — tốt nếu muốn event log, nhưng cần rõ khác outbox

Vai trò: durable domain/integration event log.

Điểm mạnh:

Có domain_event_logs.
Có event id unique, source context, event name/version.
Có aggregate/subject/workspace/correlation.
Có payload/metadata JSON.
Có nhiều index tốt.

Điểm yếu:

Cần định nghĩa rõ:
events.domain_event_logs = audit/replay log?
messaging.outbox_messages = dispatch queue?
Nếu mọi event đều ghi cả event log và outbox, phải kiểm soát duplication.
Cần retention strategy.
Cần event type registry ở code để tránh payload schema trôi.

Kết luận: tốt nếu bạn chủ động dùng event log. Nếu không, nó dễ trở thành bảng ghi cho có.

14. messaging — đây nên là canonical messaging schema

Vai trò: outbox, delivery attempts, processed events/inbox.

Điểm mạnh:

outbox_messages rất đầy đủ: event id, schema version, destination, subject, aggregate, workspace, actor, correlation, partition key, retry, lock.
Có delivery attempts.
Có processed events.
Index cho pending dispatch, timeout, source message, workspace, subject, correlation.
Đây là hướng đúng nếu sau này dùng RabbitMQ/Kafka/MassTransit.

Điểm yếu:

Bị trùng với automation.outbox_messages.
Bị trùng một phần với ops.processed_events.
Cần thêm dead-letter/archive policy rõ.
Cần dispatcher claim query được test kỹ bằng concurrent workers.
Cần thống nhất correlation/causation type: nơi dùng uuid, nơi dùng varchar(100).

Kết luận: nên giữ messaging.* làm canonical. Các outbox/processed-events cũ nên xóa hoặc migrate vào đây.

15. notifications — canonical notification model tốt

Vai trò: logical notification, recipients, preferences, delivery, email outbox, counters.

Điểm mạnh:

Tách rõ:
notification_items
notification_recipients
notification_preferences
notification_deliveries
notification_counters
email_outbox
email_delivery_attempts
Có deduplication key.
Có recipient-specific state.
Có channel delivery.
Có email outbox retry.
Có indexes khá tốt.

Điểm yếu:

Vẫn tồn tại legacy notification trong collab.
notification_counters có updated_at nhưng thiếu updated_at trigger.
Email outbox có thể chứa PII/body; cần retention/encryption/redaction policy.
Cần quyết định notification read state nằm ở notification_recipients hay activity_read_states, tránh double read-state.

Kết luận: schema notifications mới khá tốt. Nên bỏ toàn bộ notification legacy trong collab.

16. audit — tốt hơn governance audit, nên làm canonical

Vai trò: audit logs, security events, activity logs.

Điểm mạnh:

audit_logs có actor/resource/subject/correlation/before/after.
security_events có risk score/severity/ip/user agent.
activity_logs có visibility.
Index khá tốt theo workspace/time, actor/time, resource, correlation.
Phù hợp Enterprise compliance.

Điểm yếu:

Bị trùng với governance.audit_logs, governance.security_events, collab.activity_logs, activity.workspace_activity_logs.
Nếu audit là compliance log, không nên soft delete và không nên dễ update.
Cần partition/retention rõ hơn cho audit logs lớn.
Cần append-only guard, hạn chế UPDATE/DELETE.

Kết luận: nên giữ audit.* cho compliance/security. Không nên giữ audit table trong governance.

17. analytics — đơn giản, đúng hướng projection

Vai trò: daily aggregate usage.

Điểm mạnh:

Có workspace_usage_daily.
Có feature_usage_daily.
Primary key theo workspace/date/feature hợp lý.
Có non-negative check.

Điểm yếu:

Chỉ là summary projection, chưa phải analytics engine.
Chưa có event source/raw fact table nếu muốn rebuild.
Chưa có retention/downsampling strategy.
Có overlap với billing usage. Cần rõ:
billing usage = source of truth for entitlement/charging
analytics usage = read model/reporting

Kết luận: đủ cho dashboard/reporting cơ bản. Không nên dùng làm billing source of truth.

18. authz — rất đúng hướng Enterprise

Vai trò: local authorization projection cho RLS.

Điểm mạnh:

workspace_access_grants dùng (workspace_id, user_id) làm PK.
Có role codes, permission codes, admin flag.
Có source event/version để đồng bộ projection.
Giúp RLS không phụ thuộc join cross-DB.
Đây là một trong những phần thiết kế tốt nhất của file.

Điểm yếu:

Cần consumer/sync thật từ workspace_members/governance sang authz.workspace_access_grants.
Nếu projection stale, RLS có thể sai. Cần event-driven sync + reconciliation job.
Nếu user bị remove/suspended, authz projection phải update trong cùng transaction hoặc qua outbox với độ trễ được chấp nhận.
Function SECURITY DEFINER nên set search_path.

Kết luận: nên giữ. Đây là hướng chuẩn cho tenant isolation scale-ready.

19. activity — hướng canonical tốt, nhưng phải bỏ activity legacy

Vai trò: user-visible workspace activity feed và read state.

Điểm mạnh:

workspace_activity_logs giàu metadata: subject, actor, target, resource, visibility, importance, source event/message.
Có read states.
Có indexes tốt cho workspace/time, actor, resource, source event/message, JSON data.
Phù hợp làm activity feed riêng, tách khỏi audit compliance.

Điểm yếu:

Trùng với collab.activity_logs và audit.activity_logs.
workspace_activity_logs có deleted_at; cần cân nhắc activity feed có nên soft-delete hay chỉ hide/tombstone.
Cần rule rõ:
audit = immutable compliance
activity = user-facing feed, có thể hide/delete theo retention
Cần retention/archive nếu feed lớn.

Kết luận: nên giữ activity.* làm canonical user activity. Bỏ collab.activity_logs.

Những lỗi/thiếu sót cụ thể nên sửa ngay
P0 — Clean schema boundary

Nếu chưa có production data, tôi khuyên:

DROP khỏi baseline:
- collab.notifications
- collab.notification_preferences
- collab.notification_deliveries
- collab.unread_counters
- collab.activity_logs
- governance.audit_logs
- governance.security_events
- automation.outbox_messages
- ops.processed_events nếu đã dùng messaging.processed_events

Giữ canonical:

notifications.*
activity.*
audit.*
messaging.*
events.*
ops.idempotency_keys
ops.job_locks
ops.import_jobs
ops.export_jobs
P1 — Sửa consistency và migration

Cần sửa:

Header table count sai.
Không để file final chứa patch history lộn xộn.
Thêm DROP POLICY IF EXISTS trước mọi CREATE POLICY.
Thêm DROP TRIGGER IF EXISTS trước mọi CREATE TRIGGER.
Thêm SET search_path cho SECURITY DEFINER functions.
Thống nhất type của correlation_id, causation_id: chọn uuid hoặc varchar, không lẫn.
P2 — Sửa trigger/index thiếu

Các bảng có updated_at nhưng thiếu trigger:

work.board_item_connections
notifications.notification_counters
collab.resource_read_states

Một số bảng soft-delete thiếu active partial index:

automation.ai_agents
identity.api_tokens
identity.scim_directory_syncs
identity.sso_providers
work.board_relations
work.forms
work.item_dependencies
work.time_tracking_entries

Billing cần bổ sung index nhiều nhất:

billing.subscriptions(workspace_id, status)
billing.subscriptions(current_period_end)
billing.invoices(workspace_id, status, due_at)
billing.entitlements(workspace_id, feature_code, is_enabled)
billing.billing_events(provider, external_event_id)
billing.billing_events(status/created_at nếu có status xử lý)
billing.usage_metric_history(workspace_id, metric_key, recorded_at)
P3 — Sửa scope consistency

Các bảng có workspace_id cộng với child reference phải đảm bảo child thuộc cùng workspace.

Ví dụ nên rà lại:

workspace.team_members(workspace_id, team_id)
workspace.spaces(workspace_id, parent_space_id)
work.approval_steps(workspace_id, approval_request_id)
work.form_questions(workspace_id, form_id, board_field_id)
integration.calendar_event_links(workspace_id, calendar_integration_id)
reporting.dashboard_sources(workspace_id, dashboard_id, board_id)

Pattern nên dùng:

UNIQUE (workspace_id, id)

FOREIGN KEY (workspace_id, child_id)
REFERENCES schema.table(workspace_id, id)
Đánh giá cuối cùng

Hệ thống database này chưa hoàn thiện, nhưng đã có nền Enterprise khá tốt. Vấn đề không nằm ở việc thiếu nhiều bảng, mà là đang quá nhiều bảng và quá nhiều boundary chưa được quyết định dứt khoát.

Nếu mục tiêu là bắt đầu phát triển sạch, hướng đúng nhất là:

Chốt canonical schema: audit, activity, notifications, messaging, events, ops.
Xóa legacy compatibility tables vì chưa có production data.
Gộp file thành baseline sạch, không giữ patch history.
Sửa RLS/security function/migration idempotency.
Bổ sung index/trigger/scope FK còn thiếu.
Sau đó mới map EF Core + Domain aggregates.

Nói ngắn gọn: thiết kế có tư duy Enterprise, nhưng bản hiện tại chưa nên gọi là final. Nó cần một vòng “schema cleanup + boundary finalization” trước khi triển khai code thật.