/**
 * Host composition of the document page: the facts Documents does not own.
 * The department comes from Workspaces, people from the workspace member list,
 * the tasks a page refers to from Work Management (read per item through the
 * item read the board uses), and the viewer's role from Governance.
 *
 * TRANSITION: the share dialog's page permissions, a page's mentions of other
 * pages and presence have no producer yet; the host reports them as absent so
 * the page hides those parts (gap register).
 */
import { useCallback, useMemo } from "react";
import { useQueries } from "@tanstack/react-query";
import { useNavigate } from "@tanstack/react-router";
import {
  CREATABLE_BLOCK_TYPES,
  createUsePageHistory,
  createUsePagePermissions,
  createUseSharePage,
  type Page,
} from "@notrelix/docs-state";
import type {
  DocPageHost,
  DocPageHostInput,
  DocPerson,
  DocShareView,
  DocSpaceView,
  DocTaskRef,
  DocTaskResolution,
  UseDocPageHost,
} from "@notrelix/docs-web";
import { useCurrentUser } from "@notrelix/features-auth";
import {
  buildSpaceIndex,
  initialsFor,
  markToneFor,
  spaceAncestors,
} from "@notrelix/features-workspace/core";
import { useFeatureRuntimeDependencies } from "@notrelix/runtime-web";
import {
  useFullBoard,
  useWorkManagementServices,
  useWorkspaceBoards,
  wmQueryKeys,
} from "@notrelix/work-management-state";
import type { CardDetail } from "@notrelix/work-management-core";
import { useWorkspaceContext } from "@/providers/workspace-provider";
import { destinationHref } from "../workspace/workspace-destinations";
import { useSpaceHooks } from "../workspace/use-workspace-spaces";
import { useWorkspaceViewer } from "../workspace/use-workspace-viewer";

function startOfDay(date: Date): number {
  return new Date(
    date.getFullYear(),
    date.getMonth(),
    date.getDate(),
  ).getTime();
}

function dayMonth(date: Date): string {
  return `${String(date.getDate()).padStart(2, "0")}/${String(date.getMonth() + 1).padStart(2, "0")}`;
}

function statusOf(card: CardDetail): DocTaskRef["status"] {
  switch (card.statusCategory) {
    case "done":
      return "done";
    case "review":
      return "review";
    case "in_progress":
      return "doing";
    default:
      return "todo";
  }
}

function dueOf(card: CardDetail, now: Date): DocTaskRef["due"] {
  if (card.statusCategory === "done") {
    const at = card.completedAt ?? card.dueAt ?? card.dueDate;
    return at ? { kind: "done", label: dayMonth(new Date(at)) } : null;
  }
  const raw = card.dueAt ?? card.dueDate;
  if (!raw) return null;
  const due = new Date(raw);
  const diff = Math.round((startOfDay(now) - startOfDay(due)) / 86_400_000);
  if (diff > 0) return { kind: "overdue", days: diff };
  if (diff === 0) return { kind: "today" };
  return { kind: "date", label: dayMonth(due) };
}

function toTaskRef(card: CardDetail, now: Date, href: string): DocTaskRef {
  const member = card.members[0];
  return {
    id: card.id,
    key: card.itemKey ?? card.id,
    title: card.title,
    status: statusOf(card),
    assignee: member
      ? {
          id: member.userId,
          name: member.name,
          initials: member.initials || initialsFor(member.name),
          tone: markToneFor(member.userId),
        }
      : null,
    due: dueOf(card, now),
    href,
  };
}

/** The web host's `useHost` for the document page. */
export const useDocPageHost: UseDocPageHost = (
  input: DocPageHostInput,
): DocPageHost => {
  const {
    workspaceId,
    pageId,
    page,
    taskIds,
    boardIds,
    mentionQuery,
    convertBoardId,
  } = input;
  const navigate = useNavigate();
  const user = useCurrentUser();
  const featureDeps = useFeatureRuntimeDependencies();
  const services = useWorkManagementServices();
  const spaceHooks = useSpaceHooks();
  const usePageHistory = useMemo(
    () => createUsePageHistory(featureDeps.api, featureDeps.endpoints),
    [featureDeps],
  );

  const usePagePermissions = useMemo(
    () => createUsePagePermissions(featureDeps.api, featureDeps.endpoints),
    [featureDeps],
  );
  const useSharePage = useMemo(
    () => createUseSharePage(featureDeps.api, featureDeps.endpoints),
    [featureDeps],
  );

  const viewer = useWorkspaceViewer(workspaceId);
  const members = viewer.members;
  const spaceTree = spaceHooks.useSpaceTree(workspaceId);
  const boards = useWorkspaceBoards(workspaceId);
  const history = usePageHistory(workspaceId, pageId);
  const permissions = usePagePermissions(workspaceId, pageId);
  const sharePage = useSharePage(workspaceId, pageId);

  const now = useMemo(() => new Date(), []);
  const role = viewer.role;

  const personFor = useCallback(
    (userId: string): DocPerson => {
      const member = members.data?.find((entry) => entry.userId === userId);
      // A Guest does not read the member list, and a person outside it (another
      // department's guest or a colleague of the page) is named by the page's
      // own people read.
      const name =
        member?.name ??
        (page as Page | null)?.people?.find((entry) => entry.id === userId)
          ?.name ??
        "—";
      return {
        id: userId,
        name,
        initials: name === "—" ? "?" : initialsFor(name),
        tone: markToneFor(userId),
      };
    },
    [members.data, page],
  );

  // Department of the page.
  const space = useMemo<DocSpaceView | null>(() => {
    const spaceId = (page as Page | null)?.spaceId ?? null;
    if (!spaceId || !spaceTree.data) return null;
    const index = buildSpaceIndex(spaceTree.data);
    const node = index.byId.get(spaceId);
    if (!node) return null;
    const root = spaceAncestors(index, spaceId)[0] ?? node;
    return {
      id: node.id,
      name: node.name,
      mark: initialsFor(node.name),
      tone: markToneFor(root.id),
      visibility: node.visibility === "Private" ? "private" : "workspace",
      department: spaceAncestors(index, spaceId).length === 0,
    };
  }, [page, spaceTree.data]);

  const spaceMembers = spaceHooks.useSpaceMembers(
    workspaceId,
    space?.id ?? null,
  );

  // Referenced tasks: one item read each tells whether the viewer can read it
  // and which board it is on; the board read (shared with the board screen)
  // carries its status, assignee and dates.
  const taskQueries = useQueries({
    queries: taskIds.map((taskId) => ({
      queryKey: wmQueryKeys.cardDetail(workspaceId, taskId),
      queryFn: () => services.cards.getCard(taskId),
      staleTime: 30_000,
      retry: false,
    })),
  });
  // Dependency lists must keep one length, so the memos key on a signature.
  const boardKey = taskQueries
    .map((query) => query.data?.boardId ?? "")
    .join("|");
  const cardBoardIds = useMemo(
    () => [...new Set(boardKey.split("|").filter(Boolean))],
    [boardKey],
  );
  const boardQueries = useQueries({
    queries: cardBoardIds.map((id) => ({
      queryKey: wmQueryKeys.fullBoard(workspaceId, id),
      queryFn: () => services.boards.getFullBoard(id, { workspaceId }),
      staleTime: 10_000,
      retry: false,
    })),
  });
  const tasksSignature = [...taskQueries, ...boardQueries]
    .map((query) => `${query.status}:${query.dataUpdatedAt}`)
    .join("|");
  const tasksReady =
    taskQueries.every((query) => !query.isPending) &&
    boardQueries.every((query) => !query.isPending);
  const tasks = useMemo(() => {
    const boardCards = new Map<string, CardDetail>();
    for (const query of boardQueries) {
      for (const group of query.data?.groups ?? []) {
        for (const card of group.cards ?? []) {
          boardCards.set(card.id, card as unknown as CardDetail);
        }
      }
    }
    const map = new Map<string, DocTaskResolution>();
    taskIds.forEach((taskId, index) => {
      const query = taskQueries[index];
      if (!query) return;
      if (query.data) {
        const card = boardCards.get(taskId) ?? query.data;
        map.set(
          taskId,
          toTaskRef(
            card,
            now,
            destinationHref(workspaceId, {
              section: "board",
              boardId: query.data.boardId,
            }),
          ),
        );
      } else if (query.isError) {
        map.set(taskId, { id: taskId, restricted: true });
      }
    });
    return map;
    // The query arrays are new each render; their signature drives the memo.
  }, [now, taskIds, workspaceId, tasksSignature]);

  // The board of the page's task table (first referenced board).
  const boardId = boardIds[0] ?? null;
  const board = useMemo(() => {
    if (!boardId) return null;
    const found = boards.data?.find((candidate) => candidate.id === boardId);
    return {
      name: found?.title ?? "",
      href: destinationHref(workspaceId, { section: "board", boardId }),
    };
  }, [boardId, boards.data, workspaceId]);

  // The convert-to-task dialog: boards the viewer may add to, and the chosen
  // board's groups (read like the board screen reads them).
  const convertBoards = useMemo(
    () =>
      (boards.data ?? [])
        .filter((candidate) => candidate.canAddItems && !candidate.isArchived)
        .map((candidate) => ({ id: candidate.id, title: candidate.title })),
    [boards.data],
  );
  const convertBoard = useFullBoard(convertBoardId ?? undefined, workspaceId);
  const convertGroups = useMemo(
    () =>
      convertBoardId === null || convertBoard.isLoading
        ? null
        : convertBoard.groups.map((group) => ({
            id: group.id,
            name: group.title,
          })),
    [convertBoard.groups, convertBoard.isLoading, convertBoardId],
  );
  const spaceNameFor = useCallback(
    (spaceId: string): string | null =>
      spaceTree.data
        ? (buildSpaceIndex(spaceTree.data).byId.get(spaceId)?.name ?? null)
        : null,
    [spaceTree.data],
  );

  // Mention suggestions come from the board the page already reads.
  const fullBoard = useFullBoard(boardId ?? undefined, workspaceId);
  const suggestTasks = useCallback(
    (query: string): readonly DocTaskRef[] => {
      if (mentionQuery === null) return [];
      const needle = query.trim().toLowerCase();
      const cards = fullBoard.groups.flatMap((group) => group.cards ?? []);
      return cards
        .filter(
          (card) =>
            needle === "" ||
            (card.itemKey ?? "").toLowerCase().includes(needle) ||
            card.title.toLowerCase().includes(needle),
        )
        .slice(0, 4)
        .map((card) =>
          toTaskRef(
            card as unknown as CardDetail,
            now,
            destinationHref(workspaceId, {
              section: "board",
              boardId: boardId ?? "",
            }),
          ),
        );
    },
    [boardId, fullBoard.groups, mentionQuery, now, workspaceId],
  );

  // Whom the viewer may give access to: the workspace's members who are not on the
  // page yet, offered to an owner of the workspace (the producer decides each grant).
  const candidates = useMemo<readonly DocPerson[] | undefined>(() => {
    if (role !== "owner") return undefined;
    const granted = new Set(
      (permissions.data?.members ?? []).map(
        (entry) => entry.subjectId ?? entry.userId,
      ),
    );
    return (members.data ?? [])
      .filter(
        (member) => member.userId !== user?.id && !granted.has(member.userId),
      )
      .map((member) => personFor(member.userId));
  }, [members.data, permissions.data, personFor, role, user?.id]);

  // A page at the workspace root has no department: it is shared at the workspace level.
  const { workspace } = useWorkspaceContext();
  const workspaceSpace = useMemo<DocSpaceView>(
    () => ({
      id: workspaceId,
      name: workspace?.name ?? "",
      mark: initialsFor(workspace?.name ?? ""),
      tone: markToneFor(workspaceId),
      visibility: "workspace",
      department: false,
    }),
    [workspace?.name, workspaceId],
  );

  const share = useMemo<DocShareView | null>(() => {
    const shareSpace = space ?? workspaceSpace;
    // Permissions given on the page itself; a page without any lists its creator.
    const given = permissions.data?.members ?? [];
    if (given.length > 0) {
      return {
        space: shareSpace,
        spaceMemberCount: spaceMembers.data ? spaceMembers.data.length : null,
        candidates,
        members: given.map((entry) => {
          const personId = entry.subjectId ?? entry.userId ?? entry.id;
          const person =
            entry.kind !== "person"
              ? undefined
              : entry.name
                ? {
                    id: personId,
                    name: entry.name,
                    initials: initialsFor(entry.name),
                    tone: markToneFor(personId),
                  }
                : personFor(personId);
          return {
            id: entry.id,
            kind: entry.kind,
            person,
            name: person?.name ?? entry.name,
            detail: entry.detail,
            outside: entry.outside,
            creator: entry.creator,
            fixed: entry.creator,
            you: entry.userId !== null && entry.userId === user?.id,
            level: entry.level,
          };
        }),
      };
    }
    const created = (history.data ?? []).find(
      (entry) => entry.action === "created",
    );
    return {
      space: shareSpace,
      spaceMemberCount: spaceMembers.data ? spaceMembers.data.length : null,
      candidates,
      members: created
        ? [
            {
              id: created.actorId,
              kind: "person" as const,
              person: personFor(created.actorId),
              name: personFor(created.actorId).name,
              detail: "",
              level: "manage" as const,
              creator: true,
              fixed: true,
              you: created.actorId === user?.id,
            },
          ]
        : [],
    };
  }, [
    candidates,
    history.data,
    permissions.data,
    personFor,
    space,
    workspaceSpace,
    spaceMembers.data,
    user?.id,
  ]);

  return {
    space,
    personFor,
    tasks,
    tasksReady,
    board,
    suggestTasks,
    share,
    viewerReason:
      space?.visibility === "private" && (role === "owner" || role === "admin")
        ? "workspace-admin"
        : null,
    viewerId: user?.id,
    pageHref: (targetId) =>
      `/workspaces/${workspaceId}/docs/${encodeURIComponent(targetId)}`,
    canEdit: role !== "guest",
    blockTypes: CREATABLE_BLOCK_TYPES,
    canConvertTodo: convertBoards.length > 0,
    convertBoards,
    convertGroups,
    spaceNameFor,
    leaveDoc: () =>
      void navigate({
        to: "/workspaces/$workspaceId/home",
        params: { workspaceId },
      }),
    onShareInvite: candidates
      ? (userId) => sharePage.mutateAsync({ userId, level: "edit" })
      : undefined,
    now,
    openPage: (targetId) =>
      void navigate({
        to: "/workspaces/$workspaceId/docs/$docId",
        params: { workspaceId, docId: targetId },
      }),
  };
};
