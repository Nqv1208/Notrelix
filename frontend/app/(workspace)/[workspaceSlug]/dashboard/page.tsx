import Link from "next/link"
import type { ComponentType } from "react"
import { Activity, ArrowUpRight, CheckCircle2, Clock3, FileText, Gauge, MessageSquareText, SquareKanban, Users } from "lucide-react"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { Progress } from "@/components/ui/progress"
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs"
import { workspaceAssets, workspaceMembers, workspaceTasks } from "../_components/workspace-data"

export default async function WorkspacePage({ params }: { params: Promise<{ workspaceSlug: string }> }) {
  const { workspaceSlug } = await params
  const activeMembers = workspaceMembers.filter((member) => member.status === "active" || member.status === "in-call")
  const doneTasks = workspaceTasks.filter((task) => task.status === "Done").length

  return (
    <main className="mx-auto max-w-[1380px] px-4 py-5 sm:px-6 lg:px-8">
      <section className="mb-5 rounded-2xl border border-border bg-card p-5 shadow-[rgba(205,208,223,0.25)_0px_2px_28px]">
        <div className="flex flex-col gap-4 lg:flex-row lg:items-end lg:justify-between">
          <div>
            <div className="mb-3 inline-flex items-center gap-2 rounded-full border border-border bg-muted px-3 py-1 text-xs font-medium text-muted-foreground">
              <Gauge className="size-3.5 text-primary" />
              Workspace command center
            </div>
            <h1 className="text-2xl font-semibold tracking-[-0.015em] text-foreground sm:text-3xl">Notrelix OS</h1>
            <p className="mt-2 max-w-2xl text-sm leading-6 text-muted-foreground">
              One place to plan, write, track, discuss, and ship. Boards and docs stay visible together so project context does not fragment.
            </p>
          </div>
        </div>
      </section>

      <div className="mb-5 grid gap-3 md:grid-cols-4">
        <Metric icon={FileText} label="Docs" value="32" detail="8 updated today" />
        <Metric icon={SquareKanban} label="Boards" value="6" detail="4 active sprints" />
        <Metric icon={CheckCircle2} label="Tasks done" value={`${doneTasks}/${workspaceTasks.length}`} detail="Across members" />
        <Metric icon={Users} label="Active now" value={activeMembers.length.toString()} detail={`${workspaceMembers.length} members total`} />
      </div>

      <div className="grid gap-5 xl:grid-cols-[minmax(0,1fr)_380px]">
        <section className="space-y-5">
          <div className="rounded-2xl border border-border bg-card p-4">
            <Tabs defaultValue="assets">
              <div className="mb-4 flex items-center justify-between gap-3">
                <TabsList>
                  <TabsTrigger value="assets">Content</TabsTrigger>
                  <TabsTrigger value="tasks">Tasks</TabsTrigger>
                  <TabsTrigger value="updates">Updates</TabsTrigger>
                </TabsList>
                <Button variant="outline" size="sm" className="bg-card">Filter</Button>
              </div>
              <TabsContent value="assets" className="mt-0">
                <div className="grid gap-3 md:grid-cols-2">
                  {workspaceAssets.map((asset) => (
                    <Link
                      key={asset.id}
                      href={`/${workspaceSlug}${asset.href}`}
                      className="group rounded-xl border border-border bg-muted p-4 transition hover:-translate-y-0.5 hover:bg-card hover:shadow-[rgba(205,208,223,0.32)_0px_2px_24px]"
                    >
                      <div className="mb-5 flex items-center justify-between">
                        <span className="flex size-10 items-center justify-center rounded-xl bg-card text-lg">{asset.icon}</span>
                        <ArrowUpRight className="size-4 text-muted-foreground opacity-0 transition group-hover:opacity-100" />
                      </div>
                      <h3 className="line-clamp-1 text-sm font-semibold text-foreground">{asset.title}</h3>
                      <p className="mt-1 text-xs text-muted-foreground">{asset.type} · {asset.updatedAt} · {asset.owner}</p>
                    </Link>
                  ))}
                </div>
              </TabsContent>
              <TabsContent value="tasks" className="mt-0">
                <TaskTable />
              </TabsContent>
              <TabsContent value="updates" className="mt-0">
                <ActivityList />
              </TabsContent>
            </Tabs>
          </div>

          <div className="rounded-2xl border border-border bg-card p-5">
            <div className="mb-4 flex items-center justify-between">
              <div>
                <h2 className="text-sm font-semibold text-foreground">Board pulse</h2>
                <p className="text-xs text-muted-foreground">A compact health view across active boards.</p>
              </div>
              <Button variant="outline" size="sm" className="bg-card">Open boards</Button>
            </div>
            <div className="grid gap-3 md:grid-cols-3">
              {["Product delivery", "Roadmap planning", "Design QA"].map((board, index) => (
                <div key={board} className="rounded-xl border border-border p-4">
                  <div className="mb-3 flex items-center gap-2">
                    <span className="size-2 rounded-full" style={{ backgroundColor: ["#6161ff", "#2a9d99", "#ff8940"][index] }} />
                    <h3 className="text-sm font-semibold text-foreground">{board}</h3>
                  </div>
                  <Progress value={[68, 43, 81][index]} />
                  <p className="mt-3 text-xs text-muted-foreground">{[18, 9, 14][index]} open tasks · {[6, 2, 11][index]} due this week</p>
                </div>
              ))}
            </div>
          </div>
        </section>

        <aside className="space-y-5">
          <section className="rounded-2xl border border-border bg-card p-5">
            <div className="mb-4 flex items-center gap-2">
              <Users className="size-4 text-primary" />
              <h2 className="text-sm font-semibold text-foreground">Team workload</h2>
            </div>
            <div className="space-y-4">
              {workspaceMembers.map((member) => (
                <div key={member.id}>
                  <div className="mb-2 flex items-center justify-between gap-3">
                    <div className="flex min-w-0 items-center gap-2">
                      <span className="flex size-8 items-center justify-center rounded-full text-[10px] font-semibold text-primary-foreground" style={{ backgroundColor: member.color }}>{member.initials}</span>
                      <div className="min-w-0">
                        <p className="truncate text-sm font-medium text-foreground">{member.name}</p>
                        <p className="text-xs text-muted-foreground">{member.role}</p>
                      </div>
                    </div>
                    <Badge variant="secondary" className="rounded-full">{member.status}</Badge>
                  </div>
                  <Progress value={member.workload} />
                </div>
              ))}
            </div>
          </section>

          <section className="rounded-2xl border border-border bg-card p-5">
            <div className="mb-4 flex items-center gap-2">
              <MessageSquareText className="size-4 text-primary" />
              <h2 className="text-sm font-semibold text-foreground">Recent team activity</h2>
            </div>
            <ActivityList compact />
          </section>
        </aside>
      </div>
    </main>
  )
}

function Metric({ icon: Icon, label, value, detail }: { icon: ComponentType<{ className?: string }>; label: string; value: string; detail: string }) {
  return (
    <div className="rounded-2xl border border-border bg-card p-4 shadow-[rgba(205,208,223,0.18)_0px_2px_18px]">
      <div className="mb-3 flex items-center gap-2 text-xs font-medium text-muted-foreground">
        <Icon className="size-4 text-primary" />
        {label}
      </div>
      <p className="text-2xl font-semibold tracking-[-0.015em] text-foreground">{value}</p>
      <p className="mt-1 text-xs text-muted-foreground">{detail}</p>
    </div>
  )
}

function TaskTable() {
  return (
    <div className="overflow-hidden rounded-xl border border-border">
      {workspaceTasks.map((task) => (
        <div key={task.id} className="grid grid-cols-[minmax(0,1.4fr)_120px_110px_90px] items-center gap-3 border-b border-border px-3 py-3 text-sm last:border-b-0">
          <div className="min-w-0">
            <p className="truncate font-medium text-foreground">{task.title}</p>
            <p className="text-xs text-muted-foreground">{task.board}</p>
          </div>
          <span className="text-muted-foreground">{task.assignee}</span>
          <Badge className="w-fit rounded-full" style={{ backgroundColor: `${task.color}1f`, color: task.color }}>{task.status}</Badge>
          <span className="text-xs text-muted-foreground">{task.due}</span>
        </div>
      ))}
    </div>
  )
}

function ActivityList({ compact }: { compact?: boolean }) {
  const items = [
    ["Ana published", "Q3 operating plan", "12m ago"],
    ["Minh moved", "3 cards in Product delivery", "28m ago"],
    ["Sam commented", "Docs MVP specification", "1h ago"],
    ["Ivy summarized", "Customer interviews", "2h ago"],
  ]

  return (
    <div className="space-y-3">
      {items.slice(0, compact ? 4 : 6).map(([actor, target, time]) => (
        <div key={`${actor}-${target}`} className="flex gap-3 rounded-xl border border-border bg-muted p-3">
          <div className="flex size-8 shrink-0 items-center justify-center rounded-full bg-card text-primary">
            <Activity className="size-4" />
          </div>
          <div className="min-w-0">
            <p className="text-sm text-foreground"><span className="font-medium">{actor}</span> {target}</p>
            <p className="mt-1 flex items-center gap-1 text-xs text-muted-foreground"><Clock3 className="size-3" /> {time}</p>
          </div>
        </div>
      ))}
    </div>
  )
}
