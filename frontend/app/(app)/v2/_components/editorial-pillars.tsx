import { cn } from "@/lib/utils"
import { SectionLabel } from "./editorial-section-label"
import { Reveal } from "./editorial-reveal"

type Pillar = {
  no: string
  name: string
  headline: string
  body: string
  points: string[]
  mock: React.ReactNode
}

/* ----- tiny editorial mocks ----- */

function DocsMock() {
  return (
    <div className="ed-rule ed-paper-2 h-full border p-5">
      <div className="ed-bg-accent mb-3 h-1.5 w-8" />
      <div className="ed-ink ed-serif mb-3 text-lg">Launch plan</div>
      <div className="space-y-2">
        {[100, 88, 94, 70, 82, 60].map((w, i) => (
          <div
            key={i}
            className="h-2 rounded-full bg-[color-mix(in_oklab,var(--ink)_12%,transparent)]"
            style={{ width: `${w}%` }}
          />
        ))}
      </div>
      <div className="ed-rule mt-4 flex items-center gap-2 border-t pt-3">
        <span className="ed-rule size-4 border" />
        <div className="h-2 w-1/3 rounded-full bg-[color-mix(in_oklab,var(--ink)_12%,transparent)]" />
      </div>
    </div>
  )
}

function BoardMock() {
  const cols = [
    { t: "To do", n: 3 },
    { t: "Doing", n: 2 },
    { t: "Done", n: 4 },
  ]
  return (
    <div className="ed-rule ed-paper-2 grid h-full grid-cols-3 gap-2 border p-3">
      {cols.map((c, ci) => (
        <div key={c.t} className="flex flex-col gap-2">
          <div className="ed-mono ed-ink-faint flex items-center justify-between text-[0.6rem] uppercase tracking-wider">
            <span>{c.t}</span>
            <span className="tabular-nums">{c.n}</span>
          </div>
          {Array.from({ length: c.n }).map((_, i) => (
            <div
              key={i}
              className={cn(
                "ed-rule ed-paper border p-2",
                ci === 1 && i === 0 && "[border-color:var(--accent)]"
              )}
            >
              <div className="mb-1.5 h-1.5 w-3/4 rounded-full bg-[color-mix(in_oklab,var(--ink)_18%,transparent)]" />
              <div className="h-1.5 w-1/2 rounded-full bg-[color-mix(in_oklab,var(--ink)_10%,transparent)]" />
            </div>
          ))}
        </div>
      ))}
    </div>
  )
}

function CalendarMock() {
  const marked = [4, 9, 15, 16, 22, 27]
  return (
    <div className="ed-rule ed-paper-2 h-full border p-4">
      <div className="ed-mono ed-ink-faint mb-3 flex items-center justify-between text-[0.65rem] uppercase tracking-wider">
        <span>Week 24</span>
        <span className="ed-accent">●</span>
      </div>
      <div className="grid grid-cols-7 gap-1.5">
        {Array.from({ length: 28 }).map((_, i) => {
          const d = i + 1
          const on = marked.includes(d)
          return (
            <span
              key={i}
              className={cn(
                "ed-mono flex aspect-square items-center justify-center text-[0.6rem] tabular-nums",
                on ? "ed-bg-accent" : "ed-rule ed-ink-faint border"
              )}
            >
              {d}
            </span>
          )
        })}
      </div>
    </div>
  )
}

const pillars: Pillar[] = [
  {
    no: "01",
    name: "Documents",
    headline: "Write, plan, and think in one canvas.",
    body: "A block-based editor for specs, notes, and wikis. Drop in tables, embeds, and live boards — everything stays linked and searchable.",
    points: ["Block editor", "Real-time co-editing", "Version history"],
    mock: <DocsMock />,
  },
  {
    no: "02",
    name: "Boards",
    headline: "Drag, drop, done.",
    body: "Kanban boards with fractional ordering, custom fields, and views that switch in a click. Move work forward without losing the thread.",
    points: ["Kanban & table", "Custom fields", "Automations"],
    mock: <BoardMock />,
  },
  {
    no: "03",
    name: "Calendar",
    headline: "Every deadline, in sync.",
    body: "Two-way Google Calendar sync for cards and pages. Plan the week, see what's due, and never let a date drift out of view.",
    points: ["2-way sync", "Card & page dates", "Team schedule"],
    mock: <CalendarMock />,
  },
]

export function EditorialPillars() {
  return (
    <section id="product" className="mx-auto max-w-[88rem] px-5 py-20 sm:px-8 sm:py-28">
      <SectionLabel index="02" label="Product" />
      <Reveal>
        <h2 className="ed-serif mt-8 max-w-3xl text-balance text-4xl leading-[1.02] tracking-tight sm:text-5xl lg:text-6xl">
          Three surfaces. One source of truth.
        </h2>
      </Reveal>

      <div className="mt-16 flex flex-col">
        {pillars.map((p, i) => (
          <Reveal key={p.no} delay={i * 0.05}>
            <article
              className={cn(
                "ed-rule grid items-center gap-8 border-t py-12 lg:grid-cols-12 lg:gap-12",
                i === pillars.length - 1 && "border-b"
              )}
            >
              {/* index */}
              <div className="lg:col-span-1">
                <span className="ed-serif ed-ink-faint text-3xl tabular-nums">
                  {p.no}
                </span>
              </div>

              {/* copy */}
              <div className={cn("lg:col-span-5", i % 2 === 1 && "lg:order-last")}>
                <p className="ed-eyebrow mb-3">{p.name}</p>
                <h3 className="ed-serif text-2xl leading-tight tracking-tight sm:text-3xl">
                  {p.headline}
                </h3>
                <p className="ed-ink-soft mt-4 max-w-md text-base leading-relaxed">
                  {p.body}
                </p>
                <ul className="mt-6 flex flex-wrap gap-x-6 gap-y-2">
                  {p.points.map((pt) => (
                    <li
                      key={pt}
                      className="ed-mono ed-ink-soft flex items-center gap-2 text-xs"
                    >
                      <span className="ed-accent">+</span>
                      {pt}
                    </li>
                  ))}
                </ul>
              </div>

              {/* mock */}
              <div className="aspect-[4/3] lg:col-span-6">{p.mock}</div>
            </article>
          </Reveal>
        ))}
      </div>
    </section>
  )
}
