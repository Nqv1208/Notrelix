/**
 * Deterministic fixtures for the "Add item" dialog: the ERP board's groups,
 * statuses, priorities, people and labels (board data, so Vietnamese in both
 * locales) and one scenario per story state. The clock is injected (12/10/2026).
 */
import type { ItemCreateDialogProps } from "../components/item-create/item-create-dialog";
import {
  en,
  vi,
  type ItemCreateCopy,
} from "../components/item-create/item-create.copy";

function itemCreateCopy(locale: "vi" | "en"): ItemCreateCopy {
  return locale === "vi" ? vi : en;
}

export const ITEM_CREATE_STORY_TODAY = "2026-10-12";

export type ItemCreateScenario = Omit<
  ItemCreateDialogProps,
  "onOpenChange" | "onSubmit"
>;

/** The form opened from the board's header: every field empty or defaulted. */
export function itemCreateDefaultScenario(
  locale: "vi" | "en" = "vi",
): ItemCreateScenario {
  return {
    copy: itemCreateCopy(locale),
    open: true,
    groups: [
      { id: "g-1", label: "Giai đoạn 1" },
      { id: "g-2", label: "Giai đoạn 2" },
    ],
    defaultGroupId: "g-1",
    statuses: [
      { id: "s-todo", label: "Cần làm" },
      { id: "s-doing", label: "Đang làm" },
      { id: "s-review", label: "Chờ duyệt" },
      { id: "s-done", label: "Hoàn thành" },
    ],
    defaultStatusId: "s-todo",
    priorities: [
      { id: "p-high", label: "Cao" },
      { id: "p-medium", label: "Trung bình" },
      { id: "p-low", label: "Thấp" },
    ],
    members: [
      { userId: "u-1", name: "Nguyễn Thu Hà", initials: "TH" },
      { userId: "u-2", name: "Trần Minh Quân", initials: "MQ" },
      { userId: "u-3", name: "Lê Khánh Linh", initials: "KL" },
    ],
    labels: [
      { id: "l-1", name: "Dữ liệu", color: "#3b82f6" },
      { id: "l-2", name: "Khẩn", color: "#ef4444" },
    ],
    currentUserId: "u-1",
    pending: false,
    error: null,
    today: ITEM_CREATE_STORY_TODAY,
  };
}

/** The create request failed: the message shows and the form keeps what was typed. */
export function itemCreateErrorScenario(
  locale: "vi" | "en" = "vi",
): ItemCreateScenario {
  return {
    ...itemCreateDefaultScenario(locale),
    error: itemCreateCopy(locale).failed,
  };
}

/** The request is in flight: the form shows but cannot be submitted again. */
export function itemCreatePendingScenario(
  locale: "vi" | "en" = "vi",
): ItemCreateScenario {
  return { ...itemCreateDefaultScenario(locale), pending: true };
}

/** Opened from a lane's quick add: a lane chip, a typed title and long board data. */
export function itemCreateEdgeDataScenario(
  locale: "vi" | "en" = "vi",
): ItemCreateScenario {
  const base = itemCreateDefaultScenario(locale);
  return {
    ...base,
    groups: [
      {
        id: "g-1",
        label:
          "Giai đoạn triển khai và nghiệm thu hệ thống quản lý kho cho toàn bộ chi nhánh miền Nam",
      },
    ],
    defaultTitle:
      "Đối soát số liệu tồn kho cuối tháng giữa kho trung tâm và các chi nhánh trước khi chốt báo cáo",
    context: { kind: "quick", name: "Chờ duyệt" },
    defaultDueDay: "2026-10-09",
    defaultAssigneeIds: ["u-1", "u-2", "u-3"],
  };
}
