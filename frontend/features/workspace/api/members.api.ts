import { api } from "@/lib/api/api-client"
import { endpoints } from "@/lib/api/endpoints"
import type { WorkspaceMemberDtoApi } from "../types/dto"
import type { WorkspaceMember } from "../types"

const memberColors = ["var(--primary)", "var(--accent)", "var(--destructive)", "var(--muted-foreground)"]

export function normalizeMemberRole(value: string): WorkspaceMember["role"] {
  const normalized = value.trim().toLowerCase()
  if (normalized === "owner") return "owner"
  if (normalized === "admin") return "admin"
  if (normalized === "guest") return "guest"
  return "member"
}

export function getInitials(name: string) {
  const parts = name.trim().split(/\s+/).filter(Boolean)
  if (parts.length === 0) return "?"
  if (parts.length === 1) return parts[0].slice(0, 2).toUpperCase()
  return `${parts[0][0]}${parts[parts.length - 1][0]}`.toUpperCase()
}

export function mapMemberDto(member: WorkspaceMemberDtoApi, index: number): WorkspaceMember {
  return {
    id: `wm-${member.userId}`,
    userId: member.userId,
    name: member.name,
    initials: getInitials(member.name),
    role: normalizeMemberRole(member.role),
    status: "active",
    workload: 0,
    color: memberColors[index % memberColors.length],
    avatarUrl: member.avatar ?? undefined,
  }
}

export const membersApi = {
  async getMembers(workspaceId: string): Promise<WorkspaceMember[]> {
    const members = await api.get<WorkspaceMemberDtoApi[]>(`${endpoints.workspaces.detail(workspaceId)}/members`)
    return members.map(mapMemberDto)
  },

  async updateMemberRole(workspaceId: string, userId: string, role: string): Promise<WorkspaceMember> {
    const member = await api.patch<WorkspaceMemberDtoApi>(
      `${endpoints.workspaces.detail(workspaceId)}/members/${userId}`,
      { role }
    )
    return mapMemberDto(member, 0)
  },

  async removeMember(workspaceId: string, userId: string): Promise<void> {
    await api.delete<void>(`${endpoints.workspaces.detail(workspaceId)}/members/${userId}`)
  },
}
