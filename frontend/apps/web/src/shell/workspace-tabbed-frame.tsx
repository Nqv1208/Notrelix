import { createContext, useContext, useMemo, type ReactNode } from "react";
import { useLocation, useNavigate } from "@tanstack/react-router";
import { AlertCircle } from "lucide-react";
import { Skeleton } from "@notrelix/ui-web";
import { useWorkspaceContext } from "@/providers/workspace-provider";
import type { WorkspaceView } from "@notrelix/features-workspace/core";
import {
  WorkspaceCompactHeader,
  WorkspaceViewTabs,
  createUseReorderWorkspaceViews,
} from "@notrelix/features-workspace/web";
import { useAppRuntime } from "@notrelix/runtime-web";
import { useFeatureRuntimeDependencies } from "@notrelix/runtime-web";
import { createUsePageList } from "@notrelix/docs-state";
import { useWorkspaceBoards } from "@notrelix/work-management-state";
import { resolveActiveWorkspaceView } from "./workspace-view-routing";

type WorkspaceTabbedRouteContextValue = {
  workspaceId: string;
  activeView: WorkspaceView;
  views: WorkspaceView[];
  kind: string;
};

const WorkspaceTabbedRouteContext =
  createContext<WorkspaceTabbedRouteContextValue | null>(null);

export function useWorkspaceTabbedRouteContext(): WorkspaceTabbedRouteContextValue {
  const context = useContext(WorkspaceTabbedRouteContext);
  if (!context) {
    throw new Error(
      "useWorkspaceTabbedRouteContext must be used within WorkspaceTabbedFrame",
    );
  }
  return context;
}

export function WorkspaceTabbedFrame({ children }: { children: ReactNode }) {
  const location = useLocation();
  const navigate = useNavigate();
  const { api: runtimeClient } = useAppRuntime();
  const { api, endpoints } = useFeatureRuntimeDependencies();
  const { workspaceId, workspace, views, members, isLoading, isError } =
    useWorkspaceContext();

  const usePageList = useMemo(
    () => createUsePageList(api, endpoints),
    [api, endpoints],
  );
  const { data: boards = [] } = useWorkspaceBoards(workspaceId);
  const { data: pages = [] } = usePageList(workspaceId);

  const useReorderWorkspaceViews = useMemo(
    () => createUseReorderWorkspaceViews({ api: runtimeClient.api }),
    [runtimeClient],
  );

  const activeView = useMemo(
    () => resolveActiveWorkspaceView(views, location.pathname),
    [views, location.pathname],
  );

  const navigateToView = (view: WorkspaceView) => {
    switch (view.type) {
      case "kanban":
      case "table":
      case "calendar":
      case "timeline": {
        const boardId = view.target.boardId;
        if (!boardId) return;
        void navigate({
          to: "/workspaces/$workspaceId/boards/$boardId",
          params: { workspaceId, boardId },
        });
        return;
      }
      case "doc": {
        const docId = view.target.pageId;
        if (!docId) return;
        void navigate({
          to: "/workspaces/$workspaceId/docs/$docId",
          params: { workspaceId, docId },
        });
        return;
      }
      case "dashboard":
      default:
        void navigate({
          to: "/workspaces/$workspaceId/dashboard",
          params: { workspaceId },
        });
    }
  };

  const contextValue = useMemo(() => {
    if (!activeView) return null;
    return {
      workspaceId,
      activeView,
      views,
      kind: activeView.type,
    };
  }, [workspaceId, activeView, views]);

  if (isLoading) {
    return (
      <div className="flex h-full min-h-0 flex-col overflow-hidden bg-background">
        <div className="h-12 border-b px-4 flex items-center gap-3">
          <Skeleton className="h-6 w-32" />
          <Skeleton className="h-6 w-24" />
        </div>
        <div className="h-10 border-b px-4 flex items-center gap-2">
          <Skeleton className="h-7 w-20" />
          <Skeleton className="h-7 w-20" />
          <Skeleton className="h-7 w-20" />
        </div>
        <div className="flex-1 p-6">
          <Skeleton className="h-full w-full rounded-xl" />
        </div>
      </div>
    );
  }

  if (isError || !workspace) {
    return (
      <div className="flex h-full min-h-0 flex-col items-center justify-center p-6 text-center">
        <AlertCircle className="h-10 w-10 text-destructive mb-3" />
        <h2 className="text-lg font-semibold mb-1">Failed to load workspace</h2>
        <p className="text-sm text-muted-foreground max-w-sm mb-4">
          Workspace missing or access denied.
        </p>
      </div>
    );
  }

  return (
    <div className="flex h-full min-h-0 flex-col overflow-hidden bg-background">
      <WorkspaceCompactHeader workspace={workspace} members={members} />

      <WorkspaceViewTabs
        workspaceId={workspaceId}
        views={views}
        activeViewId={activeView?.id}
        boards={boards.map((board) => ({ id: board.id, title: board.title }))}
        pages={pages.map((page) => ({ id: page.id, title: page.title }))}
        onSelectView={navigateToView}
        onViewCreated={navigateToView}
        reorderHook={useReorderWorkspaceViews}
        api={runtimeClient.api}
      />

      <div className="min-h-0 flex-1 overflow-auto">
        {contextValue ? (
          <WorkspaceTabbedRouteContext.Provider value={contextValue}>
            {children}
          </WorkspaceTabbedRouteContext.Provider>
        ) : (
          children
        )}
      </div>
    </div>
  );
}
