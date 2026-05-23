import { cn } from "@/lib/utils"

export function LandingV2FeatureOwn() {
  return (
    <section id="product" className="py-20 sm:py-28">
      <div className="mx-auto max-w-6xl px-4 sm:px-6 lg:px-8">
        <div className="mx-auto max-w-2xl text-center">
          <h2
            className={cn(
              "text-balance text-3xl font-semibold tracking-tight text-zinc-950 sm:text-4xl dark:text-white",
              "[font-family:var(--font-landing-serif),ui-serif,Georgia,serif]"
            )}
          >
            A workspace created to be your own.
          </h2>
          <p className="mt-4 text-pretty text-base leading-relaxed text-zinc-600 sm:text-lg dark:text-zinc-400">
            Tùy chỉnh trường dữ liệu, view và quy trình — giữ nguyên cách team bạn làm việc,
            không phải ngược lại.
          </p>
        </div>

        <div className="mt-12 overflow-hidden rounded-2xl border border-zinc-200 bg-white shadow-xl shadow-zinc-900/5 dark:border-zinc-800 dark:bg-zinc-900 dark:shadow-black/30">
          <div className="grid gap-0 lg:grid-cols-[220px_1fr]">
            <aside className="border-b border-zinc-200 bg-zinc-50 p-4 lg:border-r lg:border-b-0 dark:border-zinc-800 dark:bg-zinc-950/80">
              <p className="text-xs font-semibold uppercase tracking-wide text-zinc-500">
                Fields
              </p>
              <ul className="mt-3 space-y-2 text-sm">
                {["Stage", "Priority", "Estimate", "Owner", "Tags"].map((f) => (
                  <li
                    key={f}
                    className="flex items-center justify-between rounded-lg border border-zinc-200 bg-white px-3 py-2 dark:border-zinc-800 dark:bg-zinc-900"
                  >
                    <span className="font-medium text-zinc-800 dark:text-zinc-100">{f}</span>
                    <span className="text-xs text-zinc-400">⋯</span>
                  </li>
                ))}
              </ul>
            </aside>
            <div className="p-4 sm:p-6">
              <div className="flex flex-wrap items-center gap-2 border-b border-zinc-100 pb-4 dark:border-zinc-800">
                <span className="rounded-full bg-zinc-900 px-3 py-1 text-xs font-medium text-white dark:bg-white dark:text-zinc-950">
                  Table
                </span>
                <span className="rounded-full border border-zinc-200 px-3 py-1 text-xs font-medium text-zinc-600 dark:border-zinc-700 dark:text-zinc-400">
                  Board
                </span>
                <span className="rounded-full border border-zinc-200 px-3 py-1 text-xs font-medium text-zinc-600 dark:border-zinc-700 dark:text-zinc-400">
                  Calendar
                </span>
              </div>
              <div className="mt-4 grid gap-3 sm:grid-cols-3">
                {[
                  { t: "Spec v2", m: "Writing", v: "12 pts" },
                  { t: "Billing flow", m: "Design", v: "8 pts" },
                  { t: "Onboarding", m: "Research", v: "5 pts" },
                ].map((c) => (
                  <div
                    key={c.t}
                    className="rounded-xl border border-zinc-200 bg-zinc-50/80 p-4 dark:border-zinc-800 dark:bg-zinc-950/60"
                  >
                    <p className="text-sm font-semibold text-zinc-900 dark:text-zinc-50">{c.t}</p>
                    <p className="mt-1 text-xs text-zinc-500">{c.m}</p>
                    <p className="mt-3 text-xs font-medium text-zinc-700 dark:text-zinc-300">{c.v}</p>
                  </div>
                ))}
              </div>
            </div>
          </div>
        </div>

        <figure className="mx-auto mt-10 max-w-xl text-center">
          <blockquote className="text-sm leading-relaxed text-zinc-700 dark:text-zinc-300">
            “Chúng tôi thay thế ba công cụ. Mọi người vào một workspace và cuối cùng cũng thấy
            cùng một sự thật.”
          </blockquote>
          <figcaption className="mt-4 flex items-center justify-center gap-3 text-xs text-zinc-500">
            <span
              className="flex size-9 items-center justify-center rounded-full bg-zinc-200 text-sm font-semibold text-zinc-700 dark:bg-zinc-800 dark:text-zinc-200"
              aria-hidden
            >
              L
            </span>
            <div className="text-left">
              <p className="font-medium text-zinc-800 dark:text-zinc-200">Lan Pham</p>
              <p>VP Ops, Northwind</p>
            </div>
          </figcaption>
        </figure>
      </div>
    </section>
  )
}
