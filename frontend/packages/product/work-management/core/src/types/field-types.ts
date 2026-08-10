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

export interface FieldOption {
  id: string;
  label: string;
  color: string;
}

export interface FieldDefinition {
  id: string;
  boardId: string;
  name: string;
  fieldType: FieldType;
  options: FieldOption[];
  position: number;
  isHidden: boolean;
  isSystemField: boolean;
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
