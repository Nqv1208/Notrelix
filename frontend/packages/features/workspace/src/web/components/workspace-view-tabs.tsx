import { useMemo } from "react";
import { useNavigate } from "@tanstack/react-router";

import type { WorkspaceApiClient } from "../../core";
import type { WorkspaceView } from "../../core/types/workspace";
import { createUseReorderWorkspaceViews } from "../hooks/mutations/use-reorder-workspace-views";
import { WorkspaceAddViewMenu } from "./workspace-add-view-menu";
import { WorkspaceViewTabsSurface } from "./workspace-view-tabs-surface";

function getViewLink(
  workspaceId: string,
  view: WorkspaceView,
  currentBoardId?: string,
): { to: string; params: Record<string, string> } {
  const workspaceRoot = {
    to: "/workspaces/$workspaceId",
    params: { workspaceId },
  };

  switch (view.type) {
    case "kanban":
    case "table":
    case "calendar":
    case "timeline": {
      const boardId = view.target.boardId || currentBoardId || "";
      if (!boardId) return workspaceRoot;
      return {
        to: "/workspaces/$workspaceId/boards/$boardId",
        params: { workspaceId, boardId },
      };
    }
    case "doc": {
      const docId = view.target.pageId || "";
      if (!docId) return workspaceRoot;
      return {
        to: "/workspaces/$workspaceId/docs/$docId",
        params: { workspaceId, docId },
      };
    }
    case "dashboard":
      return {
        to: "/workspaces/$workspaceId/dashboard",
        params: { workspaceId },
      };
    default:
      return workspaceRoot;
  }
}

export function WorkspaceViewTabs({
  workspaceId,
  views,
  activeViewId,
  currentBoardId,
  reorderHook: customReorderHook,
  api,
}: {
  workspaceId: string;
  views: WorkspaceView[];
  activeViewId?: string;
  currentBoardId?: string;
  reorderHook?: ReturnType<typeof createUseReorderWorkspaceViews>;
  api: WorkspaceApiClient;
}) {
  const navigate = useNavigate();
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
      onSelectView={(view) => {
        const link = getViewLink(workspaceId, view, currentBoardId);
        void navigate({
          to: link.to as never,
          params: link.params as never,
        });
      }}
      onReorderViews={(orderedViewIds) => {
        reorderMutation.mutate(orderedViewIds);
      }}
      addViewControl={
        <WorkspaceAddViewMenu workspaceId={workspaceId} api={api} />
      }
    />
  );
}
