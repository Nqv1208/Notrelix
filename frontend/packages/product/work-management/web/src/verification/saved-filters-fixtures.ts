/**
 * Deterministic fixtures for the saved-filters surface: a personal filter, a
 * personal filter that is applied, and filters shared by someone else, on the
 * ERP board. Filter names are board data, so they stay Vietnamese in both locales.
 */
import type { SavedFilter } from "@notrelix/work-management-core";
import {
  savedFiltersCopy,
  type SavedFiltersCopy,
} from "../components/saved-filters/saved-filters.copy";
import type { SavedFiltersMenuProps } from "../components/saved-filters/saved-filters-menu";

export function savedFiltersCopyFixture(
  locale: "vi" | "en" = "vi",
): SavedFiltersCopy {
  return savedFiltersCopy(locale);
}

export const SAVED_FILTER_URGENT: SavedFilter = {
  id: "sf-urgent",
  boardId: "board-erp",
  name: "Việc gấp tuần này",
  visibility: "me",
  isMine: true,
  criteria: { filter: "p.high~o", groupBy: "field-assignee" },
  version: 3,
};

export const SAVED_FILTER_REVIEW: SavedFilter = {
  id: "sf-review",
  boardId: "board-erp",
  name: "Chờ tôi duyệt",
  visibility: "me",
  isMine: true,
  criteria: { filter: "s.status-review" },
  version: 1,
};

export const SAVED_FILTER_SPRINT: SavedFilter = {
  id: "sf-sprint",
  boardId: "board-erp",
  name: "Sprint hiện tại",
  visibility: "board",
  isMine: false,
  criteria: { filter: "s.status-doing", groupBy: "field-status" },
  version: 2,
};

export const SAVED_FILTER_SHARED_MINE: SavedFilter = {
  id: "sf-bugs",
  boardId: "board-erp",
  name: "Lỗi chưa gán người",
  visibility: "board",
  isMine: true,
  criteria: { filter: "l.label-bug" },
  version: 1,
};

export type SavedFiltersScenario = Omit<
  SavedFiltersMenuProps,
  | "onApply"
  | "onClearApplied"
  | "onRetry"
  | "onSave"
  | "onRename"
  | "onSetVisibility"
  | "onRemove"
>;

const base = (locale: "vi" | "en"): SavedFiltersScenario => ({
  copy: savedFiltersCopyFixture(locale),
  status: "ready",
  filters: [
    SAVED_FILTER_URGENT,
    SAVED_FILTER_REVIEW,
    SAVED_FILTER_SPRINT,
    SAVED_FILTER_SHARED_MINE,
  ],
  appliedId: null,
  canEdit: true,
  canSaveCurrent: true,
});

export function savedFiltersDefaultScenario(
  locale: "vi" | "en" = "vi",
): SavedFiltersScenario {
  return base(locale);
}

/** A filter is applied: the menu button names it and the form is not offered. */
export function savedFiltersAppliedScenario(
  locale: "vi" | "en" = "vi",
): SavedFiltersScenario {
  return {
    ...base(locale),
    appliedId: SAVED_FILTER_URGENT.id,
    canSaveCurrent: false,
  };
}

/** A viewer: shared filters apply, nothing is saved, renamed, shared or deleted. */
export function savedFiltersReadOnlyScenario(
  locale: "vi" | "en" = "vi",
): SavedFiltersScenario {
  return {
    ...base(locale),
    filters: [SAVED_FILTER_SPRINT, SAVED_FILTER_SHARED_MINE],
    canEdit: false,
    canSaveCurrent: false,
  };
}

export function savedFiltersEmptyScenario(
  locale: "vi" | "en" = "vi",
): SavedFiltersScenario {
  return { ...base(locale), filters: [] };
}

export function savedFiltersLoadingScenario(
  locale: "vi" | "en" = "vi",
): SavedFiltersScenario {
  return { ...base(locale), status: "loading", filters: [] };
}

export function savedFiltersErrorScenario(
  locale: "vi" | "en" = "vi",
): SavedFiltersScenario {
  return { ...base(locale), status: "error", filters: [] };
}
