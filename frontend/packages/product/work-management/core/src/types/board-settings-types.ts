/**
 * The board settings page model, assembled from the board read
 * (`GET /boards/{id}/full`): the board, its fields with options and its
 * groups. Writes go to the board, field, option and group routes, each with
 * the version of the thing it changes; a stale one answers 409. Parts the
 * product has no contract for are named in `unavailable`.
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
  | "link"
  | "autonumber"
  | "datetime-system";

/**
 * What the page cannot do yet because the product has no defined contract
 * for it. Absent or false = available; a true flag hides or locks the part.
 */
export interface BoardSettingsUnavailable {
  /** Changing the item key prefix. */
  readonly itemKeyPrefix?: boolean;
  /** Time zone and the first day of the week of the board. */
  readonly locale?: boolean;
  /** Archiving and restoring a field (and the "archived" list). */
  readonly fieldArchive?: boolean;
  /**
   * Deleting a field. Unlike the other flags it is offered only when explicitly
   * `false` (a board Manager confirms first); absent keeps the part hidden.
   */
  readonly fieldDelete?: boolean;
  /** Required, guidance, default option and "used in" of a field. */
  readonly fieldRules?: boolean;
  /** Saved and private views of the board. */
  readonly views?: boolean;
  /** Board automations. */
  readonly automations?: boolean;
  /** Deleting a group that still holds items. */
  readonly nonEmptyGroupDelete?: boolean;
  /** The field types a new field can take; absent = every type. */
  readonly creatableFieldTypes?: readonly BoardSettingsFieldType[];
}

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
  /** Version of the field, sent back by a write on it. */
  readonly version?: number;
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
  /** Version of the group, sent back by a write on it. */
  readonly version?: number;
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
  readonly unavailable?: BoardSettingsUnavailable;
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
