"use client"

import { toast } from "sonner"
import { Button } from "@/components/ui/button"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"
import { Switch } from "@/components/ui/switch"

export function PermissionsTab() {
  return (
    <div className="space-y-6">
      <div>
        <h2 className="text-xl font-semibold tracking-tight text-foreground">Role Permissions (RBAC)</h2>
        <p className="text-sm text-muted-foreground mt-1">Define roles access policies for workspace activities.</p>
      </div>

      <div className="rounded-xl border border-border/60 bg-card overflow-hidden divide-y divide-border/60">
        <div className="flex items-center justify-between p-4 sm:p-5">
          <div className="space-y-1 pr-4">
            <p className="text-sm font-semibold text-foreground">Invite members</p>
            <p className="text-xs text-muted-foreground">Who can send invitations to new users.</p>
          </div>
          <Select defaultValue="admins">
            <SelectTrigger className="w-44 h-9 rounded-lg bg-card">
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="owner">Owner Only</SelectItem>
              <SelectItem value="admins">Admins & Owner</SelectItem>
              <SelectItem value="all">All Members</SelectItem>
            </SelectContent>
          </Select>
        </div>

        <div className="flex items-center justify-between p-4 sm:p-5">
          <div className="space-y-1 pr-4">
            <p className="text-sm font-semibold text-foreground">Create Boards</p>
            <p className="text-xs text-muted-foreground">Who can create new kanban boards in this workspace.</p>
          </div>
          <Select defaultValue="all">
            <SelectTrigger className="w-44 h-9 rounded-lg bg-card">
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="admins">Admins & Owner</SelectItem>
              <SelectItem value="all">All Members</SelectItem>
            </SelectContent>
          </Select>
        </div>

        <div className="flex items-center justify-between p-4 sm:p-5">
          <div className="space-y-1 pr-4">
            <p className="text-sm font-semibold text-foreground">Delete Boards</p>
            <p className="text-xs text-muted-foreground">Permissions required to delete/archive boards.</p>
          </div>
          <Select defaultValue="admins">
            <SelectTrigger className="w-44 h-9 rounded-lg bg-card">
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="owner">Owner Only</SelectItem>
              <SelectItem value="admins">Admins & Owner</SelectItem>
            </SelectContent>
          </Select>
        </div>

        <div className="flex items-center justify-between p-4 sm:p-5">
          <div className="space-y-1 pr-4">
            <p className="text-sm font-semibold text-foreground">Guest Invites</p>
            <p className="text-xs text-muted-foreground">Allow guests to view specific boards & documents.</p>
          </div>
          <Switch defaultChecked className="data-[state=checked]:bg-primary" />
        </div>
      </div>

      <Button onClick={() => toast.success("Permissions updated successfully.")} className="rounded-lg px-5">
        Save Permissions
      </Button>
    </div>
  )
}
