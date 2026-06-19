import { PrimaryAction, GhostAction } from "./editorial-actions"
import { HeroPreview } from "./editorial-hero-preview"
import { routes } from "@/lib/routes"

const meta = ["Free forever", "No credit card", "Set up in minutes"]

export function EditorialHero() {
  return (
    <section className="editorial-grain relative overflow-hidden">
      {/* faint column rules */}
      <div
        aria-hidden
        className="pointer-events-none absolute inset-0 z-0 mx-auto hidden max-w-[88rem] grid-cols-12 px-8 lg:grid"
      >
        {Array.from({ length: 13 }).map((_, i) => (
          <span
            key={i}
            className="ed-rule h-full border-l opacity-40 [grid-column:span_1]"
            style={{ gridColumnStart: i + 1 }}
          />
        ))}
      </div>

      <div className="relative z-10 mx-auto max-w-[88rem] px-5 pt-16 pb-12 sm:px-8 sm:pt-24 lg:pt-28">
        <div className="grid items-end gap-10 lg:grid-cols-12">
          {/* Eyebrow + headline */}
          <div className="lg:col-span-9">
            <div
              className="animate-slide-up flex items-center gap-3 opacity-0"
              style={{ animationDelay: "60ms", animationFillMode: "forwards" }}
            >
              <span className="ed-bg-accent size-2" aria-hidden />
              <span className="ed-eyebrow">The work management platform</span>
            </div>

            <h1 className="ed-serif mt-6 text-balance text-[clamp(2.75rem,7vw,6.25rem)] leading-[0.95] tracking-[-0.02em]">
              <span
                className="animate-slide-up block opacity-0"
                style={{ animationDelay: "120ms", animationFillMode: "forwards" }}
              >
                One workspace.
              </span>
              <span
                className="animate-slide-up block opacity-0"
                style={{ animationDelay: "210ms", animationFillMode: "forwards" }}
              >
                Every kind of work
                <span className="ed-accent">.</span>
              </span>
            </h1>
          </div>

          {/* Sub copy + actions */}
          <div className="lg:col-span-3">
            <p
              className="ed-ink-soft animate-slide-up max-w-sm text-pretty text-base leading-relaxed opacity-0 sm:text-lg"
              style={{ animationDelay: "300ms", animationFillMode: "forwards" }}
            >
              Notrelix folds flexible <span className="ed-ink">documents</span>,
              drag-and-drop <span className="ed-ink">boards</span>, and a synced{" "}
              <span className="ed-ink">calendar</span> into a single, fast
              workspace — so your team plans, writes, and ships in one place.
            </p>
          </div>
        </div>

        {/* Actions + meta */}
        <div
          className="animate-slide-up mt-10 flex flex-col gap-6 opacity-0 sm:flex-row sm:items-center sm:justify-between"
          style={{ animationDelay: "380ms", animationFillMode: "forwards" }}
        >
          <div className="flex flex-wrap items-center gap-2 sm:gap-4">
            <PrimaryAction href={routes.auth.register}>Start for free</PrimaryAction>
            <GhostAction href={routes.contact}>Book a demo</GhostAction>
          </div>
          <ul className="flex flex-wrap items-center gap-x-6 gap-y-2">
            {meta.map((m) => (
              <li key={m} className="ed-mono ed-ink-soft flex items-center gap-2 text-xs">
                <span className="ed-accent">✦</span>
                {m}
              </li>
            ))}
          </ul>
        </div>

        {/* Product preview */}
        <div
          className="animate-slide-up mt-14 opacity-0 sm:mt-20"
          style={{ animationDelay: "460ms", animationFillMode: "forwards" }}
        >
          <HeroPreview />
        </div>
      </div>
    </section>
  )
}
