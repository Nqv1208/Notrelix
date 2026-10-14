import { useCallback, useMemo, useState } from "react";
import { toast } from "sonner";
import { useNavigate } from "@tanstack/react-router";
import {
  isWorkspaceAdmin,
  resolveNavigationEntries,
  NAVIGATION_RESOURCE_KIND,
} from "@notrelix/features-workspace/core";
import {
  createUseWorkspacePins,
  createUseWorkspaceRecents,
  useWorkspaceHomeCopy,
  WorkspaceHomeSurface,
  AddItemDialog,
  formatCopy,
  type ContinueEntry,
  type HomeAgenda,
  type HomeReplyActions,
  type HomeReplyView,
  type MyWorkCreateBoard,
  type MyWorkGroupView,
  type SectionState,
} from "@notrelix/features-workspace/web";
import { useCurrentUser } from "@notrelix/features-auth";
import { useFormatters } from "@notrelix/i18n";
import { useAppRuntime } from "@notrelix/runtime-web";
import { useWorkspaceContext } from "@/providers/workspace-provider";
import { sectionState } from "@/shell/workspace/section-state";
import { useBoardDepartments } from "@/shell/workspace/use-board-departments";
import { useCreateResource } from "@/shell/workspace/use-create-resource";
import { useNow } from "@/shell/workspace/use-now";
import { useWorkspaceResources } from "@/shell/workspace/use-workspace-resources";
import { useWorkspaceViewer } from "@/shell/workspace/use-workspace-viewer";
import { useShellNavigation } from "@/shell/workspace/use-shell-navigation";
import { greetingNameFor } from "@/shell/workspace/my-work-view";
import { useMyWorkCompletion } from "@/shell/workspace/use-my-work-completion";
import { toastFailure } from "@/shell/workspace/failure-toast";
import {
  toAgenda,
  toExtGroups,
  toExtItemView,
  toReplyViews,
  useDepartmentsById,
} from "@/shell/workspace/home-feed-view";
import {
  useHomeFeed,
  useMyWorkExtended,
  useWorkFeedWrites,
} from "@/shell/workspace/work-feed-bridge";

const CONTINUE_LIMIT = 4;

/**
 * Workspace Home (WX-HOME-001/002, Final-Home): composes Workspaces
 * navigation state and the Work Management My work read into the pure Home
 * surface. Approvals, mentions and the schedule have no producer yet, so the
 * surface is given none of them.
 */
export function HomePage() {
  const { workspaceId, workspace } = useWorkspaceContext();
  const copy = useWorkspaceHomeCopy();
  const formatters = useFormatters();
  const user = useCurrentUser();
  const navigate = useNavigate();
  const now = useNow();
  const navigation = useShellNavigation();
  const { api: runtimeClient } = useAppRuntime();

  const hooks = useMemo(() => {
    const deps = { api: runtimeClient.api, endpoints: runtimeClient.endpoints };
    return {
      useRecents: createUseWorkspaceRecents(deps),
      usePins: createUseWorkspacePins(deps),
    };
  }, [runtimeClient]);

  const viewer = useWorkspaceViewer(workspaceId);
  const isAdmin = isWorkspaceAdmin(viewer.role);
  const resources = useWorkspaceResources(workspaceId);
  const departments = useBoardDepartments(workspaceId);
  const recents = hooks.useRecents(workspaceId);
  const pins = hooks.usePins(workspaceId);
  const myWorkQuery = useMyWorkExtended(workspaceId);
  const feedQuery = useHomeFeed(workspaceId);
  const departmentsById = useDepartmentsById(workspaceId);
  const writes = useWorkFeedWrites(workspaceId);
  const [addOpen, setAddOpen] = useState(false);
  const [addPending, setAddPending] = useState(false);
  const [pendingReplyIds, setPendingReplyIds] = useState<ReadonlySet<string>>(
    () => new Set(),
  );
  const { completingIds, onComplete } = useMyWorkCompletion(workspaceId);
  const create = useCreateResource(workspaceId, copy);

  const continueState = useMemo<SectionState<readonly ContinueEntry[]>>(() => {
    if (recents.isError || resources.failed) return { status: "error" };
    if (!recents.data || !resources.ready) return { status: "loading" };
    const docSpaces = new Map(
      resources.docs.map((doc) => [doc.id, doc.spaceId]),
    );
    const entries = resolveNavigationEntries(
      recents.data,
      { boards: resources.boards, docs: resources.docs },
      pins.data ?? [],
    )
      .slice(0, CONTINUE_LIMIT)
      .map((entry) => ({
        ...entry,
        location:
          entry.kind === NAVIGATION_RESOURCE_KIND.doc
            ? (departmentsById.get(docSpaces.get(entry.resourceId) ?? "")
                ?.name ?? null)
            : (departments.get(entry.resourceId)?.name ?? null),
      }));
    return { status: "ready", data: entries };
  }, [
    recents.data,
    recents.isError,
    resources,
    pins.data,
    departments,
    departmentsById,
  ]);

  const boardNames = useMemo(
    () => new Map(resources.boards.map((board) => [board.id, board.name])),
    [resources.boards],
  );
  const myWorkState = useMemo<SectionState<readonly MyWorkGroupView[]>>(() => {
    const state = sectionState(myWorkQuery, myWorkQuery.data);
    if (state.status !== "ready") return state;
    const views = state.data.items.map((item) =>
      toExtItemView(item, boardNames, (boardId) => {
        const id = state.data.departments[boardId];
        return (
          (id ? departmentsById.get(id) : null) ??
          departments.get(boardId) ??
          null
        );
      }),
    );
    return { status: "ready", data: toExtGroups(views, now) };
  }, [boardNames, departments, departmentsById, myWorkQuery, now]);

  const completedToday = useMemo(() => {
    const done = myWorkQuery.data?.done ?? [];
    const today = new Date(now).toDateString();
    const times = done
      .map((item) => (item.completedAt ? new Date(item.completedAt) : null))
      .filter((at): at is Date => at !== null && at.toDateString() === today)
      .sort((a, b) => b.getTime() - a.getTime());
    const latest = times[0];
    return latest && times.length <= 9
      ? {
          count: times.length,
          time: `${String(latest.getHours()).padStart(2, "0")}:${String(latest.getMinutes()).padStart(2, "0")}`,
        }
      : null;
  }, [myWorkQuery.data, now]);

  const relativeTime = formatters.relativeTime;
  const repliesState = useMemo<
    SectionState<readonly HomeReplyView[]> | undefined
  >(() => {
    const state = sectionState(feedQuery, feedQuery.data);
    if (state.status !== "ready") return state;
    return {
      status: "ready",
      data: toReplyViews(state.data.replies, (at) => relativeTime(at, now)),
    };
  }, [feedQuery, relativeTime, now]);
  const agendaState = useMemo<SectionState<HomeAgenda> | undefined>(() => {
    const state = sectionState(feedQuery, feedQuery.data);
    if (state.status !== "ready") return state;
    return {
      status: "ready",
      data: toAgenda(copy, state.data.events, now),
    };
  }, [copy, feedQuery, now]);

  const replyById = useMemo(
    () => new Map((feedQuery.data?.replies ?? []).map((r) => [r.id, r])),
    [feedQuery.data],
  );
  const withPending = useCallback(
    async (id: string, job: () => Promise<void>, success: string) => {
      setPendingReplyIds((ids) => new Set(ids).add(id));
      try {
        await job();
        toast.success(success);
      } catch (error) {
        toastFailure(
          copy.home.toasts.failed,
          error,
          copy.home.toasts.failedHint,
        );
      } finally {
        setPendingReplyIds((ids) => {
          const next = new Set(ids);
          next.delete(id);
          return next;
        });
      }
    },
    [copy],
  );
  const replyActions = useMemo<HomeReplyActions>(
    () => ({
      pendingIds: pendingReplyIds,
      onApprove: (reply) => {
        const dto = replyById.get(reply.id);
        if (!dto?.target.boardId) return;
        void withPending(
          reply.id,
          () =>
            writes.decideApproval({
              itemId: dto.target.id,
              boardId: dto.target.boardId!,
              decision: "approve",
            }),
          formatCopy(copy.home.toasts.approved, {
            name: dto.target.key ?? reply.actorName,
          }),
        );
      },
      onRequestChanges: (reply) => {
        const dto = replyById.get(reply.id);
        if (!dto?.target.boardId) return;
        void withPending(
          reply.id,
          () =>
            writes.decideApproval({
              itemId: dto.target.id,
              boardId: dto.target.boardId!,
              decision: "request-changes",
            }),
          formatCopy(copy.home.toasts.changesRequested, {
            name: dto.target.key ?? reply.actorName,
          }),
        );
      },
      onReply: (reply, text) => {
        const mentionId = reply.id.replace(/^mention:/, "");
        void withPending(
          reply.id,
          () => writes.replyToMention({ mentionId, text }),
          formatCopy(copy.home.toasts.replied, { name: reply.actorName }),
        );
      },
    }),
    [copy, pendingReplyIds, replyById, withPending, writes],
  );

  const createBoards = useMemo<MyWorkCreateBoard[]>(
    () =>
      resources.boards.map((board) => {
        const department = departments.get(board.id);
        return {
          id: board.id,
          name: board.name,
          departmentName: department?.name ?? null,
          detail: null,
        };
      }),
    [departments, resources.boards],
  );

  const isNewWorkspace =
    resources.ready &&
    resources.boards.length === 0 &&
    resources.docs.length === 0;

  return (
    <>
      <WorkspaceHomeSurface
        copy={copy}
        navigation={navigation}
        formatters={formatters}
        now={now}
        workspaceName={workspace?.name ?? null}
        greetingName={greetingNameFor(user?.name)}
        continueState={continueState}
        myWork={myWorkState}
        myWorkHasMore={false}
        completedToday={completedToday}
        replies={repliesState}
        replyActions={replyActions}
        approvalCount={feedQuery.data?.summary.approvalsPending}
        mentionCount={feedQuery.data?.summary.mentionsUnread}
        agenda={agendaState}
        meetingCount={feedQuery.data?.events.length}
        completingIds={completingIds}
        onComplete={onComplete}
        onAddItem={() => setAddOpen(true)}
        onboarding={
          isNewWorkspace
            ? {
                canCreate: viewer.role !== null && viewer.role !== "guest",
                canInvite: isAdmin,
              }
            : null
        }
        onCreateBoard={() => create.open("board")}
        onCreateDoc={() => create.open("doc")}
        onInvite={() =>
          void navigate({
            to: "/workspaces/$workspaceId/members",
            params: { workspaceId },
            search: { tab: "invitations" },
          } as never)
        }
        onRetryContinue={() => {
          void recents.refetch();
          resources.retry();
        }}
        onRetryMyWork={() => void myWorkQuery.refetch()}
      />
      {create.dialog}
      <AddItemDialog
        copy={copy}
        open={addOpen}
        boards={createBoards}
        pending={addPending}
        error={null}
        onOpenChange={setAddOpen}
        onSubmit={(input) => {
          setAddPending(true);
          void writes
            .createItem(input)
            .then(() => {
              setAddOpen(false);
              toast.success(
                formatCopy(copy.home.toasts.itemCreated, { name: input.name }),
              );
            })
            .catch((error: unknown) =>
              toastFailure(
                copy.home.toasts.failed,
                error,
                copy.home.toasts.failedHint,
              ),
            )
            .finally(() => setAddPending(false));
        }}
      />
    </>
  );
}
