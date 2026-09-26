import { useMemo } from "react";

import type { WorkspaceApiClient } from "../../core";
import type { WorkspaceView } from "../../core/types/workspace";
import { createUseReorderWorkspaceViews } from "../hooks/mutations/use-reorder-workspace-views";
import { WorkspaceAddViewMenu } from "./workspace-add-view-menu";
import { WorkspaceViewTabsSurface } from "./workspace-view-tabs-surface";

export function WorkspaceViewTabs({
  workspaceId,
  views,
  activeViewId,
  boards,
  pages,
  onSelectView,
  onViewCreated,
  reorderHook: customReorderHook,
  api,
}: {
  workspaceId: string;
  views: WorkspaceView[];
  activeViewId?: string;
  boards?: Array<{ id: string; title?: string }>;
  pages?: Array<{ id: string; title: string }>;
  onSelectView?: (view: WorkspaceView) => void;
  onViewCreated?: (view: WorkspaceView) => void;
  reorderHook?: ReturnType<typeof createUseReorderWorkspaceViews>;
  api: WorkspaceApiClient;
}) {
  const defaultReorderHook = useMemo(
    () => createUseReorderWorkspaceViews({ api }),
    [api],
  );
  const reorderHook = customReorderHook || defaultReorderHook;

  const reorderMutation = reorderHook(workspaceId);

  return (
    <WorkspaceViewTabsSurface
      workspaceId={workspaceId}
      views={views}
      activeViewId={activeViewId}
      onSelectView={onSelectView}
      onReorderViews={(orderedViewIds) => {
        reorderMutation.mutate(orderedViewIds);
      }}
      addViewControl={
        <WorkspaceAddViewMenu
          workspaceId={workspaceId}
          boards={boards}
          pages={pages}
          api={api}
          onViewCreated={onViewCreated}
        />
      }
    />
  );
}
