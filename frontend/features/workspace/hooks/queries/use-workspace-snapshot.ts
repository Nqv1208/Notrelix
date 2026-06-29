"use client"

import { useMemo } from "react"
import type { WorkspaceSnapshot } from "../../types"
import { useWorkspaceActivity } from "@/features/activity"
import { useWorkspaceMembers } from "./use-workspace-members"
import { useWorkspaceViews } from "./use-workspace-views"
import { useWorkspace } from "./use-workspace"

export function useWorkspaceSnapshot(workspaceId: string) {
  const workspaceQuery = useWorkspace(workspaceId)
  const membersQuery = useWorkspaceMembers(workspaceId)
  const viewsQuery = useWorkspaceViews(workspaceId)
  const activityQuery = useWorkspaceActivity(workspaceId)

  const data = useMemo<WorkspaceSnapshot | undefined>(() => {
    if (!workspaceQuery.data) return undefined

    const views = viewsQuery.data ?? []
    return {
      workspace: workspaceQuery.data,
      members: membersQuery.data ?? [],
      views,
      favorites: views.slice(0, 3).map((view) => ({
        id: view.id,
        title: view.name,
        type: "view",
        icon: view.icon,
        href: `/${workspaceId}?view=${view.id}`,
      })),
      recent: views.slice(0, 5).map((view) => ({
        id: view.id,
        title: view.name,
        type: "view",
        icon: view.icon,
        href: `/${workspaceId}?view=${view.id}`,
        updatedAt: view.updatedAt ?? view.createdAt,
      })),
      activity: activityQuery.data ?? [],
    }
  }, [activityQuery.data, membersQuery.data, viewsQuery.data, workspaceId, workspaceQuery.data])

  return {
    data,
    isLoading:
      workspaceQuery.isLoading ||
      membersQuery.isLoading ||
      viewsQuery.isLoading ||
      activityQuery.isLoading,
    isFetching:
      workspaceQuery.isFetching ||
      membersQuery.isFetching ||
      viewsQuery.isFetching ||
      activityQuery.isFetching,
    isError: workspaceQuery.isError || membersQuery.isError || viewsQuery.isError,
    error: workspaceQuery.error ?? membersQuery.error ?? viewsQuery.error,
    refetch: async () => {
      await Promise.all([
        workspaceQuery.refetch(),
        membersQuery.refetch(),
        viewsQuery.refetch(),
        activityQuery.refetch(),
      ])
    },
  }
}
