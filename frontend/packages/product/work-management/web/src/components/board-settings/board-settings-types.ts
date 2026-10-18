import type { ReactNode } from "react";
import type {
  BoardSettingsDto,
  BoardSettingsFieldPatch,
  BoardSettingsFieldType,
  BoardSettingsGeneralPatch,
  BoardSettingsViewDto,
  BoardSettingsViewPatch,
  BoardSettingsWriteResult,
} from "@notrelix/work-management-core";
import type { AutomationPanelProps } from "./automation-types";
import type { BoardSettingsCopy } from "./board-settings.copy";

export type { BoardSettingsWriteResult };

export type BoardSettingsSection =
  "general" | "fields" | "views" | "groups" | "automations";

export const BOARD_SETTINGS_SECTIONS: readonly BoardSettingsSection[] = [
  "general",
  "fields",
  "views",
  "groups",
  "automations",
];

/** The writes the settings page makes, each answering whether it went through. */
export interface BoardSettingsActions {
  updateGeneral(
    changes: BoardSettingsGeneralPatch,
  ): Promise<BoardSettingsWriteResult>;
  archiveBoard(): Promise<BoardSettingsWriteResult>;
  deleteBoard(confirmTitle: string): Promise<BoardSettingsWriteResult>;
  createField(type: BoardSettingsFieldType): Promise<BoardSettingsWriteResult>;
  reorderFields(ids: readonly string[]): Promise<BoardSettingsWriteResult>;
  updateField(
    fieldId: string,
    changes: BoardSettingsFieldPatch,
  ): Promise<BoardSettingsWriteResult>;
  archiveField(fieldId: string): Promise<BoardSettingsWriteResult>;
  restoreField(fieldId: string): Promise<BoardSettingsWriteResult>;
  addOption(
    fieldId: string,
    option: { name: string; color: string },
  ): Promise<BoardSettingsWriteResult>;
  updateOption(
    fieldId: string,
    optionId: string,
    changes: { name?: string; color?: string },
  ): Promise<BoardSettingsWriteResult>;
  deleteOption(
    fieldId: string,
    optionId: string,
  ): Promise<BoardSettingsWriteResult>;
  reorderOptions(
    fieldId: string,
    ids: readonly string[],
  ): Promise<BoardSettingsWriteResult>;
  createView(
    kind: BoardSettingsViewDto["kind"],
  ): Promise<BoardSettingsWriteResult>;
  reorderViews(ids: readonly string[]): Promise<BoardSettingsWriteResult>;
  updateView(
    viewId: string,
    changes: BoardSettingsViewPatch,
  ): Promise<BoardSettingsWriteResult>;
  duplicateView(viewId: string): Promise<BoardSettingsWriteResult>;
  deleteView(viewId: string): Promise<BoardSettingsWriteResult>;
  createGroup(name: string): Promise<BoardSettingsWriteResult>;
  reorderGroups(ids: readonly string[]): Promise<BoardSettingsWriteResult>;
  updateGroup(
    groupId: string,
    changes: { name?: string; color?: string },
  ): Promise<BoardSettingsWriteResult>;
  deleteGroup(
    groupId: string,
    moveToGroupId: string | null,
  ): Promise<BoardSettingsWriteResult>;
  setAutomationEnabled(
    automationId: string,
    enabled: boolean,
  ): Promise<BoardSettingsWriteResult>;
}

export type BoardSettingsMarkTone =
  "blue" | "gold" | "coral" | "sage" | "crimson" | "violet";

export interface BoardSettingsSpace {
  readonly name: string;
  readonly mark: {
    readonly initials: string;
    readonly tone: BoardSettingsMarkTone;
  };
}

export interface BoardSettingsSurfaceProps {
  status: "loading" | "error" | "forbidden" | "ready";
  settings: BoardSettingsDto | null;
  copy: BoardSettingsCopy;
  /** The board's title for the breadcrumb (known before the settings load). */
  boardTitle: string;
  space?: BoardSettingsSpace;
  /** Who to ask when the viewer cannot open the page; absent = any manager. */
  askName?: string;
  /** Time zone dates are written in ("Asia/Ho_Chi_Minh"). */
  timeZone: string;
  section: BoardSettingsSection;
  onSelectSection: (section: BoardSettingsSection) => void;
  fieldId: string | null;
  onSelectField: (fieldId: string | null) => void;
  viewId: string | null;
  onSelectView: (viewId: string | null) => void;
  actions: BoardSettingsActions;
  /** The rule editor and the run log of the automations section. */
  automation?: AutomationPanelProps;
  /** The Kanban view's own settings (lane limit, who manages statuses), shown in its panel. */
  kanbanSettings?: ReactNode;
  /** Reads the settings again (after a conflict or an error). */
  onReload: () => void;
  boardHref?: string;
  onBack: () => void;
  /** Opens the board's access dialog. */
  onOpenAccess?: () => void;
  /** The board was archived or deleted: leave the page. */
  onLeave?: (reason: "archived" | "deleted") => void;
  /** For stories and tests: start with the field-type picker open. */
  defaultPickerOpen?: boolean;
  /** For stories and tests: start with the archive dialog of a field open. */
  defaultArchiveFieldId?: string | null;
  /** For stories and tests: show a write failure at the start. */
  defaultFeedback?: "failed" | "conflict" | null;
}
