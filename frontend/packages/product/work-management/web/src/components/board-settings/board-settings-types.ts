import type { ReactNode } from "react";
import type {
  BoardSettingsDto,
  BoardSettingsFieldPatch,
  BoardSettingsFieldType,
  BoardSettingsGeneralPatch,
  BoardSettingsViewDto,
  BoardSettingsViewPatch,
  BoardSettingsWriteResult,
  CreateWorkspaceAutomationInput,
  WorkspaceAutomationDefinition,
  WorkspaceAutomationRuleDto,
  WorkspaceAutomationRunDto,
  WorkspaceAutomationWriteResult,
} from "@notrelix/work-management-core";
import type { AutomationPanelProps } from "./automation-types";
import type { BoardFormsPanelProps } from "../forms/forms-list-panel";
import type { BoardSettingsCopy } from "./board-settings.copy";

export type { BoardSettingsWriteResult };

export type BoardSettingsSection =
  | "general"
  | "fields"
  | "views"
  | "groups"
  | "labels"
  | "forms"
  | "templates"
  | "automations";

export const BOARD_SETTINGS_SECTIONS: readonly BoardSettingsSection[] = [
  "general",
  "fields",
  "views",
  "groups",
  "labels",
  "forms",
  "templates",
  "automations",
];

/** One label of the board; `itemCount` is how many items carry it, when the board is read. */
export interface BoardLabelRow {
  readonly id: string;
  readonly name: string;
  readonly color: string;
  readonly itemCount?: number;
}

/** The labels of the board and the writes on them (create, rename, recolour, delete). */
export interface BoardLabelsPanelProps {
  readonly status: "loading" | "error" | "ready";
  readonly labels: readonly BoardLabelRow[];
  readonly onReload: () => void;
  readonly onCreate: (
    name: string,
    color: string,
  ) => Promise<BoardSettingsWriteResult>;
  readonly onUpdate: (
    labelId: string,
    changes: { name?: string; color?: string },
  ) => Promise<BoardSettingsWriteResult>;
  readonly onDelete: (labelId: string) => Promise<BoardSettingsWriteResult>;
}

/** One template of the workspace (the built-in starters are not listed here). */
export interface BoardTemplateRow {
  readonly id: string;
  readonly name: string;
  readonly description: string | null;
  readonly status: "draft" | "published" | "archived";
}

/**
 * The workspace's board templates and the writes on them. `onSave` saves the
 * board whose settings are open; publish, archive and delete need the owner or
 * an admin of the workspace, which the server decides (a refusal is a failed write).
 */
export interface BoardTemplatesPanelProps {
  readonly status: "loading" | "error" | "ready";
  readonly templates: readonly BoardTemplateRow[];
  /** How many items the board has, for "include the items"; unknown until the board is read. */
  readonly itemCount?: number;
  readonly onReload: () => void;
  readonly onSave: (input: {
    name: string;
    description?: string;
    includeItems: boolean;
  }) => Promise<BoardSettingsWriteResult>;
  readonly onPublish: (templateId: string) => Promise<BoardSettingsWriteResult>;
  readonly onArchive: (templateId: string) => Promise<BoardSettingsWriteResult>;
  readonly onDelete: (templateId: string) => Promise<BoardSettingsWriteResult>;
}

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
  /** Deletes a field for good; a board Manager confirms first. */
  deleteField(fieldId: string): Promise<BoardSettingsWriteResult>;
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
  /** The tab a board opens on. */
  setDefaultView(viewId: string): Promise<BoardSettingsWriteResult>;
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
  /** The workspace's automation rules: when given, the section shows them instead of the rule editor. */
  workspaceRules?: WorkspaceRulesPanelProps;
  /** The labels of the board: when given, the page has a "Labels" section. */
  labels?: BoardLabelsPanelProps;
  /** The forms of the board: when given, the page has a "Forms" section. */
  forms?: BoardFormsPanelProps;
  /** The workspace's board templates: when given, the page has a "Board templates" section. */
  templates?: BoardTemplatesPanelProps;
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

/** One choice of the rule editor: a group, a status, a person or a field of the board. */
export interface WorkspaceRuleChoice {
  readonly id: string;
  readonly name: string;
}

/**
 * The automation rules of the board shown in the "Tự động hoá" section: a rule
 * has one trigger, optional conditions that must all hold, and up to five
 * actions that run in order on the item that triggered it.
 */
export interface WorkspaceRulesPanelProps {
  /** The board whose rules the panel lists and creates. */
  readonly boardId: string;
  readonly status: "loading" | "error" | "forbidden" | "ready";
  readonly rules: readonly WorkspaceAutomationRuleDto[];
  /** What the editor offers and what a rule's sentence names: all of this board. */
  readonly groups: readonly WorkspaceRuleChoice[];
  readonly statuses: readonly WorkspaceRuleChoice[];
  readonly people: readonly WorkspaceRuleChoice[];
  /** The fields a rule can write or test (text and number fields). */
  readonly fields: readonly WorkspaceRuleChoice[];
  readonly onReload: () => void;
  readonly onCreate: (
    input: CreateWorkspaceAutomationInput,
  ) => Promise<WorkspaceAutomationWriteResult>;
  readonly onUpdate: (
    ruleId: string,
    definition: WorkspaceAutomationDefinition,
  ) => Promise<WorkspaceAutomationWriteResult>;
  readonly onDelete: (
    ruleId: string,
  ) => Promise<WorkspaceAutomationWriteResult>;
  readonly onSetEnabled: (
    ruleId: string,
    enabled: boolean,
  ) => Promise<WorkspaceAutomationWriteResult>;
  /** The rule whose runs are open, with the runs read for it. */
  readonly openRuleId: string | null;
  readonly onOpenRuns: (ruleId: string | null) => void;
  readonly runs: {
    readonly status: "loading" | "error" | "ready";
    readonly data: readonly WorkspaceAutomationRunDto[];
  };
}
