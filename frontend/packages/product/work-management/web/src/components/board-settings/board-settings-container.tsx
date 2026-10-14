import { useMemo } from "react";
import {
  useBoardAccess,
  useBoardSettings,
  useBoardView,
  useFullBoard,
} from "@notrelix/work-management-state";
import { resolveKanbanSettings } from "@notrelix/work-management-core";
import { KanbanSettingsPanel } from "../views/kanban/kanban-settings-panel";
import { useKanbanLaneCopy } from "../views/kanban/use-kanban-lane-copy";
import { useBoardSettingsCopy } from "./use-board-settings-copy";
import { BoardSettingsSurface } from "./board-settings-surface";
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
}

/**
 * The settings of one board: loads them (only a manager reads them), wires
 * every write to the settings endpoints and refetches the settings and the
 * board after each one.
 */
export function BoardSettingsPage(props: BoardSettingsPageProps) {
  const { workspaceId, boardId } = props;
  const copy = useBoardSettingsCopy();
  const laneCopy = useKanbanLaneCopy();
  const { board } = useFullBoard(boardId, workspaceId);
  const { viewConfig, updateViewConfig } = useBoardView(boardId, workspaceId);
  const kanban = useMemo(() => resolveKanbanSettings(viewConfig), [viewConfig]);
  const { access } = useBoardAccess(boardId, workspaceId);
  const { settings, isLoading, isError, forbidden, refetch, write } =
    useBoardSettings(boardId, workspaceId);

  const actions = useMemo<BoardSettingsActions>(
    () => ({
      updateGeneral: (changes) =>
        write((api, v) => api.updateGeneral(boardId, v, changes)),
      archiveBoard: () => write((api, v) => api.archiveBoard(boardId, v)),
      deleteBoard: (confirmTitle) =>
        write((api, v) => api.deleteBoard(boardId, v, confirmTitle)),
      createField: (type) =>
        write((api, v) => api.createField(boardId, v, type)),
      reorderFields: (ids) =>
        write((api, v) => api.reorderFields(boardId, v, ids)),
      updateField: (fieldId, changes) =>
        write((api, v) => api.updateField(boardId, v, fieldId, changes)),
      archiveField: (fieldId) =>
        write((api, v) => api.archiveField(boardId, v, fieldId)),
      restoreField: (fieldId) =>
        write((api, v) => api.restoreField(boardId, v, fieldId)),
      addOption: (fieldId, option) =>
        write((api, v) => api.addOption(boardId, v, fieldId, option)),
      updateOption: (fieldId, optionId, changes) =>
        write((api, v) =>
          api.updateOption(boardId, v, fieldId, optionId, changes),
        ),
      deleteOption: (fieldId, optionId) =>
        write((api, v) => api.deleteOption(boardId, v, fieldId, optionId)),
      reorderOptions: (fieldId, ids) =>
        write((api, v) => api.reorderOptions(boardId, v, fieldId, ids)),
      createView: (kind) => write((api, v) => api.createView(boardId, v, kind)),
      reorderViews: (ids) =>
        write((api, v) => api.reorderViews(boardId, v, ids)),
      updateView: (viewId, changes) =>
        write((api, v) => api.updateView(boardId, v, viewId, changes)),
      duplicateView: (viewId) =>
        write((api, v) => api.duplicateView(boardId, v, viewId)),
      deleteView: (viewId) =>
        write((api, v) => api.deleteView(boardId, v, viewId)),
      createGroup: (name) =>
        write((api, v) => api.createGroup(boardId, v, name)),
      reorderGroups: (ids) =>
        write((api, v) => api.reorderGroups(boardId, v, ids)),
      updateGroup: (groupId, changes) =>
        write((api, v) => api.updateGroup(boardId, v, groupId, changes)),
      deleteGroup: (groupId, moveToGroupId) =>
        write((api, v) => api.deleteGroup(boardId, v, groupId, moveToGroupId)),
      setAutomationEnabled: (automationId, enabled) =>
        write((api, v) =>
          api.setAutomationEnabled(boardId, v, automationId, enabled),
        ),
    }),
    [boardId, write],
  );

  const status = forbidden
    ? "forbidden"
    : isError
      ? "error"
      : isLoading || !settings
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
