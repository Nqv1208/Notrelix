import { createContext, useContext, useMemo, type ReactNode } from 'react';
import { useLocation } from '@tanstack/react-router';
import { AlertCircle } from 'lucide-react';
import { Skeleton } from '@notrelix/ui-web';
import { useWorkspaceContext } from '@/providers/workspace-provider';
import type { WorkspaceView, WorkspaceSnapshot } from '@notrelix/features-workspace/core';
import {
  WorkspaceCompactHeader,
  WorkspaceViewTabs,
  WorkspaceContextualToolbar,
  createUseReorderWorkspaceViews,
} from '@notrelix/features-workspace/web';
import { api, endpoints } from '@notrelix/contracts';

const useReorderWorkspaceViews = createUseReorderWorkspaceViews({ api });

type WorkspaceTabbedRouteContextValue = {
  workspaceId: string;
  activeView: WorkspaceView;
  snapshot: WorkspaceSnapshot;
  views: WorkspaceView[];
  kind: string;
};

const WorkspaceTabbedRouteContext = createContext<WorkspaceTabbedRouteContextValue | null>(null);

export function useWorkspaceTabbedRoute() {
  const context = useContext(WorkspaceTabbedRouteContext);
  if (!context) {
    throw new Error('useWorkspaceTabbedRoute must be used inside WorkspaceTabbedRouteFrame');
  }
  return context;
}

function resolveViewFromLocation(
  pathname: string,
  workspaceId: string,
  views: WorkspaceView[],
): { activeView: WorkspaceView; kind: string; showToolbar?: boolean } {
  const path = pathname.replace(`/workspaces/${workspaceId}`, '') || '/';

  if (path.startsWith('/dashboard')) {
    const dashboardView = views.find((v) => v.type === 'dashboard') || views[0];
    return { activeView: dashboardView, kind: 'dashboard' };
  }

  if (path.startsWith('/docs')) {
    const docView = views.find((v) => v.type === 'doc') || views[0];
    return { activeView: docView, kind: 'docs', showToolbar: false };
  }

  if (path.startsWith('/boards')) {
    const boardView = views.find((v) => v.type === 'table' || v.type === 'kanban') || views[0];
    return { activeView: boardView, kind: 'board' };
  }

  const defaultView = views.find((v) => v.isDefault) || views[0];
  return { activeView: defaultView, kind: 'home' };
}

/**
 * WorkspaceTabbedRouteFrame wraps workspace content routes (home, dashboard, boards, docs)
 * with the compact header, view tabs, and contextual toolbar.
 *
 * NOTE: This component does NOT render its own <main>. The parent workspace layout
 * provides the main shell (sidebar + topbar + main). This component only renders
 * the tabbed chrome (header + tabs + toolbar) and then children below.
 */
export function WorkspaceTabbedRouteFrame({ children }: { children: ReactNode }) {
  const { workspaceId, workspace, views, isLoading, isError } = useWorkspaceContext();
  const location = useLocation();

  const members: WorkspaceSnapshot['members'] = useMemo(() => [], []);

  const routeInfo = useMemo(
    () => resolveViewFromLocation(location.pathname, workspaceId, views),
    [location.pathname, workspaceId, views],
  );

  if (isLoading) return <WorkspaceTabbedSkeleton />;

  if (isError || !workspace) return <WorkspaceTabbedError />;

  const snapshot: WorkspaceSnapshot = {
    workspace,
    members,
    views,
    favorites: [],
    recent: [],
    activity: [],
  };

  return (
    <WorkspaceTabbedRouteContext.Provider
      value={{
        workspaceId,
        activeView: routeInfo.activeView,
        snapshot,
        views,
        kind: routeInfo.kind,
      }}
    >
      <WorkspaceCompactHeader workspace={workspace} members={members} />
      <WorkspaceViewTabs
        workspaceId={workspaceId}
        views={views}
        activeViewId={routeInfo.activeView.id}
        reorderHook={useReorderWorkspaceViews}
      />
      {routeInfo.showToolbar !== false ? (
        <WorkspaceContextualToolbar activeType={routeInfo.activeView.type} activeView={routeInfo.activeView} />
      ) : null}
      {children}
    </WorkspaceTabbedRouteContext.Provider>
  );
}

export function WorkspaceTabbedSkeleton() {
  return (
    <div className="flex h-full min-h-0 flex-col bg-card">
      <div className="border-b border-border bg-card p-6">
        <Skeleton className="mb-3 h-10 w-72 rounded-xl" />
        <Skeleton className="h-5 w-full max-w-2xl rounded-lg" />
      </div>
      <div className="border-b border-border bg-card px-6 py-3">
        <Skeleton className="h-9 w-full max-w-2xl rounded-xl" />
      </div>
      <div className="border-b border-border px-6 py-3">
        <Skeleton className="h-9 w-full max-w-3xl rounded-xl" />
      </div>
      <div className="p-6">
        <div className="rounded-2xl border border-border bg-card p-4">
          <Skeleton className="mb-4 h-10 rounded-xl" />
          <Skeleton className="mb-2 h-12 rounded-xl" />
          <Skeleton className="mb-2 h-12 rounded-xl" />
          <Skeleton className="h-12 rounded-xl" />
        </div>
      </div>
    </div>
  );
}

export function WorkspaceTabbedError({
  title = 'Workspace unavailable',
  detail = 'The workspace views could not be loaded.',
}: {
  title?: string;
  detail?: string;
}) {
  return (
    <div className="p-4 sm:p-6">
      <div className="rounded-2xl border border-border bg-card p-8 text-center">
        <AlertCircle className="mx-auto mb-3 size-8 text-destructive" />
        <h1 className="text-lg font-semibold text-foreground">{title}</h1>
        <p className="mt-2 text-sm text-muted-foreground">{detail}</p>
      </div>
    </div>
  );
}
