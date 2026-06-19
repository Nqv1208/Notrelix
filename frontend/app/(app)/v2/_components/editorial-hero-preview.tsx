import { cn } from "@/lib/utils"

const views = ["Table", "Board", "Calendar", "Docs"] as const

type Row = {
  task: string
  status: "On track" | "In review" | "At risk" | "Done"
  owner: string
  due: string
}

const rows: Row[] = [
  { task: "Q3 product roadmap", status: "On track", owner: "You", due: "Jun 28" },
  { task: "Design system audit", status: "In review", owner: "Mai", due: "Jun 24" },
  { task: "Launch checklist", status: "At risk", owner: "Team", due: "Jun 30" },
  { task: "Customer interviews", status: "Done", owner: "An", due: "Jun 18" },
  { task: "Pricing page rewrite", status: "On track", owner: "Linh", due: "Jul 02" },
]

function statusDot(status: Row["status"]) {
  if (status === "Done") return "var(--ink-faint)"
  if (status === "At risk") return "var(--accent)"
  return "var(--ink)"
}

export function HeroPreview() {
  return (
    <div className="relative">
      {/* Main framed window */}
      <div className="ed-rule-strong ed-paper-2 relative z-10 border shadow-[0_40px_80px_-32px_rgba(0,0,0,0.28)]">
        {/* Title bar */}
        <div className="ed-rule flex items-center gap-3 border-b px-4 py-3">
          <div className="flex gap-1.5">
            <span className="ed-rule-strong size-2.5 rounded-full border" />
            <span className="ed-rule-strong size-2.5 rounded-full border" />
            <span className="ed-rule-strong size-2.5 rounded-full border" />
          </div>
          <span className="ed-mono ed-ink-faint ml-1 truncate text-xs">
            notrelix / acme — all work
          </span>
          <div className="ml-auto hidden items-center gap-4 sm:flex">
            {views.map((v, i) => (
              <span
                key={v}
                className={cn(
                  "ed-mono text-[0.7rem] uppercase tracking-wider",
                  i === 0 ? "ed-ink border-b pb-0.5 [border-color:var(--accent)]" : "ed-ink-faint"
                )}
              >
                {v}
              </span>
            ))}
          </div>
        </div>

        {/* Table */}
        <div className="overflow-x-auto">
          <table className="min-w-full border-collapse text-left">
            <thead>
              <tr className="ed-rule border-b">
                {["Task", "Status", "Owner", "Due"].map((c) => (
                  <th
                    key={c}
                    className="ed-mono ed-ink-faint px-4 py-2.5 text-[0.7rem] font-medium uppercase tracking-wider"
                  >
                    {c}
                  </th>
                ))}
              </tr>
            </thead>
            <tbody>
              {rows.map((r) => (
                <tr
                  key={r.task}
                  className="ed-rule border-b transition-colors last:border-b-0 hover:bg-[color-mix(in_oklab,var(--ink)_4%,transparent)]"
                >
                  <td className="ed-ink px-4 py-3 text-sm font-medium tracking-tight">
                    {r.task}
                  </td>
                  <td className="px-4 py-3">
                    <span className="ed-ink-soft flex items-center gap-2 text-sm">
                      <span
                        className="size-2 rounded-full"
                        style={{ backgroundColor: statusDot(r.status) }}
                      />
                      {r.status}
                    </span>
                  </td>
                  <td className="ed-ink-soft px-4 py-3 text-sm">{r.owner}</td>
                  <td className="ed-ink-faint ed-mono px-4 py-3 text-sm tabular-nums">
                    {r.due}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>

      {/* Floating calendar chip — overlap for depth */}
      <div className="ed-rule-strong ed-paper absolute -right-3 -bottom-8 z-20 hidden w-52 border p-3 shadow-[0_24px_48px_-20px_rgba(0,0,0,0.3)] sm:block">
        <div className="ed-eyebrow mb-2 flex items-center justify-between">
          <span>June</span>
          <span className="ed-accent">●</span>
        </div>
        <div className="grid grid-cols-7 gap-1">
          {Array.from({ length: 28 }).map((_, i) => {
            const day = i + 1
            const marked = [18, 24, 28].includes(day)
            return (
              <span
                key={i}
                className={cn(
                  "ed-mono flex aspect-square items-center justify-center text-[0.6rem] tabular-nums",
                  marked ? "ed-bg-accent" : "ed-ink-faint"
                )}
              >
                {day}
              </span>
            )
          })}
        </div>
      </div>
    </div>
  )
}
