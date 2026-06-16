import Link from "next/link"
import { ArrowRight } from "lucide-react"

import { routes } from "@/lib/routes"

export function EditorialFinalCta() {
  return (
    <section className="ed-bg-accent">
      <div className="mx-auto max-w-[88rem] px-5 py-24 sm:px-8 sm:py-32">
        <div className="flex items-center gap-3">
          <span className="size-2 bg-[var(--accent-ink)]" aria-hidden />
          <span className="ed-mono text-[0.7rem] uppercase tracking-[0.14em] opacity-70">
            07 / 08 — Get started
          </span>
        </div>

        <h2 className="ed-serif mt-8 max-w-4xl text-balance text-5xl leading-[0.98] tracking-[-0.02em] sm:text-7xl lg:text-8xl">
          Maximize productivity.
        </h2>
        <p className="mt-6 max-w-xl text-lg leading-relaxed opacity-80">
          Bring your docs, boards, and calendar together today. Free forever, no
          credit card required.
        </p>

        <div className="mt-10 flex flex-wrap items-center gap-2 sm:gap-5">
          <Link
            href={routes.auth.register}
            className="group inline-flex items-center gap-2.5 bg-[var(--accent-ink)] px-7 py-4 text-sm font-medium tracking-tight text-[var(--accent)] transition-transform hover:-translate-y-0.5"
          >
            Start for free
            <ArrowRight className="size-4 transition-transform duration-300 group-hover:translate-x-1" />
          </Link>
          <Link
            href={routes.contact}
            className="group inline-flex items-center gap-2 px-2 py-4 text-sm font-medium tracking-tight"
          >
            <span className="ed-link">Talk to sales</span>
            <ArrowRight className="size-4 transition-transform duration-300 group-hover:translate-x-1" />
          </Link>
        </div>
      </div>
    </section>
  )
}
