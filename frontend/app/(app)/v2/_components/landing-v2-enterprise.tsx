import { cn } from "@/lib/utils"

const badges = ["SOC 2", "GDPR", "SSO / SAML", "99.9% SLA"] as const

export function LandingV2Enterprise() {
  return (
    <section className="border-t border-zinc-200 bg-white py-16 dark:border-zinc-800 dark:bg-zinc-950">
      <div className="mx-auto flex max-w-6xl flex-col items-start justify-between gap-10 px-4 sm:flex-row sm:items-center sm:px-6 lg:px-8">
        <div className="max-w-xl">
          <h2
            className={cn(
              "text-3xl font-semibold tracking-tight text-zinc-950 sm:text-4xl dark:text-white",
              "[font-family:var(--font-landing-serif),ui-serif,Georgia,serif]"
            )}
          >
            Built for the enterprise.
          </h2>
          <p className="mt-3 text-base leading-relaxed text-zinc-600 dark:text-zinc-400">
            Kiểm soát truy cập, tuân thủ và độ tin cậy — sẵn sàng cho team vận hành ở quy mô
            lớn.
          </p>
        </div>
        <div className="flex flex-wrap gap-3">
          {badges.map((b) => (
            <span
              key={b}
              className="rounded-full border border-zinc-200 bg-zinc-50 px-4 py-2 text-sm font-semibold text-zinc-800 dark:border-zinc-800 dark:bg-zinc-900 dark:text-zinc-100"
            >
              {b}
            </span>
          ))}
        </div>
      </div>
    </section>
  )
}
