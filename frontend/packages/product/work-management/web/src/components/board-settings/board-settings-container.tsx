import { useMemo } from "react";
import {
  useBoardAccess,
  useBoardLabels,
  useBoardLabelWrites,
  useDeletedFields,
  useBoardTemplates,
  useBoardTemplateWrites,
  useBoardSettings,
  useBoardView,
  useFullBoard,
  usePeopleDirectory,
  useSavedViews,
  useWorkspaceAutomations,
} from "@notrelix/work-management-state";
import type { BoardSettingsFieldType } from "@notrelix/work-management-core";
import {
  resolveKanbanSettings,
  type BoardSettingsDto,
  type BoardSettingsViewDto,
  type BoardSettingsWriteResult,
} from "@notrelix/work-management-core";
import { KanbanSettingsPanel } from "../views/kanban/kanban-settings-panel";
import { useKanbanLaneCopy } from "../views/kanban/use-kanban-lane-copy";
import { useBoardFormsPanel } from "../forms/forms-containers";
import { useBoardSettingsCopy } from "./use-board-settings-copy";
import { BoardSettingsSurface } from "./board-settings-surface";
import { fillCopy } from "./board-settings-format";
import type {
  BoardSettingsActions,
  BoardSettingsSection,
  BoardSettingsSpace,
  BoardLabelsPanelProps,
  DeletedFieldsPanelProps,
  BoardTemplatesPanelProps,
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
  /** Opens a form of the board in its builder. */
  readonly onOpenForm: (formId: string) => void;
  /** Opens the response inbox of a form. */
  readonly onOpenFormResponses: (formId: string) => void;
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

/** "Table", "Table 2", ...: a view name the board has not used yet. */
function newViewName(base: string, settings: BoardSettingsDto): string {
  const taken = new Set(settings.views.map((view) => view.name.toLowerCase()));
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
  const { board, groups, paging } = useFullBoard(boardId, workspaceId);
  // The Kanban panel edits the Kanban view that is open (or the board's first one).
  const { viewConfig, updateViewConfig } = useBoardView(boardId, workspaceId, {
    viewId: props.viewId ?? undefined,
    mode: "kanban",
  });
  const savedViews = useSavedViews(boardId, workspaceId);
  const kanban = useMemo(() => resolveKanbanSettings(viewConfig), [viewConfig]);
  const { access, error: accessError } = useBoardAccess(boardId, workspaceId);
  const {
    settings,
    isLoading,
    isError,
    forbidden,
    refetch,
    write,
    readLatest,
  } = useBoardSettings(boardId, workspaceId);

  const actions = useMemo<BoardSettingsActions>(
    () => ({
      updateGeneral: (changes) =>
        write((api, s) => api.updateGeneral(s, changes)),
      archiveBoard: () => write((api, s) => api.archiveBoard(s)),
      unarchiveBoard: () => write((api, s) => api.unarchiveBoard(s)),
      deleteBoard: () => write((api, s) => api.deleteBoard(s)),
      createField: (type, input) =>
        write((api, s) =>
          api.createField(
            s,
            type,
            input?.name ?? newFieldName(copy.types[type], s),
            input?.settings,
          ),
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
      createView: (kind: BoardSettingsViewDto["kind"]) =>
        write((api, s) =>
          api.createView(s, kind, newViewName(copy.views.kinds[kind], s)),
        ),
      reorderViews: unsupported,
      updateView: (viewId, changes) =>
        write((api, s) => api.updateView(s, viewId, changes)),
      duplicateView: (viewId) => {
        const source = savedViews.views.find((view) => view.id === viewId);
        if (!source) return Promise.resolve({ ok: false, reason: "failed" });
        return write((api, s) =>
          api.createView(
            s,
            source.viewMode,
            newViewName(
              fillCopy(copy.viewPanel.copyName, { name: source.name }),
              s,
            ),
            source.viewConfig,
            source.visibility,
          ),
        );
      },
      setDefaultView: (viewId) =>
        write((api, s) => api.setDefaultView(s, viewId)),
      deleteView: (viewId, newDefaultViewId) =>
        write((api, s) => api.deleteView(s, viewId, newDefaultViewId)),
      createGroup: (name) => write((api, s) => api.createGroup(s, name)),
      reorderGroups: (ids) => write((api, s) => api.reorderGroups(s, ids)),
      updateGroup: (groupId, changes) =>
        write((api, s) => api.updateGroup(s, groupId, changes)),
      deleteGroup: (groupId, moveToGroupId) =>
        write((api, s) => api.deleteGroup(s, groupId, moveToGroupId)),
      duplicateGroup: (groupId, title) =>
        write((api, s) => api.duplicateGroup(s, groupId, title)),
      archiveGroup: (groupId) =>
        write((api, s) => api.archiveGroup(s, groupId)),
      unarchiveGroup: (groupId) =>
        write((api, s) => api.unarchiveGroup(s, groupId)),
      setAutomationEnabled: unsupported,
    }),
    [copy, savedViews.views, write],
  );

  const deletedList = useDeletedFields(boardId, workspaceId, !forbidden);
  const deletedFields = useMemo<DeletedFieldsPanelProps>(
    () => ({
      status: deletedList.isError
        ? "error"
        : deletedList.fields
          ? "ready"
          : "loading",
      fields: (deletedList.fields ?? []).flatMap((field) =>
        field.id && field.deletedAt && field.restorableUntil
          ? [
              {
                id: field.id,
                name: field.name ?? "",
                type: (
                  field.fieldType ?? "text"
                ).toLowerCase() as BoardSettingsFieldType,
                deletedAt: field.deletedAt,
                restorableUntil: field.restorableUntil,
                deletedByName: field.deletedByName ?? null,
                valueCount: field.valueCount ?? 0,
              },
            ]
          : [],
      ),
      restoringId: deletedList.restoringId,
      onReload: () => void deletedList.refetch(),
      onRestore: async (fieldId) => {
        try {
          await deletedList.restore(fieldId);
          return "restored";
        } catch (error) {
          const failure = error as { code?: string; status?: number } | null;
          if (failure?.code === "work-management.field.name-taken")
            return "name-taken";
          if (failure?.code === "work-management.field.restore-window-closed")
            return "expired";
          if (failure?.status === 403 || failure?.status === 404)
            return "forbidden";
          return "failed";
        }
      },
    }),
    [deletedList],
  );
  const boardLabels = useBoardLabels(boardId, workspaceId);
  const labelWrites = useBoardLabelWrites(boardId, workspaceId);
  const labels = useMemo<BoardLabelsPanelProps>(() => {
    // Items per label from the board read; unknown until the board is loaded.
    const counts = board ? new Map<string, number>() : null;
    if (counts)
      for (const group of groups)
        for (const card of group.cards)
          for (const label of card.labels)
            counts.set(label.id, (counts.get(label.id) ?? 0) + 1);
    return {
      status: boardLabels.isError
        ? "error"
        : boardLabels.isLoading
          ? "loading"
          : "ready",
      labels: (boardLabels.data ?? []).map((label) => ({
        ...label,
        ...(counts ? { itemCount: counts.get(label.id) ?? 0 } : {}),
      })),
      onReload: () => void boardLabels.refetch(),
      onCreate: labelWrites.create,
      onUpdate: labelWrites.update,
      onDelete: labelWrites.remove,
    };
  }, [board, boardLabels, groups, labelWrites]);

  const formNav = useMemo(
    () => ({
      onOpenBuilder: props.onOpenForm,
      onOpenResponses: props.onOpenFormResponses,
    }),
    [props.onOpenForm, props.onOpenFormResponses],
  );
  const forms = useBoardFormsPanel(boardId, workspaceId, formNav);

  const boardTemplates = useBoardTemplates(workspaceId);
  const templateWrites = useBoardTemplateWrites(workspaceId);
  const templates = useMemo<BoardTemplatesPanelProps>(() => {
    let itemCount: number | undefined;
    if (board) {
      itemCount = paging?.totalItems ?? 0;
      if (!paging) for (const group of groups) itemCount += group.cards.length;
    }
    return {
      status: boardTemplates.isError
        ? "error"
        : boardTemplates.isLoading
          ? "loading"
          : "ready",
      templates: (boardTemplates.data ?? []).filter(
        (template) => template.builtInKey === null,
      ),
      ...(itemCount === undefined ? {} : { itemCount }),
      onReload: () => void boardTemplates.refetch(),
      onSave: (input) => templateWrites.save(boardId, input),
      onPublish: templateWrites.publish,
      onArchive: templateWrites.archive,
      onDelete: templateWrites.remove,
    };
  }, [board, boardId, boardTemplates, groups, paging, templateWrites]);

  const rules = useWorkspaceAutomations(
    workspaceId,
    boardId,
    access?.myLevel === "manager",
  );
  const { directory } = usePeopleDirectory(boardId, workspaceId);
  const workspaceRules = useMemo<WorkspaceRulesPanelProps>(
    () => ({
      boardId,
      status: rules.forbidden
        ? "forbidden"
        : rules.isError
          ? "error"
          : rules.isLoading
            ? "loading"
            : "ready",
      rules: rules.rules,
      groups: groups.map((group) => ({ id: group.id, name: group.title })),
      statuses: (settings?.fields ?? [])
        .filter((field) => field.type === "status")
        .flatMap((field) => field.options)
        .map((option) => ({ id: option.id, name: option.name })),
      people: (directory?.people ?? []).map((person) => ({
        id: person.userId,
        name: person.name,
      })),
      fields: (settings?.fields ?? [])
        .filter((field) => field.type === "text" || field.type === "number")
        .map((field) => ({ id: field.id, name: field.name })),
      onReload: () => void rules.refetch(),
      onCreate: rules.create,
      onUpdate: rules.update,
      onDelete: rules.remove,
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
    [boardId, directory, groups, rules, settings],
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
      labels={labels}
      deletedFields={deletedFields}
      forms={forms}
      templates={templates}
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
      onReload={refetch}
      onReadLatest={readLatest}
      boardHref={props.boardHref}
      onBack={props.onBack}
      onOpenAccess={props.onOpenAccess}
      onLeave={props.onLeave}
    />
  );
}
