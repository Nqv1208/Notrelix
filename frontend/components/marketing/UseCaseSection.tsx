"use client"

import { Code2, Target, Megaphone, ArrowRight } from "lucide-react"
import { cn } from "@/lib/utils"

export function UseCaseSection() {
  const useCases = [
    {
      icon: Code2,
      title: "Engineering Spec Sheets & QA Boards",
      description: "Manage complex release pipelines. Write tech spec documentation, link it to active boards, and track code issues side-by-side with two-way calendar sync.",
      color: "border-blue-500/20 bg-blue-50/10 text-blue-600 dark:border-blue-500/10 dark:bg-blue-950/10 dark:text-blue-450",
    },
    {
      icon: Target,
      title: "Product Roadmaps & Launch Plans",
      description: "Keep engineering and product aligned. Draft PRDs, launch briefs, and checklists. Watch updates to tasks automatically reflect inside dashboards.",
      color: "border-emerald-500/20 bg-emerald-50/10 text-emerald-600 dark:border-emerald-500/10 dark:bg-emerald-950/10 dark:text-emerald-450",
    },
    {
      icon: Megaphone,
      title: "Marketing Calendars & Assets",
      description: "Coordinate campaign tasks, social calendars, and agency partners. Store launch collateral as S3-backed URLs directly within task details.",
      color: "border-indigo-500/20 bg-indigo-50/10 text-indigo-600 dark:border-indigo-500/10 dark:bg-indigo-950/10 dark:text-indigo-450",
    },
  ]

  return (
    <section className="py-20 border-t border-zinc-200/80 bg-white dark:border-zinc-800 dark:bg-zinc-950">
      <div className="mx-auto max-w-6xl px-4 sm:px-6 lg:px-8">
        
        {/* Section Header */}
        <div className="mx-auto max-w-3xl text-center mb-16">
          <span className="text-[10px] font-extrabold tracking-wider text-blue-600 uppercase dark:text-blue-450">
            USE CASES
          </span>
          <h2
            className={cn(
              "mt-2.5 text-3xl font-extrabold tracking-tight text-zinc-950 sm:text-4xl dark:text-white",
              "[font-family:var(--font-landing-serif),ui-serif,Georgia,serif]"
            )}
          >
            How teams deliver with Notrelix.
          </h2>
          <p className="mt-4 text-base text-zinc-650 dark:text-zinc-400">
            Whether ship code repositories, plan product releases, or orchestrate marketing runs, keep everything connected.
          </p>
        </div>

        {/* Use Case Columns */}
        <div className="grid grid-cols-1 md:grid-cols-3 gap-8 text-left">
          {useCases.map((uc, index) => {
            const Icon = uc.icon
            return (
              <div
                key={index}
                className="flex flex-col rounded-2xl border border-zinc-200 bg-zinc-50/20 p-7 shadow-2xs hover:shadow-md hover:border-zinc-300 transition-all duration-300 dark:border-zinc-850 dark:bg-zinc-900/10 dark:hover:border-zinc-800"
              >
                <div className={cn("inline-flex h-11 w-11 items-center justify-center rounded-xl border mb-6", uc.color)}>
                  <Icon className="h-5.5 w-5.5 shrink-0" />
                </div>
                
                <h3 className="text-sm font-extrabold text-zinc-950 dark:text-white mb-3 leading-snug">
                  {uc.title}
                </h3>
                
                <p className="text-xs leading-relaxed text-zinc-600 dark:text-zinc-400 mb-6 flex-1">
                  {uc.description}
                </p>

                <div className="group flex items-center gap-1 text-[11px] font-bold text-zinc-850 dark:text-zinc-250 cursor-pointer hover:text-blue-600 dark:hover:text-blue-400 transition-colors duration-150">
                  <span>Learn more</span>
                  <ArrowRight className="h-3.5 w-3.5 transition-transform group-hover:translate-x-0.5" />
                </div>
              </div>
            )
          })}
        </div>
      </div>
    </section>
  )
}
