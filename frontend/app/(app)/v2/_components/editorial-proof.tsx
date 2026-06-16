import { Star } from "lucide-react"

const logos = [
  "Atlas Labs",
  "Northwind",
  "Vela",
  "Monogram",
  "Foundry",
  "Lumen",
  "Cobalt",
  "Field Notes",
  "Юника",
  "Saigon Co",
]

export function EditorialProof() {
  return (
    <section className="ed-rule border-y" id="customers">
      <div className="mx-auto grid max-w-[88rem] grid-cols-1 lg:grid-cols-12">
        {/* Caption */}
        <div className="ed-rule flex flex-col justify-center gap-3 px-5 py-8 sm:px-8 lg:col-span-4 lg:border-r">
          <div className="flex items-center gap-1.5">
            {Array.from({ length: 5 }).map((_, i) => (
              <Star
                key={i}
                className="size-3.5 fill-[var(--accent)] text-[var(--accent)]"
              />
            ))}
            <span className="ed-mono ed-ink ml-2 text-sm tabular-nums">4.7 / 5</span>
          </div>
          <p className="ed-ink-soft text-sm leading-relaxed">
            Rated by{" "}
            <span className="ed-ink font-medium tabular-nums">10,000+</span> teams on
            G2 — the{" "}
            <span className="ed-ink">#1 most referenced</span> workspace in category
            reports.
          </p>
        </div>

        {/* Marquee */}
        <div className="relative flex items-center overflow-hidden py-8 lg:col-span-8">
          <div
            aria-hidden
            className="pointer-events-none absolute inset-y-0 left-0 z-10 w-16 bg-gradient-to-r from-[var(--paper)] to-transparent"
          />
          <div
            aria-hidden
            className="pointer-events-none absolute inset-y-0 right-0 z-10 w-16 bg-gradient-to-l from-[var(--paper)] to-transparent"
          />
          <div className="ed-marquee-track gap-12 px-8">
            {[...logos, ...logos].map((name, i) => (
              <span
                key={`${name}-${i}`}
                className="ed-serif ed-ink-faint shrink-0 text-xl tracking-tight transition-colors hover:[color:var(--ink)]"
              >
                {name}
              </span>
            ))}
          </div>
        </div>
      </div>
    </section>
  )
}
