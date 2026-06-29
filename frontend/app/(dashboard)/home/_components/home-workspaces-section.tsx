"use client"

import Link from "next/link"
import { ArrowUpRight, RefreshCw, Users } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Skeleton } from "@/components/ui/skeleton"
import { useWorkspaceList, type WorkspaceSummary, getWorkspaceRootHref } from "@/features/workspace"

const workspaceColors = ["#6161ff", "#2a9d99", "#ff8940", "#8b5cf6", "#0f9f6e", "#dc3f6d"]

function colorForWorkspace(slug: string) {
  const hash = Array.from(slug).reduce((value, char) => value + char.charCodeAt(0), 0)
  return workspaceColors[hash % workspaceColors.length]
}

function formatWorkspacePlan(workspace: WorkspaceSummary) {
  return workspace.isPersonal
    ? "Personal"
    : workspace.plan.charAt(0).toUpperCase() + workspace.plan.slice(1)
}

function WorkspaceCard({ workspace }: { workspace: WorkspaceSummary }) {
  const memberLabel = workspace.memberCount === 1 ? "member" : "members"

  return (
    <Link
      key={workspace.id}
      href={getWorkspaceRootHref(workspace) as never}
      className="group rounded-2xl border border-border bg-card p-4 transition hover:-translate-y-0.5 hover:shadow-[rgba(205,208,223,0.35)_0px_2px_24px]"
    >
      <div className="mb-5 flex items-center justify-between">
        <span
          className="flex size-11 items-center justify-center rounded-xl text-sm font-semibold text-primary-foreground"
          style={{ backgroundColor: colorForWorkspace(workspace.id) }}
        >
          {workspace.icon}
        </span>
        <ArrowUpRight className="size-4 text-muted-foreground opacity-0 transition group-hover:opacity-100" />
      </div>
      <h3 className="line-clamp-1 text-sm font-semibold text-foreground">{workspace.name}</h3>
      <p className="mt-1 flex items-center gap-2 text-xs text-muted-foreground">
        <Users className="size-3.5" />
        {workspace.memberCount} {memberLabel} · {formatWorkspacePlan(workspace)}
      </p>
    </Link>
  )
}

function WorkspaceSkeleton() {
  return (
    <div className="rounded-2xl border border-border bg-card p-4">
      <div className="mb-5 flex items-center justify-between">
        <Skeleton className="size-11 rounded-xl" />
        <Skeleton className="size-4 rounded-full" />
      </div>
      <Skeleton className="h-4 w-2/3" />
      <Skeleton className="mt-3 h-3 w-1/2" />
    </div>
  )
}

export function HomeWorkspacesSection() {
  const { data: workspaces = [], isError, isFetching, isLoading, refetch } = useWorkspaceList()

  return (
    <section>
      <div className="mb-3 flex items-center justify-between">
        <h2 className="text-sm font-semibold text-foreground">Your workspaces</h2>
        {!isLoading && !isError ? (
          <span className="text-xs text-muted-foreground">{workspaces.length} total</span>
        ) : null}
      </div>

      {isLoading ? (
        <div className="grid gap-3 md:grid-cols-3">
          <WorkspaceSkeleton />
          <WorkspaceSkeleton />
          <WorkspaceSkeleton />
        </div>
      ) : isError ? (
        <div className="rounded-2xl border border-border bg-card p-4">
          <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
            <p className="text-sm text-muted-foreground">Unable to load your workspaces.</p>
            <Button variant="outline" size="sm" onClick={() => refetch()} disabled={isFetching}>
              <RefreshCw className={isFetching ? "size-4 animate-spin" : "size-4"} />
              Retry
            </Button>
          </div>
        </div>
      ) : workspaces.length === 0 ? (
        <div className="rounded-2xl border border-border bg-card p-4 text-sm text-muted-foreground">
          No workspaces are available for this account.
        </div>
      ) : (
        <div className="grid gap-3 md:grid-cols-3">
          {workspaces.map((workspace) => (
            <WorkspaceCard key={workspace.id} workspace={workspace} />
          ))}
        </div>
      )}
    </section>
  )
}
