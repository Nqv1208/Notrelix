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
  erpPageLongChildItems,
  erpPageNavigation,
  erpPagePeople,
  erpPageSpace,
  erpPageUploads,
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
  extra,
  children,
}: {
  /** Props the state under test adds (lifecycle, offline, people, uploads…). */
  extra?: Partial<ItemPageSurfaceProps>;
  /** Sub-items instead of the five of ERP-142. */
  children?: ItemPageSurfaceProps["detailData"] extends infer D
    ? D extends { childItems?: infer C }
      ? C
      : never
    : never;
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
        people={erpPagePeople}
        {...extra}
        activeTab={activeTab}
        onSelectTab={setActiveTab}
        detailData={
          status === "ready"
            ? {
                ...data,
                updates,
                childItems:
                  children ?? (withChildren ? erpPageChildItems : undefined),
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

/** Not found or not visible: the same words for both (Không tìm thấy ERP-171). */
export const NotFound: Story = {
  render: () => (
    <ItemPageStory status="notFound" extra={{ itemKey: "ERP-171" }} />
  ),
  tags: ["fui-surface--wm.item-page.surface", "fui-state--Unavailable"],
};

/** Loading, slower than expected: the hint appears under the frame. */
export const LoadingSlow: Story = {
  render: () => (
    <ItemPageStory
      status="loading"
      extra={{ slow: true, itemKey: "ERP-142" }}
    />
  ),
  tags: ["fui-surface--wm.item-page.surface", "fui-state--Loading"],
};

/** A new item: nothing filled, every block says what to do next. */
export const Empty: Story = {
  render: () => (
    <ItemPageStory
      withChildren={false}
      initialItem={erpPageItem({
        title: "Kiểm tra quyền xem báo cáo thuế",
        itemKey: "ERP-154",
        descriptionMd: undefined,
        dueAt: null,
        dueDate: undefined,
        startDate: undefined,
        labels: [],
        members: [],
        watchers: [],
        isWatched: false,
        childProgress: null,
        statusOptionId: "status-todo",
        statusCategory: "todo",
        status: "status-todo",
        updates: [],
        files: [],
        activity: [],
      })}
    />
  ),
  tags: ["fui-surface--wm.item-page.surface", "fui-state--EdgeData"],
};

/** Done: a green notice with the way to reopen; the due day says how early it finished. */
export const Completed: Story = {
  render: () => (
    <ItemPageStory
      withChildren={false}
      initialItem={erpPageItem({
        title: "Sao lưu cơ sở dữ liệu tự động hằng đêm",
        itemKey: "ERP-131",
        statusOptionId: "status-done",
        statusCategory: "done",
        status: "status-done",
        completedAt: "2026-10-06T09:00:00.000Z",
        dueAt: "2026-10-08T10:00:00.000Z",
      })}
      extra={{
        lifecycle: {
          completed: { at: "2026-10-06T09:00:00.000Z", byName: "Vũ Thanh" },
          onReopen: () => undefined,
        },
      }}
    />
  ),
  tags: ["fui-surface--wm.item-page.surface", "fui-state--EdgeData"],
};

/** Archived: read-only for everyone, with the way to restore it. */
export const Archived: Story = {
  render: () => (
    <ItemPageStory
      withChildren={false}
      initialItem={erpPageItem({
        title: "Khảo sát nhà cung cấp phần mềm kho",
        itemKey: "ERP-118",
        descriptionMd: "Không còn cần vì đã chọn phân hệ kho của ERP.",
      })}
      extra={{
        lifecycle: {
          archived: { at: "2026-10-08T03:00:00.000Z", byName: "Nguyễn Thu Hà" },
          onRestore: () => undefined,
        },
      }}
    />
  ),
  tags: ["fui-surface--wm.item-page.surface", "fui-state--ReadOnly"],
};

/** The network is gone: writes wait, marked "chờ lưu", and save by themselves. */
export const Offline: Story = {
  render: () => (
    <ItemPageStory
      extra={{
        offline: {
          pendingCount: 2,
          properties: ["due"],
          childItems: true,
        },
      }}
    />
  ),
  tags: ["fui-surface--wm.item-page.surface", "fui-state--EdgeData"],
};

/** A long description folds after a few lines; sub-items show in part. */
export const LongContent: Story = {
  render: () => (
    <ItemPageStory
      children={erpPageLongChildItems}
      initialItem={erpPageItem({
        title:
          "Rà soát và chuẩn hoá toàn bộ danh mục vật tư, hàng hoá, mã kho và đơn vị tính của 14 chi nhánh trước khi nhập số dư đầu kỳ vào ERP",
        itemKey: "ERP-160",
        descriptionMd: Array.from(
          { length: 6 },
          (_, index) =>
            `Bước ${index + 1}: đối chiếu danh mục của từng chi nhánh với danh mục dùng chung, ghi lại mã trùng, mã thiếu đơn vị tính và mã kho sai quy ước để chi nhánh xác nhận trước khi nạp vào ERP.`,
        ).join("\n\n"),
      })}
    />
  ),
  tags: ["fui-surface--wm.item-page.surface", "fui-state--EdgeData"],
};

/** Files on their way: one sending, one over the limit, one done. */
export const Uploading: Story = {
  render: () => <ItemPageStory extra={{ uploads: erpPageUploads }} />,
  tags: ["fui-surface--wm.item-page.surface", "fui-state--EdgeData"],
};
