import { SectionLabel } from "./editorial-section-label"
import { Reveal } from "./editorial-reveal"

const bigStats = [
  { value: "85%", label: "of the Fortune 500 run work in tools like Notrelix" },
  { value: "3M+", label: "tasks automated every month by workspace rules" },
]

export function EditorialScale() {
  return (
    <section id="scale" className="mx-auto max-w-[88rem] px-5 py-20 sm:px-8 sm:py-28">
      <SectionLabel index="04" label="Scale" />

      <div className="mt-8 grid gap-12 lg:grid-cols-12 lg:gap-16">
        {/* Headline + quote */}
        <div className="lg:col-span-6">
          <Reveal>
            <h2 className="ed-serif text-balance text-4xl leading-[1.02] tracking-tight sm:text-5xl">
              Powering teams of every size.
            </h2>
          </Reveal>
          <Reveal delay={0.05}>
            <figure className="ed-rule mt-10 border-l-2 pl-6 [border-color:var(--accent)]">
              <blockquote className="ed-serif text-2xl leading-snug tracking-tight sm:text-3xl">
                “We folded four tools into Notrelix. Planning, docs, and delivery
                finally live in the same place — and shipping got measurably faster.”
              </blockquote>
              <figcaption className="ed-mono ed-ink-soft mt-6 text-xs uppercase tracking-wider">
                Mai Tran — Head of Operations, Northwind
              </figcaption>
            </figure>
          </Reveal>
        </div>

        {/* Big numbers */}
        <div className="lg:col-span-6">
          <div className="ed-rule flex h-full flex-col border-t">
            {bigStats.map((s) => (
              <Reveal key={s.value}>
                <div className="ed-rule flex items-baseline gap-6 border-b py-10">
                  <span className="ed-serif shrink-0 text-6xl tracking-tight tabular-nums sm:text-7xl">
                    {s.value}
                  </span>
                  <span className="ed-ink-soft max-w-xs text-base leading-relaxed">
                    {s.label}
                  </span>
                </div>
              </Reveal>
            ))}
          </div>
        </div>
      </div>
    </section>
  )
}
