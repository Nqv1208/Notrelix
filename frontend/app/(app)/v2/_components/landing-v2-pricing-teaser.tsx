import Link from "next/link"

import { Button } from "@/components/ui/button"
import { routes } from "@/lib/routes"
import { cn } from "@/lib/utils"

export function LandingV2PricingTeaser() {
  return (
    <section id="pricing" className="border-t border-zinc-200 bg-zinc-50 py-20 dark:border-zinc-800 dark:bg-zinc-900/40">
      <div className="mx-auto max-w-6xl px-4 sm:px-6 lg:px-8">
        <div className="mx-auto max-w-2xl text-center">
          <h2
            className={cn(
              "text-3xl font-semibold tracking-tight text-zinc-950 sm:text-4xl dark:text-white",
              "[font-family:var(--font-landing-serif),ui-serif,Georgia,serif]"
            )}
          >
            Giá minh bạch, nâng cấp khi bạn sẵn sàng
          </h2>
          <p className="mt-3 text-base leading-relaxed text-zinc-600 dark:text-zinc-400">
            Bắt đầu miễn phí cho core workspace. Thêm SSO, retention và hỗ trợ ưu tiên ở gói
            Team.
          </p>
          <div className="mt-8 flex flex-wrap items-center justify-center gap-3">
            <Link href={routes.auth.register}>
              <Button className="rounded-full bg-zinc-900 px-6 text-white hover:bg-zinc-800 dark:bg-white dark:text-zinc-950 dark:hover:bg-zinc-200">
                Xem bảng giá đầy đủ
              </Button>
            </Link>
            <Link
              href={routes.home}
              className="text-sm font-semibold text-zinc-800 underline-offset-4 hover:underline dark:text-zinc-200"
            >
              So sánh với landing v1
            </Link>
          </div>
        </div>
      </div>
    </section>
  )
}
