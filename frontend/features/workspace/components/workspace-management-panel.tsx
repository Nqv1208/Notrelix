"use client"

import { useState } from "react"
import { useRouter, useSearchParams, usePathname } from "next/navigation"
import {
  Activity,
  LockKeyhole,
  Mail,
  Settings,
  Sparkles,
  Trash2,
  UserMinus,
  Users,
  Workflow,
} from "lucide-react"
import { toast } from "sonner"
import { useAuthUser } from "@/features/auth"
import {
  useUpdateWorkspace,
  useUpdateMemberRole,
  useRemoveMember,
  useWorkspaceInvitations,
  useCreateInvitation,
  useDeleteInvitation,
} from "@/features/workspace/hooks"
import type { WorkspaceSnapshot } from "@/features/workspace/types"
import { Avatar, AvatarFallback } from "@/components/ui/avatar"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table"
import { cn } from "@/lib/utils"
import { PermissionsTab } from "@/features/governance"
import { IntegrationsTab } from "@/features/integrations"
import { AutomationsTab } from "@/features/automation"
import { ActivityLogsTab } from "@/features/activity"

interface WorkspaceManagementPanelProps {
  panel: string
  workspaceId: string
  snapshot: WorkspaceSnapshot
}

const SETTINGS_TABS = [
  { id: "general", label: "General", icon: Settings },
  { id: "members", label: "Members", icon: Users },
  { id: "permissions", label: "Permissions", icon: LockKeyhole },
  { id: "integrations", label: "Integrations", icon: Sparkles },
  { id: "automations", label: "Automations", icon: Workflow },
  { id: "activity", label: "Activity Logs", icon: Activity },
]

export function WorkspaceManagementPanel({ workspaceId, snapshot }: WorkspaceManagementPanelProps) {
  const router = useRouter()
  const searchParams = useSearchParams()
  const pathname = usePathname()
  
  const tabParam = searchParams.get("tab")
  const activeTab = (tabParam && SETTINGS_TABS.some(t => t.id === tabParam)) ? tabParam : "general"

  const handleTabChange = (tabId: string) => {
    const params = new URLSearchParams(searchParams.toString())
    params.set("tab", tabId)
    router.replace(`${pathname}?${params.toString()}` as never)
  }

  return (
    <div className="max-w-6xl mx-auto py-2">
      <div className="flex flex-col md:flex-row gap-8 items-start">
        {/* Settings Sidebar navigation */}
        <aside className="w-full md:w-56 shrink-0 self-start">
          <div className="px-3 mb-4 hidden md:block">
            <h1 className="text-lg font-bold tracking-tight text-foreground">Workspace settings</h1>
            <p className="text-xs text-muted-foreground mt-0.5">Manage preferences & members</p>
          </div>
          <nav className="flex md:flex-col overflow-x-auto md:overflow-x-visible pb-2 md:pb-0 gap-1 border-b md:border-b-0 border-border/60">
            {SETTINGS_TABS.map((tab) => {
              const Icon = tab.icon
              const isActive = activeTab === tab.id
              return (
                <button
                  key={tab.id}
                  onClick={() => handleTabChange(tab.id)}
                  className={cn(
                    "flex items-center gap-2.5 rounded-lg px-3 py-2 text-sm font-medium transition-all duration-150 text-left whitespace-nowrap md:w-full",
                    isActive
                      ? "bg-primary/10 text-primary font-semibold"
                      : "text-muted-foreground hover:bg-muted hover:text-foreground"
                  )}
                >
                  <Icon className="size-4 shrink-0" />
                  <span>{tab.label}</span>
                </button>
              )
            })}
          </nav>
        </aside>

        {/* Content Area */}
        <main className="flex-1 min-w-0 w-full min-h-[550px] rounded-2xl border border-border/40 bg-card/40 backdrop-blur-md p-6 sm:p-8">
          {activeTab === "general" && (
            <GeneralSettingsTab
              workspaceId={workspaceId}
              initialName={snapshot.workspace.name}
              initialSlug={snapshot.workspace.slug}
            />
          )}
          {activeTab === "members" && (
            <MembersTab workspaceId={workspaceId} snapshotMembers={snapshot.members} />
          )}
          {activeTab === "permissions" && <PermissionsTab />}
          {activeTab === "integrations" && <IntegrationsTab />}
          {activeTab === "automations" && <AutomationsTab />}
          {activeTab === "activity" && <ActivityLogsTab workspaceId={workspaceId} />}
        </main>
      </div>
    </div>
  )
}

// ==========================================
// Sub-components to prevent giant component
// ==========================================

interface GeneralSettingsTabProps {
  workspaceId: string
  initialName: string
  initialSlug: string
}

function GeneralSettingsTab({ workspaceId, initialName, initialSlug }: GeneralSettingsTabProps) {
  const updateWorkspaceMutation = useUpdateWorkspace(workspaceId)
  const [wsName, setWsName] = useState(initialName)
  const [wsSlug, setWsSlug] = useState(initialSlug)

  const handleUpdateWorkspace = (e: React.FormEvent) => {
    e.preventDefault()
    if (!wsName.trim()) {
      toast.error("Workspace name cannot be empty.")
      return
    }
    if (!wsSlug.trim()) {
      toast.error("Workspace slug cannot be empty.")
      return
    }
    updateWorkspaceMutation.mutate({ name: wsName, slug: wsSlug })
  }

  return (
    <div className="space-y-8">
      <div>
        <h2 className="text-xl font-semibold tracking-tight text-foreground">Workspace details</h2>
        <p className="text-sm text-muted-foreground mt-1">Customize your workspace name and access URL.</p>
      </div>

      <form onSubmit={handleUpdateWorkspace} className="space-y-5 max-w-lg">
        <div className="space-y-2">
          <Label htmlFor="wsName" className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">Workspace Name</Label>
          <Input
            id="wsName"
            value={wsName}
            onChange={(e) => setWsName(e.target.value)}
            placeholder="Enter workspace name"
            className="rounded-lg border-border/80 focus-visible:ring-primary"
          />
        </div>
        <div className="space-y-2">
          <Label htmlFor="wsSlug" className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">Workspace Slug (URL)</Label>
          <div className="flex rounded-lg border border-border/80 overflow-hidden bg-muted/30 focus-within:ring-2 focus-within:ring-ring focus-within:ring-offset-0">
            <span className="flex items-center px-3 text-sm text-muted-foreground select-none bg-muted border-r border-border/80">
              notrelix.com/
            </span>
            <input
              id="wsSlug"
              aria-label="Workspace Slug"
              value={wsSlug}
              onChange={(e) => setWsSlug(e.target.value.toLowerCase().replace(/[^a-z0-9-]+/g, ""))}
              placeholder="enter-workspace-slug"
              className="flex-1 bg-transparent px-3 py-2 text-sm outline-none text-foreground placeholder:text-muted-foreground"
            />
          </div>
          <p className="text-xs text-muted-foreground/70">Slugs can only contain lowercase letters, numbers, and hyphens.</p>
        </div>
        <Button type="submit" disabled={updateWorkspaceMutation.isPending} className="rounded-lg px-5">
          {updateWorkspaceMutation.isPending ? "Saving..." : "Save Changes"}
        </Button>
      </form>

      <div className="pt-6 border-t border-border/60">
        <h3 className="text-sm font-semibold text-destructive uppercase tracking-wider">Danger Zone</h3>
        <div className="mt-3 p-4 rounded-xl border border-destructive/20 bg-destructive/5 flex flex-col sm:flex-row sm:items-center justify-between gap-4">
          <div>
            <h4 className="text-sm font-semibold text-foreground">Delete Workspace</h4>
            <p className="text-xs text-muted-foreground mt-0.5">Permanently delete this workspace and all of its content. This action is irreversible.</p>
          </div>
          <Button variant="destructive" className="rounded-lg shrink-0">Delete Workspace</Button>
        </div>
      </div>
    </div>
  )
}

interface MembersTabProps {
  workspaceId: string
  snapshotMembers: WorkspaceSnapshot["members"]
}

function MembersTab({ workspaceId, snapshotMembers }: MembersTabProps) {
  const { user: currentUser } = useAuthUser()
  const updateMemberRoleMutation = useUpdateMemberRole(workspaceId)
  const removeMemberMutation = useRemoveMember(workspaceId)
  const createInvitationMutation = useCreateInvitation(workspaceId)
  const deleteInvitationMutation = useDeleteInvitation(workspaceId)
  const { data: invitations = [] } = useWorkspaceInvitations(workspaceId)

  const [inviteEmail, setInviteEmail] = useState("")
  const [inviteRole, setInviteRole] = useState("member")

  const handleSendInvite = (e: React.FormEvent) => {
    e.preventDefault()
    if (!inviteEmail.trim()) {
      toast.error("Please enter an email address.")
      return
    }
    createInvitationMutation.mutate(
      { email: inviteEmail, role: inviteRole },
      {
        onSuccess: () => {
          setInviteEmail("")
        },
      }
    )
  }

  const handleRoleChange = (userId: string, newRole: string) => {
    updateMemberRoleMutation.mutate({ userId, role: newRole })
  }

  const handleRemoveMember = (userId: string, name: string) => {
    if (window.confirm(`Are you sure you want to remove ${name} from this workspace?`)) {
      removeMemberMutation.mutate(userId)
    }
  }

  const handleRevokeInvitation = (invitationId: string, email: string) => {
    if (window.confirm(`Are you sure you want to revoke the invitation to ${email}?`)) {
      deleteInvitationMutation.mutate(invitationId)
    }
  }

  return (
    <div className="space-y-8">
      <div>
        <h2 className="text-xl font-semibold tracking-tight text-foreground">Workspace members</h2>
        <p className="text-sm text-muted-foreground mt-1">Manage who has access to this workspace and control their permission levels.</p>
      </div>

      {/* Invite Member form */}
      <div className="p-5 rounded-xl border border-border/80 bg-muted/10">
        <h3 className="text-sm font-semibold text-foreground mb-3">Invite new member</h3>
        <form onSubmit={handleSendInvite} className="flex flex-col sm:flex-row gap-3">
          <div className="flex-1">
            <Label htmlFor="inviteEmail" className="sr-only">Email address</Label>
            <Input
              id="inviteEmail"
              type="email"
              placeholder="Enter email address (name@example.com)"
              value={inviteEmail}
              onChange={(e) => setInviteEmail(e.target.value)}
              className="rounded-lg bg-card"
            />
          </div>
          <div className="w-full sm:w-36">
            <Select value={inviteRole} onValueChange={setInviteRole}>
              <SelectTrigger className="rounded-lg bg-card">
                <SelectValue placeholder="Select role" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="member">Member</SelectItem>
                <SelectItem value="admin">Admin</SelectItem>
                <SelectItem value="guest">Guest</SelectItem>
              </SelectContent>
            </Select>
          </div>
          <Button type="submit" disabled={createInvitationMutation.isPending} className="rounded-lg px-5">
            <Mail className="size-4 mr-2" />
            Send Invite
          </Button>
        </form>
      </div>

      {/* Members table */}
      <div className="space-y-3">
        <h3 className="text-sm font-semibold text-foreground">Active members</h3>
        <div className="rounded-xl border border-border/60 overflow-hidden bg-card">
          <Table>
            <TableHeader className="bg-muted/30">
              <TableRow className="hover:bg-transparent">
                <TableHead className="py-3 px-4 font-semibold text-xs uppercase tracking-wider text-muted-foreground">Member</TableHead>
                <TableHead className="py-3 px-4 font-semibold text-xs uppercase tracking-wider text-muted-foreground">Role</TableHead>
                <TableHead className="py-3 px-4 w-[80px]"></TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {snapshotMembers.map((member) => {
                const isSelf = currentUser && currentUser.id === member.userId
                const initials = member.name.trim().split(/\s+/).map(p => p[0]).join("").toUpperCase().slice(0, 2)

                return (
                  <TableRow key={member.id} className="hover:bg-muted/10 transition-colors border-border/50">
                    <TableCell className="py-3 px-4">
                      <div className="flex items-center gap-3">
                        <Avatar className="size-8 ring-1 ring-border/20">
                          <AvatarFallback className="text-[11px] font-semibold text-primary-foreground" style={{ backgroundColor: member.color }}>
                            {initials}
                          </AvatarFallback>
                        </Avatar>
                        <div>
                          <p className="font-semibold text-foreground text-sm flex items-center gap-1.5">
                            {member.name}
                            {isSelf && <Badge variant="outline" className="text-[9px] py-0 px-1.5 font-normal tracking-wide bg-primary/5 border-primary/20 text-primary rounded-md">You</Badge>}
                          </p>
                        </div>
                      </div>
                    </TableCell>
                    <TableCell className="py-3 px-4">
                      {isSelf || member.role === "owner" ? (
                        <Badge variant="secondary" className="capitalize text-[11px] font-medium bg-muted/65 rounded-md">{member.role}</Badge>
                      ) : (
                        <Select
                          defaultValue={member.role}
                          onValueChange={(val) => handleRoleChange(member.userId, val)}
                        >
                          <SelectTrigger className="w-28 h-8 rounded-lg text-xs bg-card">
                            <SelectValue />
                          </SelectTrigger>
                          <SelectContent>
                            <SelectItem value="member">Member</SelectItem>
                            <SelectItem value="admin">Admin</SelectItem>
                            <SelectItem value="guest">Guest</SelectItem>
                          </SelectContent>
                        </Select>
                      )}
                    </TableCell>
                    <TableCell className="py-3 px-4 text-right">
                      {!isSelf && member.role !== "owner" && (
                        <Button
                          variant="ghost"
                          size="icon-sm"
                          aria-label={`Remove ${member.name} from workspace`}
                          onClick={() => handleRemoveMember(member.userId, member.name)}
                          className="rounded-lg hover:bg-destructive/5 text-muted-foreground hover:text-destructive transition"
                        >
                          <UserMinus className="size-4" />
                        </Button>
                      )}
                    </TableCell>
                  </TableRow>
                )
              })}
            </TableBody>
          </Table>
        </div>
      </div>

      {/* Pending invitations */}
      {invitations.length > 0 && (
        <div className="space-y-3 pt-4 border-t border-border/40">
          <h3 className="text-sm font-semibold text-foreground">Pending invitations</h3>
          <div className="rounded-xl border border-border/60 overflow-hidden bg-card">
            <Table>
              <TableHeader className="bg-muted/30">
                <TableRow className="hover:bg-transparent">
                  <TableHead className="py-3 px-4 font-semibold text-xs uppercase tracking-wider text-muted-foreground">Email</TableHead>
                  <TableHead className="py-3 px-4 font-semibold text-xs uppercase tracking-wider text-muted-foreground">Role</TableHead>
                  <TableHead className="py-3 px-4 font-semibold text-xs uppercase tracking-wider text-muted-foreground">Sent date</TableHead>
                  <TableHead className="py-3 px-4 w-[80px]"></TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {invitations.map((inv) => (
                  <TableRow key={inv.id} className="hover:bg-muted/10 transition-colors border-border/50">
                    <TableCell className="py-3 px-4 font-medium text-foreground text-sm">{inv.email}</TableCell>
                    <TableCell className="py-3 px-4">
                      <Badge variant="secondary" className="capitalize text-[11px] font-medium rounded-md">{inv.role}</Badge>
                    </TableCell>
                    <TableCell className="py-3 px-4 text-muted-foreground text-xs">
                      {new Date(inv.createdAt).toLocaleDateString()}
                    </TableCell>
                    <TableCell className="py-3 px-4 text-right">
                      <Button
                        variant="ghost"
                        size="icon-sm"
                        aria-label={`Revoke invitation to ${inv.email}`}
                        onClick={() => handleRevokeInvitation(inv.id, inv.email)}
                        className="rounded-lg hover:bg-destructive/5 text-muted-foreground hover:text-destructive transition"
                      >
                        <Trash2 className="size-4" />
                      </Button>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </div>
        </div>
      )}
    </div>
  )
}

// Tabs are now imported from their respective FSD-aligned feature slices, decoupling the workspace feature.
