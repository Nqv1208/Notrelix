/**
 * Deterministic fixtures for the Kanban toolbar surface: the ERP board's view
 * controls (filter, group, swimlane) as the mockup shows them, in the toolbar's
 * `viewBar` slot. Option names are board data, so they stay Vietnamese in both
 * locales.
 */
import {
  kanbanToolbarCopy,
  type KanbanToolbarCopy,
} from "../components/views/kanban/kanban-toolbar.copy";
import type { KanbanToolbarProps } from "../components/views/kanban/kanban-toolbar";
import { erpViewBar } from "./view-state-fixtures";

export function kanbanToolbarCopyFixture(
  locale: "vi" | "en" = "vi",
): KanbanToolbarCopy {
  return kanbanToolbarCopy(locale);
}

export type KanbanToolbarScenario = Omit<
  KanbanToolbarProps,
  "onSearchChange" | "onClearFilter" | "onAddItem"
>;

export function kanbanToolbarDefaultScenario(
  locale: "vi" | "en" = "vi",
): KanbanToolbarScenario {
  return {
    copy: kanbanToolbarCopyFixture(locale),
    searchQuery: "",
    viewBar: erpViewBar({ locale, view: "kanban" }),
    canAdd: true,
    noResults: null,
  };
}

export function kanbanToolbarFilteredScenario(
  locale: "vi" | "en" = "vi",
): KanbanToolbarScenario {
  return {
    ...kanbanToolbarDefaultScenario(locale),
    viewBar: erpViewBar({
      locale,
      view: "kanban",
      state: {
        filter: {
          join: "and",
          conditions: [
            {
              fieldId: "field-erp-status",
              operator: "has_any",
              values: ["status-doing"],
            },
            {
              fieldId: "$label",
              operator: "has_any",
              values: ["label-security"],
            },
          ],
        },
      },
      counts: { shown: 3, total: 16 },
    }),
  };
}

/** Board khong-ket-qua: the filter hides all 16 items of the ERP board. */
export function kanbanToolbarNoResultsScenario(
  locale: "vi" | "en" = "vi",
): KanbanToolbarScenario {
  return {
    ...kanbanToolbarDefaultScenario(locale),
    viewBar: erpViewBar({
      locale,
      view: "kanban",
      state: {
        filter: {
          join: "and",
          conditions: [
            { fieldId: "$overdue", operator: "is_true", values: [] },
            {
              fieldId: "field-erp-priority",
              operator: "has_any",
              values: ["p-low"],
            },
          ],
        },
      },
      counts: { shown: 0, total: 16 },
    }),
    noResults: { hiddenCount: 16 },
  };
}

/** Board chi-xem: a commenter cannot add items. */
export function kanbanToolbarReadOnlyScenario(
  locale: "vi" | "en" = "vi",
): KanbanToolbarScenario {
  return { ...kanbanToolbarDefaultScenario(locale), canAdd: false };
}

/** A long search and a full filter with a swimlane field. */
export function kanbanToolbarEdgeDataScenario(
  locale: "vi" | "en" = "vi",
): KanbanToolbarScenario {
  return {
    ...kanbanToolbarDefaultScenario(locale),
    searchQuery:
      "đối soát hoá đơn đầu vào quý 3 cho toàn bộ chi nhánh phía Bắc",
    viewBar: erpViewBar({
      locale,
      view: "kanban",
      state: {
        filter: {
          join: "or",
          conditions: [
            {
              fieldId: "field-erp-priority",
              operator: "has_any",
              values: ["p-urgent", "p-high", "p-medium", "p-low"],
            },
            {
              fieldId: "field-erp-owner",
              operator: "has_any",
              values: ["user-dang-minh-tuan", "user-phan-ngoc-anh"],
            },
            {
              fieldId: "$title",
              operator: "contains",
              values: ["đối soát hoá đơn đầu vào quý 3 chi nhánh phía Bắc"],
            },
            { fieldId: "$overdue", operator: "is_true", values: [] },
          ],
        },
        lane: { fieldId: "field-erp-owner" },
      },
    }),
  };
}
