import { useState } from "react";
import type { Meta, StoryObj } from "@storybook/react";
import type {
  CardDetail,
  CardUpdate,
  UpdateFieldValueInput,
} from "@notrelix/work-management-core";
import { TaskDetailPanelSurface } from "./task-detail-panel-surface";
import { itemSheetCopy } from "./item-sheet.copy";
import {
  ERP_SHEET_NOW,
  ERP_SHEET_TIME_ZONE,
  commenterSheetCapabilities,
  editorSheetCapabilities,
  erpSheetBoard,
  erpSheetDetailData,
  erpSheetGroups,
  erpSheetItem,
  erpSheetMark,
} from "./item-sheet.fixture";
import type {
  TaskDetailCapabilities,
  TaskDetailData,
} from "./task-detail-types";
import type { TaskDetailPanelStatus } from "./task-detail-panel-surface";

const meta = {
  title: "Work Management/Task Detail/Panel",
  component: TaskDetailPanelSurface,
} satisfies Meta<typeof TaskDetailPanelSurface>;
export default meta;
type Story = StoryObj;

const copy = itemSheetCopy("vi");
const board = erpSheetBoard();

/**
 * Stateful story harness at the mockup size (560 px, right sheet). Edits,
 * comments and title changes apply locally so the interactions are visible.
 */
function ItemSheetStory({
  status = "ready",
  initialItem = erpSheetItem(),
  capabilities = editorSheetCapabilities,
  dataOverrides = {},
}: {
  status?: TaskDetailPanelStatus;
  initialItem?: CardDetail;
  capabilities?: TaskDetailCapabilities;
  dataOverrides?: Partial<TaskDetailData>;
}) {
  const [item, setItem] = useState(initialItem);
  const [updates, setUpdates] = useState<readonly CardUpdate[]>(
    initialItem.updates,
  );

  function editField({ fieldDefinitionId, value }: UpdateFieldValueInput) {
    if (fieldDefinitionId === "field-status" && typeof value === "string") {
      const category = (
        {
          "status-todo": "todo",
          "status-doing": "in_progress",
          "status-review": "review",
          "status-done": "done",
        } as const
      )[value];
      setItem((current) => ({
        ...current,
        status: value,
        statusOptionId: value,
        statusCategory: category ?? null,
      }));
    }
  }

  return (
    <div className="ml-auto h-dvh max-h-200 w-full max-w-140 overflow-hidden border-l border-border bg-popover">
      <TaskDetailPanelSurface
        status={status}
        board={board}
        card={status === "ready" ? item : null}
        capabilities={capabilities}
        copy={copy}
        now={ERP_SHEET_NOW}
        timeZone={ERP_SHEET_TIME_ZONE}
        departmentMark={erpSheetMark}
        groups={erpSheetGroups}
        detailData={
          status === "ready"
            ? erpSheetDetailData(item, { updates, ...dataOverrides })
            : undefined
        }
        callbacks={{
          onClose: () => undefined,
          onRenameTitle: (_id, patch) =>
            setItem((current) => ({
              ...current,
              title: patch.title ?? current.title,
            })),
          onToggleWatch: () => undefined,
          onDuplicate: () => undefined,
          onArchive: () => undefined,
          onUpdateFieldValue: editField,
          onRefreshActivity: () => undefined,
          onCreateUpdate: (input, options) => {
            setUpdates((current) => [
              ...current,
              {
                id: `comment-${current.length + 1}`,
                cardId: input.cardId,
                author: current[0]?.author ?? item.members[0]!,
                body: input.body,
                mentionUserIds: input.mentionUserIds,
                attachmentIds: input.attachmentIds,
                createdAt: ERP_SHEET_NOW.toISOString(),
              },
            ]);
            options?.onSuccess?.();
          },
          onUpdateUpdate: (updateId, body) =>
            setUpdates((current) =>
              current.map((update) =>
                update.id === updateId ? { ...update, body } : update,
              ),
            ),
          onDeleteUpdate: (updateId) =>
            setUpdates((current) =>
              current.filter((update) => update.id !== updateId),
            ),
          onSelectTab: () => undefined,
          onPrevious: () => undefined,
          onNext: () => undefined,
          onCopyLink: () => undefined,
          onMoveToGroup: () => undefined,
        }}
      />
    </div>
  );
}

/** ERP-142 as Hà sees it: Đang làm, overdue 2 days, Khẩn, 3/5 sub-items. */
export const Default: Story = {
  render: () => <ItemSheetStory />,
  tags: ["fui-surface--wm.task-detail.panel", "fui-state--Default"],
};

export const Loading: Story = {
  render: () => <ItemSheetStory status="loading" />,
  tags: ["fui-surface--wm.task-detail.panel", "fui-state--Loading"],
};

/** The item failed to load or no longer exists (the error presentation). */
export const Unavailable: Story = {
  render: () => <ItemSheetStory status="error" />,
  tags: ["fui-surface--wm.task-detail.panel", "fui-state--Unavailable"],
};

/** No key, due, priority, label, description, sub-items or comments; a long title. */
export const EdgeData: Story = {
  render: () => (
    <ItemSheetStory
      initialItem={erpSheetItem({
        title:
          "Đối chiếu công nợ đầu kỳ của 48.200 hồ sơ khách hàng với phòng Kế toán trước buổi chốt số liệu cuối quý — 日本語 🚀",
        itemKey: null,
        dueAt: null,
        dueDate: undefined,
        priority: undefined,
        labels: [],
        members: [],
        descriptionMd: undefined,
        childProgress: null,
        statusOptionId: null,
        statusCategory: null,
        status: "",
        updates: [],
        activity: [],
      })}
      dataOverrides={{ childItems: undefined }}
    />
  ),
  tags: ["fui-surface--wm.task-detail.panel", "fui-state--EdgeData"],
};

/** Linh (Có thể bình luận): every property read-only, the composer stays. */
export const ReadOnly: Story = {
  render: () => <ItemSheetStory capabilities={commenterSheetCapabilities} />,
  tags: ["fui-surface--wm.task-detail.panel", "fui-state--ReadOnly"],
};
