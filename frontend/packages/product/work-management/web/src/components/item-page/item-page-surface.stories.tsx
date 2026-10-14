import { useState } from "react";
import type { Meta, StoryObj } from "@storybook/react";
import type {
  CardDetail,
  CardUpdate,
  UpdateFieldValueInput,
} from "@notrelix/work-management-core";
import { ItemPageSurface } from "./item-page-surface";
import { itemPageCopy } from "./item-page.copy";
import { itemSheetCopy } from "../card-detail/item-sheet.copy";
import {
  commenterSheetCapabilities,
  editorSheetCapabilities,
} from "../../verification/item-sheet-fixtures";
import {
  ERP_PAGE_NOW,
  ERP_PAGE_TIME_ZONE,
  erpPageBoard,
  erpPageChildItems,
  erpPageCommenterAccess,
  erpPageConflict,
  erpPageDetailData,
  erpPageItem,
  erpPageNavigation,
  erpPageSpace,
  erpPageViewer,
} from "../../verification/item-page-fixtures";
import type { ItemPageSurfaceProps, ItemPageTab } from "./item-page-types";
import type { TaskDetailCapabilities } from "../card-detail/task-detail-types";

const meta = {
  title: "Work Management/Item Page/Surface",
  component: ItemPageSurface,
  parameters: { layout: "fullscreen" },
} satisfies Meta<typeof ItemPageSurface>;
export default meta;
type Story = StoryObj;

const copy = itemPageCopy("vi");
const sheetCopy = itemSheetCopy("vi");
const board = erpPageBoard();

/**
 * Stateful story harness at the main-area size of the mockup (1108 x 900, the
 * rail and the sidebar pinned). Edits, comments and tabs apply locally so the
 * interactions are visible.
 */
function ItemPageStory({
  status = "ready",
  initialItem = erpPageItem(),
  capabilities = editorSheetCapabilities,
  tab = "all",
  conflict = false,
  withChildren = true,
  access,
}: {
  status?: ItemPageSurfaceProps["status"];
  initialItem?: CardDetail;
  capabilities?: TaskDetailCapabilities;
  tab?: ItemPageTab;
  conflict?: boolean;
  withChildren?: boolean;
  access?: ItemPageSurfaceProps["access"];
}) {
  const [item, setItem] = useState(initialItem);
  const [updates, setUpdates] = useState<readonly CardUpdate[]>(
    initialItem.updates,
  );
  const [activeTab, setActiveTab] = useState<ItemPageTab>(tab);
  const [hasConflict, setHasConflict] = useState(conflict);

  function editField({ fieldDefinitionId, value }: UpdateFieldValueInput) {
    if (fieldDefinitionId === "field-status" && typeof value === "string") {
      setItem((current) => ({
        ...current,
        status: value,
        statusOptionId: value,
      }));
    }
  }

  const data = erpPageDetailData(item);
  return (
    <div className="h-225 w-277 overflow-hidden">
      <ItemPageSurface
        status={status}
        board={board}
        card={status === "ready" ? item : null}
        capabilities={capabilities}
        copy={copy}
        sheetCopy={sheetCopy}
        now={ERP_PAGE_NOW}
        timeZone={ERP_PAGE_TIME_ZONE}
        space={erpPageSpace}
        viewer={erpPageViewer}
        creatorName="Nguyễn Thu Hà"
        access={access}
        conflict={hasConflict ? erpPageConflict : null}
        onResolveConflict={() => setHasConflict(false)}
        navigation={erpPageNavigation()}
        activeTab={activeTab}
        onSelectTab={setActiveTab}
        detailData={
          status === "ready"
            ? {
                ...data,
                updates,
                childItems: withChildren ? erpPageChildItems : undefined,
              }
            : undefined
        }
        callbacks={{
          onClose: () => undefined,
          onRenameTitle: (_id, patch) =>
            setItem((current) => ({
              ...current,
              title: patch.title ?? current.title,
            })),
          onUpdateItem: () => undefined,
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
                createdAt: ERP_PAGE_NOW.toISOString(),
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
          onPrevious: undefined,
          onNext: () => undefined,
          onCopyLink: () => undefined,
        }}
      />
    </div>
  );
}

/** ERP-142 as Hà sees it: Đang làm, overdue 2 days, Khẩn, 3/5 sub-items (tong-quan). */
export const Default: Story = {
  render: () => <ItemPageStory />,
  tags: ["fui-surface--wm.item-page.surface", "fui-state--Default"],
};

/** The "Lịch sử" tab: every recorded change, no composer (lich-su). */
export const History: Story = {
  render: () => <ItemPageStory tab="history" />,
  tags: ["fui-surface--wm.item-page.surface", "fui-state--Default"],
};

/** The "Bình luận" tab with the composer (binh-luan). */
export const Comments: Story = {
  render: () => <ItemPageStory tab="comments" />,
  tags: ["fui-surface--wm.item-page.surface", "fui-state--Default"],
};

export const Loading: Story = {
  render: () => <ItemPageStory status="loading" />,
  tags: ["fui-surface--wm.item-page.surface", "fui-state--Loading"],
};

/** The item failed to load or no longer exists. */
export const Unavailable: Story = {
  render: () => <ItemPageStory status="error" />,
  tags: ["fui-surface--wm.item-page.surface", "fui-state--Unavailable"],
};

/** No key, due, priority, label, sub-items, files or comments; a long title. */
export const EdgeData: Story = {
  render: () => (
    <ItemPageStory
      withChildren={false}
      initialItem={erpPageItem({
        title:
          "Đối chiếu công nợ đầu kỳ của 48.200 hồ sơ khách hàng với phòng Kế toán trước buổi chốt số liệu cuối quý — 日本語 🚀",
        descriptionMd: undefined,
        itemKey: null,
        dueAt: null,
        dueDate: undefined,
        startDate: undefined,
        priority: undefined,
        labels: [],
        members: [],
        watchers: [],
        isWatched: false,
        childProgress: null,
        statusOptionId: null,
        statusCategory: null,
        status: "",
        updates: [],
        files: [],
        activity: [],
      })}
    />
  ),
  tags: ["fui-surface--wm.item-page.surface", "fui-state--EdgeData"],
};

/** A write the server refused: both values stay on screen (xung-dot). */
export const Conflict: Story = {
  render: () => <ItemPageStory conflict />,
  tags: ["fui-surface--wm.item-page.surface", "fui-state--EdgeData"],
};

/** Linh (Có thể bình luận): every property read-only, the composer stays (chi-xem). */
export const ReadOnly: Story = {
  render: () => (
    <ItemPageStory
      capabilities={commenterSheetCapabilities}
      access={erpPageCommenterAccess}
    />
  ),
  tags: ["fui-surface--wm.item-page.surface", "fui-state--ReadOnly"],
};
