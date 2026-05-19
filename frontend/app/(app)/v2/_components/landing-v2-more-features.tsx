import {
  Globe2,
  LayoutTemplate,
  PlugZap,
  Smartphone,
  Sparkles,
  Webhook,
} from "lucide-react"

import { cn } from "@/lib/utils"

const items = [
  {
    icon: LayoutTemplate,
    title: "Templates",
    desc: "Bắt đầu nhanh với playbook cho sản phẩm, marketing và ops.",
  },
  {
    icon: Webhook,
    title: "API & Webhooks",
    desc: "Đồng bộ hai chiều với hệ thống nội bộ của bạn.",
  },
  {
    icon: Smartphone,
    title: "Mobile friendly",
    desc: "Theo dõi board và comment khi đang di chuyển.",
  },
  {
    icon: PlugZap,
    title: "Email sync",
    desc: "Biến hộp thư thành context có cấu trúc.",
  },
  {
    icon: Globe2,
    title: "Chrome extension",
    desc: "Lưu link và tạo task từ bất kỳ tab nào.",
  },
  {
    icon: Sparkles,
    title: "AI assist",
    desc: "Tóm tắt doc, gợi ý checklist, và giữ giọng điệu nhất quán.",
  },
] as const

export function LandingV2MoreFeatures() {
  return (
    <section className="bg-zinc-950 py-20 text-white sm:py-28">
      <div className="mx-auto max-w-6xl px-4 sm:px-6 lg:px-8">
        <h2
          className={cn(
            "max-w-xl text-balance text-3xl font-semibold tracking-tight sm:text-4xl",
            "[font-family:var(--font-landing-serif),ui-serif,Georgia,serif]"
          )}
        >
          And so much more…
        </h2>
        <p className="mt-4 max-w-2xl text-sm leading-relaxed text-zinc-400 sm:text-base">
          Một nền tảng — nhiều cách làm việc. Mở rộng khi team lớn lên mà không phải đổi
          stack.
        </p>

        <div className="mt-12 grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {items.map(({ icon: Icon, title, desc }) => (
            <div
              key={title}
              className="rounded-2xl border border-zinc-800 bg-zinc-900/40 p-5 shadow-sm"
            >
              <div className="flex size-9 items-center justify-center rounded-lg border border-zinc-800 bg-zinc-950">
                <Icon className="size-4 text-zinc-200" aria-hidden />
              </div>
              <p className="mt-4 text-base font-semibold">{title}</p>
              <p className="mt-2 text-sm leading-relaxed text-zinc-400">{desc}</p>
            </div>
          ))}
        </div>
      </div>
    </section>
  )
}
