import { cn } from "@/lib/utils"

const posts = [
  {
    name: "minh.builds",
    handle: "@minhq",
    body: "Không còn phải paste link Slack vào Notion rồi paste ngược lại. Notrelix là interface của team mình.",
  },
  {
    name: "Ha — Design",
    handle: "@hadesign",
    body: "Board trông đẹp mà không “toy”. Cuối cùng cũng có chỗ cho design crit có version.",
  },
  {
    name: "engineering.vn",
    handle: "@engvn",
    body: "Webhooks ổn, schema rõ. Chúng tôi mirror status sang internal dashboard trong một buổi chiều.",
  },
  {
    name: "ops weekly",
    handle: "@opsweekly",
    body: "Audit log + roles = CFO vui. Dev vẫn ship nhanh. Hiếm khi thấy combo đó.",
  },
  {
    name: "lien.pm",
    handle: "@lienpm",
    body: "Mình dùng cho roadmap và sprint board. Onboarding intern chỉ mất một video 6 phút.",
  },
  {
    name: "startupfounder",
    handle: "@sf_alex",
    body: "Landing v2 này vibe đúng kiểu product thật — hy vọng app cũng vậy (spoiler: đang vậy).",
  },
] as const

export function LandingV2LovedByBuilders() {
  return (
    <section
      id="resources"
      className="border-t border-zinc-900 bg-zinc-950 py-20 text-white sm:py-28"
    >
      <div className="mx-auto max-w-6xl px-4 sm:px-6 lg:px-8">
        <h2
          className={cn(
            "text-balance text-center text-3xl font-semibold tracking-tight sm:text-4xl",
            "[font-family:var(--font-landing-serif),ui-serif,Georgia,serif]"
          )}
        >
          Loved by builders
        </h2>
        <p className="mx-auto mt-3 max-w-2xl text-center text-sm text-zinc-400 sm:text-base">
          Những team muốn công cụ trông chỉn chu nhưng vẫn linh hoạt như spreadsheet.
        </p>

        <div className="mt-12 columns-1 gap-4 sm:columns-2 lg:columns-3">
          {posts.map((p) => (
            <article
              key={p.handle}
              className="mb-4 break-inside-avoid rounded-2xl border border-zinc-800 bg-zinc-900/50 p-4"
            >
              <div className="flex items-center gap-3">
                <span className="flex size-9 items-center justify-center rounded-full bg-zinc-800 text-xs font-semibold">
                  {p.name.slice(0, 1).toUpperCase()}
                </span>
                <div>
                  <p className="text-sm font-semibold">{p.name}</p>
                  <p className="text-xs text-zinc-500">{p.handle}</p>
                </div>
              </div>
              <p className="mt-3 text-sm leading-relaxed text-zinc-200">{p.body}</p>
            </article>
          ))}
        </div>
      </div>
    </section>
  )
}
