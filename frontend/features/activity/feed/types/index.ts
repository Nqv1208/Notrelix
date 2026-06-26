// Type definitions for the activity feed domain.

export type WorkspaceActivityResponseApi = {
  data: Array<{
    id: string
    actorId: string
    action: string
    resourceTitle?: string | null
    createdAt: string
  }>
}

export type WorkspaceActivityItem = {
  id: string
  actor: string
  action: string
  target: string
  createdAt: string
}
