import { RevealGroup, RevealItem } from "./editorial-reveal"

type Stat = {
  label: string
  value: string
  note: string
}

const stats: Stat[] = [
  {
    label: "Return on investment",
    value: "384%",
    note: "Average three-year ROI reported by teams that consolidated onto Notrelix.",
  },
  {
    label: "Hours saved / year",
    value: "92,400",
    note: "Manual work recaptured across an organization by replacing fragmented tools.",
  },
  {
    label: "Revenue impact",
    value: "$3.9M",
    note: "Gains driven by shipping faster and keeping context in one workspace.",
  },
  {
    label: "Payback period",
    value: "< 6 mo",
    note: "Time to break even — a proven investment with rapid returns.",
  },
]

export function EditorialStats() {
  return (
    <section className="ed-ink-block">
      <div className="mx-auto max-w-[88rem] px-5 py-20 sm:px-8 sm:py-28">
        <div className="flex items-center gap-4">
          <span className="ed-bg-accent size-2" aria-hidden />
          <span className="ed-mono text-[0.7rem] uppercase tracking-[0.14em] opacity-60">
            Measured outcomes
          </span>
        </div>

        <p className="ed-serif mt-8 max-w-3xl text-balance text-2xl leading-snug tracking-tight sm:text-4xl">
          Third-party research found the average company saves over{" "}
          <span className="[color:var(--accent)]">30,000 hours</span> a year — with
          industry-leading returns.
        </p>

        <RevealGroup className="mt-16 grid grid-cols-1 gap-px sm:grid-cols-2 lg:grid-cols-4">
          {stats.map((s) => (
            <RevealItem key={s.label}>
              <div className="flex h-full flex-col">
                <span className="ed-mono text-[0.7rem] uppercase tracking-[0.14em] opacity-55">
                  {s.label}
                </span>
                <span className="ed-serif mt-4 text-5xl tracking-tight tabular-nums sm:text-6xl">
                  {s.value}
                </span>
                <span className="mt-4 max-w-[24ch] text-sm leading-relaxed opacity-65">
                  {s.note}
                </span>
              </div>
            </RevealItem>
          ))}
        </RevealGroup>
      </div>
    </section>
  )
}
