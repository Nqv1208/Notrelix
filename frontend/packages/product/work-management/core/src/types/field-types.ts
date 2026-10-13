export type FieldType =
  | "text"
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
  | "formula";

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
  semanticRole?: FieldSemanticRole;
  /**
   * The field's aggregate version from the board read: an option write sends
   * it as `expectedVersion`. Absent on older reads.
   */
  version?: number;
}

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
