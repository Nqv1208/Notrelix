import Link from "next/link"
import { Activity, Clock3, FileText, Search, SquareKanban } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Progress } from "@/components/ui/progress"
import { getWorkspaceBoardHref, getWorkspaceDocHref } from "@/features/workspace/utils/workspace-routes"
import { recentBoards, recentDocs, homeActivity } from "../_components/home-data"
import { HomeWorkspacesSection } from "./_components/home-workspaces-section"

export default function HomePage() {
  return (
    <div className="mx-auto max-w-[1240px] space-y-6">
      <section className="rounded-2xl border border-border bg-card p-6 shadow-[rgba(205,208,223,0.22)_0px_2px_24px]">
        <div className="flex flex-col gap-4 lg:flex-row lg:items-end lg:justify-between">
          <div>
            <div className="mb-3 inline-flex items-center gap-2 rounded-full border border-border bg-muted px-3 py-1 text-xs font-medium text-muted-foreground">
              <Clock3 className="size-3.5 text-primary" />
              Work hub
            </div>
            <h1 className="text-3xl font-semibold tracking-[-0.015em] text-foreground">Home</h1>
            <p className="mt-2 max-w-2xl text-sm leading-6 text-muted-foreground">
              Jump back into recent workspaces, docs, boards, and team updates.
            </p>
          </div>
          <Button variant="outline" className="w-fit bg-card">
            <Search className="size-4" />
            Search all work
          </Button>
        </div>
      </section>

      <HomeWorkspacesSection />

      <div className="grid gap-6 xl:grid-cols-[minmax(0,1fr)_360px]">
        <section className="rounded-2xl border border-border bg-card p-5">
          <div className="mb-4 flex items-center gap-2">
            <FileText className="size-4 text-primary" />
            <h2 className="text-sm font-semibold text-foreground">Recent docs</h2>
          </div>
          <div className="grid gap-3 md:grid-cols-3">
            {recentDocs.map((doc) => (
              <Link key={doc.id} href={getWorkspaceDocHref(doc.workspaceId, doc.id) as never} className="rounded-xl border border-border bg-muted p-4 transition hover:bg-card">
                <span className="mb-4 block text-2xl">{doc.icon}</span>
                <h3 className="line-clamp-1 text-sm font-semibold text-foreground">{doc.title}</h3>
                <p className="mt-1 text-xs text-muted-foreground">{doc.owner} · {doc.updatedAt}</p>
              </Link>
            ))}
          </div>
        </section>

        <section className="rounded-2xl border border-border bg-card p-5">
          <div className="mb-4 flex items-center gap-2">
            <Activity className="size-4 text-primary" />
            <h2 className="text-sm font-semibold text-foreground">Activity</h2>
          </div>
          <div className="space-y-3">
            {homeActivity.map((item) => (
              <div key={`${item.actor}-${item.target}`} className="rounded-xl border border-border bg-muted p-3 text-sm">
                <p className="text-foreground"><span className="font-medium">{item.actor}</span> {item.action} {item.target}</p>
                <p className="mt-1 text-xs text-muted-foreground">{item.time}</p>
              </div>
            ))}
          </div>
        </section>
      </div>

      <section className="rounded-2xl border border-border bg-card p-5">
        <div className="mb-4 flex items-center gap-2">
          <SquareKanban className="size-4 text-primary" />
          <h2 className="text-sm font-semibold text-foreground">Recent boards</h2>
        </div>
        <div className="grid gap-3 md:grid-cols-3">
          {recentBoards.map((board) => (
            <Link key={board.id} href={getWorkspaceBoardHref(board.workspaceId, board.id) as never} className="rounded-xl border border-border p-4 transition hover:bg-muted">
              <div className="mb-3 flex items-center gap-2">
                <span className="size-2 rounded-full" style={{ backgroundColor: board.color }} />
                <h3 className="text-sm font-semibold text-foreground">{board.title}</h3>
              </div>
              <Progress value={board.progress} />
              <p className="mt-3 text-xs text-muted-foreground">Updated {board.updatedAt}</p>
            </Link>
          ))}
        </div>
      </section>
    </div>
  )
}
