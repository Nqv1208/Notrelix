import { useCallback, useMemo, useState } from "react";
import { useNavigate } from "@tanstack/react-router";
import { toast } from "sonner";
import {
  groupMyWorkItems,
  isWorkspaceAdmin,
  limitMyWorkGroups,
  pendingInvitations,
  resolveNavigationEntries,
  NAVIGATION_RESOURCE_KIND,
  type MyWorkItem,
} from "@notrelix/features-workspace/core";
import {
  createUseWorkspaceActivity,
  createUseWorkspaceInvitations,
  createUseWorkspacePins,
  createUseWorkspaceRecents,
  formatCopy,
  useWorkspaceHomeCopy,
  WorkspaceHomeSurface,
  type ContinueEntry,
  type MyWorkGroupView,
  type SectionState,
} from "@notrelix/features-workspace/web";
import { useFormatters } from "@notrelix/i18n";
import { useAppRuntime } from "@notrelix/runtime-web";
import { useWorkspaceContext } from "@/providers/workspace-provider";
import { toastFailure } from "@/shell/workspace/failure-toast";
import { sectionState } from "@/shell/workspace/section-state";
import { useCreateResource } from "@/shell/workspace/use-create-resource";
import { useNow } from "@/shell/workspace/use-now";
import { useWorkspaceResources } from "@/shell/workspace/use-workspace-resources";
import { useWorkspaceViewer } from "@/shell/workspace/use-workspace-viewer";
import { useShellNavigation } from "@/shell/workspace/use-shell-navigation";
import {
  flattenMyWork,
  useAttemptKey,
  useCompleteItem,
  useMyWorkItems,
} from "@/shell/workspace/work-management-bridge";

const CONTINUE_LIMIT = 8;
const MY_WORK_PREVIEW = 6;

/** Assigned Items with Board names from the authorized Board list. */
export function toMyWorkGroups(
  items: readonly MyWorkItem[],
  boardNames: ReadonlyMap<string, string>,
  now: number,
): MyWorkGroupView[] {
  return groupMyWorkItems(items, now).map((group) => ({
    range: group.range,
    items: group.items.map((item) => ({
      ...item,
      boardName: boardNames.get(item.boardId) ?? null,
    })),
  }));
}

/** Completes My work Items with one idempotency key per attempt. */
export function useMyWorkCompletion(workspaceId: string) {
  const copy = useWorkspaceHomeCopy();
  const complete = useCompleteItem(workspaceId);
  const nextAttemptKey = useAttemptKey();
  const [completingIds, setCompletingIds] = useState<ReadonlySet<string>>(
    () => new Set(),
  );

  const onComplete = useCallback(
    (item: MyWorkItem) => {
      setCompletingIds((ids) => new Set(ids).add(item.id));
      complete.mutate(
        { item, attemptKey: nextAttemptKey() },
        {
          onSuccess: () =>
            toast.success(
              formatCopy(copy.toasts.completed, { name: item.name }),
            ),
          onError: (error) =>
            toastFailure(
              formatCopy(copy.toasts.completeFailed, { name: item.name }),
              error,
              copy.toasts.completeFailedHint,
            ),
          onSettled: () =>
            setCompletingIds((ids) => {
              const next = new Set(ids);
              next.delete(item.id);
              return next;
            }),
        },
      );
    },
    [complete, copy, nextAttemptKey],
  );

  return { completingIds, onComplete };
}

/**
 * Workspace Home (WX-HOME-001/002): composes Workspaces navigation state,
 * Work Management and Documents lists into the pure Home surface.
 */
export function HomePage() {
  const { workspaceId, workspace } = useWorkspaceContext();
  const copy = useWorkspaceHomeCopy();
  const formatters = useFormatters();
  const navigate = useNavigate();
  const now = useNow();
  const navigation = useShellNavigation();
  const { api: runtimeClient } = useAppRuntime();

  const hooks = useMemo(() => {
    const deps = { api: runtimeClient.api, endpoints: runtimeClient.endpoints };
    return {
      useRecents: createUseWorkspaceRecents(deps),
      usePins: createUseWorkspacePins(deps),
      useActivity: createUseWorkspaceActivity(deps),
      useInvitations: createUseWorkspaceInvitations(deps),
    };
  }, [runtimeClient]);

  const viewer = useWorkspaceViewer(workspaceId);
  const isAdmin = isWorkspaceAdmin(viewer.role);
  const resources = useWorkspaceResources(workspaceId);
  const recents = hooks.useRecents(workspaceId);
  const pins = hooks.usePins(workspaceId);
  const myWorkQuery = useMyWorkItems(workspaceId);
  const activity = hooks.useActivity(workspaceId);
  const invitations = hooks.useInvitations(workspaceId, { enabled: isAdmin });
  const { completingIds, onComplete } = useMyWorkCompletion(workspaceId);
  const create = useCreateResource(workspaceId, copy);

  const continueState = useMemo<SectionState<readonly ContinueEntry[]>>(() => {
    if (recents.isError || resources.failed) return { status: "error" };
    if (!recents.data || !resources.ready) return { status: "loading" };
    const parents = new Map(
      resources.docs.map((doc) => [doc.id, doc.parentName]),
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
            ? (parents.get(entry.resourceId) ?? null)
            : null,
      }));
    return { status: "ready", data: entries };
  }, [recents.data, recents.isError, resources, pins.data]);

  const boardNames = useMemo(
    () => new Map(resources.boards.map((board) => [board.id, board.name])),
    [resources.boards],
  );
  const allMyWork = useMemo(
    () => flattenMyWork(myWorkQuery.data),
    [myWorkQuery.data],
  );
  const myWorkState = useMemo<SectionState<readonly MyWorkGroupView[]>>(() => {
    const state = sectionState(myWorkQuery, myWorkQuery.data);
    if (state.status !== "ready") return state;
    return {
      status: "ready",
      data: limitMyWorkGroups(
        toMyWorkGroups(allMyWork, boardNames, now),
        MY_WORK_PREVIEW,
      ) as MyWorkGroupView[],
    };
  }, [allMyWork, boardNames, myWorkQuery, now]);

  const pendingState = useMemo(() => {
    if (!isAdmin) return null;
    const state = sectionState(invitations, invitations.data);
    return state.status === "ready"
      ? { status: "ready" as const, data: pendingInvitations(state.data, now) }
      : state;
  }, [invitations, isAdmin, now]);

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
        continueState={continueState}
        myWork={myWorkState}
        myWorkHasMore={
          allMyWork.length > MY_WORK_PREVIEW || Boolean(myWorkQuery.hasNextPage)
        }
        completingIds={completingIds}
        onComplete={onComplete}
        activity={sectionState(activity, activity.data?.entries)}
        invitations={pendingState}
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
        onRetryActivity={() => void activity.refetch()}
        onRetryInvitations={() => void invitations.refetch()}
      />
      {create.dialog}
    </>
  );
}
