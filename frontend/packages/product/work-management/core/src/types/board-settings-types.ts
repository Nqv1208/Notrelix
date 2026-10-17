/**
 * "Thiết lập board" reads and writes beyond the generated contract (gap
 * `CTR-GAP-WM-BOARD-SETTINGS`, see docs/superpowers/be-blockers-2026-10.md,
 * "BE requirement from FE: Settings"). Only a manager of the board reads or
 * writes them (403 otherwise). A write may carry the `version` of the read
 * it was made from; a stale one answers 409.
 */
export type BoardSettingsFieldType =
  | "text"
  | "longtext"
  | "number"
  | "date"
  | "checkbox"
  | "file"
  | "select"
  | "multiselect"
  | "status"
  | "person"
  | "relation"
  | "formula"
  | "rollup"
  | "progress"
  | "autonumber"
  | "datetime-system";

export interface BoardSettingsOptionDto {
  readonly id: string;
  readonly name: string;
  readonly color: string;
  /** How many items carry the option. */
  readonly itemCount: number;
}

export interface BoardSettingsUsageDto {
  readonly kind: "view" | "card" | "filter" | "rule";
  readonly name: string;
  readonly how: string;
}

export interface BoardSettingsFieldDto {
  readonly id: string;
  readonly key: string;
  readonly type: BoardSettingsFieldType;
  readonly name: string;
  /** The words under the type, produced by the server ("nhiều người"). */
  readonly detail: string;
  /** The short "Đang dùng" cell, produced by the server ("5 view"). */
  readonly usage: string;
  /** The field splits the Kanban: drawn as a chip. */
  readonly accent: boolean;
  readonly unit: string | null;
  /** The field is a stored column (it has values and options to edit). */
  readonly hasColumn: boolean;
  readonly valueCount: number;
  readonly options: readonly BoardSettingsOptionDto[];
  readonly defaultOptionId: string | null;
  readonly required: boolean;
  readonly guidance: string;
  readonly usedIn: readonly BoardSettingsUsageDto[];
}

export interface BoardSettingsSystemFieldDto {
  readonly id: string;
  readonly key: string;
  readonly name: string;
  readonly type: BoardSettingsFieldType;
  readonly detail: string;
  readonly lock: "rename" | "locked";
}

export interface BoardSettingsArchivedFieldDto {
  readonly id: string;
  readonly key: string;
  readonly type: BoardSettingsFieldType;
  readonly name: string;
  readonly archivedAt: string;
  readonly archivedByName: string;
  readonly valueCount: number;
}

export interface BoardSettingsViewDto {
  readonly id: string;
  readonly name: string;
  readonly kind: "table" | "kanban" | "calendar" | "timeline";
  readonly summary: string;
  readonly visibility: "board" | "me";
  readonly isDefault: boolean;
  /** The field the view splits or places by. */
  readonly fieldId: string | null;
  readonly filters: readonly {
    readonly fieldName: string;
    readonly operator: string;
    readonly values: string;
  }[];
  readonly sort: { readonly fieldName: string; readonly text: string } | null;
  readonly cardFieldIds: readonly string[];
  /** The fields a card of the view can show. */
  readonly cardFieldChoices: readonly string[];
  readonly lastEditedBy: string;
  readonly createdBy: string;
  /** The view reads an archived field and shows nothing. */
  readonly needsFix: boolean;
}

export interface BoardSettingsGroupDto {
  readonly id: string;
  readonly name: string;
  readonly color: string;
  readonly itemCount: number;
}

export interface BoardSettingsAutomationDto {
  readonly id: string;
  readonly name: string;
  readonly enabled: boolean;
  readonly when: string;
  readonly condition?: string;
  readonly then: readonly string[];
  readonly stats: string;
  readonly runAs: { readonly name: string; readonly initials: string };
  /** A draft: it never ran and is not in the running or paused lists. */
  readonly draft?: boolean;
  readonly pausedReason?: {
    readonly lead: string;
    readonly rest: string;
    readonly action: string;
  };
  /** "lỗi một phần" inside `stats`, drawn in the danger colour, then `failureDetail`. */
  readonly partialFailure?: string;
  readonly failureDetail?: string;
}

export interface BoardSettingsGeneralDto {
  readonly title: string;
  readonly description: string;
  readonly color: string;
  readonly icon: string;
  /** An existing item key the key hint names. */
  readonly sampleItemKey: string | null;
  readonly itemKeyPrefix: string;
  readonly department: { readonly id: string; readonly name: string } | null;
  readonly timeZone: string;
  readonly weekStart: "monday" | "sunday";
  readonly isArchived: boolean;
}

export interface BoardSettingsDto {
  readonly boardId: string;
  /** Version of this read: a write may send it back to detect a conflict. */
  readonly version: number;
  readonly itemCount: number;
  readonly general: BoardSettingsGeneralDto;
  readonly fields: readonly BoardSettingsFieldDto[];
  readonly systemFields: readonly BoardSettingsSystemFieldDto[];
  readonly archivedFields: readonly BoardSettingsArchivedFieldDto[];
  readonly views: readonly BoardSettingsViewDto[];
  readonly groups: readonly BoardSettingsGroupDto[];
  readonly automations: readonly BoardSettingsAutomationDto[];
}

export interface BoardSettingsGeneralPatch {
  readonly title?: string;
  readonly description?: string;
  readonly color?: string;
  readonly itemKeyPrefix?: string;
  readonly timeZone?: string;
  readonly weekStart?: "monday" | "sunday";
}

export interface BoardSettingsFieldPatch {
  readonly name?: string;
  readonly defaultOptionId?: string | null;
  readonly required?: boolean;
  readonly guidance?: string;
}

export interface BoardSettingsViewPatch {
  readonly name?: string;
  readonly fieldId?: string | null;
  readonly cardFieldIds?: readonly string[];
  readonly visibility?: "board" | "me";
}

/** Why a write did not go through, as the page tells it. */
export type BoardSettingsWriteFailure = "conflict" | "invalid" | "failed";

export type BoardSettingsWriteResult =
  | { ok: true }
  | {
      ok: false;
      reason: BoardSettingsWriteFailure;
      /** The request field a refused value belongs to ("name"). */
      field?: string;
    };
