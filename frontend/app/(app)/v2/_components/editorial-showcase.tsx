import { cn } from "@/lib/utils"
import { SectionLabel } from "./editorial-section-label"
import { Reveal } from "./editorial-reveal"

type Cell = {
  tag: string
  title: string
  body: string
  className: string
  feature?: boolean
}

const cells: Cell[] = [
  {
    tag: "Integrations",
    title: "Connected to 50+ apps",
    body: "Slack, GitHub, Figma, Drive, and more. Bring the tools your team already lives in into one operating surface — no more tab sprawl.",
    className: "lg:col-span-7 lg:row-span-2",
    feature: true,
  },
  {
    tag: "Automations",
    title: "Rules that run the busywork",
    body: "Trigger moves, assignments, and updates automatically. Let the workflow do the repetitive part.",
    className: "lg:col-span-5",
  },
  {
    tag: "Intelligence",
    title: "AI that drafts, summarizes, plans",
    body: "Turn a doc into tasks, summarize a thread, or draft the next step — without leaving the page.",
    className: "lg:col-span-5",
  },
  {
    tag: "Collaboration",
    title: "Real-time, everywhere",
    body: "Cursors, comments, and presence across docs and boards.",
    className: "lg:col-span-4",
  },
  {
    tag: "Permissions",
    title: "Granular by design",
    body: "Workspace, board, and page-level access that scales with the org.",
    className: "lg:col-span-4",
  },
  {
    tag: "Search",
    title: "Find anything, instantly",
    body: "One command bar across every document, card, and comment.",
    className: "lg:col-span-4",
  },
]

export function EditorialShowcase() {
  return (
    <section
      id="solutions"
      className="ed-rule ed-paper-2 border-y"
    >
      <div className="mx-auto max-w-[88rem] px-5 py-20 sm:px-8 sm:py-28">
        <SectionLabel index="03" label="Capabilities" />
        <Reveal>
          <h2 className="ed-serif mt-8 max-w-3xl text-balance text-4xl leading-[1.02] tracking-tight sm:text-5xl lg:text-6xl">
            Replace a dozen tools. Keep all the context.
          </h2>
        </Reveal>

        <div className="ed-rule-strong mt-14 grid grid-cols-1 gap-px border [background-color:var(--rule-strong)] lg:grid-cols-12 lg:grid-rows-2">
          {cells.map((c) => (
            <div
              key={c.title}
              className={cn(
                "ed-paper group flex flex-col p-6 sm:p-8",
                c.className
              )}
            >
              <div className="ed-eyebrow flex items-center gap-2">
                <span className="ed-accent">/</span>
                {c.tag}
              </div>
              <h3
                className={cn(
                  "ed-serif mt-auto pt-10 leading-tight tracking-tight",
                  c.feature ? "text-3xl sm:text-4xl" : "text-xl sm:text-2xl"
                )}
              >
                {c.title}
              </h3>
              <p
                className={cn(
                  "ed-ink-soft mt-3 leading-relaxed",
                  c.feature ? "max-w-md text-base" : "text-sm"
                )}
              >
                {c.body}
              </p>
            </div>
          ))}
        </div>
      </div>
    </section>
  )
}
