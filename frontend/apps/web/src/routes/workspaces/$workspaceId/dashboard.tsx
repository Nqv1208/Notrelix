import { useMemo } from "react";
import { useParams } from "@tanstack/react-router";
import { useWorkspaceContext } from "@/providers/workspace-provider";
import { useWorkspaceBoards } from "@notrelix/work-management-state";
import { createUsePageList } from "@notrelix/docs-state";
import {
  WorkspaceOverview,
  ActiveBoards,
  PinnedDocs,
  ActivityFeed,
  UpcomingDeadlines,
} from "@notrelix/features-workspace/web";

import { useFeatureRuntimeDependencies } from "@notrelix/runtime-web";

export function DashboardPage() {
  const { workspaceId } = useParams({ from: "/workspaces/$workspaceId" });
  const { api, endpoints } = useFeatureRuntimeDependencies();
  const { workspace, isLoading: workspaceLoading } = useWorkspaceContext();

  const usePageList = useMemo(
    () => createUsePageList(api, endpoints),
    [api, endpoints],
  );

  const { data: boards = [], isLoading: boardsLoading } =
    useWorkspaceBoards(workspaceId);
  const { data: pages = [], isLoading: pagesLoading } =
    usePageList(workspaceId);

  return (
    <div className="p-8 max-w-[1600px]">
      <div className="space-y-6">
        <WorkspaceOverview
          workspaceName={workspace?.name ?? "Workspace"}
          pageCount={pages.length}
          boardCount={boards.length}
          memberCount={workspace?.memberCount ?? 0}
          isLoading={workspaceLoading}
        />

        <div className="grid grid-cols-1 xl:grid-cols-3 gap-6">
          <div className="xl:col-span-2 space-y-6">
            <ActiveBoards
              workspaceId={workspaceId}
              boards={boards.map(
                (b: { id: string; title: string; description?: string }) => ({
                  id: b.id,
                  title: b.title,
                  description: b.description,
                }),
              )}
              isLoading={boardsLoading}
            />
            <UpcomingDeadlines deadlines={[]} isLoading={boardsLoading} />
          </div>

          <div className="space-y-6">
            <PinnedDocs
              workspaceId={workspaceId}
              docs={pages.map(
                (p: { id: string; title: string; updatedAt?: string }) => ({
                  id: p.id,
                  title: p.title,
                  updatedAt: p.updatedAt,
                }),
              )}
              isLoading={pagesLoading}
            />
            <ActivityFeed activities={[]} isLoading={false} />
          </div>
        </div>
      </div>
    </div>
  );
}
