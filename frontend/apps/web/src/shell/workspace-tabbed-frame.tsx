import { createContext, useContext, useMemo, type ReactNode } from 'react';
import { useLocation } from '@tanstack/react-router';
import { AlertCircle } from 'lucide-react';
import { Skeleton } from '@notrelix/ui-web';
import { useWorkspaceContext } from '@/providers/workspace-provider';
import type { WorkspaceView } from '@notrelix/features-workspace/core';
import {
  WorkspaceCompactHeader,
  WorkspaceViewTabs,
  WorkspaceContextualToolbar,
  createUseReorderWorkspaceViews,
} from '@notrelix/features-workspace/web';
import { useAppRuntime } from '@notrelix/runtime-web';

type WorkspaceTabbedRouteContextValue = {
  workspaceId: string;
  activeView: WorkspaceView;
  views: WorkspaceView[];
  kind: string;
};

const WorkspaceTabbedRouteContext = createContext<WorkspaceTabbedRouteContextValue | null>(null);

export function useWorkspaceTabbedRouteContext(): WorkspaceTabbedRouteContextValue {
  const context = useContext(WorkspaceTabbedRouteContext);
  if (!context) {
    throw new Error('useWorkspaceTabbedRouteContext must be used within WorkspaceTabbedFrame');
  }
  return context;
}

export function WorkspaceTabbedFrame({ children }: { children: ReactNode }) {
  const location = useLocation();
  const { api: runtimeClient } = useAppRuntime();
  const { workspaceId, workspace, views, isLoading, isError } = useWorkspaceContext();

  const useReorderWorkspaceViews = useMemo(
    () => createUseReorderWorkspaceViews({ api: runtimeClient.api }),
    [runtimeClient],
  );

  const activeView = useMemo(() => {
    if (!views || views.length === 0) return null;
    const pathParts = location.pathname.split('/').filter(Boolean);

    if (pathParts.includes('boards')) {
      const boardId = pathParts[pathParts.indexOf('boards') + 1];
      const found = views.find((v) => v.target.boardId === boardId);
      if (found) return found;
    }

    if (pathParts.includes('docs')) {
      const docId = pathParts[pathParts.indexOf('docs') + 1];
      const found = views.find((v) => v.target.pageId === docId);
      if (found) return found;
    }

    return views[0] ?? null;
  }, [views, location.pathname]);

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
      <div className="flex flex-col h-screen overflow-hidden bg-background">
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
      <div className="flex flex-col items-center justify-center h-screen p-6 text-center">
        <AlertCircle className="h-10 w-10 text-destructive mb-3" />
        <h2 className="text-lg font-semibold mb-1">Failed to load workspace</h2>
        <p className="text-sm text-muted-foreground max-w-sm mb-4">
          Workspace missing or access denied.
        </p>
      </div>
    );
  }

  return (
    <div className="flex flex-col h-screen overflow-hidden bg-background">
      <WorkspaceCompactHeader workspace={workspace} members={[]} />

      <WorkspaceViewTabs
        workspaceId={workspaceId}
        views={views}
        activeViewId={activeView?.id}
        reorderHook={useReorderWorkspaceViews}
      />

      <WorkspaceContextualToolbar
        activeType={activeView?.type || 'table'}
        activeView={activeView || undefined}
      />

      <main className="flex-1 overflow-auto">
        {contextValue ? (
          <WorkspaceTabbedRouteContext.Provider value={contextValue}>
            {children}
          </WorkspaceTabbedRouteContext.Provider>
        ) : (
          children
        )}
      </main>
    </div>
  );
}
