"use client"

import { FileText, CheckSquare, Sparkles, Calendar, Layers, ShieldCheck } from "lucide-react"
import { cn } from "@/lib/utils"

export function FeatureGrid() {
  const features = [
    {
      icon: FileText,
      title: "Notion-like Documents",
      description: "Write specs, launch plans, and meeting notes in rich markdown docs. Seamlessly reference cards or embed boards directly inside standard page blocks.",
      className: "md:col-span-3",
      tag: "DOCS",
      color: "border-blue-500/20 bg-blue-50/5 text-blue-600 dark:border-blue-500/10 dark:bg-blue-950/5",
    },
    {
      icon: CheckSquare,
      title: "Trello-like Boards",
      description: "Organize workflow visually with board column lists, priorities, subtask checklists, and custom due dates. Drag-and-drop seamlessly to ship v1 fast.",
      className: "md:col-span-3",
      tag: "BOARDS",
      color: "border-emerald-500/20 bg-emerald-50/5 text-emerald-600 dark:border-emerald-500/10 dark:bg-emerald-950/5",
    },
    {
      icon: Calendar,
      title: "Two-way Calendar Sync",
      description: "Keep deadlines aligned automatically with external platforms. Two-way Google Calendar integration ensures updates sync back instantly without manual imports.",
      className: "md:col-span-2",
      tag: "CALENDAR",
      color: "border-purple-500/20 bg-purple-50/5 text-purple-600 dark:border-purple-500/10 dark:bg-purple-950/5",
    },
    {
      icon: Sparkles,
      title: "AI Automation Workflows",
      description: "Draft documents, populate kanban cards, and transition status states using simple plain text commands. Write 'Create launch checklists' and watch AI build them.",
      className: "md:col-span-2",
      tag: "AI AUTOMATIONS",
      color: "border-amber-500/20 bg-amber-50/5 text-amber-600 dark:border-amber-500/10 dark:bg-amber-950/5",
    },
    {
      icon: Layers,
      title: "Multi-tenant Workspaces",
      description: "Organize projects, boards, and access configurations inside independent workspaces. Perfect for managing multiple client projects or departmental channels.",
      className: "md:col-span-2",
      tag: "ARCHITECTURE",
      color: "border-indigo-500/20 bg-indigo-50/5 text-indigo-600 dark:border-indigo-500/10 dark:bg-indigo-950/5",
    },
  ]

  return (
    <section className="py-20 border-t border-zinc-200/80 bg-white dark:border-zinc-800 dark:bg-zinc-950">
      <div className="mx-auto max-w-6xl px-4 sm:px-6 lg:px-8">
        
        {/* Section Header */}
        <div className="mx-auto max-w-3xl text-center mb-16">
          <h2
            className={cn(
              "text-3xl font-extrabold tracking-tight text-zinc-950 sm:text-4xl dark:text-white",
              "[font-family:var(--font-landing-serif),ui-serif,Georgia,serif]"
            )}
          >
            A cohesive productivity suite built for modern delivery.
          </h2>
          <p className="mt-4.5 text-base text-zinc-650 dark:text-zinc-400">
            No more jumping between tab browser tabs. Write notes, assign tasks, and visualize progress inside the same high-performing workspace.
          </p>
        </div>

        {/* Bento Grid */}
        <div className="grid grid-cols-1 md:grid-cols-6 gap-6">
          {features.map((f, idx) => {
            const Icon = f.icon
            return (
              <div
                key={idx}
                className={cn(
                  "rounded-2xl border border-zinc-200 bg-zinc-50/40 p-6.5 shadow-xs transition-all duration-300 hover:border-zinc-350 hover:bg-white hover:shadow-lg dark:border-zinc-850 dark:bg-zinc-900/30 dark:hover:border-zinc-750 dark:hover:bg-zinc-900/50",
                  f.className
                )}
              >
                <div className={cn("inline-flex items-center justify-center rounded-xl p-3 border mb-5", f.color)}>
                  <Icon className="h-5.5 w-5.5 shrink-0" />
                </div>
                
                <span className="block text-[10px] font-extrabold tracking-wider text-zinc-450 uppercase mb-1.5 dark:text-zinc-500">
                  {f.tag}
                </span>
                
                <h3 className="text-base font-bold text-zinc-950 dark:text-white mb-2 leading-snug">
                  {f.title}
                </h3>
                
                <p className="text-xs leading-relaxed text-zinc-600 dark:text-zinc-400">
                  {f.description}
                </p>
              </div>
            )
          })}
        </div>
      </div>
    </section>
  )
}
