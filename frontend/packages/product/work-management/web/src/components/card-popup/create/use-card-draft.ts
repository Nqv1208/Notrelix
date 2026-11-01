import { useCallback, useMemo, useState } from "react";
import {
  useBoardLabels,
  useWorkManagementServices,
  usePeopleDirectory,
  type CreateItemWithDetailsInput,
} from "@notrelix/work-management-state";
import {
  personInitials,
  type Board,
  type CardDetail,
  type CardFile,
  type CardMember,
  type Checklist,
  type UpdateCardInput,
  type UpdateFieldValueInput,
} from "@notrelix/work-management-core";
import { findItemFields } from "../../card-detail/item-field-editors";
import { withBoardPriority } from "../../shared/item-priority";
import type { ItemChecklistsModel } from "../../checklists/item-checklists";
import type {
  TaskDetailCallbacks,
  TaskDetailData,
} from "../../card-detail/task-detail-types";
import type { CardPopupActions } from "../card-popup-context";

/** What the popup collects before the card exists. */
export interface CardDraft {
  readonly title: string;
  readonly descriptionMd: string;
  readonly listId: string;
  readonly statusOptionId: string | null;
  readonly priorityOptionId: string | null;
  /** ISO instants; the due one carries its time. */
  readonly startDate: string | null;
  readonly dueDate: string | null;
  readonly assigneeIds: readonly string[];
  readonly labelIds: readonly string[];
  readonly fieldValues: Readonly<Record<string, unknown>>;
  readonly checklists: readonly {
    readonly id: string;
    readonly title: string;
    readonly items: readonly {
      readonly id: string;
      readonly title: string;
      readonly isDone: boolean;
    }[];
  }[];
  readonly links: readonly {
    readonly id: string;
    readonly url: string;
    readonly name: string;
  }[];
  readonly files: readonly { readonly id: string; readonly file: File }[];
}

export interface CardDraftDefaults {
  readonly title?: string;
  readonly descriptionMd?: string;
  readonly listId: string;
  readonly statusOptionId?: string | null;
  readonly priorityOptionId?: string | null;
  readonly startDate?: string | null;
  readonly dueDate?: string | null;
  readonly assigneeIds?: readonly string[];
}

function emptyDraft(defaults: CardDraftDefaults): CardDraft {
  return {
    title: defaults.title ?? "",
    descriptionMd: defaults.descriptionMd ?? "",
    listId: defaults.listId,
    statusOptionId: defaults.statusOptionId ?? null,
    priorityOptionId: defaults.priorityOptionId ?? null,
    startDate: defaults.startDate ?? null,
    dueDate: defaults.dueDate ?? null,
    assigneeIds: defaults.assigneeIds ?? [],
    labelIds: [],
    fieldValues: {},
    checklists: [],
    links: [],
    files: [],
  };
}

const localId = () => crypto.randomUUID();
const NOOP = () => undefined;

/** The parts of the draft the person typed (not the entry point's defaults). */
export function draftParts(draft: CardDraft, initial: CardDraft) {
  return {
    name: draft.title.trim() !== "",
    description: draft.descriptionMd.trim() !== "",
    labels: draft.labelIds.length,
    assignees: draft.assigneeIds.join() !== initial.assigneeIds.join(),
    dates:
      draft.startDate !== initial.startDate ||
      draft.dueDate !== initial.dueDate,
    checklists: draft.checklists.length > 0,
    attachments: draft.links.length + draft.files.length,
    fields:
      JSON.stringify(draft.fieldValues) !==
        JSON.stringify(initial.fieldValues) ||
      draft.priorityOptionId !== initial.priorityOptionId,
  };
}

/**
 * The draft of a card held in local state, shaped as a card and with the
 * sheet's callbacks written against it, so the pickers, chips and sections of
 * the popup work on it unchanged. `toInput` is what the create sends.
 */
export function useCardDraft({
  board,
  workspaceId,
  defaults,
  laneValue,
  viewerUserId,
}: {
  board: Board;
  workspaceId: string;
  defaults: CardDraftDefaults;
  laneValue?: CreateItemWithDetailsInput["laneValue"];
  viewerUserId?: string | null;
}) {
  const fields = useMemo(() => findItemFields(board), [board]);
  const initial = useMemo(() => emptyDraft(defaults), [defaults]);
  const [draft, setDraft] = useState<CardDraft>(initial);
  const [createdAt] = useState(() => new Date().toISOString());
  const { labels: labelsApi } = useWorkManagementServices();
  const catalogue = useBoardLabels(board.id, workspaceId);
  const { directory } = usePeopleDirectory(board.id, workspaceId);
  const patch = useCallback(
    (change: Partial<CardDraft>) =>
      setDraft((current) => ({ ...current, ...change })),
    [],
  );

  const memberOf = useCallback(
    (userId: string): CardMember => {
      const onBoard = board.members.find((member) => member.userId === userId);
      const person = directory?.people.find((entry) => entry.userId === userId);
      const name = onBoard?.name ?? person?.name ?? "";
      return {
        id: userId,
        userId,
        name,
        initials: onBoard?.initials ?? personInitials(name),
        avatarUrl: onBoard?.avatarUrl,
        color: onBoard?.color ?? "",
      };
    },
    [board.members, directory],
  );

  const card = useMemo<CardDetail>(() => {
    const labels = draft.labelIds
      .map((id) => catalogue.data?.find((label) => label.id === id))
      .filter((label): label is NonNullable<typeof label> => Boolean(label));
    const checklists: Checklist[] = draft.checklists.map((list, index) => ({
      id: list.id,
      title: list.title,
      position: index,
      items: list.items.map((item, at) => ({ ...item, position: at })),
    }));
    return withBoardPriority(
      {
        id: "",
        listId: draft.listId,
        boardId: board.id,
        workspaceId,
        title: draft.title,
        descriptionMd: draft.descriptionMd || undefined,
        position: 0,
        status: draft.statusOptionId ?? "",
        statusOptionId: draft.statusOptionId,
        priorityOptionId: draft.priorityOptionId,
        statusCategory:
          fields.status?.options.find((o) => o.id === draft.statusOptionId)
            ?.category ?? null,
        startDate: draft.startDate ?? undefined,
        dueDate: draft.dueDate ?? undefined,
        dueAt: draft.dueDate,
        isArchived: false,
        isDeleted: false,
        members: draft.assigneeIds.map(memberOf),
        labels,
        checklists,
        fieldValues: {
          ...draft.fieldValues,
          ...(fields.priority && draft.priorityOptionId
            ? { [fields.priority.id]: draft.priorityOptionId }
            : {}),
        },
        _count: {
          comments: 0,
          attachments: draft.links.length + draft.files.length,
          checklistItems: checklists.reduce((n, l) => n + l.items.length, 0),
        },
        createdAt,
        boardTitle: board.title,
        watchers: [],
        isWatched: false,
        updates: [],
        files: [],
        activity: [],
      },
      board,
    );
  }, [draft, catalogue.data, board, workspaceId, fields, memberOf, createdAt]);

  const draftFiles = useMemo<CardFile[]>(
    () => [
      ...draft.files.map(({ id, file }) => ({
        id,
        cardId: "",
        name: file.name,
        size: file.size,
        contentType: file.type,
        url: "",
        source: "upload" as const,
        kind: "file" as const,
        createdBy: memberOf(""),
        createdAt,
      })),
      ...draft.links.map((link) => ({
        id: link.id,
        cardId: "",
        name: link.name,
        size: 0,
        contentType: "",
        url: link.url,
        source: "link" as const,
        kind: "link" as const,
        createdBy: memberOf(""),
        createdAt,
      })),
    ],
    [draft.files, draft.links, memberOf, createdAt],
  );

  const checklistModel = useMemo<ItemChecklistsModel>(() => {
    const edit = (
      fn: (lists: CardDraft["checklists"]) => CardDraft["checklists"],
    ) =>
      setDraft((current) => ({
        ...current,
        checklists: fn(current.checklists),
      }));
    const mapList = (
      id: string,
      fn: (
        list: CardDraft["checklists"][number],
      ) => CardDraft["checklists"][number],
    ) =>
      edit((lists) => lists.map((list) => (list.id === id ? fn(list) : list)));
    const reorder = <T>(items: readonly T[], from: number, to: number) => {
      const next = [...items];
      const [moved] = next.splice(from, 1);
      if (moved !== undefined) next.splice(to, 0, moved);
      return next;
    };
    return {
      checklists: card.checklists,
      canEdit: true,
      canConvert: false,
      addList: (title) =>
        edit((lists) => [...lists, { id: localId(), title, items: [] }]),
      renameList: (id, title) => mapList(id, (list) => ({ ...list, title })),
      deleteList: (id) => edit((lists) => lists.filter((l) => l.id !== id)),
      moveList: (id, to) =>
        edit((lists) =>
          reorder(
            lists,
            lists.findIndex((l) => l.id === id),
            to,
          ),
        ),
      addItem: (id, title) =>
        mapList(id, (list) => ({
          ...list,
          items: [...list.items, { id: localId(), title, isDone: false }],
        })),
      renameItem: (id, itemId, title) =>
        mapList(id, (list) => ({
          ...list,
          items: list.items.map((i) => (i.id === itemId ? { ...i, title } : i)),
        })),
      toggleItem: (id, itemId) =>
        mapList(id, (list) => ({
          ...list,
          items: list.items.map((i) =>
            i.id === itemId ? { ...i, isDone: !i.isDone } : i,
          ),
        })),
      deleteItem: (id, itemId) =>
        mapList(id, (list) => ({
          ...list,
          items: list.items.filter((i) => i.id !== itemId),
        })),
      moveItem: (id, itemId, to) =>
        mapList(id, (list) => ({
          ...list,
          items: reorder(
            list.items,
            list.items.findIndex((i) => i.id === itemId),
            to,
          ),
        })),
      convertItem: () => Promise.reject(new Error("draft")),
      undo: NOOP,
      retry: NOOP,
      discard: NOOP,
      dismissConflict: NOOP,
      failure: null,
      conflict: false,
      pendingDelete: null,
      busy: false,
    };
  }, [card.checklists]);

  const data = useMemo<TaskDetailData>(
    () => ({
      updates: [],
      updatesLoading: false,
      files: draftFiles,
      filesLoading: false,
      activity: [],
      activityLoading: false,
      activityFetching: false,
      boardLabels: catalogue.data,
      checklists: checklistModel,
      viewerUserId,
      people: directory
        ? { people: directory.people, teams: directory.teams, canInvite: false }
        : undefined,
    }),
    [draftFiles, catalogue.data, checklistModel, directory, viewerUserId],
  );

  const onUpdateFieldValue = useCallback(
    (payload: UpdateFieldValueInput) => {
      const id = payload.fieldDefinitionId;
      if (id === fields.status?.id)
        patch({
          statusOptionId:
            typeof payload.value === "string" ? payload.value : null,
        });
      else if (id === fields.priority?.id)
        patch({
          priorityOptionId:
            typeof payload.value === "string" ? payload.value : null,
        });
      else if (fields.person && id === fields.person.id)
        patch({
          assigneeIds: Array.isArray(payload.value)
            ? (payload.value as string[])
            : [],
        });
      else
        setDraft((current) => ({
          ...current,
          fieldValues: { ...current.fieldValues, [id]: payload.value },
        }));
    },
    [fields, patch],
  );

  const callbacks = useMemo<TaskDetailCallbacks>(() => {
    const change = (input: UpdateCardInput) => {
      const next: { -readonly [K in keyof CardDraft]?: CardDraft[K] } = {};
      if (input.title !== undefined) next.title = input.title;
      if (input.descriptionMd !== undefined)
        next.descriptionMd = input.descriptionMd;
      if (input.startDate !== undefined) next.startDate = input.startDate;
      if (input.dueDate !== undefined) next.dueDate = input.dueDate;
      patch(next);
    };
    return {
      onClose: NOOP,
      onRenameTitle: (_id, input) => change(input),
      onUpdateItem: (_id, input) => change(input),
      onToggleWatch: NOOP,
      onDuplicate: NOOP,
      onArchive: NOOP,
      onUpdateFieldValue,
      onToggleLabel: (labelId, on) =>
        setDraft((current) => ({
          ...current,
          labelIds: on
            ? [...current.labelIds.filter((id) => id !== labelId), labelId]
            : current.labelIds.filter((id) => id !== labelId),
        })),
      // The label belongs to the board, so it exists at once; only its tick is the draft's.
      onCreateLabel: (name, color) =>
        labelsApi
          .createLabel({ boardId: board.id, name, color: color ?? "" })
          .then(
            async (label) => {
              await catalogue.refetch();
              setDraft((current) => ({
                ...current,
                labelIds: [...current.labelIds, label.id],
              }));
              return true;
            },
            () => false,
          ),
      onAddFile: (file) =>
        setDraft((current) => ({
          ...current,
          files: [...current.files, { id: localId(), file }],
        })),
      onDeleteFile: (file) => {
        setDraft((current) => ({
          ...current,
          files: current.files.filter((entry) => entry.id !== file.id),
          links: current.links.filter((entry) => entry.id !== file.id),
        }));
        return Promise.resolve(true);
      },
      onRefreshActivity: NOOP,
      onCreateUpdate: NOOP,
      onUpdateUpdate: NOOP,
      onDeleteUpdate: NOOP,
      onSelectTab: NOOP,
    };
  }, [onUpdateFieldValue, patch, labelsApi, board.id, catalogue]);

  const actions = useMemo<CardPopupActions>(
    () => ({
      moveToGroup: (listId) => patch({ listId }),
      addWebLink: (url, name) => {
        setDraft((current) => ({
          ...current,
          links: [...current.links, { id: localId(), url, name }],
        }));
        return Promise.resolve(true);
      },
    }),
    [patch],
  );

  /** Clears the card for "Tạo tiếp thẻ khác": column, labels and people stay. */
  const resetKeeping = useCallback(
    () =>
      setDraft((current) => ({
        ...emptyDraft(defaults),
        listId: current.listId,
        statusOptionId: current.statusOptionId,
        labelIds: current.labelIds,
        assigneeIds: current.assigneeIds,
      })),
    [defaults],
  );

  const toInput = (idempotencyKey: string): CreateItemWithDetailsInput => ({
    listId: draft.listId,
    title: draft.title.trim(),
    descriptionMd: draft.descriptionMd.trim() || undefined,
    startDate: draft.startDate ?? undefined,
    dueDate: draft.dueDate ?? undefined,
    statusOptionId: draft.statusOptionId,
    priority:
      fields.priority && draft.priorityOptionId
        ? {
            fieldDefinitionId: fields.priority.id,
            value: draft.priorityOptionId,
          }
        : null,
    assigneeIds: draft.assigneeIds,
    labelIds: draft.labelIds,
    laneValue: laneValue ?? null,
    fieldValues: Object.entries(draft.fieldValues)
      .filter(
        ([, value]) => value !== null && value !== undefined && value !== "",
      )
      .map(([fieldDefinitionId, value]) => ({ fieldDefinitionId, value })),
    checklists: draft.checklists
      .filter((list) => list.title.trim() !== "")
      .map((list) => ({
        title: list.title,
        items: list.items.map((i) => i.title),
      })),
    links: draft.links.map(({ url, name }) => ({ url, name })),
    files: draft.files.map((entry) => entry.file),
    idempotencyKey,
  });

  return {
    draft,
    initial,
    card,
    data,
    callbacks,
    actions,
    fields,
    resetKeeping,
    toInput,
    parts: draftParts(draft, initial),
  };
}
