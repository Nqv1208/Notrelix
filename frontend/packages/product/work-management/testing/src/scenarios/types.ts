import type {
  Board,
  BoardGroup,
  BoardTableColumn,
  CardDetail,
} from "@notrelix/work-management-core";
import type { WorkManagementUiCapabilities } from "../fixtures/capabilities.fixture";

export type UiScenarioState =
  | "Default"
  | "Empty"
  | "EdgeData"
  | "HighDensity"
  | "ReadOnly"
  | "Loading"
  | "Unavailable";

export interface WorkManagementScenario<TData> {
  readonly id: string;
  readonly state: UiScenarioState;
  readonly capabilities: WorkManagementUiCapabilities;
  readonly data: TData;
}

export interface KanbanScenarioData {
  readonly workspaceId: string;
  readonly board: Board;
  readonly columns: BoardGroup[];
}

export interface TableScenarioData {
  readonly board: Board;
  readonly groups: BoardGroup[];
  readonly columns: BoardTableColumn[];
}

export interface CalendarScenarioData {
  readonly groups: BoardGroup[];
  readonly weekStartIso: string;
}

export interface TimelineScenarioData {
  readonly groups: BoardGroup[];
  readonly rangeStartIso: string;
  readonly rangeEndIso: string;
}

export interface TaskDetailScenarioData {
  readonly board: Board;
  readonly card: CardDetail | null;
  readonly isLoading: boolean;
  readonly error: string | null;
}
