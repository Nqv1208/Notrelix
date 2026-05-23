import Link from "next/link"
import { ArrowRight } from "lucide-react"

import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { routes } from "@/lib/routes"
import { cn } from "@/lib/utils"

const columns = ["Workspace", "Status", "Owner", "Due"] as const

const rows = [
  { w: "Product roadmap", s: "On track", o: "You", d: "Apr 28" },
  { w: "Design QA", s: "Review", o: "Mai", d: "Apr 24" },
  { w: "Launch checklist", s: "At risk", o: "Team", d: "Apr 30" },
  { w: "Customer research", s: "Done", o: "An", d: "Apr 18" },
] as const

export function LandingV2Hero() {
  return (
    <section className="relative overflow-hidden pt-10 pb-16 sm:pt-14 sm:pb-24 lg:pt-20 lg:pb-28">
      <div
        aria-hidden
        className="pointer-events-none absolute inset-0 bg-[linear-gradient(to_right,#e4e4e7_1px,transparent_1px),linear-gradient(to_bottom,#e4e4e7_1px,transparent_1px)] bg-size-[48px_48px] opacity-40 dark:bg-[linear-gradient(to_right,#27272a_1px,transparent_1px),linear-gradient(to_bottom,#27272a_1px,transparent_1px)]"
      />
      <div className="relative mx-auto max-w-6xl px-4 sm:px-6 lg:px-8">
        <div className="mx-auto max-w-3xl text-center">
          <Badge
            variant="secondary"
            className="mb-6 rounded-full border border-zinc-200 bg-white px-3 py-1 text-xs font-medium text-zinc-700 shadow-xs dark:border-zinc-800 dark:bg-zinc-900 dark:text-zinc-200"
          >
            Now in general availability
          </Badge>
          <h1
            className={cn(
              "text-balance text-4xl font-semibold leading-[1.05] tracking-tight text-zinc-950 sm:text-5xl lg:text-6xl dark:text-white",
              "[font-family:var(--font-landing-serif),ui-serif,Georgia,serif]"
            )}
          >
            Workspace magic for modern teams.
          </h1>
          <p className="mx-auto mt-5 max-w-2xl text-pretty text-base leading-relaxed text-zinc-600 sm:text-lg dark:text-zinc-400">
            Notrelix kết hợp docs linh hoạt và board Kanban — mạnh mẽ, theo dữ liệu, và
            thiết kế để bạn làm việc thật nhanh.
          </p>
          <div className="mt-8 flex flex-wrap items-center justify-center gap-3">
            <Link href={routes.auth.register}>
              <Button
                size="lg"
                className="rounded-full bg-zinc-900 px-6 text-base text-white hover:bg-zinc-800 dark:bg-white dark:text-zinc-950 dark:hover:bg-zinc-200"
              >
                Start for free
              </Button>
            </Link>
            <Link
              href={routes.contact}
              className="group inline-flex items-center gap-1.5 rounded-full px-4 py-2 text-sm font-semibold text-zinc-900 dark:text-zinc-100"
            >
              Book a demo
              <ArrowRight className="size-4 transition-transform group-hover:translate-x-0.5" />
            </Link>
          </div>
        </div>

        <div className="mx-auto mt-14 max-w-5xl">
          <div className="rounded-2xl border border-zinc-200 bg-white p-2 shadow-2xl shadow-zinc-900/10 ring-1 ring-black/5 dark:border-zinc-800 dark:bg-zinc-900 dark:shadow-black/40 dark:ring-white/10">
            <div className="overflow-hidden rounded-xl border border-zinc-100 bg-zinc-50/80 dark:border-zinc-800 dark:bg-zinc-950/60">
              <div className="flex items-center gap-2 border-b border-zinc-200 bg-white px-4 py-3 dark:border-zinc-800 dark:bg-zinc-950">
                <span className="size-2.5 rounded-full bg-red-400/90" />
                <span className="size-2.5 rounded-full bg-amber-400/90" />
                <span className="size-2.5 rounded-full bg-emerald-400/90" />
                <span className="ml-2 text-xs font-medium text-zinc-500">
                  All workspaces — Table
                </span>
              </div>
              <div className="overflow-x-auto">
                <table className="min-w-full text-left text-sm">
                  <thead>
                    <tr className="border-b border-zinc-200 bg-white text-xs font-semibold uppercase tracking-wide text-zinc-500 dark:border-zinc-800 dark:bg-zinc-950 dark:text-zinc-400">
                      {columns.map((c) => (
                        <th key={c} className="whitespace-nowrap px-4 py-3">
                          {c}
                        </th>
                      ))}
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-zinc-100 bg-white dark:divide-zinc-800 dark:bg-zinc-950">
                    {rows.map((r) => (
                      <tr key={r.w} className="hover:bg-zinc-50/80 dark:hover:bg-zinc-900/60">
                        <td className="px-4 py-3 font-medium text-zinc-900 dark:text-zinc-100">
                          {r.w}
                        </td>
                        <td className="px-4 py-3">
                          <span
                            className={cn(
                              "inline-flex rounded-full px-2.5 py-0.5 text-xs font-medium",
                              r.s === "On track" &&
                              "bg-emerald-50 text-emerald-800 dark:bg-emerald-950/50 dark:text-emerald-200",
                              r.s === "Review" &&
                              "bg-amber-50 text-amber-900 dark:bg-amber-950/40 dark:text-amber-200",
                              r.s === "At risk" &&
                              "bg-rose-50 text-rose-800 dark:bg-rose-950/40 dark:text-rose-200",
                              r.s === "Done" &&
                              "bg-zinc-100 text-zinc-700 dark:bg-zinc-800 dark:text-zinc-200"
                            )}
                          >
                            {r.s}
                          </span>
                        </td>
                        <td className="px-4 py-3 text-zinc-600 dark:text-zinc-400">{r.o}</td>
                        <td className="px-4 py-3 text-zinc-500 tabular-nums dark:text-zinc-500">
                          {r.d}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </div>
          </div>
        </div>
      </div>
    </section>
  )
}
