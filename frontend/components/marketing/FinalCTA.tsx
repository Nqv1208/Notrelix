"use client"

import Link from "next/link"
import { Button } from "@/components/ui/button"
import { routes } from "@/lib/routes"
import { cn } from "@/lib/utils"

export function FinalCTA() {
  return (
    <section className="bg-blue-600 py-20 text-white sm:py-28 relative overflow-hidden">
      {/* Decorative background visual grids */}
      <div
        aria-hidden="true"
        className="pointer-events-none absolute inset-0 bg-[linear-gradient(to_right,rgba(255,255,255,0.06)_1px,transparent_1px),linear-gradient(to_bottom,rgba(255,255,255,0.06)_1px,transparent_1px)] bg-size-[32px_32px]"
      />
      <div
        aria-hidden="true"
        className="pointer-events-none absolute -bottom-48 left-1/2 h-[350px] w-[500px] -translate-x-1/2 bg-white/10 blur-[120px] rounded-full"
      />

      <div className="relative mx-auto max-w-4xl px-4 text-center sm:px-6 lg:px-8">
        <span className="text-[10px] font-extrabold tracking-wider text-blue-100 uppercase bg-white/10 backdrop-blur-xs px-3 py-1 rounded-full">
          GET STARTED TODAY
        </span>
        
        <h2
          className={cn(
            "mt-6 text-balance text-3xl font-extrabold tracking-tight sm:text-4xl lg:text-5xl",
            "[font-family:var(--font-landing-serif),ui-serif,Georgia,serif]"
          )}
        >
          Align your teams and ship faster.
        </h2>
        
        <p className="mx-auto mt-4 max-w-xl text-pretty text-xs leading-relaxed text-blue-100 sm:text-sm">
          Set up your workspace in less than two minutes. Invite your builders, plan your cards, write your docs, and watch execution unfold seamlessly.
        </p>

        <div className="mt-9 flex justify-center gap-3">
          <Link href={routes.auth.register} aria-label="Start using Notrelix for free">
            <Button
              size="lg"
              className="rounded-full bg-white px-8 text-sm font-bold text-blue-600 shadow-xl hover:bg-blue-50 transition-all duration-150"
            >
              Start for free
            </Button>
          </Link>
          <Link href={routes.contact} aria-label="Contact sales team">
            <Button
              size="lg"
              variant="outline"
              className="rounded-full border-white/40 bg-transparent text-white px-8 text-sm font-bold hover:bg-white/10 hover:text-white"
            >
              Talk to Sales
            </Button>
          </Link>
        </div>
      </div>
    </section>
  )
}
