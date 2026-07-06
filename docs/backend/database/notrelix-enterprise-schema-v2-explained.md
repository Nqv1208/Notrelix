# Notrelix Enterprise Schema V2 — Clean Baseline Explanation

## 1. Mục tiêu

Tài liệu này giải thích bản schema V2 được xây lại từ file V5 hiện có theo hướng SaaS Enterprise sạch hơn. Bản V2 không giữ compatibility/legacy tables vì hệ thống chưa có production data. Trọng tâm của bản này là phân biệt rõ source-of-truth, projection/read model và infrastructure/runtime storage.

## 2. Quyết định kiến trúc đã chốt

- Tenant root là `account`, không phải `workspace`.
- `workspace` là product workspace nằm bên trong account.
- Billing, SSO, SCIM, data residency root theo `account_id`.
- Notification canonical nằm ở `notifications.*`, không nằm trong `collab.*`.
- Activity feed canonical nằm ở `activity.workspace_activity_logs`, không nằm trong `collab` hay `audit`.
- Compliance/security audit canonical nằm ở `audit.*`, không nằm trong `governance`.
- IntegrationEvent outbox canonical nằm ở `messaging.outbox_messages`.
- Consumer idempotency canonical nằm ở `messaging.processed_events`.
- `events.domain_event_logs` là durable business fact log, không phải broker outbox.
- `search`, `analytics`, `activity`, `notifications` là projection/read model, có thể rebuild từ event.
- `ops` chỉ chứa runtime mechanics: idempotency key, locks, import/export, cleanup.

## 3. Schema ownership map

| Nhóm | Schema | Vai trò |
|---|---|---|
| Platform | `account` | Enterprise tenant/customer/account boundary; legal, billing, SSO, SCIM, data residency root. |
| Platform | `identity` | Global user identity, authentication, sessions, MFA, OAuth and user-owned tokens. |
| Platform | `workspace` | Product workspace, spaces, teams, workspace membership and workspace invitations. |
| Platform | `governance` | Authorization source model: custom roles, permissions, share links, policies and inheritance cache. |
| Platform | `authz` | Local RLS authorization projection generated from account/workspace/governance source state. |
| Product source-of-truth | `work` | Work-management source state: boards, groups, fields, items, views, forms, relations, approvals and workload. |
| Product source-of-truth | `docs` | Document/page/block source state. |
| Product source-of-truth | `collab` | Collaboration source state: comments, mentions, reactions, attachments, watchers, presence and read state. |
| Product source-of-truth | `automation` | Automation rules, executions, schedules, templates and AI agent execution state. |
| Product source-of-truth | `integration` | External integrations, OAuth/secret versions, outbound/inbound webhooks and calendar sync. |
| Product source-of-truth | `billing` | Account-level commercial model: customers, plans, prices, subscriptions, invoices, entitlements and usage. |
| Product source-of-truth | `reporting` | Dashboard/report configuration and report snapshots. |
| Projection/read model | `search` | Rebuildable search projection and indexing jobs. |
| Projection/read model | `notifications` | Canonical notification center, recipient state, channel delivery and email outbox. |
| Projection/read model | `activity` | Canonical user-facing activity feed projection and read state. |
| Projection/read model | `analytics` | Analytical daily projections; not billing source of truth. |
| Infrastructure/runtime | `events` | Append-only durable business event log; not broker/outbox. |
| Infrastructure/runtime | `messaging` | Canonical IntegrationEvent outbox, delivery attempts and inbox/processed-event idempotency. |
| Infrastructure/runtime | `audit` | Compliance/security audit only; append-only operational history. |
| Infrastructure/runtime | `ops` | Runtime mechanics: API idempotency, locks, import/export and cleanup runs. |

## 4. Bảng theo từng schema

### `account`

Enterprise tenant/customer/account boundary; legal, billing, SSO, SCIM, data residency root.

| Table | Loại | Ghi chú |
|---|---|---|
| `account.accounts` | business | Enterprise account/tenant root. Billing, SSO, SCIM and data residency attach here, not directly to workspace. |
| `account.account_members` | business | Enterprise account membership. Workspace membership must be scoped under this root. |
| `account.account_invitations` | business | Enterprise account invitation, separate from workspace invitation. |
| `account.account_domains` | business | account-scoped |
| `account.account_settings` | business | account-scoped |
| `account.account_regions` | business | account-scoped |
| `account.account_identity_providers` | business | Enterprise SSO provider. Moved out of identity because this is account-level tenant config. |
| `account.scim_directories` | business | account-scoped, soft-delete |
| `account.scim_sync_runs` | internal | account-scoped, worker/internal write |
| `account.workspace_routes` | business | Stable account-scoped route registry for workspace URLs. FK to workspace added after workspace table exists by application migration if needed. |

### `identity`

Global user identity, authentication, sessions, MFA, OAuth and user-owned tokens.

| Table | Loại | Ghi chú |
|---|---|---|
| `identity.users` | business | Global user identity. Enterprise tenant membership lives in account/workspace schemas. |
| `identity.user_profiles` | business | User-owned profile/preferences; one row per user. |
| `identity.user_sessions` | business | Refresh-session state. Revocation/expiry must be state-machine guarded in Domain. |
| `identity.oauth_accounts` | business | source table |
| `identity.user_security_settings` | business | source table |
| `identity.user_mfa_methods` | business | soft-delete |
| `identity.user_login_attempts` | audit | worker/internal write |
| `identity.email_verification_tokens` | internal | worker/internal write |
| `identity.password_reset_tokens` | internal | worker/internal write |
| `identity.user_api_tokens` | business | User API tokens may be account-scoped; global tokens use null account_id. |

### `workspace`

Product workspace, spaces, teams, workspace membership and workspace invitations.

| Table | Loại | Ghi chú |
|---|---|---|
| `workspace.workspaces` | business | account-scoped, workspace-scoped, soft-delete |
| `workspace.workspace_members` | business | account-scoped, workspace-scoped, soft-delete |
| `workspace.workspace_invitations` | business | account-scoped, workspace-scoped |
| `workspace.spaces` | business | account-scoped, workspace-scoped, soft-delete |
| `workspace.teams` | business | account-scoped, workspace-scoped, soft-delete |
| `workspace.team_members` | business | account-scoped, workspace-scoped, soft-delete |

### `governance`

Authorization source model: custom roles, permissions, share links, policies and inheritance cache.

| Table | Loại | Ghi chú |
|---|---|---|
| `governance.custom_roles` | business | account-scoped, workspace-scoped, soft-delete |
| `governance.custom_role_permissions` | business | account-scoped, workspace-scoped |
| `governance.workspace_member_role_assignments` | business | account-scoped, workspace-scoped |
| `governance.resource_permissions` | business | account-scoped, workspace-scoped, soft-delete |
| `governance.field_permissions` | business | account-scoped, workspace-scoped, soft-delete |
| `governance.permission_rules` | business | account-scoped, workspace-scoped, soft-delete |
| `governance.permission_templates` | business | account-scoped, workspace-scoped, soft-delete |
| `governance.workspace_policies` | business | account-scoped, workspace-scoped |
| `governance.share_links` | business | account-scoped, workspace-scoped, soft-delete |
| `governance.resource_permission_inheritance_cache` | projection | account-scoped, workspace-scoped, worker/internal write |

### `authz`

Local RLS authorization projection generated from account/workspace/governance source state.

| Table | Loại | Ghi chú |
|---|---|---|
| `authz.access_grants` | internal | RLS read model. Source of truth remains account/workspace/governance. |

### `work`

Work-management source state: boards, groups, fields, items, views, forms, relations, approvals and workload.

| Table | Loại | Ghi chú |
|---|---|---|
| `work.boards` | business | account-scoped, workspace-scoped, soft-delete |
| `work.board_groups` | business | account-scoped, workspace-scoped, soft-delete |
| `work.board_fields` | business | account-scoped, workspace-scoped, soft-delete |
| `work.field_options` | business | account-scoped, workspace-scoped |
| `work.board_items` | business | account-scoped, workspace-scoped, soft-delete |
| `work.board_item_values` | business | account-scoped, workspace-scoped |
| `work.board_item_members` | business | account-scoped, workspace-scoped |
| `work.labels` | business | account-scoped, workspace-scoped, soft-delete |
| `work.board_item_labels` | business | account-scoped, workspace-scoped |
| `work.board_views` | business | account-scoped, workspace-scoped, soft-delete |
| `work.board_view_user_preferences` | business | account-scoped, workspace-scoped |
| `work.saved_filters` | business | account-scoped, workspace-scoped, soft-delete |
| `work.board_view_pins` | business | account-scoped, workspace-scoped |
| `work.board_item_links` | business | account-scoped, workspace-scoped, soft-delete |
| `work.checklists` | business | account-scoped, workspace-scoped, soft-delete |
| `work.checklist_items` | business | account-scoped, workspace-scoped, soft-delete |
| `work.relation_field_configs` | business | account-scoped, workspace-scoped |
| `work.board_relations` | business | account-scoped, workspace-scoped, soft-delete |
| `work.board_item_connections` | business | account-scoped, workspace-scoped, soft-delete |
| `work.formula_dependencies` | business | account-scoped, workspace-scoped |
| `work.mirror_value_snapshots` | projection | account-scoped, workspace-scoped, worker/internal write |
| `work.rollup_snapshots` | projection | account-scoped, workspace-scoped, worker/internal write |
| `work.approval_requests` | business | account-scoped, workspace-scoped, soft-delete |
| `work.approval_steps` | business | account-scoped, workspace-scoped |
| `work.workload_allocations` | business | account-scoped, workspace-scoped |
| `work.board_templates` | business | account-scoped, workspace-scoped, soft-delete |
| `work.item_templates` | business | account-scoped, workspace-scoped, soft-delete |
| `work.board_subscribers` | business | account-scoped, workspace-scoped |
| `work.item_dependencies` | business | account-scoped, workspace-scoped, soft-delete |
| `work.time_tracking_entries` | business | account-scoped, workspace-scoped, soft-delete |
| `work.forms` | business | account-scoped, workspace-scoped, soft-delete |
| `work.form_questions` | business | account-scoped, workspace-scoped, soft-delete |
| `work.form_submissions` | business | account-scoped, workspace-scoped |

### `docs`

Document/page/block source state.

| Table | Loại | Ghi chú |
|---|---|---|
| `docs.pages` | business | account-scoped, workspace-scoped, soft-delete |
| `docs.blocks` | business | account-scoped, workspace-scoped, soft-delete |
| `docs.document_versions` | eventlog | account-scoped, workspace-scoped, worker/internal write |
| `docs.resource_links` | business | account-scoped, workspace-scoped |
| `docs.page_templates` | business | account-scoped, workspace-scoped, soft-delete |

### `collab`

Collaboration source state: comments, mentions, reactions, attachments, watchers, presence and read state.

| Table | Loại | Ghi chú |
|---|---|---|
| `collab.comments` | business | account-scoped, workspace-scoped, soft-delete |
| `collab.reactions` | business | account-scoped, workspace-scoped |
| `collab.mentions` | business | account-scoped, workspace-scoped |
| `collab.attachments` | business | account-scoped, workspace-scoped, soft-delete |
| `collab.resource_watchers` | business | account-scoped, workspace-scoped |
| `collab.presence_sessions` | business | account-scoped, workspace-scoped |
| `collab.resource_read_states` | business | account-scoped, workspace-scoped |

### `automation`

Automation rules, executions, schedules, templates and AI agent execution state.

| Table | Loại | Ghi chú |
|---|---|---|
| `automation.automation_rules` | business | account-scoped, workspace-scoped, soft-delete |
| `automation.automation_executions` | internal | account-scoped, workspace-scoped, worker/internal write |
| `automation.scheduled_jobs` | internal | account-scoped, workspace-scoped, worker/internal write |
| `automation.automation_templates` | business | account-scoped, workspace-scoped, soft-delete |
| `automation.ai_agents` | business | account-scoped, workspace-scoped, soft-delete |
| `automation.ai_agent_runs` | internal | account-scoped, workspace-scoped, worker/internal write |

### `integration`

External integrations, OAuth/secret versions, outbound/inbound webhooks and calendar sync.

| Table | Loại | Ghi chú |
|---|---|---|
| `integration.integration_connections` | business | account-scoped, workspace-scoped, soft-delete |
| `integration.integration_scopes` | business | account-scoped, workspace-scoped |
| `integration.integration_secret_versions` | internal | account-scoped, workspace-scoped, worker/internal write |
| `integration.webhook_subscriptions` | business | account-scoped, workspace-scoped, soft-delete |
| `integration.webhook_deliveries` | internal | account-scoped, workspace-scoped, worker/internal write |
| `integration.inbound_webhook_events` | internal | account-scoped, workspace-scoped, worker/internal write |
| `integration.calendar_integrations` | business | account-scoped, workspace-scoped, soft-delete |
| `integration.calendar_event_links` | business | account-scoped, workspace-scoped |
| `integration.integration_sync_cursors` | internal | account-scoped, workspace-scoped, worker/internal write |

### `billing`

Account-level commercial model: customers, plans, prices, subscriptions, invoices, entitlements and usage.

| Table | Loại | Ghi chú |
|---|---|---|
| `billing.billing_customers` | business | account-scoped, soft-delete |
| `billing.plans` | business | soft-delete, worker/internal write |
| `billing.plan_prices` | business | soft-delete, worker/internal write |
| `billing.plan_limits` | business | worker/internal write |
| `billing.subscriptions` | business | account-scoped, soft-delete |
| `billing.subscription_items` | business | account-scoped, soft-delete |
| `billing.payment_methods` | business | account-scoped, soft-delete |
| `billing.invoices` | business | account-scoped, soft-delete |
| `billing.invoice_line_items` | business | account-scoped |
| `billing.entitlements` | business | account-scoped, soft-delete |
| `billing.usage_metrics` | business | account-scoped |
| `billing.usage_metric_history` | eventlog | account-scoped, worker/internal write |
| `billing.feature_usage_ledger` | eventlog | account-scoped, workspace-scoped, worker/internal write |
| `billing.billing_events` | internal | account-scoped, worker/internal write |

### `reporting`

Dashboard/report configuration and report snapshots.

| Table | Loại | Ghi chú |
|---|---|---|
| `reporting.dashboards` | business | account-scoped, workspace-scoped, soft-delete |
| `reporting.dashboard_widgets` | business | account-scoped, workspace-scoped, soft-delete |
| `reporting.dashboard_sources` | business | account-scoped, workspace-scoped |
| `reporting.reporting_snapshots` | projection | account-scoped, workspace-scoped, worker/internal write |

### `search`

Rebuildable search projection and indexing jobs.

| Table | Loại | Ghi chú |
|---|---|---|
| `search.search_documents` | projection | account-scoped, workspace-scoped, worker/internal write |
| `search.search_index_jobs` | internal | account-scoped, workspace-scoped, worker/internal write |

### `notifications`

Canonical notification center, recipient state, channel delivery and email outbox.

| Table | Loại | Ghi chú |
|---|---|---|
| `notifications.notification_items` | projection | account-scoped, workspace-scoped, worker/internal write |
| `notifications.notification_recipients` | projection | account-scoped, workspace-scoped |
| `notifications.notification_preferences` | business | account-scoped, workspace-scoped |
| `notifications.notification_deliveries` | internal | account-scoped, workspace-scoped, worker/internal write |
| `notifications.notification_counters` | projection | account-scoped, workspace-scoped, worker/internal write |
| `notifications.email_outbox` | internal | account-scoped, workspace-scoped, worker/internal write |
| `notifications.email_delivery_attempts` | internal | worker/internal write |

### `activity`

Canonical user-facing activity feed projection and read state.

| Table | Loại | Ghi chú |
|---|---|---|
| `activity.workspace_activity_logs` | projection | account-scoped, workspace-scoped, soft-delete, worker/internal write |
| `activity.activity_read_states` | projection | account-scoped, workspace-scoped |

### `analytics`

Analytical daily projections; not billing source of truth.

| Table | Loại | Ghi chú |
|---|---|---|
| `analytics.workspace_usage_daily` | projection | account-scoped, workspace-scoped, worker/internal write |
| `analytics.feature_usage_daily` | projection | account-scoped, workspace-scoped, worker/internal write |

### `events`

Append-only durable business event log; not broker/outbox.

| Table | Loại | Ghi chú |
|---|---|---|
| `events.domain_event_logs` | eventlog | account-scoped, workspace-scoped, worker/internal write |

### `messaging`

Canonical IntegrationEvent outbox, delivery attempts and inbox/processed-event idempotency.

| Table | Loại | Ghi chú |
|---|---|---|
| `messaging.outbox_messages` | internal | account-scoped, workspace-scoped, worker/internal write |
| `messaging.outbox_delivery_attempts` | internal | worker/internal write |
| `messaging.processed_events` | internal | account-scoped, workspace-scoped, worker/internal write |

### `audit`

Compliance/security audit only; append-only operational history.

| Table | Loại | Ghi chú |
|---|---|---|
| `audit.audit_logs` | audit | account-scoped, workspace-scoped, worker/internal write |
| `audit.security_events` | audit | account-scoped, workspace-scoped, worker/internal write |

### `ops`

Runtime mechanics: API idempotency, locks, import/export and cleanup runs.

| Table | Loại | Ghi chú |
|---|---|---|
| `ops.idempotency_keys` | internal | account-scoped, workspace-scoped, worker/internal write |
| `ops.job_locks` | internal | worker/internal write |
| `ops.import_jobs` | internal | account-scoped, workspace-scoped, worker/internal write |
| `ops.export_jobs` | internal | account-scoped, workspace-scoped, worker/internal write |
| `ops.cleanup_runs` | internal | worker/internal write |

## 5. Các bảng bị loại khỏi V5

Bản V2 loại bỏ hoàn toàn các bảng compatibility/legacy sau khỏi baseline:

- `collab.notifications`
- `collab.notification_preferences`
- `collab.notification_deliveries`
- `collab.unread_counters`
- `collab.activity_logs`
- `audit.activity_logs`
- `governance.audit_logs`
- `governance.security_events`
- `automation.outbox_messages`
- `ops.processed_events`

Lý do: các responsibility này đã có canonical owner rõ ràng. Giữ bảng legacy trong một baseline mới sẽ làm EF mapping, repository, event handler và query service dễ ghi nhầm state.

## 6. Tenancy model

Bản V2 dùng mô hình tenant hai tầng:

```text
account
  └── workspace
        └── product resources: board, item, page, comment, automation, dashboard, ...
```

`account_id` xuất hiện ở hầu hết bảng business để hỗ trợ enterprise contract, billing, SSO, SCIM, data residency, audit và sau này tách service/database. `workspace_id` chỉ thể hiện product workspace boundary.

## 7. RLS và authz projection

RLS không join trực tiếp sang `workspace.workspace_members`. Bản V2 dùng `authz.access_grants` làm local projection cho tenant isolation. Source of truth vẫn là `account`, `workspace`, `governance`; projection này được worker cập nhật qua domain/integration events.

RLS helper functions:

- `authz.current_user_id()`
- `authz.current_account_id()`
- `authz.current_workspace_id()`
- `authz.can_access_account(account_id)`
- `authz.can_access_workspace(account_id, workspace_id)`

Application phải set session variables bằng `SET LOCAL` trong transaction/connection scope:

```sql
SET LOCAL app.current_user_id = '<user-id>'; 
SET LOCAL app.current_account_id = '<account-id>'; 
SET LOCAL app.current_workspace_id = '<workspace-id>'; 
SET LOCAL app.request_scope = 'app';
```

Worker dùng `app.request_scope = 'worker'` hoặc role `notrelix_worker` để xử lý projection/outbox/internal jobs.

## 8. Event/outbox model

Bản V2 chốt rõ 3 loại:

| Loại | Lưu ở đâu | Mục đích |
|---|---|---|
| LocalDomainEvent | Không persist mặc định | Xử lý in-process trong modular monolith |
| DurableDomainEvent | `events.domain_event_logs` | Business fact log, audit/replay/projection nội bộ |
| IntegrationEvent | `messaging.outbox_messages` | Gửi ra bus/consumer khác qua outbox dispatcher |

Consumer idempotency dùng `messaging.processed_events` với unique `(event_id, consumer_name)`. Không dùng `ops.processed_events` nữa.

## 9. Billing model

Billing trong V2 root theo `account_id`, không root bằng `workspace_id`. Các bảng chính:

- `billing.billing_customers`
- `billing.plans`
- `billing.plan_prices`
- `billing.plan_limits`
- `billing.subscriptions`
- `billing.subscription_items`
- `billing.invoices`
- `billing.invoice_line_items`
- `billing.entitlements`
- `billing.usage_metrics`
- `billing.feature_usage_ledger`

Entitlement có `target_scope = Account | Workspace` để hỗ trợ cả account-level feature và workspace-level add-on.

## 10. EF Core mapping rules

Domain aggregates chỉ map source-of-truth schemas:

```text
account, identity, workspace, governance, work, docs, collab, automation, integration, billing, reporting
```

Không tạo Domain aggregate cho:

```text
search, notifications delivery internals, activity feed projection, analytics, events, messaging, audit, ops, authz
```

Các schema này dùng Infrastructure persistence model hoặc read model riêng.

## 11. Future database split

Hiện tại giữ một PostgreSQL database, nhiều schema. Khi scale, không tách 20 schema thành 20 instance. Tách theo workload và ownership:

```text
notrelix_core_db: account, identity, workspace, governance, authz, ops, local events/messaging
notrelix_work_db: work + local authz/events/messaging
notrelix_content_db: docs, collab + local authz/events/messaging
notrelix_billing_db: billing + local authz/events/messaging
notrelix_integration_db: integration, automation + local events/messaging
notrelix_notification_db: notifications + local messaging/authz
notrelix_projection_db: search, activity, analytics, reporting read models
notrelix_audit_db: audit
```

Khi split, mỗi service/database phải có local `events`, `messaging`, `authz` projection. Không dùng central outbox database vì outbox phải commit cùng transaction với aggregate source.

## 12. Cách chạy SQL

```bash
psql "$DATABASE_URL" -f notrelix-enterprise-schema-v2-clean-baseline.sql
```

Sau khi chạy, dùng các verification queries cuối file SQL để kiểm tra:

- Không còn legacy tables.
- Table count theo schema đúng.
- RLS-enabled table không bị thiếu policy.
- SECURITY DEFINER function có `SET search_path`.

## 13. Lưu ý triển khai tiếp theo

Bản SQL này là target baseline để coding agent triển khai EF Core/migration. Trước khi merge vào backend thật, cần chạy trên PostgreSQL container để validate syntax và điều chỉnh tên constraint/index nếu trùng với migration hiện tại.