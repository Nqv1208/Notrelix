import { useCallback, useMemo, useState } from "react";
import { toast } from "sonner";
import { useNavigate, useSearch } from "@tanstack/react-router";
import {
  identityToneFor,
  initialsFor,
  NAVIGATION_RESOURCE_KIND,
} from "@notrelix/features-workspace/core";
import { useAppRuntime } from "@notrelix/runtime-web";
import {
  createUseWorkspaceRecents,
  formatCopy,
  useWorkspaceHomeCopy,
  WorkspaceMyWorkSurface,
  type MyWorkActions,
  type MyWorkItemView,
  type MyWorkWaitingView,
  type SectionState,
  type WorkStatusBucket,
} from "@notrelix/features-workspace/web";
import { useFormatters } from "@notrelix/i18n";
import { useCurrentUser } from "@notrelix/features-auth";
import {
  BoardCardCreate,
  BoardItemSheetHost,
} from "@notrelix/work-management-web";
import { useWorkspaceContext } from "@/providers/workspace-provider";
import { toastFailure } from "@/shell/workspace/failure-toast";
import {
  toExtItemView,
  useDepartmentsById,
} from "@/shell/workspace/home-feed-view";
import { useBoardDepartments } from "@/shell/workspace/use-board-departments";
import { useMyWorkCompletion } from "@/shell/workspace/use-my-work-completion";
import { useNow } from "@/shell/workspace/use-now";
import { useShellNavigation } from "@/shell/workspace/use-shell-navigation";
import { useWorkspaceResources } from "@/shell/workspace/use-workspace-resources";
import {
  useMyWorkExtended,
  useRefreshWorkFeed,
  useWorkFeedWrites,
  type MyWorkWaitingDto,
} from "@/shell/workspace/work-feed-bridge";
import type { MyWorkSearch } from "@/router/schemas/my-work-search-schema";

/**
 * My work (WX-HOME-001, Final-MyWork): every open Item assigned to the member
 * in this workspace (WX-BE-005), by due window or by Board, paged by cursor,
 * with the item open in the shared side sheet through the `item` search
 * parameter. The read carries no Status, Priority, key or relationship beyond
 * "assigned", so the page draws none of them; approvals, followed items, done
 * items, waiting-on, adding an item and a personal calendar have no producer
 * and are drawn inert or left out.
 */
export function MyWorkPage() {
  const { workspaceId } = useWorkspaceContext();
  const copy = useWorkspaceHomeCopy();
  const formatters = useFormatters();
  const navigation = useShellNavigation();
  const now = useNow();
  const navigate = useNavigate({
    from: "/workspaces/$workspaceId/my-work",
  });
  const search: MyWorkSearch = useSearch({
    from: "/workspaces/$workspaceId/my-work",
  });
  const { api: runtimeClient } = useAppRuntime();
  const useRecents = useMemo(
    () =>
      createUseWorkspaceRecents({
        api: runtimeClient.api,
        endpoints: runtimeClient.endpoints,
      }),
    [runtimeClient],
  );
  const recents = useRecents(workspaceId);
  const resources = useWorkspaceResources(workspaceId);
  const departments = useBoardDepartments(workspaceId);
  const query = useMyWorkExtended(workspaceId);
  const departmentsById = useDepartmentsById(workspaceId);
  const writes = useWorkFeedWrites(workspaceId);
  const refreshFeed = useRefreshWorkFeed(workspaceId);
  const user = useCurrentUser();
  const [form, setForm] = useState<{ boardId: string; name: string } | null>(
    null,
  );
  const [pendingIds, setPendingIds] = useState<ReadonlySet<string>>(
    () => new Set(),
  );
  const { completingIds, onComplete } = useMyWorkCompletion(workspaceId);

  const boardNames = useMemo(
    () => new Map(resources.boards.map((board) => [board.id, board.name])),
    [resources.boards],
  );
  const viewOf = useCallback(
    (
      dto: Parameters<typeof toExtItemView>[0],
      boardOf: Record<string, string | null>,
    ) =>
      toExtItemView(dto, boardNames, (boardId) => {
        const id = boardOf[boardId];
        return (
          (id ? departmentsById.get(id) : null) ??
          departments.get(boardId) ??
          null
        );
      }),
    [boardNames, departments, departmentsById],
  );
  const state = useMemo<SectionState<readonly MyWorkItemView[]>>(() => {
    if (query.isError) return { status: "error" };
    if (!query.data) return { status: "loading" };
    const boardOf = query.data.departments;
    return {
      status: "ready",
      data: query.data.items.map((item) => viewOf(item, boardOf)),
    };
  }, [query.data, query.isError, viewOf]);
  const doneState = useMemo<SectionState<readonly MyWorkItemView[]>>(() => {
    if (query.isError) return { status: "error" };
    if (!query.data) return { status: "loading" };
    const boardOf = query.data.departments;
    return {
      status: "ready",
      data: query.data.done.map((item) => viewOf(item, boardOf)),
    };
  }, [query.data, query.isError, viewOf]);

  const dueNoteFor = useCallback(
    (entry: MyWorkWaitingDto) => {
      if (!entry.dueAt) return "";
      const due = new Date(entry.dueAt);
      const today = new Date(now);
      const same = due.toDateString() === today.toDateString();
      const date = same
        ? copy.work.due.today.toLowerCase()
        : `${String(due.getDate()).padStart(2, "0")}/${String(due.getMonth() + 1).padStart(2, "0")}`;
      return formatCopy(copy.list.dueOn, { date });
    },
    [copy, now],
  );
  const waiting = useMemo<readonly MyWorkWaitingView[]>(
    () =>
      (query.data?.waiting ?? []).map((entry) => ({
        id: entry.itemId,
        title: entry.title,
        key: entry.key ?? "",
        destination: {
          section: "board" as const,
          boardId: entry.boardId,
          item: entry.itemId,
        },
        ownerName: entry.owner.name,
        ownerTone: identityToneFor(entry.owner.id),
        note: entry.note,
        dueNote: dueNoteFor(entry),
      })),
    [dueNoteFor, query.data],
  );

  const statusOptions = useMemo(
    () =>
      (["todo", "doing", "review", "done"] as const).map((bucket) => ({
        bucket,
        label: copy.work.status[bucket],
      })),
    [copy],
  );

  const run = useCallback(
    async (
      ids: readonly string[],
      job: () => Promise<unknown>,
      success: string,
    ) => {
      setPendingIds((set) => new Set([...set, ...ids]));
      try {
        await job();
        toast.success(success);
      } catch (error) {
        toastFailure(copy.toasts.failed, error, copy.toasts.failedHint);
      } finally {
        setPendingIds((set) => {
          const next = new Set(set);
          for (const id of ids) next.delete(id);
          return next;
        });
      }
    },
    [copy],
  );
  const bulk = useCallback(
    (
      list: readonly MyWorkItemView[],
      action: Parameters<typeof writes.bulk>[0]["action"],
      success: string,
    ) =>
      run(
        list.map((item) => item.id),
        async () => {
          const result = await writes.bulk({
            itemIds: list.map((item) => item.id),
            boardIds: list.map((item) => item.boardId),
            action,
          });
          const failed = result.results.filter((entry) => !entry.ok).length;
          if (failed > 0)
            toast.error(
              formatCopy(copy.toasts.partial, {
                failed,
                total: list.length,
              }),
            );
        },
        success,
      ),
    [copy, run, writes],
  );
  const approvalRequestOf = useCallback(
    (itemId: string) =>
      query.data?.items.find((entry) => entry.id === itemId)?.approval
        ?.requestId ?? null,
    [query.data],
  );
  const actions = useMemo<MyWorkActions>(
    () => ({
      pendingIds,
      onApprove: (item) => {
        const requestId = approvalRequestOf(item.id);
        if (!requestId) return;
        void run(
          [item.id],
          () =>
            writes.decideApproval({
              requestId,
              boardId: item.boardId,
              decision: "approve",
            }),
          formatCopy(copy.toasts.approved, { name: item.key ?? item.name }),
        );
      },
      onRequestChanges: (item) => {
        const requestId = approvalRequestOf(item.id);
        if (!requestId) return;
        void run(
          [item.id],
          () =>
            writes.decideApproval({
              requestId,
              boardId: item.boardId,
              decision: "reject",
            }),
          formatCopy(copy.toasts.changesRequested, {
            name: item.key ?? item.name,
          }),
        );
      },
      onReopen: (item) =>
        void run(
          [item.id],
          () => writes.reopen({ itemId: item.id, boardId: item.boardId }),
          formatCopy(copy.toasts.reopened, { name: item.key ?? item.name }),
        ),
      onRemind: (entry) =>
        void run(
          [entry.id],
          () => writes.remind(entry.id),
          formatCopy(copy.toasts.reminded, { name: entry.ownerName }),
        ),
      onSetDue: (list, dueAt) =>
        void bulk(
          list,
          { type: "set-due", dueAt },
          formatCopy(dueAt ? copy.toasts.dueChanged : copy.toasts.dueCleared, {
            count: list.length,
          }),
        ),
      onSetStatus: (list, bucket) =>
        void bulk(
          list,
          { type: "set-status", bucket },
          formatCopy(copy.toasts.statusChangedMany, {
            count: list.length,
            status: copy.work.status[bucket],
          }),
        ),
      onUnfollow: (list) =>
        void bulk(
          list,
          { type: "unfollow" },
          formatCopy(copy.toasts.unfollowed, { count: list.length }),
        ),
    }),
    [bulk, copy, pendingIds, run, writes],
  );
  const onChangeStatus = useCallback(
    (item: MyWorkItemView, bucket: WorkStatusBucket) =>
      void run(
        [item.id],
        () =>
          writes.setStatus({
            itemId: item.id,
            boardId: item.boardId,
            bucket,
          }),
        formatCopy(copy.toasts.statusChanged, {
          name: item.key ?? item.name,
          status: copy.work.status[bucket],
        }),
      ),
    [copy, run, writes],
  );
  const onCreate = useCallback(
    (input: { boardId: string; name: string }) =>
      void run(
        [],
        () => writes.createItem(input),
        formatCopy(copy.toasts.itemCreated, { name: input.name }),
      ),
    [copy, run, writes],
  );

  const items = state.status === "ready" ? state.data : [];
  const doneItems = doneState.status === "ready" ? doneState.data : [];
  const openItem = search.item
    ? ([...items, ...doneItems].find((item) => item.id === search.item) ?? null)
    : null;

  const setItem = useCallback(
    (item: string | undefined, options?: { replace?: boolean }) => {
      void navigate({
        search: (previous: MyWorkSearch) => ({ ...previous, item }),
        replace: options?.replace ?? false,
      });
    },
    [navigate],
  );

  const createBoards = useMemo(() => {
    // The Board used most recently first, then the ones in a department.
    const recentIds = (recents.data ?? [])
      .filter((entry) => entry.kind === NAVIGATION_RESOURCE_KIND.board)
      .map((entry) => entry.resourceId);
    const rank = (id: string) => {
      const recent = recentIds.indexOf(id);
      return recent >= 0 ? recent : departments.has(id) ? 1000 : 2000;
    };
    return [...resources.boards]
      .sort((a, b) => rank(a.id) - rank(b.id))
      .map((board, index) => {
        const department = departments.get(board.id);
        const detail = [
          department
            ? formatCopy(copy.list.departmentOf, { name: department.name })
            : null,
          index === 0 && recentIds.includes(board.id)
            ? copy.list.addRecent
            : null,
        ]
          .filter(Boolean)
          .join(" · ");
        return {
          id: board.id,
          name: board.name,
          departmentName: department?.name ?? null,
          detail: detail || null,
        };
      });
  }, [copy, departments, recents.data, resources.boards]);

  const sheet = openItem ? (
    <BoardItemSheetHost
      workspaceId={workspaceId}
      boardId={openItem.boardId}
      itemId={openItem.id}
      viewerUserId={user?.id ?? null}
      onClose={() => setItem(undefined, { replace: true })}
      onRestoreFocus={(itemId) =>
        document
          .querySelector<HTMLElement>(`[data-item-id="${itemId}"] a`)
          ?.focus()
      }
    />
  ) : undefined;

  return (
    <>
      <WorkspaceMyWorkSurface
        copy={copy}
        navigation={navigation}
        formatters={formatters}
        now={now}
        state={state}
        updatedAt={query.dataUpdatedAt || null}
        hasMore={false}
        loadingMore={false}
        onLoadMore={() => undefined}
        done={doneState}
        tabsAvailable={{ approve: true, follow: true }}
        waiting={waiting}
        statusOptions={statusOptions}
        onChangeStatus={onChangeStatus}
        actions={actions}
        onRetry={() => void query.refetch()}
        completingIds={completingIds}
        onComplete={(item) => onComplete(item)}
        createBoards={createBoards}
        onCreate={onCreate}
        onOpenCreateForm={setForm}
        openItemId={openItem?.id ?? null}
        onOpenItem={(item) => setItem(item.id)}
        sheet={sheet}
        firstDepartment={[...departments.values()][0] ?? null}
      />
      <BoardCardCreate
        open={form !== null}
        onOpenChange={(next) => {
          if (!next) setForm(null);
        }}
        workspaceId={workspaceId}
        boards={createBoards.map((board) => ({
          id: board.id,
          label: board.departmentName
            ? `${board.name} · ${board.departmentName}`
            : board.name,
        }))}
        initialBoardId={form?.boardId}
        initialTitle={form?.name}
        assigneeId={user?.id ?? null}
        onCreated={({ boardId }) => refreshFeed([boardId])}
      />
    </>
  );
}
