import { CalendarDays, Filter, Plus, Search, SquareKanban, Table2 } from "lucide-react"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { Progress } from "@/components/ui/progress"
import { Tabs, TabsList, TabsTrigger } from "@/components/ui/tabs"
import { workspaceTasks } from "../../_components/dashboard/workspace-data"

export function BoardWorkbench({ boardId }: { workspaceId: string; boardId?: string }) {
  const title = boardId === "board-roadmap" ? "Roadmap planning" : "Product delivery"
  const groups = ["This week", "Next", "Blocked", "Done"]

  return (
    <main className="mx-auto max-w-[1380px] px-4 py-5 sm:px-6 lg:px-8">
      <section className="mb-5 rounded-2xl border border-border bg-card p-5 shadow-[rgba(205,208,223,0.25)_0px_2px_28px]">
        <div className="flex flex-col gap-4 lg:flex-row lg:items-end lg:justify-between">
          <div>
            <div className="mb-3 inline-flex items-center gap-2 rounded-full border border-border bg-muted px-3 py-1 text-xs font-medium text-muted-foreground">
              <SquareKanban className="size-3.5 text-primary" />
              Board workspace
            </div>
            <h1 className="text-2xl font-semibold tracking-[-0.015em] text-foreground sm:text-3xl">{title}</h1>
            <p className="mt-2 max-w-2xl text-sm leading-6 text-muted-foreground">Track work with docs-linked tasks, owners, status, due dates, and progress.</p>
          </div>
          <div className="flex flex-wrap gap-2">
            <Button variant="outline" className="bg-card"><Search className="size-4" /> Search</Button>
            <Button variant="outline" className="bg-card"><Filter className="size-4" /> Filter</Button>
            <Button className="rounded-full bg-primary text-primary-foreground hover:bg-primary/90"><Plus className="size-4" /> New task</Button>
          </div>
        </div>
      </section>

      <div className="mb-5 rounded-2xl border border-border bg-card p-3">
        <Tabs defaultValue="table">
          <TabsList>
            <TabsTrigger value="table"><Table2 className="size-4" /> Table</TabsTrigger>
            <TabsTrigger value="kanban"><SquareKanban className="size-4" /> Kanban</TabsTrigger>
            <TabsTrigger value="calendar"><CalendarDays className="size-4" /> Calendar</TabsTrigger>
          </TabsList>
        </Tabs>
      </div>

      <section className="overflow-hidden rounded-2xl border border-border bg-card">
        <div className="grid grid-cols-[minmax(260px,1.4fr)_150px_150px_140px_180px] border-b border-border bg-muted px-4 py-3 text-xs font-semibold uppercase tracking-[0.06em] text-muted-foreground">
          <span>Task</span>
          <span>Owner</span>
          <span>Status</span>
          <span>Due</span>
          <span>Progress</span>
        </div>
        {groups.map((group) => (
          <div key={group}>
            <div className="border-b border-border px-4 py-3 text-sm font-semibold text-foreground">{group}</div>
            {workspaceTasks.map((task, index) => (
              <div key={`${group}-${task.id}`} className="grid grid-cols-[minmax(260px,1.4fr)_150px_150px_140px_180px] items-center gap-3 border-b border-border px-4 py-3 text-sm last:border-b-0">
                <div>
                  <p className="font-medium text-foreground">{task.title}</p>
                  <p className="text-xs text-muted-foreground">Linked doc · Docs MVP specification</p>
                </div>
                <span>{task.assignee}</span>
                <Badge className="w-fit rounded-full" style={{ backgroundColor: `${task.color}1f`, color: task.color }}>{task.status}</Badge>
                <span className="text-muted-foreground">{task.due}</span>
                <Progress value={[65, 35, 100, 12][index]} />
              </div>
            ))}
          </div>
        ))}
      </section>
    </main>
  )
}
