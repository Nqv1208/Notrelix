/**
 * Deterministic fixtures for the item lifecycle surfaces (delete, move and the
 * board's trash): the ERP board's items and boards, so Vietnamese in both
 * locales.
 */
import type { BoardTrashRow } from "../components/item-lifecycle/board-trash-sheet";
import type { MoveTargetBoard } from "../components/item-actions/move-board-dialog";
import {
  itemLifecycleCopy,
  type ItemLifecycleCopy,
} from "../components/item-lifecycle/item-lifecycle.copy";

export const ITEM_LIFECYCLE_ITEM_TITLE = "Đối soát số liệu tồn kho";

export function itemLifecycleCopyFixture(
  locale: "vi" | "en" = "vi",
): ItemLifecycleCopy {
  return itemLifecycleCopy(locale);
}

export const ITEM_LIFECYCLE_BOARDS: readonly MoveTargetBoard[] = [
  { id: "b-1", title: "Triển khai ERP" },
  { id: "b-2", title: "Hỗ trợ khách hàng" },
  { id: "b-3", title: "Kế hoạch quý IV" },
];

export const ITEM_LIFECYCLE_TRASH_ROWS: readonly BoardTrashRow[] = [
  {
    id: "i-1",
    name: "Cấu hình phân quyền kho",
    itemKey: "ERP-9",
    restorableUntilDay: "2026-11-20",
  },
  {
    id: "i-2",
    name: "Nhập danh mục vật tư",
    itemKey: "ERP-14",
    restorableUntilDay: "2026-11-24",
  },
];

/** Long names that must truncate, one row with no key. */
export const ITEM_LIFECYCLE_TRASH_EDGE_ROWS: readonly BoardTrashRow[] = [
  {
    id: "i-3",
    name: "Đối soát số liệu tồn kho cuối tháng giữa kho trung tâm và toàn bộ các chi nhánh miền Nam trước khi chốt báo cáo",
    itemKey: "ERP-120",
    restorableUntilDay: "2026-11-27",
  },
  {
    id: "i-4",
    name: "Việc không có mã",
    itemKey: null,
    restorableUntilDay: "2026-11-28",
  },
];
