import { useMemo } from 'react';
import { useParams } from '@tanstack/react-router';
import { useWorkspaceContext } from '@/providers/workspace-provider';
import { useWorkspaceBoards } from '@notrelix/work-management-state';
import { createUsePageList, createUseDocsFavorites } from '@notrelix/docs-state';
import { createUseWorkspaceMembers } from '@notrelix/features-workspace/web';
import { useAppRuntime } from '@notrelix/runtime-web';
import {
  WorkspaceOverview,
  ActiveBoards,
  PinnedDocs,
  ActivityFeed,
  UpcomingDeadlines,
} from '@notrelix/features-workspace/web';

import { useFeatureRuntimeDependencies } from '@notrelix/runtime-web';

export function DashboardPage() {
  const { workspaceId } = useParams({ from: '/workspaces/$workspaceId' });
  const { api, endpoints } = useFeatureRuntimeDependencies();
  const { api: runtimeClient } = useAppRuntime();
  const { workspace, isLoading: workspaceLoading } = useWorkspaceContext();

  const usePageList = useMemo(
    () => createUsePageList(api, endpoints),
    [api, endpoints],
  );

  const useDocsFavorites = useMemo(
    () => createUseDocsFavorites(api, endpoints),
    [api, endpoints],
  );

  const useWorkspaceMembers = useMemo(
    () => createUseWorkspaceMembers({ api: runtimeClient.api }),
    [runtimeClient],
  );

  const { data: boards = [], isLoading: boardsLoading } = useWorkspaceBoards(workspaceId);
  const { data: pages = [], isLoading: pagesLoading } = usePageList(workspaceId);
  const { data: _favorites = [], isLoading: _favoritesLoading } = useDocsFavorites(workspaceId);
  const { data: members = [], isLoading: _membersLoading } = useWorkspaceMembers(workspaceId);

  return (
    <div className="p-8 max-w-[1600px]">
      <div className="space-y-6">
        <WorkspaceOverview
          workspaceName={workspace?.name ?? 'Workspace'}
          pageCount={pages.length}
          boardCount={boards.length}
          memberCount={members.length}
          isLoading={workspaceLoading}
        />

        <div className="grid grid-cols-1 xl:grid-cols-3 gap-6">
          <div className="xl:col-span-2 space-y-6">
            <ActiveBoards
              workspaceId={workspaceId}
              boards={boards.map((b: { id: string; title: string; description?: string }) => ({
                id: b.id,
                title: b.title,
                description: b.description,
              }))}
              isLoading={boardsLoading}
            />
            <UpcomingDeadlines deadlines={[]} isLoading={boardsLoading} />
          </div>

          <div className="space-y-6">
            <PinnedDocs
              workspaceId={workspaceId}
              docs={pages.map((p: { id: string; title: string; updatedAt?: string }) => ({
                id: p.id,
                title: p.title,
                updatedAt: p.updatedAt,
              }))}
              isLoading={pagesLoading}
            />
            <ActivityFeed activities={[]} isLoading={false} />
          </div>
        </div>
      </div>
    </div>
  );
}
