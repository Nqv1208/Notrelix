# WorkManagement Product Roadmap v3 — Capability-Driven Execution Plan

**Version:** 3.0  
**Date:** 2026-07-18  
**Branch:** `feature/workmanagement`  
**Baseline:** Sprint 2 complete — 12 commits (37b2d28 → 4842174), 836 tests passing

---

## Table of Contents

1. [Capability Model](#1-capability-model)
2. [Use Case Matrix](#2-use-case-matrix)
3. [Aggregate Lifecycle Matrix](#3-aggregate-lifecycle-matrix)
4. [Permission Matrix](#4-permission-matrix)
5. [Concurrency Matrix](#5-concurrency-matrix)
6. [Idempotency Matrix](#6-idempotency-matrix)
7. [Event Taxonomy](#7-event-taxonomy)
8. [Consumer Matrix](#8-consumer-matrix)
9. [Projection Matrix](#9-projection-matrix)
10. [Transaction Boundary](#10-transaction-boundary)
11. [Failure Strategy](#11-failure-strategy)
12. [Cross-BC Contract Matrix](#12-cross-bc-contract-matrix)
13. [Definition of Done](#13-definition-of-done)
14. [Phase Roadmap](#14-phase-roadmap)

---

## 1. Capability Model

WorkManagement capabilities organized as **vertical slices**: Domain → Application → API → Authorization → Concurrency → Idempotency → Domain Events → Integration Events → Consumers → Projection → Realtime → Audit → Tests.

### Capability Hierarchy

```
WorkManagement
├── Board Lifecycle
│   ├── Create Board
│   ├── Update Board (name, description, icon, color)
│   ├── Archive Board
│   ├── Restore Board
│   └── Delete Board (soft delete)
├── Board Schema
│   ├── Create Field
│   ├── Update Field (name, type, config)
│   ├── Delete Field (soft delete)
│   ├── Reorder Fields
│   └── Create Field Option
├── Board Groups
│   ├── Create Group
│   ├── Update Group (name, color)
│   ├── Archive Group
│   ├── Restore Group
│   ├── Delete Group
│   └── Reorder Groups
├── Item Lifecycle
│   ├── Create Item
│   ├── Update Item (title, description)
│   ├── Archive Item
│   ├── Restore Item
│   ├── Delete Item (soft delete)
│   ├── Move Item (group, position)
│   └── Duplicate Item
├── Item Field Values
│   ├── Set Field Value
│   ├── Clear Field Value
│   └── Bulk Set Field Values
├── Item Links & Dependencies
│   ├── Link Items (blocking, related, duplicate)
│   ├── Unlink Items
│   └── Query Item Links
├── Board Views
│   ├── Create View (kanban, table, calendar, timeline, dashboard)
│   ├── Update View Config
│   ├── Delete View
│   └── Set Default View
├── Labels & Checklists
│   ├── Create Label
│   ├── Update Label (name, color)
│   ├── Delete Label
│   ├── Assign Label to Item
│   ├── Remove Label from Item
│   ├── Create Checklist
│   ├── Add Checklist Item
│   ├── Toggle Checklist Item
│   └── Delete Checklist
├── Saved Filters & Preferences
│   ├── Create Saved Filter
│   ├── Update Saved Filter
│   ├── Delete Saved Filter
│   ├── Set Board Preferences
│   └── Get Board Preferences
├── Board Members
│   ├── Add Board Member
│   ├── Remove Board Member
│   ├── Update Board Member Role
│   └── Query Board Members
├── Approval Workflows
│   ├── Create Approval Request
│   ├── Approve Request
│   ├── Reject Request
│   ├── Cancel Request
│   └── Query Approval Status
└── Form Submissions
    ├── Submit Form
    ├── Review Submission
    └── Query Submissions
```

---

## 2. Use Case Matrix

### Board Lifecycle

| Use Case | Aggregate | Domain Events | Integration Events | Consumers |
|----------|-----------|---------------|-------------------|-----------|
| CreateBoard | Board | BoardCreated | board.created | BoardCreatedConsumer (Activity), AutomationTriggerConsumer |
| UpdateBoard | Board | BoardUpdated | board.updated | BoardUpdatedConsumer (Activity) |
| ArchiveBoard | Board | BoardArchived | board.archived | BoardArchivedConsumer (Activity) |
| RestoreBoard | Board | BoardRestored | board.restored | BoardRestoredConsumer (Activity) |
| DeleteBoard | Board | BoardDeleted | board.deleted | BoardDeletedConsumer (Activity, Search) |

### Board Schema

| Use Case | Aggregate | Domain Events | Integration Events | Consumers |
|----------|-----------|---------------|-------------------|-----------|
| CreateField | BoardField | FieldCreated | field.created | FieldCreatedConsumer (Activity) |
| UpdateField | BoardField | FieldUpdated | field.updated | FieldUpdatedConsumer (Activity) |
| DeleteField | BoardField | FieldDeleted | field.deleted | FieldDeletedConsumer (Activity, Search) |
| ReorderFields | BoardField | FieldsReordered | — | — |
| CreateFieldOption | BoardField | FieldOptionCreated | field.option.created | FieldOptionCreatedConsumer (Activity) |

### Board Groups

| Use Case | Aggregate | Domain Events | Integration Events | Consumers |
|----------|-----------|---------------|-------------------|-----------|
| CreateGroup | BoardGroup | GroupCreated | group.created | GroupCreatedConsumer (Activity) |
| UpdateGroup | BoardGroup | GroupUpdated | group.updated | GroupUpdatedConsumer (Activity) |
| ArchiveGroup | BoardGroup | GroupArchived | group.archived | GroupArchivedConsumer (Activity) |
| RestoreGroup | BoardGroup | GroupRestored | group.restored | GroupRestoredConsumer (Activity) |
| DeleteGroup | BoardGroup | GroupDeleted | group.deleted | GroupDeletedConsumer (Activity, Search) |
| ReorderGroups | BoardGroup | GroupsReordered | — | — |

### Item Lifecycle

| Use Case | Aggregate | Domain Events | Integration Events | Consumers |
|----------|-----------|---------------|-------------------|-----------|
| CreateItem | BoardItem | ItemCreated | item.created | ItemCreatedConsumer (Activity), AutomationTriggerConsumer, NotificationConsumer |
| UpdateItem | BoardItem | ItemUpdated | item.updated | ItemUpdatedConsumer (Activity) |
| ArchiveItem | BoardItem | ItemArchived | item.archived | ItemArchivedConsumer (Activity) |
| RestoreItem | BoardItem | ItemRestored | item.restored | ItemRestoredConsumer (Activity) |
| DeleteItem | BoardItem | ItemDeleted | item.deleted | ItemDeletedConsumer (Activity, Search) |
| MoveItem | BoardItem | ItemMoved | item.moved | ItemMovedConsumer (Activity) |
| DuplicateItem | BoardItem | ItemDuplicated | item.duplicated | ItemDuplicatedConsumer (Activity) |

### Item Field Values

| Use Case | Aggregate | Domain Events | Integration Events | Consumers |
|----------|-----------|---------------|-------------------|-----------|
| SetFieldValue | BoardItem | FieldValueChanged | field.value.changed | FieldValueChangedConsumer (Activity, Search) |
| ClearFieldValue | BoardItem | FieldValueCleared | field.value.cleared | FieldValueClearedConsumer (Activity, Search) |
| BulkSetFieldValues | BoardItem | FieldValuesBulkSet | field.values.bulk | FieldValuesBulkSetConsumer (Activity, Search) |

### Item Links & Dependencies

| Use Case | Aggregate | Domain Events | Integration Events | Consumers |
|----------|-----------|---------------|-------------------|-----------|
| LinkItems | BoardItem | ItemLinked | item.linked | ItemLinkedConsumer (Activity) |
| UnlinkItems | BoardItem | ItemUnlinked | item.unlinked | ItemUnlinkedConsumer (Activity) |

### Board Views

| Use Case | Aggregate | Domain Events | Integration Events | Consumers |
|----------|-----------|---------------|-------------------|-----------|
| CreateView | BoardView | ViewCreated | view.created | ViewCreatedConsumer (Activity) |
| UpdateView | BoardView | ViewUpdated | view.updated | ViewUpdatedConsumer (Activity) |
| DeleteView | BoardView | ViewDeleted | view.deleted | ViewDeletedConsumer (Activity) |
| SetDefaultView | BoardView | DefaultViewSet | — | — |

### Labels & Checklists

| Use Case | Aggregate | Domain Events | Integration Events | Consumers |
|----------|-----------|---------------|-------------------|-----------|
| CreateLabel | Label | LabelCreated | label.created | LabelCreatedConsumer (Activity) |
| UpdateLabel | Label | LabelUpdated | label.updated | LabelUpdatedConsumer (Activity) |
| DeleteLabel | Label | LabelDeleted | label.deleted | LabelDeletedConsumer (Activity, Search) |
| AssignLabelToItem | BoardItem | LabelAssigned | label.assigned | LabelAssignedConsumer (Activity) |
| RemoveLabelFromItem | BoardItem | LabelRemoved | label.removed | LabelRemovedConsumer (Activity) |
| CreateChecklist | BoardItem | ChecklistCreated | checklist.created | ChecklistCreatedConsumer (Activity) |
| AddChecklistItem | BoardItem | ChecklistItemAdded | checklist.item.added | ChecklistItemAddedConsumer (Activity) |
| ToggleChecklistItem | BoardItem | ChecklistItemToggled | checklist.item.toggled | ChecklistItemToggledConsumer (Activity) |
| DeleteChecklist | BoardItem | ChecklistDeleted | checklist.deleted | ChecklistDeletedConsumer (Activity) |

### Saved Filters & Preferences

| Use Case | Aggregate | Domain Events | Integration Events | Consumers |
|----------|-----------|---------------|-------------------|-----------|
| CreateSavedFilter | SavedFilter | FilterCreated | filter.created | FilterCreatedConsumer (Activity) |
| UpdateSavedFilter | SavedFilter | FilterUpdated | filter.updated | FilterUpdatedConsumer (Activity) |
| DeleteSavedFilter | SavedFilter | FilterDeleted | filter.deleted | FilterDeletedConsumer (Activity) |
| SetBoardPreferences | BoardPreferences | PreferencesSet | — | — |
| GetBoardPreferences | BoardPreferences | — | — | — |

### Board Members

| Use Case | Aggregate | Domain Events | Integration Events | Consumers |
|----------|-----------|---------------|-------------------|-----------|
| AddBoardMember | BoardMember | MemberAdded | board.member.added | BoardMemberAddedConsumer (Activity, Notification) |
| RemoveBoardMember | BoardMember | MemberRemoved | board.member.removed | BoardMemberRemovedConsumer (Activity) |
| UpdateBoardMemberRole | BoardMember | MemberRoleUpdated | board.member.role.updated | BoardMemberRoleUpdatedConsumer (Activity) |

### Approval Workflows

| Use Case | Aggregate | Domain Events | Integration Events | Consumers |
|----------|-----------|---------------|-------------------|-----------|
| CreateApprovalRequest | ApprovalRequest | ApprovalRequested | approval.requested | ApprovalRequestedConsumer (Activity, Notification) |
| ApproveRequest | ApprovalRequest | ApprovalApproved | approval.approved | ApprovalApprovedConsumer (Activity, Notification) |
| RejectRequest | ApprovalRequest | ApprovalRejected | approval.rejected | ApprovalRejectedConsumer (Activity, Notification) |
| CancelRequest | ApprovalRequest | ApprovalCancelled | approval.cancelled | ApprovalCancelledConsumer (Activity) |

### Form Submissions

| Use Case | Aggregate | Domain Events | Integration Events | Consumers |
|----------|-----------|---------------|-------------------|-----------|
| SubmitForm | FormSubmission | FormSubmitted | form.submitted | FormSubmittedConsumer (Activity, Notification) |
| ReviewSubmission | FormSubmission | FormReviewed | form.reviewed | FormReviewedConsumer (Activity) |

---

## 3. Aggregate Lifecycle Matrix

| Aggregate | Create | Update | Archive | Restore | Soft Delete | Status Transitions |
|-----------|--------|--------|---------|---------|-------------|-------------------|
| Board | ✅ | ✅ | ✅ | ✅ | ✅ | Active ↔ Archived → Deleted |
| BoardField | ✅ | ✅ | — | — | ✅ | Active → Deleted |
| BoardGroup | ✅ | ✅ | ✅ | ✅ | ✅ | Active ↔ Archived → Deleted |
| BoardItem | ✅ | ✅ | ✅ | ✅ | ✅ | Active ↔ Archived → Deleted |
| BoardView | ✅ | ✅ | — | — | ✅ | Active → Deleted |
| SavedFilter | ✅ | ✅ | — | — | ✅ | Active → Deleted |
| BoardPreferences | ✅ | ✅ | — | — | — | Active (upsert) |
| Label | ✅ | ✅ | — | — | ✅ | Active → Deleted |
| BoardMember | ✅ | ✅ | — | — | ✅ | Active → Removed |
| ApprovalRequest | ✅ | — | — | — | — | Pending → Approved/Rejected/Cancelled |
| FormSubmission | ✅ | — | — | — | — | Pending → Reviewed/Rejected |
| BoardItemLink | ✅ | — | — | — | ✅ | Active → Deleted |

**Note:** `BoardItemValue` is a child entity of `BoardItem`, not an aggregate. It inherits lifecycle from parent.

---

## 4. Permission Matrix

### Workspace Level

| Permission | Owner | Admin | Member | Guest |
|-----------|-------|-------|--------|-------|
| Create Board | ✅ | ✅ | ✅ | ❌ |
| Delete Workspace | ✅ | ❌ | ❌ | ❌ |
| Manage Members | ✅ | ✅ | ❌ | ❌ |
| Manage Billing | ✅ | ❌ | ❌ | ❌ |
| Create Space | ✅ | ✅ | ✅ | ❌ |

### Board Level

| Permission | Owner | Admin | Editor | Viewer |
|-----------|-------|-------|--------|--------|
| Create Field | ✅ | ✅ | ✅ | ❌ |
| Delete Field | ✅ | ✅ | ✅ | ❌ |
| Create Group | ✅ | ✅ | ✅ | ❌ |
| Archive Group | ✅ | ✅ | ✅ | ❌ |
| Create Item | ✅ | ✅ | ✅ | ❌ |
| Update Item | ✅ | ✅ | ✅ | ❌ |
| Archive Item | ✅ | ✅ | ✅ | ❌ |
| Delete Item | ✅ | ✅ | ✅ | ❌ |
| View Board | ✅ | ✅ | ✅ | ✅ |
| Manage Board Settings | ✅ | ✅ | ❌ | ❌ |
| Manage Board Members | ✅ | ✅ | ❌ | ❌ |

### Field Level

| Permission | Owner | Admin | Editor | Viewer |
|-----------|-------|-------|--------|--------|
| Create Field Option | ✅ | ✅ | ✅ | ❌ |
| Update Field | ✅ | ✅ | ✅ | ❌ |
| Delete Field | ✅ | ✅ | ✅ | ❌ |

### Item Level

| Permission | Owner | Admin | Editor | Viewer | Assignee |
|-----------|-------|-------|--------|--------|----------|
| Update Item | ✅ | ✅ | ✅ | ❌ | ✅ (own fields) |
| Archive Item | ✅ | ✅ | ✅ | ❌ | ❌ |
| Delete Item | ✅ | ✅ | ✅ | ❌ | ❌ |
| Move Item | ✅ | ✅ | ✅ | ❌ | ❌ |
| Set Field Value | ✅ | ✅ | ✅ | ❌ | ✅ (assigned fields) |
| Create Comment | ✅ | ✅ | ✅ | ✅ | ✅ |
| Add Checklist Item | ✅ | ✅ | ✅ | ✅ | ✅ |

### View Level

| Permission | Owner | Admin | Editor | Viewer |
|-----------|-------|-------|--------|--------|
| Create View | ✅ | ✅ | ✅ | ❌ |
| Update View | ✅ | ✅ | ✅ | ❌ (own views) |
| Delete View | ✅ | ✅ | ❌ | ❌ |
| Set Default View | ✅ | ✅ | ❌ | ❌ |

---

## 5. Concurrency Matrix

| Aggregate | Concurrency Strategy | ExpectedVersion Required | Conflict Resolution |
|-----------|---------------------|------------------------|-------------------|
| Board | Optimistic (version) | ✅ | 409 Conflict |
| BoardField | Optimistic (version) | ✅ | 409 Conflict |
| BoardGroup | Optimistic (version) | ✅ | 409 Conflict |
| BoardItem | Optimistic (version) | ✅ | 409 Conflict |
| BoardView | Optimistic (version) | ✅ | 409 Conflict |
| SavedFilter | Optimistic (version) | ✅ | 409 Conflict |
| BoardPreferences | Optimistic (version) | ✅ | 409 Conflict |
| Label | Optimistic (version) | ✅ | 409 Conflict |
| BoardMember | Optimistic (version) | ✅ | 409 Conflict |
| ApprovalRequest | None (state machine) | ❌ | State guard |
| FormSubmission | None (state machine) | ❌ | State guard |
| BoardItemLink | None (unique constraint) | ❌ | DB unique violation |

**Rules:**
- All mutation commands must implement `IExpectedVersionRequest`
- `ConcurrencyBehavior` validates version before save
- State machine aggregates (ApprovalRequest, FormSubmission) use domain guards, not version
- BoardItemLink uses unique constraint on (LinkId, LinkedItemId, LinkType)

---

## 6. Idempotency Matrix

| Use Case | Idempotency Key Source | TTL | Scope | Notes |
|----------|----------------------|-----|-------|-------|
| CreateBoard | Client-generated GUID | 24h | Workspace | Prevents duplicate boards |
| UpdateBoard | Client-generated GUID | 24h | Board | Prevents duplicate updates |
| ArchiveBoard | Client-generated GUID | 24h | Board | Prevents duplicate archive |
| CreateField | Client-generated GUID | 24h | Board | Prevents duplicate fields |
| CreateGroup | Client-generated GUID | 24h | Board | Prevents duplicate groups |
| CreateItem | Client-generated GUID | 24h | Board | Prevents duplicate items |
| MoveItem | Client-generated GUID | 24h | Item | Prevents duplicate moves |
| SetFieldValue | Client-generated GUID | 24h | Item+Field | Prevents duplicate value sets |
| LinkItems | Client-generated GUID | 24h | Item | Prevents duplicate links |
| CreateView | Client-generated GUID | 24h | Board | Prevents duplicate views |
| CreateLabel | Client-generated GUID | 24h | Board | Prevents duplicate labels |
| AddBoardMember | Client-generated GUID | 24h | Board | Prevents duplicate memberships |
| SubmitForm | Client-generated GUID | 24h | Board | Prevents duplicate submissions |

**Rules:**
- All mutation commands must implement `IIdempotentRequest`
- `IdempotencyBehavior` checks/sets idempotency key in transaction
- `SetResultAsync` enqueued via `IPostCommitActionQueue` (not direct call)
- Read-only queries are NOT idempotent (no write, no idempotency needed)

---

## 7. Event Taxonomy

### Category A: Publish (Integration Event + Consumer)

Events that trigger external side effects via consumers.

| Event | Integration Event | Consumer(s) |
|-------|------------------|-------------|
| BoardCreated | board.created | BoardCreatedConsumer (Activity), AutomationTriggerConsumer |
| BoardUpdated | board.updated | BoardUpdatedConsumer (Activity) |
| BoardArchived | board.archived | BoardArchivedConsumer (Activity) |
| BoardRestored | board.restored | BoardRestoredConsumer (Activity) |
| BoardDeleted | board.deleted | BoardDeletedConsumer (Activity, Search) |
| FieldCreated | field.created | FieldCreatedConsumer (Activity) |
| FieldUpdated | field.updated | FieldUpdatedConsumer (Activity) |
| FieldDeleted | field.deleted | FieldDeletedConsumer (Activity, Search) |
| FieldOptionCreated | field.option.created | FieldOptionCreatedConsumer (Activity) |
| GroupCreated | group.created | GroupCreatedConsumer (Activity) |
| GroupUpdated | group.updated | GroupUpdatedConsumer (Activity) |
| GroupArchived | group.archived | GroupArchivedConsumer (Activity) |
| GroupRestored | group.restored | GroupRestoredConsumer (Activity) |
| GroupDeleted | group.deleted | GroupDeletedConsumer (Activity, Search) |
| ItemCreated | item.created | ItemCreatedConsumer (Activity), AutomationTriggerConsumer, NotificationConsumer |
| ItemUpdated | item.updated | ItemUpdatedConsumer (Activity) |
| ItemArchived | item.archived | ItemArchivedConsumer (Activity) |
| ItemRestored | item.restored | ItemRestoredConsumer (Activity) |
| ItemDeleted | item.deleted | ItemDeletedConsumer (Activity, Search) |
| ItemMoved | item.moved | ItemMovedConsumer (Activity) |
| ItemDuplicated | item.duplicated | ItemDuplicatedConsumer (Activity) |
| FieldValueChanged | field.value.changed | FieldValueChangedConsumer (Activity, Search) |
| FieldValueCleared | field.value.cleared | FieldValueClearedConsumer (Activity, Search) |
| FieldValuesBulkSet | field.values.bulk | FieldValuesBulkSetConsumer (Activity, Search) |
| ItemLinked | item.linked | ItemLinkedConsumer (Activity) |
| ItemUnlinked | item.unlinked | ItemUnlinkedConsumer (Activity) |
| ViewCreated | view.created | ViewCreatedConsumer (Activity) |
| ViewUpdated | view.updated | ViewUpdatedConsumer (Activity) |
| ViewDeleted | view.deleted | ViewDeletedConsumer (Activity) |
| LabelCreated | label.created | LabelCreatedConsumer (Activity) |
| LabelUpdated | label.updated | LabelUpdatedConsumer (Activity) |
| LabelDeleted | label.deleted | LabelDeletedConsumer (Activity, Search) |
| LabelAssigned | label.assigned | LabelAssignedConsumer (Activity) |
| LabelRemoved | label.removed | LabelRemovedConsumer (Activity) |
| ChecklistCreated | checklist.created | ChecklistCreatedConsumer (Activity) |
| ChecklistItemAdded | checklist.item.added | ChecklistItemAddedConsumer (Activity) |
| ChecklistItemToggled | checklist.item.toggled | ChecklistItemToggledConsumer (Activity) |
| ChecklistDeleted | checklist.deleted | ChecklistDeletedConsumer (Activity) |
| FilterCreated | filter.created | FilterCreatedConsumer (Activity) |
| FilterUpdated | filter.updated | FilterUpdatedConsumer (Activity) |
| FilterDeleted | filter.deleted | FilterDeletedConsumer (Activity) |
| MemberAdded | board.member.added | BoardMemberAddedConsumer (Activity, Notification) |
| MemberRemoved | board.member.removed | BoardMemberRemovedConsumer (Activity) |
| MemberRoleUpdated | board.member.role.updated | BoardMemberRoleUpdatedConsumer (Activity) |
| ApprovalRequested | approval.requested | ApprovalRequestedConsumer (Activity, Notification) |
| ApprovalApproved | approval.approved | ApprovalApprovedConsumer (Activity, Notification) |
| ApprovalRejected | approval.rejected | ApprovalRejectedConsumer (Activity, Notification) |
| ApprovalCancelled | approval.cancelled | ApprovalCancelledConsumer (Activity) |
| FormSubmitted | form.submitted | FormSubmittedConsumer (Activity, Notification) |
| FormReviewed | form.reviewed | FormReviewedConsumer (Activity) |

### Category B: Audit-Only (No Integration Event)

Events that are logged to DomainEventLog for audit trail only.

| Event | Notes |
|-------|-------|
| FieldsReordered | UI-only state change |
| GroupsReordered | UI-only state change |
| DefaultViewSet | UI preference, no external effect |
| PreferencesSet | User preference, no external effect |

---

## 8. Consumer Matrix

| Consumer | BC | Trigger | Action | Failure Mode |
|---------|-----|---------|--------|-------------|
| BoardCreatedConsumer | Activity | board.created | Create ActivityEntry | Silent swallow |
| BoardUpdatedConsumer | Activity | board.updated | Create ActivityEntry | Silent swallow |
| BoardArchivedConsumer | Activity | board.archived | Create ActivityEntry | Silent swallow |
| BoardRestoredConsumer | Activity | board.restored | Create ActivityEntry | Silent swallow |
| BoardDeletedConsumer | Activity | board.deleted | Create ActivityEntry + SoftDelete projection | Silent swallow |
| FieldCreatedConsumer | Activity | field.created | Create ActivityEntry | Silent swallow |
| FieldUpdatedConsumer | Activity | field.updated | Create ActivityEntry | Silent swallow |
| FieldDeletedConsumer | Activity | field.deleted | Create ActivityEntry + SoftDelete projection | Silent swallow |
| FieldOptionCreatedConsumer | Activity | field.option.created | Create ActivityEntry | Silent swallow |
| GroupCreatedConsumer | Activity | group.created | Create ActivityEntry | Silent swallow |
| GroupUpdatedConsumer | Activity | group.updated | Create ActivityEntry | Silent swallow |
| GroupArchivedConsumer | Activity | group.archived | Create ActivityEntry | Silent swallow |
| GroupRestoredConsumer | Activity | group.restored | Create ActivityEntry | Silent swallow |
| GroupDeletedConsumer | Activity | group.deleted | Create ActivityEntry + SoftDelete projection | Silent swallow |
| ItemCreatedConsumer | Activity | item.created | Create ActivityEntry + Update projection | Silent swallow |
| ItemUpdatedConsumer | Activity | item.updated | Create ActivityEntry + Update projection | Silent swallow |
| ItemArchivedConsumer | Activity | item.archived | Create ActivityEntry + Update projection | Silent swallow |
| ItemRestoredConsumer | Activity | item.restored | Create ActivityEntry + Update projection | Silent swallow |
| ItemDeletedConsumer | Activity | item.deleted | Create ActivityEntry + SoftDelete projection | Silent swallow |
| ItemMovedConsumer | Activity | item.moved | Create ActivityEntry + Update projection | Silent swallow |
| ItemDuplicatedConsumer | Activity | item.duplicated | Create ActivityEntry + Update projection | Silent swallow |
| FieldValueChangedConsumer | Activity | field.value.changed | Create ActivityEntry + Update projection | Silent swallow |
| FieldValueClearedConsumer | Activity | field.value.cleared | Create ActivityEntry + Update projection | Silent swallow |
| FieldValuesBulkSetConsumer | Activity | field.values.bulk | Create ActivityEntry + Update projection | Silent swallow |
| ItemLinkedConsumer | Activity | item.linked | Create ActivityEntry | Silent swallow |
| ItemUnlinkedConsumer | Activity | item.unlinked | Create ActivityEntry | Silent swallow |
| ViewCreatedConsumer | Activity | view.created | Create ActivityEntry | Silent swallow |
| ViewUpdatedConsumer | Activity | view.updated | Create ActivityEntry | Silent swallow |
| ViewDeletedConsumer | Activity | view.deleted | Create ActivityEntry + SoftDelete projection | Silent swallow |
| LabelCreatedConsumer | Activity | label.created | Create ActivityEntry | Silent swallow |
| LabelUpdatedConsumer | Activity | label.updated | Create ActivityEntry | Silent swallow |
| LabelDeletedConsumer | Activity | label.deleted | Create ActivityEntry + SoftDelete projection | Silent swallow |
| LabelAssignedConsumer | Activity | label.assigned | Create ActivityEntry | Silent swallow |
| LabelRemovedConsumer | Activity | label.removed | Create ActivityEntry | Silent swallow |
| ChecklistCreatedConsumer | Activity | checklist.created | Create ActivityEntry | Silent swallow |
| ChecklistItemAddedConsumer | Activity | checklist.item.added | Create ActivityEntry | Silent swallow |
| ChecklistItemToggledConsumer | Activity | checklist.item.toggled | Create ActivityEntry | Silent swallow |
| ChecklistDeletedConsumer | Activity | checklist.deleted | Create ActivityEntry | Silent swallow |
| FilterCreatedConsumer | Activity | filter.created | Create ActivityEntry | Silent swallow |
| FilterUpdatedConsumer | Activity | filter.updated | Create ActivityEntry | Silent swallow |
| FilterDeletedConsumer | Activity | filter.deleted | Create ActivityEntry + SoftDelete projection | Silent swallow |
| BoardMemberAddedConsumer | Activity | board.member.added | Create ActivityEntry + Notification | Silent swallow |
| BoardMemberRemovedConsumer | Activity | board.member.removed | Create ActivityEntry | Silent swallow |
| BoardMemberRoleUpdatedConsumer | Activity | board.member.role.updated | Create ActivityEntry | Silent swallow |
| ApprovalRequestedConsumer | Activity | approval.requested | Create ActivityEntry + Notification | Silent swallow |
| ApprovalApprovedConsumer | Activity | approval.approved | Create ActivityEntry + Notification | Silent swallow |
| ApprovalRejectedConsumer | Activity | approval.rejected | Create ActivityEntry + Notification | Silent swallow |
| ApprovalCancelledConsumer | Activity | approval.cancelled | Create ActivityEntry | Silent swallow |
| FormSubmittedConsumer | Activity | form.submitted | Create ActivityEntry + Notification | Silent swallow |
| FormReviewedConsumer | Activity | form.reviewed | Create ActivityEntry | Silent swallow |
| AutomationTriggerConsumer | Automation | board.created, item.created | Trigger matching automation rules | Silent swallow |

---

## 9. Projection Matrix

| Read Model | Source Aggregate | Update Strategy | Consistency | Query |
|-----------|-----------------|-----------------|-------------|-------|
| BoardListItem | Board | Real-time (transactional) | Strong | GetBoardsForWorkspace |
| BoardDetail | Board | Real-time (transactional) | Strong | GetBoardById |
| BoardFieldList | BoardField | Real-time (transactional) | Strong | GetFieldsForBoard |
| BoardGroupList | BoardGroup | Real-time (transactional) | Strong | GetGroupsForBoard |
| BoardItemList | BoardItem | Real-time (transactional) | Strong | GetItemsForBoard |
| BoardItemDetail | BoardItem | Real-time (transactional) | Strong | GetItemById |
| BoardViewList | BoardView | Real-time (transactional) | Strong | GetViewsForBoard |
| SavedFilterList | SavedFilter | Real-time (transactional) | Strong | GetFiltersForUser |
| LabelList | Label | Real-time (transactional) | Strong | GetLabelsForBoard |
| BoardMemberList | BoardMember | Real-time (transactional) | Strong | GetMembersForBoard |
| ApprovalRequestList | ApprovalRequest | Real-time (transactional) | Strong | GetApprovalsForBoard |
| FormSubmissionList | FormSubmission | Real-time (transactional) | Strong | GetSubmissionsForBoard |
| BoardPreferencesView | BoardPreferences | Real-time (transactional) | Strong | GetPreferencesForBoard |
| ItemSearchDocument | BoardItem | Async (consumer) | Eventual | SearchItems |

**Note:** All read models use EF Core LINQ queries (no separate read database). Projections are updated in-transaction via aggregate state. `ItemSearchDocument` is updated async via `FieldValueChangedConsumer`.

---

## 10. Transaction Boundary

### In-Transaction (Atomic)

All of these execute in a single DB transaction:

1. Domain entity mutations
2. DomainEventLog append
3. OutboxMessage append
4. Version increment

```
BEGIN
  -- Domain entity mutations
  UPDATE boards SET ... WHERE version = @expectedVersion
  -- DomainEventLog append
  INSERT INTO domain_event_logs (...)
  -- OutboxMessage append (for integration events)
  INSERT INTO outbox_messages (...)
  -- Version increment
  UPDATE boards SET version = version + 1
COMMIT
```

### Post-Commit (Fire-and-Forget)

These execute after the transaction commits, via `IPostCommitActionQueue`:

1. Realtime notifications (SignalR)
2. Idempotency result storage
3. Search index updates
4. Automation trigger evaluation

**Failure mode:** Silent swallow — post-commit failures do not roll back the transaction.

### Async (Background Job)

These execute via `OutboxDispatcher` background job:

1. OutboxMessage processing
2. Integration event dispatch to consumers
3. Consumer execution (Activity, Search, Automation, Notification)

**Failure mode:** Exponential backoff (2s → 60s, max 5 retries → DeadLetter).

---

## 11. Failure Strategy

| Failure Type | Strategy | User Impact |
|-------------|----------|-------------|
| Concurrency conflict | 409 Conflict | "This item was modified by someone else. Please refresh." |
| Authorization failure | 401/403 | "You don't have permission." |
| Validation failure | 400 Bad Request | "Invalid input: [details]." |
| Domain rule violation | 422 Unprocessable | "Cannot archive board with active items." |
| Idempotency conflict | 409 Conflict | "This action was already performed." |
| Post-commit failure | Silent swallow | No user impact |
| Outbox dispatch failure | Exponential backoff | No user impact (eventual delivery) |
| Consumer failure | Silent swallow + DeadLetter | No user impact |
| Realtime failure | Silent swallow | No user impact (UI polls as fallback) |
| Search index failure | Silent swallow + retry | No user impact (eventual consistency) |

---

## 12. Cross-BC Contract Matrix

| Consumer BC | Producer BC | Event | Contract |
|------------|------------|-------|----------|
| Activity | WorkManagement | board.created | IActivityEntryService.CreateEntryAsync |
| Activity | WorkManagement | item.created | IActivityEntryService.CreateEntryAsync |
| Automation | WorkManagement | board.created | IAutomationRuleService.EvaluateTriggerAsync |
| Automation | WorkManagement | item.created | IAutomationRuleService.EvaluateTriggerAsync |
| Notification | WorkManagement | item.created | INotificationService.SendAsync |
| Notification | WorkManagement | board.member.added | INotificationService.SendAsync |
| Notification | WorkManagement | approval.requested | INotificationService.SendAsync |
| Notification | WorkManagement | form.submitted | INotificationService.SendAsync |
| Search | WorkManagement | board.deleted | ISearchIndexService.DeleteAsync |
| Search | WorkManagement | item.deleted | ISearchIndexService.DeleteAsync |
| Search | WorkManagement | field.value.changed | ISearchIndexService.UpdateAsync |
| Governance | WorkManagement | — | IResourcePermissionService (sync, in-transaction) |
| Documents | WorkManagement | — | IResourceReferenceResolver (sync, in-transaction) |
| Workspaces | WorkManagement | — | IWorkspaceContext (sync, in-transaction) |

---

## 13. Definition of Done

A capability is **DONE** when ALL of the following are complete:

### Domain Layer
- [ ] Aggregate root with all state transitions
- [ ] Domain events raised for all state changes
- [ ] Domain rules enforced (guards, invariants)
- [ ] No `DateTime.UtcNow` — timestamps from Application
- [ ] No repository/EF/HTTP dependencies

### Application Layer
- [ ] Command/Query handlers
- [ ] Request DTOs with validation
- [ ] Response DTOs
- [ ] `IRequirePermission` markers
- [ ] `IExpectedVersionRequest` markers (mutations)
- [ ] `IIdempotentRequest` markers (mutations)
- [ ] Authorization behavior

### API Layer
- [ ] Endpoint registration
- [ ] Request binding
- [ ] Response mapping
- [ ] Error handling

### Events
- [ ] Domain events in aggregate
- [ ] Integration events (Category A)
- [ ] Consumer implementations
- [ ] Outbox message creation

### Projections
- [ ] Read model queries (EF Core LINQ)
- [ ] Search index updates (async consumer)

### Realtime
- [ ] SignalR notification (post-commit)

### Tests
- [ ] Domain unit tests
- [ ] Application handler tests
- [ ] API endpoint tests
- [ ] Architecture tests (markers, pipeline)
- [ ] No regression (all existing tests pass)

---

## 14. Phase Roadmap

### Phase 1: Foundation Fixes (Vertical)

**Goal:** Fix domain bugs, add missing markers, ensure all aggregates are production-ready.

**Tasks:**
1. Fix `ApprovalRequest` — add `Approve()`, `Reject()`, `Cancel()` methods
2. Fix `BoardGroup` — add `Archive()` method
3. Fix `Label` — add `EnsureNotDeleted()` guard in `Update()`
4. Fix `BoardField` — `ReorderOptions()` should raise `FieldsReordered` event
5. Fix `Template` — add lifecycle methods (`Archive()`, `Restore()`)
6. Fix `FormSubmission` — add state guards (`Review()`, `Reject()`)
7. Add `IRequirePermission` to all commands missing it
8. Add `IExpectedVersionRequest` to all mutation commands missing it
9. Add `IIdempotentRequest` to all mutation commands missing it
10. Verify all aggregates raise domain events for state changes

**Definition of Done:**
- [ ] All domain bugs fixed
- [ ] All markers added
- [ ] All existing tests pass
- [ ] No regression

### Phase 2: Board Lifecycle (Vertical)

**Goal:** Full board CRUD with authorization, concurrency, idempotency, events, projections.

**Capability:** Create Board → Update Board → Archive Board → Restore Board → Delete Board

**Vertical slice per capability:**
- Domain: Board aggregate with lifecycle methods
- Application: Commands with validation, authorization, idempotency
- API: Endpoints with request binding, response mapping
- Events: Domain events + integration events
- Consumers: BoardCreatedConsumer, BoardUpdatedConsumer, etc.
- Projections: BoardListItem, BoardDetail
- Realtime: SignalR notification
- Tests: Domain, Application, API, Architecture

### Phase 3: Board Schema (Vertical)

**Goal:** Full field management with options, reordering, authorization.

**Capability:** Create Field → Update Field → Delete Field → Reorder Fields → Create Field Option

### Phase 4: Board Groups (Vertical)

**Goal:** Full group management with archive/restore, reordering.

**Capability:** Create Group → Update Group → Archive Group → Restore Group → Delete Group → Reorder Groups

### Phase 5: Item Lifecycle (Vertical)

**Goal:** Full item CRUD with move, duplicate, assignment, authorization.

**Capability:** Create Item → Update Item → Archive Item → Restore Item → Delete Item → Move Item → Duplicate Item

### Phase 6: Item Field Values (Vertical)

**Goal:** Full field value management with bulk operations, search indexing.

**Capability:** Set Field Value → Clear Field Value → Bulk Set Field Values

### Phase 7: Board Views (Vertical)

**Goal:** Full view management with config, default view.

**Capability:** Create View → Update View → Delete View → Set Default View

### Phase 8: Labels & Checklists (Vertical)

**Goal:** Full label/checklist management with item assignment.

**Capability:** Create Label → Update Label → Delete Label → Assign/Remove Label → Create Checklist → Add/Toggle/Delete Checklist Item

### Phase 9: Saved Filters & Preferences (Vertical)

**Goal:** Full filter/preference management.

**Capability:** Create Saved Filter → Update Saved Filter → Delete Saved Filter → Set Board Preferences

### Phase 10: Board Members (Vertical)

**Goal:** Full board membership management with roles.

**Capability:** Add Board Member → Remove Board Member → Update Board Member Role

### Phase 11: Approval Workflows (Vertical)

**Goal:** Full approval lifecycle with state machine.

**Capability:** Create Approval Request → Approve → Reject → Cancel

### Phase 12: Form Submissions (Vertical)

**Goal:** Full form submission lifecycle.

**Capability:** Submit Form → Review Submission

### Phase 13: Item Links & Dependencies (Vertical)

**Goal:** Full item linking with types, uniqueness.

**Capability:** Link Items → Unlink Items → Query Item Links

### Phase 14: Cross-BC Integration (Vertical)

**Goal:** Wire up all consumers, notifications, search, automation triggers.

**Tasks:**
1. Implement all Activity consumers
2. Implement Notification consumers
3. Implement Search consumers (delete + index)
4. Implement AutomationTriggerConsumer
5. Wire up real-time notifications via SignalR
6. Update search projections
7. End-to-end testing

---

## Appendix: Current State (Sprint 2 Complete)

### Domain Maturity
- **15 aggregates** identified
- **68 domain events** defined
- **11 domain rules** implemented
- **Domain completeness: 85%**

### Application Maturity
- **48 commands** defined
- **12 queries** defined
- **Application completeness: 45%**

### API Maturity
- **52 endpoints** defined
- **API completeness: 55%**

### Event-Driven Maturity
- **18 integration events** defined
- **3 stub consumers** (BoardCreatedConsumer, CardAssignedConsumer, ActivityConsumer)
- **DevNullRealtimePublisher** placeholder
- **Event-driven completeness: 15%**

### Test Maturity
- **836 tests** passing (239 arch + 391 app + 206 API)
- **Test completeness: 60%**

### Pipeline Order (19 behaviors)
1. ExceptionMapping
2. Tracing
3. Validation
4. ContractGuard
5. TokenValidation
6. TenantBootstrap
7. SystemOperationAudit
8. ResourceScope
9. PostCommitScope
10. PublicCache
11. DbRequestScope
12. Authorization
13. VerifiedEmail
14. Concurrency
15. SubscriptionGate
16. FeatureGate
17. Idempotency
18. PostCommitEnqueue
19. AuthorizedCache

### Middleware Chain (15 steps)
1. ForwardedHeaders
2. ExceptionHandler
3. CorrelationId
4. Csrf
5. SecurityHeaders
6. PreAuthRateLimit
7. HSTS
8. CORS
9. HTTPS
10. Authentication
11. HttpRequestContext
12. AuthRateLimit
13. SecurityAudit
14. Authorization
15. Endpoints

---

*End of WorkManagement Product Roadmap v3*
