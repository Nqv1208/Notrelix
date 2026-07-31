# RULE.md — Notrelix Enterprise Development Rules

> Bộ luật phát triển bắt buộc cho Notrelix theo hướng **Monday-like Enterprise Work Management Platform**.  
> Mục tiêu: giữ tư duy phát triển nhất quán, tránh refactor lặp lại, tránh biến hệ thống thành Trello clone hoặc CRUD app rời rạc.

---

# 1. Product Direction Rules

## RULE-PRODUCT-001 — Notrelix là Work Management Platform, không phải app Kanban đơn giản

Luôn phát triển theo mô hình:

```txt
Workspace
  → Space / Folder
    → Board
      → Field
      → Item
      → View
```

Không phát triển theo mô hình:

```txt
Workspace
  → Kanban Board
    → List
      → Card
```

Kanban chỉ là một **View**, không phải core data model.

---

## RULE-PRODUCT-002 — Board là database/work table

Một Board phải được hiểu là một bảng dữ liệu công việc có schema động.

```txt
Board = schema + items + views + permissions + automations
```

Không được coi Board chỉ là container chứa cards.

---

## RULE-PRODUCT-003 — View không lưu dữ liệu riêng

Các view như:

```txt
Table
Kanban
Calendar
Timeline
Gantt
Dashboard
```

phải dùng chung:

```txt
BoardItem
BoardField
BoardViewConfig
```

Không được tạo data model riêng cho từng view.

---

## RULE-PRODUCT-004 — Docs là supporting module trước

Docs phục vụ:

```txt
- Requirement
- Meeting note
- Project spec
- Linked document
- Wiki
```

Không clone Google Docs ngay.  
Không để Docs làm lệch trọng tâm khỏi WorkManagement core.

---

# 2. Architecture Rules

## RULE-ARCH-001 — Giữ Clean Architecture

Dependency rule:

```txt
API → Application → Domain
Infrastructure → Application → Domain
Domain không phụ thuộc layer nào
```

Không để Domain reference Infrastructure, EF Core, API, HTTP, Redis, SignalR.

---

## RULE-ARCH-002 — Modular Monolith trước, microservices sau

Không tách microservices sớm.  
Tổ chức theo bounded contexts trong monolith:

```txt
Identity
Workspaces
WorkManagement
Documents
Collaboration
Automation
Integrations
Governance
Billing
```

---

## RULE-ARCH-003 — Không viết lại toàn bộ

Mọi refactor phải theo phase:

```txt
1. Scan hiện trạng
2. Map schema/entity cũ sang model mới
3. Tạo migration plan
4. Thêm model mới song song nếu cần
5. Migrate use case
6. Build/test
7. Remove legacy sau
```

Không đổi tên/xóa hàng loạt trong một commit lớn.

---

## RULE-ARCH-004 — Không để database đi trước business logic quá xa

Không tạo quá nhiều bảng nếu chưa có:

```txt
- Domain entity
- Application command/query
- Permission check
- Test case tối thiểu
```

Schema target là blueprint dài hạn, không phải lý do để implement tất cả cùng lúc.

---

# 3. Naming Rules

## RULE-NAME-001 — Tên domain phải thống nhất

Target naming:

```txt
BoardColumn  → BoardField
List         → BoardGroup
Card         → BoardItem
Permission   → ResourcePermission
FieldValues  → Values
```

Không để tồn tại lâu dài cả hai naming:

```txt
Card và BoardItem
List và BoardGroup
BoardColumn và BoardField
```

Có thể có compatibility trong migration, nhưng target domain phải thống nhất.

---

## RULE-NAME-002 — Không dùng hậu tố Context cho folder domain

Không đặt:

```txt
BoardsContext
DocumentsContext
WorkspaceContext
```

Vì dễ nhầm với EF DbContext.

Nên đặt:

```txt
WorkManagement
Documents
Workspaces
Governance
Collaboration
Automation
Integrations
```

---

# 4. Database / Schema Rules

## RULE-DB-001 — Không drop bảng cũ trực tiếp khi migration

Migration phải staged:

```txt
1. Tạo bảng mới
2. Copy dữ liệu
3. Tạo compatibility view nếu cần
4. Update backend đọc bảng mới
5. Chạy song song
6. Remove bảng cũ sau
```

Không được `DROP TABLE` legacy khi backend/frontend chưa chuyển xong.

---

## RULE-DB-002 — Mapping legacy bắt buộc

Khi migrate từ schema cũ:

```txt
permissions    → resource_permissions
board_columns  → board_fields
lists          → board_groups
cards          → board_items
card_members   → board_item_members
card_labels    → board_item_labels
card_links     → board_item_links
```

---

## RULE-DB-003 — Bảng có workspace_id nếu dữ liệu thuộc workspace

Các bảng nên có `workspace_id` nếu cần query/filter theo workspace:

```txt
board_items
pages
comments
notifications
activity_logs
audit_logs
resource_permissions
automation_rules
integration_connections
```

Lý do:

```txt
- Permission filtering
- Multi-tenant isolation
- Query performance
- Audit/report
```

---

## RULE-DB-004 — JSONB chỉ dùng cho tính linh hoạt, không thay thế toàn bộ query model

`board_items.values jsonb` dùng cho render linh hoạt.

Nhưng field cần filter/sort/report nhiều phải sync sang:

```txt
board_item_values
```

Các field nên sync:

```txt
Status
Priority
Assignee
DueDate
Timeline
Number
Select
People
Relation
```

Flow bắt buộc:

```txt
UpdateBoardItemFieldValueCommand
  → update board_items.values
  → upsert board_item_values
  → raise BoardItemFieldValueChangedEvent
```

---

## RULE-DB-005 — Mọi JSON config phải có validator ở domain/application

Các cột JSONB sau không được ghi bừa:

```txt
board_fields.settings
board_views.config
automation_rules.trigger_json
automation_rules.conditions_json
automation_rules.actions_json
blocks.content_json
blocks.properties_json
```

Bắt buộc có validator:

```txt
FieldSettingsValidator
BoardViewConfigValidator
AutomationRuleValidator
BlockContentValidator
```

---

# 5. WorkManagement Table Rules

## RULE-TABLE-001 — boards

Bảng `boards` là aggregate chính của WorkManagement.

Field cần chú ý:

```txt
workspace_id:
- Bắt buộc.
- Mọi board thuộc một workspace.

space_id:
- Optional.
- Dùng để đặt board vào folder/space.

board_type:
- Không hard-code chỉ Kanban.
- Có thể là WorkManagement, CRM, Dev, Service.

visibility:
- Private, Workspace, PublicLink.
- Phải đi qua PermissionService khi query.

settings:
- JSONB config chung.
- Phải validate nếu có schema cụ thể.

is_template:
- Dùng cho template board.
```

Không được query board mà bỏ qua workspace/permission.

---

## RULE-TABLE-002 — board_fields

`board_fields` là schema/cột của board.

Field cần chú ý:

```txt
key:
- Stable key trong board.
- Unique theo board nếu chưa deleted.

name:
- Label hiển thị.

field_type:
- Bắt buộc map với FieldType enum.

settings:
- Config theo field type.
- Status/Select phải có options hợp lệ.

default_value:
- Giá trị mặc định khi tạo item.

is_system:
- Field hệ thống không được xóa tùy tiện.

position:
- Dùng fractional/double indexing.
```

Không hard-code columns trong UI.  
Frontend phải render theo board schema.

---

## RULE-TABLE-003 — board_groups

`board_groups` là section trong board/table.

Không nhầm với Kanban column.

```txt
board_groups:
- Dùng cho Main Table group/section.
- Có thể tương đương group như Monday.
- Không phải source duy nhất của Kanban.
```

Kanban phải group theo field trong `BoardView.config`.

---

## RULE-TABLE-004 — board_items

`board_items` là item/row/task chính.

Field cần chú ý:

```txt
workspace_id:
- Để filter permission/audit nhanh.

board_id:
- Item thuộc board.

group_id:
- Section/group trong table.
- Không đồng nghĩa với Kanban column.

name:
- Tên item/task.

values:
- JSONB chứa field values linh hoạt.
- Không query/report toàn bộ dựa vào values nếu dữ liệu lớn.

position:
- Dùng cho reorder.

is_deleted:
- Soft delete.
```

Không gọi item là Card trong domain mới.

---

## RULE-TABLE-005 — board_item_values

Bảng này dùng cho filter/sort/report scale tốt.

```txt
item_id
field_id
value_text
value_number
value_bool
value_date
value_json
```

Rule:

```txt
- Một item + field chỉ có một row.
- Upsert khi update field value.
- Dùng cho field cần query nhiều.
- Không bắt buộc lưu mọi field trong MVP.
```

---

## RULE-TABLE-006 — board_views

`board_views` chỉ lưu cách nhìn dữ liệu.

Field cần chú ý:

```txt
view_type:
- Table, Kanban, Calendar, Timeline, Gantt, Form, Dashboard.

config:
- JSONB.
- Phải validate theo view_type.

is_default:
- Mỗi board nên có 1 default view.

is_private:
- Private view chỉ owner/user có quyền thấy.
```

Không lưu item data trong view.

---

# 6. Field Engine Rules

## RULE-FIELD-001 — FieldType là trung tâm của table engine

FieldType tối thiểu:

```txt
Text
LongText
Number
Checkbox
Date
DateTime
Status
Select
MultiSelect
People
File
Link
Priority
Timeline
Progress
Dependency
Relation
Rollup
Formula
CreatedAt
UpdatedAt
CreatedBy
LastUpdatedBy
```

---

## RULE-FIELD-002 — Mỗi field type phải có contract

Mỗi FieldType cần định nghĩa:

```txt
- settings schema
- default value
- value validator
- value normalizer
- filter operators
- sort behavior
- frontend renderer
- frontend editor
- automation compatibility
```

Không xử lý field type bằng nhiều `if` rải rác ở frontend/backend.

---

## RULE-FIELD-003 — People/Relation phải validate ở Application Layer

Domain có thể normalize format.  
Application phải validate:

```txt
People:
- user tồn tại
- user thuộc workspace
- user active

Relation:
- target board/item tồn tại
- user có quyền view target
```

---

# 7. View Engine Rules

## RULE-VIEW-001 — View config phải validate theo board schema

Trước khi save `board_views.config`, phải check:

```txt
visibleFields tồn tại
hiddenFields tồn tại
sort.fieldId tồn tại
filter.fieldId tồn tại
groupBy field hợp lệ
```

---

## RULE-VIEW-002 — Kanban phải group theo field

Kanban config bắt buộc:

```json
{
  "kanban": {
    "columnFieldId": "statusFieldId"
  }
}
```

`columnFieldId` phải là field type tương thích:

```txt
Status
Select
People
Priority
```

Kéo card ở Kanban:

```txt
Drag card column A → B
= update BoardItem field value
```

Không chỉ đổi `group_id`.

---

## RULE-VIEW-003 — Calendar/Timeline phải dùng field date

Calendar:

```txt
dateFieldId phải là Date/DateTime
```

Timeline:

```txt
startFieldId, endFieldId phải là Date/DateTime
```

---

# 8. Governance / Permission Rules

## RULE-PERM-001 — ResourcePermission chỉ là ACL entry, không phải toàn bộ permission system

Không được dừng ở class/bảng `ResourcePermission`.

Bắt buộc có:

```txt
PermissionAction
PermissionLevel
PermissionSubjectType
ResourceType
PermissionContext
PermissionDecision
IPermissionService
PermissionService
IPermissionMatrix
PermissionMatrix
IRequirePermission
AuthorizationBehavior
AuditLog
```

---

## RULE-PERM-002 — Backend là nguồn quyết định cuối cùng

Frontend được phép hide/disable UI, nhưng backend phải check mọi command/query quan trọng.

Không được để frontend tự quyết định quyền.

---

## RULE-PERM-003 — Không hard-code role trong handler

Sai:

```csharp
if (role == WorkspaceRole.Admin) { ... }
```

Đúng:

```csharp
await permissionService.AuthorizeAsync(context);
```

Hoặc command/query implement:

```txt
IRequirePermission
```

và đi qua:

```txt
AuthorizationBehavior
```

---

## RULE-PERM-004 — Owner và Admin không ngang nhau

Owner:

```txt
- Toàn quyền workspace.
- Có thể delete workspace.
- Có thể manage billing/security/audit.
- Có thể manage Owner/Admin.
```

Admin:

```txt
- Quản trị vận hành.
- Có thể invite member nếu policy cho phép.
- Có thể manage resource permission nếu policy cho phép.
- Không được delete workspace.
- Không được remove/hạ quyền Owner cuối cùng.
- Không được disable audit/security.
```

---

## RULE-PERM-005 — Guest luôn bị giới hạn

Guest:

```txt
- Không list toàn bộ boards/pages.
- Chỉ thấy resource được share trực tiếp.
- Không tạo board mặc định.
- Không manage permission.
- Không export data mặc định.
```

Query cho Guest phải filter ở backend.

---

## RULE-PERM-006 — Permission changes phải audit

Bắt buộc ghi audit cho:

```txt
GrantResourcePermission
RevokeResourcePermission
ChangeMemberRole
InviteMember
RemoveMember
CreateShareLink
DisableShareLink
ChangeBoardVisibility
ExportData
DeleteWorkspace
ViewAuditLog
```

AuditLog append-only.

---

# 9. Document Rules

## RULE-DOC-001 — Docs dùng Page + Block

Docs target:

```txt
Page
  → Block
```

Không lưu cả document như một string HTML thô.

---

## RULE-DOC-002 — Block content phải validate

Các block field cần chú ý:

```txt
block_type:
- Paragraph, Heading1, Todo, Code, Image, BoardRef, ItemRef...

content_json:
- Nội dung chính.
- Validate theo block_type.

properties_json:
- UI/style/config phụ.

version:
- Tăng khi update content.
```

Không save block content không rõ schema.

---

## RULE-DOC-003 — ResourceLink là cầu nối giữa Docs và WorkManagement

Dùng `resource_links` cho:

```txt
Page ↔ BoardItem
Block ↔ Board
Item ↔ Page
Item ↔ Item
```

Khi trả linked resource phải check permission target.

---

# 10. Automation Rules

## RULE-AUTO-001 — Automation phải chạy qua Outbox

Không xử lý automation trực tiếp trong request chính.

Flow bắt buộc:

```txt
DomainEvent
  → OutboxMessage
  → Background Worker
  → RuleMatcher
  → ConditionEvaluator
  → ActionExecutor
  → AutomationExecution
```

---

## RULE-AUTO-002 — Automation action phải giới hạn theo phase

MVP chỉ nên hỗ trợ:

```txt
notification.send
item.update_field
item.assign
webhook.send
```

Không cho automation thay đổi permission trong MVP.

---

## RULE-AUTO-003 — Automation phải idempotent

Cần tránh xử lý lặp:

```txt
automation_executions unique(rule_id, event_id)
webhook_deliveries unique(subscription_id, event_id)
```

Retry không được tạo tác dụng phụ trùng lặp.

---

# 11. Collaboration Rules

## RULE-COLLAB-001 — ActivityLog khác AuditLog

ActivityLog:

```txt
User-facing feed.
Có thể hiển thị đẹp.
Có thể gom nhóm.
```

AuditLog:

```txt
Security/admin record.
Append-only.
Không update/delete thông thường.
```

---

## RULE-COLLAB-002 — Notification phải gắn user cụ thể

Notification phải có:

```txt
workspace_id
user_id
notification_type
payload
resource_type
resource_id
is_read
```

Không broadcast notification mơ hồ không có recipient.

---

# 12. Realtime Rules

## RULE-REALTIME-001 — Realtime không thay thế query

Realtime event chỉ dùng để update cache.

Frontend vẫn phải có query chính:

```txt
GET board schema
GET board items
GET notifications
```

Realtime event nên nhỏ:

```txt
item.updated
item.field_value_changed
notification.created
```

---

## RULE-REALTIME-002 — Channel theo resource

Channel:

```txt
workspace:{workspaceId}
board:{boardId}
item:{itemId}
page:{pageId}
user:{userId}:notifications
```

Không broadcast mọi event cho toàn bộ user.

---

# 13. API Rules

## RULE-API-001 — API theo use case, không chỉ CRUD bảng

Ưu tiên:

```txt
GET    /boards/{boardId}/schema
GET    /boards/{boardId}/items?viewId=...
PATCH  /boards/{boardId}/items/{itemId}/values/{fieldId}
POST   /boards/{boardId}/items/reorder
POST   /boards/{boardId}/fields/reorder
PATCH  /boards/{boardId}/views/{viewId}/config
```

Không tạo CRUD máy móc cho mọi bảng nếu chưa có use case.

---

## RULE-API-002 — Không trả EF graph dư

Response DTO phải rõ:

```txt
BoardSchemaDto
BoardItemDto
BoardViewDto
EffectivePermissions
```

Không return entity trực tiếp ra API.

---

## RULE-API-003 — Query cũng phải authorize

Không chỉ command mới check permission.

Các query bắt buộc check:

```txt
GetBoard
GetBoardSchema
GetBoardItems
GetPage
GetWorkspaceBoards
GetAuditLogs
```

---

# 14. Frontend Rules

## RULE-FE-001 — Frontend theo feature/widget/shared

Target:

```txt
features/
  board
  field
  item
  view
  docs
  permission
  automation
  notification

widgets/
  board-table
  board-kanban
  board-calendar
  docs-editor
```

Không để mọi thứ vào một service/hook lớn.

---

## RULE-FE-002 — Field Registry bắt buộc

Frontend không hard-code render từng field rải rác.

Cần:

```txt
FieldRegistry
  renderer
  editor
  filter operators
  default settings
```

---

## RULE-FE-003 — PermissionGuard chỉ hỗ trợ UX

Frontend dùng:

```txt
effectivePermissions
can()
PermissionGuard
```

Nhưng backend vẫn enforce.

---

## RULE-FE-004 — Import Alias Strategy

Mọi import phải tuân theo alias convention sau:

```txt
apps/*       →  @/     trỏ về src/ của app đó
packages/*   →  ~/     trỏ về src/ của package đó
Cross-package →  @notrelix/<package-name>  qua exports map
Relative ./  →  chỉ cùng cấp hoặc xuống 1 cấp con
Relative ../../  →  CẤM trong packages
Package → App     →  CẤM tuyệt đối
```

Ví dụ:
```ts
// Trong packages/features/auth/src/web/hooks/use-login.ts
import { authApi } from '~/core/api/auth.service';      // ✅ ~/ alias
import { Button } from '@notrelix/ui-web';              // ✅ cross-package
import { cn } from './button';                           // ✅ relative cùng cấp
import { something } from '../../../core/other';         // ❌ CẤM ../../

// Trong apps/web/src/routes/sign-in.tsx
import { AuthLayout } from '@/routes/auth-layout';       // ✅ @/ alias
import { LoginForm } from '@notrelix/features-auth/web'; // ✅ cross-package
```

---

# 15. Testing Rules

## RULE-TEST-001 — Permission test là bắt buộc

Test tối thiểu:

```txt
Owner có toàn quyền workspace
Admin không delete workspace
Admin không remove Owner cuối cùng
Member không manage permission
Guest không list private board
Viewer không update item
Commenter comment được nhưng không update item
Editor update item được
Manager manage board permission được
Revoked permission không còn hiệu lực
Expired permission không còn hiệu lực
```

---

## RULE-TEST-002 — Field/View Engine phải có test

Test tối thiểu:

```txt
Status value phải thuộc options
Number parse đúng
Date normalize đúng
People validate user thuộc workspace
Kanban columnFieldId phải là field hợp lệ
Calendar dateFieldId phải là Date/DateTime
Timeline start/end phải là Date/DateTime
```

---

# 16. Migration Rules

## RULE-MIGRATION-001 — Migrate theo phase

Phase đề xuất:

```txt
Phase 1: Governance / Permission
Phase 2: WorkManagement Core
Phase 3: Field Engine
Phase 4: View Engine
Phase 5: Main Table
Phase 6: Kanban/Calendar/Timeline
Phase 7: Docs linking
Phase 8: Automation/Outbox
Phase 9: Realtime
```

---

## RULE-MIGRATION-002 — Compatibility views được phép dùng tạm

Có thể dùng:

```txt
v_legacy_cards
v_legacy_lists
v_legacy_board_columns
v_legacy_permissions
```

nhưng phải có kế hoạch remove.

---

# 17. Performance Rules

## RULE-PERF-001 — Không query toàn bộ board lớn

Board items phải hỗ trợ:

```txt
pagination
cursor
filter
sort
group
viewId
```

Không load toàn bộ item nếu board lớn.

---

## RULE-PERF-002 — Cache đúng chỗ

Nên cache:

```txt
Board schema
Board view config
Workspace membership
Effective permissions
Field options
```

Không cache lâu:

```txt
Permission without invalidation
Audit log
Highly mutable item list
```

Invalidation bắt buộc:

```txt
Update field → clear board schema cache
Update view → clear board view cache
Grant/revoke permission → clear effective permission cache
Update member role → clear workspace permission cache
```

---

# 18. Definition of Done

Một phase chỉ hoàn thành khi:

```txt
- Build pass
- Test liên quan pass
- Không phá flow cũ
- Có migration nếu DB thay đổi
- Có permission check cho command/query mới
- Có audit cho action nhạy cảm
- Có report file thay đổi
- Có ghi chú technical debt
```

---

# 19. Absolute Do Not

Không được:

```txt
- Dừng permission ở ResourcePermission entity.
- Check role trực tiếp trong handler.
- Để Guest query toàn bộ board/page.
- Để BoardView lưu data riêng.
- Để Kanban phụ thuộc ListId trong Monday-like mode.
- Dùng JSONB không validator.
- Return EF entity trực tiếp ra API.
- Tạo bảng mới không có use case/test.
- Drop bảng legacy khi chưa migration xong.
- Build fail nhưng vẫn coi phase hoàn thành.
```

---

# 20. Development Mindset

Khi thêm tính năng mới, luôn hỏi:

```txt
1. Tính năng này thuộc bounded context nào?
2. Resource nào cần permission?
3. Có cần audit không?
4. Có cần activity không?
5. Có sinh domain event không?
6. Có cần outbox không?
7. Có ảnh hưởng field/view engine không?
8. Có cần migration không?
9. Frontend có dùng effectivePermissions không?
10. Có test permission và business rule chưa?
```

Nếu chưa trả lời được, chưa nên code.
