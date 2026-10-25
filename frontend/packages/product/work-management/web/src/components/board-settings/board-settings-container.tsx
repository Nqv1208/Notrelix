import { useMemo } from "react";
import {
  useBoardAccess,
  useBoardSettings,
  useBoardView,
  useFullBoard,
  useWorkspaceAutomations,
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
import type {
  BoardSettingsActions,
  BoardSettingsSection,
  BoardSettingsSpace,
  WorkspaceRulesPanelProps,
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
  const { board, groups } = useFullBoard(boardId, workspaceId);
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
      deleteField: (fieldId) => write((api, s) => api.deleteField(s, fieldId)),
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
      deleteGroup: (groupId, moveToGroupId) =>
        write((api, s) => api.deleteGroup(s, groupId, moveToGroupId)),
      setAutomationEnabled: unsupported,
    }),
    [copy, write],
  );

  const rules = useWorkspaceAutomations(workspaceId);
  const workspaceRules = useMemo<WorkspaceRulesPanelProps>(
    () => ({
      status: rules.forbidden
        ? "forbidden"
        : rules.isError
          ? "error"
          : rules.isLoading
            ? "loading"
            : "ready",
      rules: rules.rules,
      items: groups.flatMap((group) =>
        group.cards.map((card) => ({
          id: card.id,
          name: card.title,
          groupId: group.id,
        })),
      ),
      groups: groups.map((group) => ({ id: group.id, name: group.title })),
      onReload: () => void rules.refetch(),
      onCreate: rules.create,
      onSetEnabled: rules.setEnabled,
      openRuleId: rules.openRuleId,
      onOpenRuns: rules.openRuns,
      runs: {
        status: rules.runsLoading
          ? "loading"
          : rules.runsError
            ? "error"
            : "ready",
        data: rules.runs,
      },
    }),
    [groups, rules],
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
      workspaceRules={workspaceRules}
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
