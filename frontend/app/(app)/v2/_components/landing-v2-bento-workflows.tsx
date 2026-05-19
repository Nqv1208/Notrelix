import { Layers, Mail, MessageSquare, Webhook } from "lucide-react"

import { cn } from "@/lib/utils"

export function LandingV2BentoWorkflows() {
  return (
    <section id="solutions" className="border-t border-zinc-200 bg-white py-20 sm:py-28 dark:border-zinc-800 dark:bg-zinc-950">
      <div className="mx-auto max-w-6xl px-4 sm:px-6 lg:px-8">
        <div className="mx-auto max-w-2xl text-center">
          <h2
            className={cn(
              "text-balance text-3xl font-semibold tracking-tight text-zinc-950 sm:text-4xl dark:text-white",
              "[font-family:var(--font-landing-serif),ui-serif,Georgia,serif]"
            )}
          >
            Modeled around your data and workflows.
          </h2>
          <p className="mt-4 text-pretty text-base leading-relaxed text-zinc-600 sm:text-lg dark:text-zinc-400">
            Engine dữ liệu thống nhất: board cho pipeline, automation cho lặp lại, tích hợp cho
            hệ sinh thái bạn đang dùng.
          </p>
        </div>

        <div className="mt-12 grid gap-4 lg:grid-cols-[minmax(0,1fr)_minmax(0,1.05fr)] lg:items-stretch">
          <div className="flex flex-col gap-4">
            <div className="flex flex-1 flex-col justify-between rounded-2xl border border-zinc-200 bg-zinc-50 p-6 dark:border-zinc-800 dark:bg-zinc-900/60">
              <div>
                <p className="text-xs font-semibold uppercase tracking-wide text-zinc-500">
                  Data engine
                </p>
                <p className="mt-3 text-lg font-semibold text-zinc-900 dark:text-zinc-50">
                  Một schema, nhiều view
                </p>
                <p className="mt-2 text-sm leading-relaxed text-zinc-600 dark:text-zinc-400">
                  Liên kết page, task và board — filter theo owner, trạng thái hoặc bất kỳ
                  trường tùy chỉnh nào.
                </p>
              </div>
              <div className="mt-6 rounded-xl border border-zinc-200 bg-white p-4 text-xs text-zinc-600 shadow-sm dark:border-zinc-800 dark:bg-zinc-950 dark:text-zinc-400">
                <div className="flex items-center justify-between border-b border-zinc-100 pb-2 dark:border-zinc-800">
                  <span className="font-semibold text-zinc-800 dark:text-zinc-100">Rollups</span>
                  <span className="rounded-full bg-emerald-50 px-2 py-0.5 font-medium text-emerald-800 dark:bg-emerald-950/50 dark:text-emerald-200">
                    Live
                  </span>
                </div>
                <p className="mt-2 tabular-nums">Cycle time · 4.2d · −18% WoW</p>
              </div>
            </div>

            <div className="grid gap-4 sm:grid-cols-2">
              <div className="rounded-2xl border border-zinc-200 bg-white p-5 dark:border-zinc-800 dark:bg-zinc-950">
                <p className="text-xs font-semibold uppercase tracking-wide text-zinc-500">
                  Workflow
                </p>
                <div className="mt-4 space-y-3">
                  <div className="flex items-center gap-2 rounded-lg border border-zinc-200 bg-zinc-50 px-3 py-2 text-xs dark:border-zinc-800 dark:bg-zinc-900/60">
                    <span className="rounded-md bg-zinc-900 px-2 py-0.5 font-semibold text-white dark:bg-white dark:text-zinc-950">
                      When
                    </span>
                    <span className="text-zinc-600 dark:text-zinc-400">Task moved to Done</span>
                  </div>
                  <div className="flex items-center gap-2 rounded-lg border border-zinc-200 bg-zinc-50 px-3 py-2 text-xs dark:border-zinc-800 dark:bg-zinc-900/60">
                    <span className="rounded-md bg-zinc-900 px-2 py-0.5 font-semibold text-white dark:bg-white dark:text-zinc-950">
                      Then
                    </span>
                    <span className="text-zinc-600 dark:text-zinc-400">
                      Notify #launch channel
                    </span>
                  </div>
                </div>
              </div>

              <div className="rounded-2xl border border-zinc-200 bg-zinc-50 p-5 dark:border-zinc-800 dark:bg-zinc-900/60">
                <p className="text-xs font-semibold uppercase tracking-wide text-zinc-500">
                  Integrations
                </p>
                <div className="mt-4 flex flex-wrap gap-3">
                  {[
                    { icon: MessageSquare, label: "Slack" },
                    { icon: Mail, label: "Email" },
                    { icon: Webhook, label: "Webhooks" },
                    { icon: Layers, label: "API" },
                  ].map(({ icon: Icon, label }) => (
                    <div
                      key={label}
                      className="flex items-center gap-2 rounded-xl border border-zinc-200 bg-white px-3 py-2 text-xs font-medium text-zinc-800 shadow-xs dark:border-zinc-800 dark:bg-zinc-950 dark:text-zinc-100"
                    >
                      <Icon className="size-4 text-zinc-500" aria-hidden />
                      {label}
                    </div>
                  ))}
                </div>
              </div>
            </div>
          </div>

          <div className="flex min-h-[320px] flex-col rounded-2xl border border-zinc-200 bg-white p-4 shadow-sm lg:min-h-0 dark:border-zinc-800 dark:bg-zinc-950">
            <p className="text-xs font-semibold uppercase tracking-wide text-zinc-500">
              Pipeline
            </p>
            <div className="mt-4 grid min-h-[240px] flex-1 grid-cols-3 gap-3 sm:min-h-[280px] lg:min-h-0">
              {["Ideas", "In progress", "Shipped"].map((col, idx) => (
                <div
                  key={col}
                  className="rounded-xl border border-dashed border-zinc-200 bg-zinc-50/80 p-3 dark:border-zinc-800 dark:bg-zinc-900/40"
                >
                  <p className="text-xs font-semibold text-zinc-600 dark:text-zinc-400">{col}</p>
                  <div className="mt-3 space-y-2">
                    {(idx === 0
                      ? ["Research brief", "Beta list"]
                      : idx === 1
                        ? ["Onboarding v2", "Billing"]
                        : ["April release"]
                    ).map((card) => (
                      <div
                        key={card}
                        className="rounded-lg border border-zinc-200 bg-white p-2 text-xs font-medium text-zinc-800 shadow-xs dark:border-zinc-800 dark:bg-zinc-950 dark:text-zinc-100"
                      >
                        {card}
                      </div>
                    ))}
                  </div>
                </div>
              ))}
            </div>
          </div>
        </div>

        <figure className="mx-auto mt-12 max-w-xl text-center">
          <blockquote className="text-sm leading-relaxed text-zinc-700 dark:text-zinc-300">
            “Board và docs nằm cùng một chỗ nên handoff không còn là cuộc săn link.”
          </blockquote>
          <figcaption className="mt-4 flex items-center justify-center gap-3 text-xs text-zinc-500">
            <span
              className="flex size-9 items-center justify-center rounded-full bg-zinc-200 text-sm font-semibold text-zinc-700 dark:bg-zinc-800 dark:text-zinc-200"
              aria-hidden
            >
              K
            </span>
            <div className="text-left">
              <p className="font-medium text-zinc-800 dark:text-zinc-200">Khoa Nguyen</p>
              <p>Head of Product, Stacklane</p>
            </div>
          </figcaption>
        </figure>
      </div>
    </section>
  )
}
