import Link from "next/link"

import { Button } from "@/components/ui/button"
import { routes } from "@/lib/routes"
import { cn } from "@/lib/utils"

export function LandingV2FinalCta() {
  return (
    <section className="bg-[#2563eb] py-20 text-white sm:py-24">
      <div className="mx-auto max-w-4xl px-4 text-center sm:px-6 lg:px-8">
        <h2
          className={cn(
            "text-balance text-3xl font-semibold tracking-tight sm:text-4xl lg:text-5xl",
            "[font-family:var(--font-landing-serif),ui-serif,Georgia,serif]"
          )}
        >
          Ready to build your workspace?
        </h2>
        <p className="mx-auto mt-4 max-w-2xl text-pretty text-sm leading-relaxed text-blue-100 sm:text-base">
          Tạo workspace trong vài phút. Mời team, nhập board, và giữ mọi quyết định có bối
          cảnh.
        </p>
        <div className="mt-8">
          <Link href={routes.auth.register}>
            <Button
              size="lg"
              className="rounded-full bg-white px-8 text-base font-semibold text-blue-700 shadow-lg hover:bg-blue-50"
            >
              Start for free
            </Button>
          </Link>
        </div>
      </div>
    </section>
  )
}
