export type FieldType =
  | "text"
  | "long_text"
  | "link"
  | "number"
  | "checkbox"
  | "select"
  | "multi_select"
  | "date"
  | "timeline"
  | "person"
  | "linked_page"
  | "progress"
  | "relation"
  | "formula"
  | "rollup";

/** Work status category of a Status option (board read contract). */
export type WorkStatusCategory = "todo" | "in_progress" | "review" | "done";

export interface FieldOption {
  id: string;
  label: string;
  color: string;
  /** Ordering key from the board read; options are listed in this order. */
  position?: string;
  /** Category of a Status option; absent on other fields and older reads. */
  category?: WorkStatusCategory | null;
}

/**
 * The board's semantic fields, resolved by the board read mapper: the Status
 * field (field type Status) and the priority field (first Select field named
 * priority / ưu tiên / độ ưu tiên / mức ưu tiên).
 */
export type FieldSemanticRole = "status" | "priority";

export interface FieldDefinition {
  id: string;
  boardId: string;
  name: string;
  fieldType: FieldType;
  options: FieldOption[];
  position: number;
  isHidden: boolean;
  isSystemField: boolean;
  /** Unit shown after a number ("giờ"), from the field settings. */
  unit?: string;
  /** "mark": a select value drawn with a coloured initials mark (departments). */
  display?: "mark";
  semanticRole?: FieldSemanticRole;
  /**
   * The field's aggregate version from the board read: an option write sends
   * it as `expectedVersion`. Absent on older reads.
   */
  version?: number;
}

/**
 * A Formula or Rollup result as the board read carries it: evaluated by the
 * backend on that read, never stored. `error` carries the stable code
 * (`divide-by-zero`, `unknown-field`, `cycle`, ...). `stale` is client-side
 * only: a source value was written and the read has not been refetched yet.
 */
export type ComputedValue =
  | { readonly state: "ok"; readonly value: unknown }
  | { readonly state: "empty" }
  | { readonly state: "error"; readonly errorCode: string }
  | { readonly state: "stale"; readonly value: unknown };

export interface FieldValue {
  cardId: string;
  fieldDefinitionId: string;
  value: unknown;
}

export interface BoardTableColumn {
  id: string;
  field: FieldDefinition;
  width: number;
  minWidth: number;
  isVisible: boolean;
}

export interface BoardColumnDtoApi {
  id: string;
  boardId: string;
  name: string;
  fieldType: string;
  settings?: Record<string, unknown> | string | null;
  position: number;
  isHidden: boolean;
  isSystemField: boolean;
}
