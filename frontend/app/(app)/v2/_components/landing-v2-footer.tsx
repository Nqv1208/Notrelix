import Link from "next/link"

import { Separator } from "@/components/ui/separator"
import { routes } from "@/lib/routes"

const groups = [
  {
    title: "Product",
    links: [
      { label: "Overview", href: "#product" },
      { label: "Solutions", href: "#solutions" },
      { label: "Pricing", href: "#pricing" },
    ],
  },
  {
    title: "Resources",
    links: [
      { label: "Docs", href: "#resources" },
      { label: "API", href: "#resources" },
      { label: "Status", href: "#" },
    ],
  },
  {
    title: "Company",
    links: [
      { label: "Contact", href: routes.contact },
      { label: "Privacy", href: routes.auth.privacy },
      { label: "Terms", href: routes.auth.terms },
    ],
  },
] as const

export function LandingV2Footer() {
  const year = new Date().getFullYear()

  return (
    <footer className="border-t border-zinc-200 bg-white dark:border-zinc-800 dark:bg-zinc-950">
      <div className="mx-auto max-w-6xl px-4 py-14 sm:px-6 lg:px-8">
        <div className="grid gap-10 sm:grid-cols-2 lg:grid-cols-4">
          <div>
            <Link href={routes.home} className="text-lg font-semibold text-zinc-900 dark:text-white">
              Notrelix
            </Link>
            <p className="mt-3 text-sm leading-relaxed text-zinc-600 dark:text-zinc-400">
              Docs, tasks, and boards — một workspace cho team hiện đại.
            </p>
          </div>
          {groups.map((g) => (
            <div key={g.title}>
              <p className="text-sm font-semibold text-zinc-900 dark:text-zinc-100">{g.title}</p>
              <ul className="mt-3 space-y-2 text-sm">
                {g.links.map((l) => (
                  <li key={l.label}>
                    <a
                      href={l.href}
                      className="text-zinc-600 hover:text-zinc-900 dark:text-zinc-400 dark:hover:text-white"
                    >
                      {l.label}
                    </a>
                  </li>
                ))}
              </ul>
            </div>
          ))}
        </div>
        <Separator className="my-10 bg-zinc-200 dark:bg-zinc-800" />
        <div className="flex flex-col gap-3 text-xs text-zinc-500 sm:flex-row sm:items-center sm:justify-between dark:text-zinc-500">
          <p>© {year} Notrelix. All rights reserved.</p>
          <p className="text-zinc-400">Landing v2 — experimental marketing surface</p>
        </div>
      </div>
    </footer>
  )
}
