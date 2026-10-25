import { useMemo } from "react";
import {
  useBoardAccess,
  useBoardAutomationRule,
  useBoardAutomationRuns,
  useBoardSettings,
  useBoardView,
  useFullBoard,
} from "@notrelix/work-management-state";
import {
  resolveKanbanSettings,
  type BoardSettingsDto,
  type BoardSettingsWriteResult,
} from "@notrelix/work-management-core";
import { KanbanSettingsPanel } from "../views/kanban/kanban-settings-panel";
import { useKanbanLaneCopy } from "../views/kanban/use-kanban-lane-copy";
import { useBoardSettingsCopy } from "./use-board-settings-copy";
import { BoardSettingsSurface } from "./board-settings-surface";
import type { AutomationPanelProps } from "./automation-types";
import type {
  BoardSettingsActions,
  BoardSettingsSection,
  BoardSettingsSpace,
} from "./board-settings-types";

/** What the host (the route) knows about where the page sits. */
export interface BoardSettingsHost {
  readonly space?: BoardSettingsSpace;
  readonly boardHref?: string;
  readonly onBack: () => void;
  readonly onOpenAccess?: () => void;
  /** The board was archived or deleted: leave the page. */
  readonly onLeave?: (reason: "archived" | "deleted") => void;
}

export interface BoardSettingsPageProps extends BoardSettingsHost {
  readonly workspaceId: string;
  readonly boardId: string;
  readonly section: BoardSettingsSection;
  readonly onSelectSection: (section: BoardSettingsSection) => void;
  readonly fieldId: string | null;
  readonly onSelectField: (fieldId: string | null) => void;
  readonly viewId: string | null;
  readonly onSelectView: (viewId: string | null) => void;
  /** The automation rule open in its editor; null shows the list of rules. */
  readonly ruleId: string | null;
  readonly onSelectRule: (ruleId: string | null) => void;
  /** The run log of the automations is open. */
  readonly log: boolean;
  readonly onSelectLog: (open: boolean) => void;
  /** The run open in the log's sheet. */
  readonly runId: string | null;
  readonly onSelectRun: (runId: string | null) => void;
  /** The Integrations and the billing pages, when they exist. */
  readonly integrationsHref?: string;
  readonly billingHref?: string;
}

const unsupported = (): Promise<BoardSettingsWriteResult> =>
  Promise.resolve({ ok: false, reason: "failed" });

/** "Date", "Date 2", ... : a name no field of the board has yet. */
function newFieldName(base: string, settings: BoardSettingsDto): string {
  const taken = new Set(
    [...settings.fields, ...settings.systemFields].map((field) =>
      field.name.toLowerCase(),
    ),
  );
  let name = base;
  for (let n = 2; taken.has(name.toLowerCase()); n += 1) name = `${base} ${n}`;
  return name;
}

/**
 * The settings of one board: read from the board itself (only a manager
 * opens the page), every write goes to the board, field, option and group
 * routes, and the board is read again after each one.
 */
export function BoardSettingsPage(props: BoardSettingsPageProps) {
  const { workspaceId, boardId } = props;
  const copy = useBoardSettingsCopy();
  const laneCopy = useKanbanLaneCopy();
  const { board } = useFullBoard(boardId, workspaceId);
  const { viewConfig, updateViewConfig } = useBoardView(boardId, workspaceId);
  const kanban = useMemo(() => resolveKanbanSettings(viewConfig), [viewConfig]);
  const { access, error: accessError } = useBoardAccess(boardId, workspaceId);
  const { settings, isLoading, isError, forbidden, refetch, write } =
    useBoardSettings(boardId, workspaceId);

  const actions = useMemo<BoardSettingsActions>(
    () => ({
      updateGeneral: (changes) =>
        write((api, s) => api.updateGeneral(s, changes)),
      archiveBoard: () => write((api, s) => api.archiveBoard(s)),
      deleteBoard: () => write((api, s) => api.deleteBoard(s)),
      createField: (type) =>
        write((api, s) =>
          api.createField(s, type, newFieldName(copy.types[type], s)),
        ),
      reorderFields: (ids) => write((api, s) => api.reorderFields(s, ids)),
      updateField: (fieldId, changes) =>
        write((api, s) => api.updateField(s, fieldId, changes)),
      archiveField: unsupported,
      restoreField: unsupported,
      addOption: (fieldId, option) =>
        write((api, s) => api.addOption(s, fieldId, option)),
      updateOption: (fieldId, optionId, changes) =>
        write((api, s) => api.updateOption(s, fieldId, optionId, changes)),
      deleteOption: (fieldId, optionId) =>
        write((api, s) => api.deleteOption(s, fieldId, optionId)),
      reorderOptions: (fieldId, ids) =>
        write((api, s) => api.reorderOptions(s, fieldId, ids)),
      createView: unsupported,
      reorderViews: unsupported,
      updateView: unsupported,
      duplicateView: unsupported,
      deleteView: unsupported,
      createGroup: (name) => write((api, s) => api.createGroup(s, name)),
      reorderGroups: (ids) => write((api, s) => api.reorderGroups(s, ids)),
      updateGroup: (groupId, changes) =>
        write((api, s) => api.updateGroup(s, groupId, changes)),
      deleteGroup: (groupId) => write((api, s) => api.deleteGroup(s, groupId)),
      setAutomationEnabled: unsupported,
    }),
    [copy, write],
  );

  const automationRule = useBoardAutomationRule(
    boardId,
    workspaceId,
    props.log ? null : props.ruleId,
  );
  const automationRuns = useBoardAutomationRuns(
    boardId,
    workspaceId,
    props.log,
  );
  const { onSelectRule } = props;
  const automation = useMemo<AutomationPanelProps>(
    () => ({
      ruleId: props.log ? null : props.ruleId,
      log: props.log,
      runId: props.runId,
      rule: {
        status: !props.ruleId
          ? "idle"
          : automationRule.isLoading
            ? "loading"
            : automationRule.notFound
              ? "not-found"
              : automationRule.isError
                ? "error"
                : automationRule.rule
                  ? "ready"
                  : "loading",
        data: automationRule.rule,
      },
      runs: {
        status: automationRuns.isLoading
          ? "loading"
          : automationRuns.isError
            ? "error"
            : automationRuns.runs
              ? "ready"
              : "idle",
        data: automationRuns.runs,
      },
      onOpenRule: (ruleId) => onSelectRule(ruleId),
      onCreateRule: async () => {
        const result = await automationRule.create();
        if (result.ok && result.id) onSelectRule(result.id);
        return result.ok;
      },
      onCloseRule: () => onSelectRule(null),
      onOpenLog: () => props.onSelectLog(true),
      onOpenRun: props.onSelectRun,
      onReloadRule: () => void automationRule.refetch(),
      onReloadRuns: () => void automationRuns.refetch(),
      actions: {
        save: automationRule.save,
        setEnabled: actions.setAutomationEnabled,
        changeRunner: automationRule.changeRunner,
        restoreField: actions.restoreField,
        rerunStep: automationRuns.rerunStep,
      },
      integrationsHref: props.integrationsHref,
      billingHref: props.billingHref,
    }),
    [actions, automationRule, automationRuns, onSelectRule, props],
  );

  // Only a manager of the board opens its settings.
  const notManager = access ? access.myLevel !== "manager" : false;
  const waitingForAccess = !access && !accessError;
  const status =
    forbidden || notManager
      ? "forbidden"
      : isError
        ? "error"
        : isLoading || !settings || waitingForAccess
          ? "loading"
          : "ready";

  return (
    <BoardSettingsSurface
      status={status}
      settings={settings}
      copy={copy}
      boardTitle={settings?.general.title ?? board?.title ?? ""}
      space={props.space}
      askName={access?.mySource.grantedBy?.name}
      timeZone={settings?.general.timeZone ?? "Asia/Ho_Chi_Minh"}
      section={props.section}
      onSelectSection={props.onSelectSection}
      fieldId={props.fieldId}
      onSelectField={props.onSelectField}
      viewId={props.viewId}
      onSelectView={props.onSelectView}
      actions={actions}
      automation={automation}
      kanbanSettings={
        <KanbanSettingsPanel
          copy={laneCopy}
          settings={kanban}
          onChange={({ statusManageRoles, ...rest }) =>
            updateViewConfig(
              statusManageRoles
                ? { ...rest, statusManageRoles: [...statusManageRoles] }
                : rest,
            )
          }
        />
      }
      onReload={() => void refetch()}
      boardHref={props.boardHref}
      onBack={props.onBack}
      onOpenAccess={props.onOpenAccess}
      onLeave={props.onLeave}
    />
  );
}
