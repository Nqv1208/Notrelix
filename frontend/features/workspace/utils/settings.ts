import type { WorkspaceView } from "../types"

export interface WorkspaceSettings {
  customViews?: WorkspaceView[]
  customViewsOrder?: string[]
  [key: string]: unknown
}

export function parseSettings(settings: string | null | undefined): WorkspaceSettings {
  if (!settings) return {}

  try {
    const parsed = JSON.parse(settings)
    if (!parsed || typeof parsed !== "object" || Array.isArray(parsed)) return {}

    return parsed as WorkspaceSettings
  } catch {
    return {}
  }
}

export function stringifySettings(settings: WorkspaceSettings): string {
  return JSON.stringify(settings)
}
