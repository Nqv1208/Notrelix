import { cn } from "@/lib/utils"

export function LandingV2BentoCollab() {
  return (
    <section className="py-20 sm:py-28">
      <div className="mx-auto max-w-6xl px-4 sm:px-6 lg:px-8">
        <div className="mx-auto max-w-2xl text-center">
          <h2
            className={cn(
              "text-balance text-3xl font-semibold tracking-tight text-zinc-950 sm:text-4xl dark:text-white",
              "[font-family:var(--font-landing-serif),ui-serif,Georgia,serif]"
            )}
          >
            Designed for multiplayer.
          </h2>
          <p className="mt-4 text-pretty text-base leading-relaxed text-zinc-600 sm:text-lg dark:text-zinc-400">
            Cùng chỉnh sửa, comment theo ngữ cảnh, và nhật ký hoạt động rõ ràng — ai làm gì,
            khi nào, đều có dấu vết.
          </p>
        </div>

        <div className="mt-12 grid gap-4 md:grid-cols-2">
          <div className="rounded-2xl border border-zinc-200 bg-white p-5 dark:border-zinc-800 dark:bg-zinc-950">
            <p className="text-xs font-semibold uppercase tracking-wide text-zinc-500">
              Presence
            </p>
            <div className="mt-4 rounded-xl border border-zinc-100 bg-zinc-50 p-4 dark:border-zinc-800 dark:bg-zinc-900/50">
              <p className="text-sm font-semibold text-zinc-900 dark:text-zinc-50">
                Q2 planning doc
              </p>
              <div className="mt-3 flex -space-x-2">
                {["A", "B", "C"].map((initial) => (
                  <span
                    key={initial}
                    className="inline-flex size-8 items-center justify-center rounded-full border-2 border-white bg-zinc-300 text-xs font-semibold text-zinc-800 dark:border-zinc-950 dark:bg-zinc-700 dark:text-zinc-100"
                  >
                    {initial}
                  </span>
                ))}
                <span className="inline-flex size-8 items-center justify-center rounded-full border-2 border-white bg-zinc-900 text-xs font-medium text-white dark:border-zinc-950 dark:bg-white dark:text-zinc-950">
                  +4
                </span>
              </div>
              <p className="mt-3 text-xs text-zinc-500">3 đang xem · autosave on</p>
            </div>
          </div>

          <div className="rounded-2xl border border-zinc-200 bg-white p-5 dark:border-zinc-800 dark:bg-zinc-950">
            <p className="text-xs font-semibold uppercase tracking-wide text-zinc-500">
              Activity
            </p>
            <ul className="mt-4 space-y-3 text-sm">
              {[
                "Mai assigned you on Billing flow",
                "Huy commented on Launch checklist",
                "Automation posted to #growth",
              ].map((line) => (
                <li
                  key={line}
                  className="rounded-lg border border-zinc-100 bg-zinc-50 px-3 py-2 text-zinc-700 dark:border-zinc-800 dark:bg-zinc-900/60 dark:text-zinc-300"
                >
                  {line}
                </li>
              ))}
            </ul>
          </div>

          <div className="rounded-2xl border border-zinc-200 bg-zinc-50 p-5 dark:border-zinc-800 dark:bg-zinc-900/60">
            <p className="text-xs font-semibold uppercase tracking-wide text-zinc-500">Sync</p>
            <p className="mt-2 text-sm font-semibold text-zinc-900 dark:text-zinc-50">
              Import từ CSV
            </p>
            <div className="mt-4 h-2 overflow-hidden rounded-full bg-zinc-200 dark:bg-zinc-800">
              <div className="h-full w-[72%] rounded-full bg-zinc-900 dark:bg-white" />
            </div>
            <p className="mt-2 text-xs text-zinc-500">72% · 1.2k rows</p>
          </div>

          <div className="rounded-2xl border border-zinc-200 bg-white p-5 dark:border-zinc-800 dark:bg-zinc-950">
            <p className="text-xs font-semibold uppercase tracking-wide text-zinc-500">
              Audit log
            </p>
            <div className="mt-4 max-h-40 space-y-2 overflow-hidden text-xs text-zinc-600 dark:text-zinc-400">
              {[
                "09:12 — Role changed (Editor → Admin)",
                "09:04 — Board exported",
                "08:51 — Webhook delivered 200",
                "08:40 — Field “Priority” updated",
              ].map((row) => (
                <div
                  key={row}
                  className="rounded-md border border-zinc-100 bg-zinc-50 px-2 py-1.5 font-mono dark:border-zinc-800 dark:bg-zinc-900/60"
                >
                  {row}
                </div>
              ))}
            </div>
          </div>
        </div>

        <figure className="mx-auto mt-12 max-w-xl text-center">
          <blockquote className="text-sm leading-relaxed text-zinc-700 dark:text-zinc-300">
            “On-call thích audit log. Design thích board. Tất cả đều trong Notrelix.”
          </blockquote>
          <figcaption className="mt-4 flex items-center justify-center gap-3 text-xs text-zinc-500">
            <span
              className="flex size-9 items-center justify-center rounded-full bg-zinc-200 text-sm font-semibold text-zinc-700 dark:bg-zinc-800 dark:text-zinc-200"
              aria-hidden
            >
              T
            </span>
            <div className="text-left">
              <p className="font-medium text-zinc-800 dark:text-zinc-200">Thu Vo</p>
              <p>CTO, Bluehour</p>
            </div>
          </figcaption>
        </figure>
      </div>
    </section>
  )
}
