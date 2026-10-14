import { useMemo } from "react";
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
  type ContinueEntry,
  type MyWorkGroupView,
  type SectionState,
} from "@notrelix/features-workspace/web";
import { useCurrentUser } from "@notrelix/features-auth";
import { useFormatters, useLocale } from "@notrelix/i18n";
import { useAppRuntime } from "@notrelix/runtime-web";
import { useWorkspaceContext } from "@/providers/workspace-provider";
import { sectionState } from "@/shell/workspace/section-state";
import { useBoardDepartments } from "@/shell/workspace/use-board-departments";
import { useCreateResource } from "@/shell/workspace/use-create-resource";
import { useNow } from "@/shell/workspace/use-now";
import { useWorkspaceResources } from "@/shell/workspace/use-workspace-resources";
import { useWorkspaceViewer } from "@/shell/workspace/use-workspace-viewer";
import { useShellNavigation } from "@/shell/workspace/use-shell-navigation";
import {
  greetingNameFor,
  toMyWorkGroups,
} from "@/shell/workspace/my-work-view";
import { useMyWorkCompletion } from "@/shell/workspace/use-my-work-completion";
import {
  flattenMyWork,
  useMyWorkItems,
} from "@/shell/workspace/work-management-bridge";

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
  const { locale } = useLocale();
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
  const myWorkQuery = useMyWorkItems(workspaceId);
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
      data: toMyWorkGroups(allMyWork, boardNames, now, departments),
    };
  }, [allMyWork, boardNames, departments, myWorkQuery, now]);

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
        greetingName={greetingNameFor(user?.name, locale)}
        continueState={continueState}
        myWork={myWorkState}
        myWorkHasMore={Boolean(myWorkQuery.hasNextPage)}
        completingIds={completingIds}
        onComplete={onComplete}
        onAddItem={null}
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
    </>
  );
}
