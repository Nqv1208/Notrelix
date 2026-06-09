"use client"

import Link from "next/link"
import { ArrowRight, Sparkles, ShieldCheck, Zap } from "lucide-react"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { routes } from "@/lib/routes"
import { cn } from "@/lib/utils"
import { HeroInteractiveDemo } from "./HeroInteractiveDemo"

export function LandingHero() {
  return (
    <section className="relative overflow-hidden pt-12 pb-20 sm:pt-16 sm:pb-28 lg:pt-24 lg:pb-36 bg-zinc-50 dark:bg-zinc-950">
      
      {/* Decorative Grid Background */}
      <div
        aria-hidden="true"
        className="pointer-events-none absolute inset-0 bg-[linear-gradient(to_right,#e4e4e7_1px,transparent_1px),linear-gradient(to_bottom,#e4e4e7_1px,transparent_1px)] bg-size-[40px_40px] opacity-40 dark:bg-[linear-gradient(to_right,#27272a_1px,transparent_1px),linear-gradient(to_bottom,#27272a_1px,transparent_1px)]"
      />

      {/* Decorative radial gradient glow */}
      <div
        aria-hidden="true"
        className="pointer-events-none absolute -top-40 left-1/2 h-96 w-[600px] -translate-x-1/2 bg-blue-500/10 blur-[100px] dark:bg-blue-600/10"
      />

      <div className="relative mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
        
        {/* Hero Copy Content */}
        <div className="mx-auto max-w-3xl text-center">
          <Badge
            variant="secondary"
            className="mb-6 rounded-full border border-zinc-200 bg-white/80 backdrop-blur-xs px-3.5 py-1 text-xs font-semibold text-zinc-700 shadow-xs dark:border-zinc-800 dark:bg-zinc-900/80 dark:text-zinc-200"
          >
            <Sparkles className="mr-1.5 h-3.5 w-3.5 text-blue-500 fill-blue-500/20" />
            Notrelix Workspace v2 is now live
          </Badge>
          
          <h1
            className={cn(
              "text-balance text-4xl font-extrabold leading-[1.08] tracking-tight text-zinc-950 sm:text-5xl lg:text-6.5xl dark:text-white",
              "[font-family:var(--font-landing-serif),ui-serif,Georgia,serif]"
            )}
          >
            One workspace. <br className="sm:hidden" />
            Perfect execution.
          </h1>
          
          <p className="mx-auto mt-6 max-w-2xl text-pretty text-base leading-relaxed text-zinc-600 sm:text-lg dark:text-zinc-400">
            Combine flexible documents, drag-and-drop boards, and powerful calendars into a single, cohesive workspace. Orchestrated by AI to keep your teams synchronized.
          </p>

          {/* CTA Buttons */}
          <div className="mt-8.5 flex flex-wrap items-center justify-center gap-4">
            <Link href={routes.auth.register} aria-label="Start using Notrelix for free">
              <Button
                size="lg"
                className="rounded-full bg-zinc-900 px-7 text-base font-bold text-white hover:bg-zinc-850 shadow-md dark:bg-white dark:text-zinc-950 dark:hover:bg-zinc-200"
              >
                Start for free
              </Button>
            </Link>
            <Link
              href={routes.contact}
              className="group inline-flex items-center gap-1.5 rounded-full px-5 py-2.5 text-sm font-bold text-zinc-900 transition-colors duration-150 hover:bg-zinc-100 dark:text-zinc-100 dark:hover:bg-zinc-900"
              aria-label="Book a product demo"
            >
              <span>Book a demo</span>
              <ArrowRight className="h-4 w-4 transition-transform group-hover:translate-x-0.5" />
            </Link>
          </div>
        </div>

        {/* Browser Demo mockup frame container */}
        <div className="mx-auto mt-16 max-w-5.5xl">
          <div className="relative group rounded-2xl p-1 bg-gradient-to-b from-zinc-200/50 via-zinc-100/30 to-transparent dark:from-zinc-800/50 dark:via-zinc-900/30">
            <div className="absolute inset-0 bg-blue-500/5 opacity-0 group-hover:opacity-100 blur-2xl transition-opacity duration-700" />
            <HeroInteractiveDemo />
          </div>
        </div>
      </div>
    </section>
  )
}
