"use client"

import { useState } from "react"
import { useWorkspaceActivity } from "../hooks/use-workspace-activity"
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table"
import { Button } from "@/components/ui/button"
import { Badge } from "@/components/ui/badge"

interface ActivityLogsTabProps {
  workspaceId: string
}

export function ActivityLogsTab({ workspaceId }: ActivityLogsTabProps) {
  const [activityPage, setActivityPage] = useState(1)
  const { data: activities = [], isLoading: activitiesLoading } = useWorkspaceActivity(workspaceId, activityPage, 10)

  return (
    <div className="space-y-6">
      <div>
        <h2 className="text-xl font-semibold tracking-tight text-foreground">Workspace activity logs</h2>
        <p className="text-sm text-muted-foreground mt-1">A complete chronological history of actions performed in this workspace.</p>
      </div>

      {activitiesLoading ? (
        <div className="py-12 text-center text-sm text-muted-foreground">Loading activity logs...</div>
      ) : activities.length === 0 ? (
        <div className="py-12 text-center text-sm text-muted-foreground">No activities recorded yet.</div>
      ) : (
        <div className="space-y-4">
          <div className="rounded-xl border border-border/60 overflow-hidden bg-card">
            <Table>
              <TableHeader className="bg-muted/30">
                <TableRow className="hover:bg-transparent">
                  <TableHead className="py-3 px-4 font-semibold text-xs uppercase tracking-wider text-muted-foreground">Actor</TableHead>
                  <TableHead className="py-3 px-4 font-semibold text-xs uppercase tracking-wider text-muted-foreground">Action</TableHead>
                  <TableHead className="py-3 px-4 font-semibold text-xs uppercase tracking-wider text-muted-foreground">Target</TableHead>
                  <TableHead className="py-3 px-4 font-semibold text-xs uppercase tracking-wider text-muted-foreground">Time</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {activities.map((act) => (
                  <TableRow key={act.id} className="hover:bg-muted/5 transition-colors border-border/50">
                    <TableCell className="py-3 px-4 font-semibold text-foreground text-sm">{act.actor}</TableCell>
                    <TableCell className="py-3 px-4 capitalize text-sm">
                      <Badge variant="outline" className="rounded-md font-normal text-xs px-2 py-0.5 border-border/80">
                        {act.action}
                      </Badge>
                    </TableCell>
                    <TableCell className="py-3 px-4 text-muted-foreground text-sm font-medium">{act.target}</TableCell>
                    <TableCell className="py-3 px-4 text-muted-foreground text-xs">
                      {new Date(act.createdAt).toLocaleString()}
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </div>

          <div className="flex items-center justify-between pt-2">
            <Button
              variant="outline"
              size="sm"
              onClick={() => setActivityPage((prev) => Math.max(1, prev - 1))}
              disabled={activityPage === 1}
              className="rounded-lg px-4"
            >
              Previous
            </Button>
            <span className="text-xs text-muted-foreground font-medium">Page {activityPage}</span>
            <Button
              variant="outline"
              size="sm"
              onClick={() => setActivityPage((prev) => prev + 1)}
              disabled={activities.length < 10}
              className="rounded-lg px-4"
            >
              Next
            </Button>
          </div>
        </div>
      )}
    </div>
  )
}
