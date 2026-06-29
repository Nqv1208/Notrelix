import type { ID } from "./ids.types"

export type DocsRole = "owner" | "editor" | "commenter" | "viewer"
export type PresenceStatus = "active" | "idle" | "offline"

export interface DocsUser {
  id: ID
  name: string
  email: string
  avatarUrl: string | null
  color: string
  role: DocsRole
}
