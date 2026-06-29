import Image from "next/image"
import Link from "next/link"

import { routes } from "@/lib/routes"

const columns: { heading: string; links: { label: string; href: any }[] }[] = [
  {
    heading: "Product",
    links: [
      { label: "Documents", href: "#product" },
      { label: "Boards", href: "#product" },
      { label: "Calendar", href: "#product" },
      { label: "Automations", href: "#solutions" },
    ],
  },
  {
    heading: "Company",
    links: [
      { label: "Customers", href: "#customers" },
      { label: "Enterprise", href: "#enterprise" },
      { label: "Pricing", href: "#pricing" },
      { label: "Contact", href: routes.contact },
    ],
  },
  {
    heading: "Resources",
    links: [
      { label: "Docs", href: "#" },
      { label: "Changelog", href: "#" },
      { label: "Status", href: "#" },
      { label: "Security", href: "#enterprise" },
    ],
  },
  {
    heading: "Legal",
    links: [
      { label: "Terms", href: routes.auth.terms },
      { label: "Privacy", href: routes.auth.privacy },
    ],
  },
]

export function EditorialFooter() {
  return (
    <footer className="ed-rule border-t">
      <div className="mx-auto max-w-[88rem] px-5 sm:px-8">
        {/* Big wordmark row */}
        <div className="ed-rule flex flex-col gap-8 border-b py-14 lg:flex-row lg:items-end lg:justify-between">
          <div>
            <Link href={routes.home} className="flex items-center gap-2.5">
              <Image
                src="/logo_no_text.png"
                alt="Notrelix"
                width={32}
                height={32}
                className="size-8"
              />
            </Link>
            <p className="ed-ink-soft mt-4 max-w-sm text-sm leading-relaxed">
              One workspace for docs, boards, and work. Plan, write, and ship — all
              in the same place.
            </p>
          </div>
          <Link
            href={routes.auth.register}
            className="ed-ink-block inline-flex w-fit items-center px-5 py-3 text-sm font-medium tracking-tight transition-colors hover:[background-color:var(--accent)] hover:[color:var(--accent-ink)]"
          >
            Start for free
          </Link>
        </div>

        {/* Link columns */}
        <div className="grid grid-cols-2 gap-8 py-14 sm:grid-cols-4">
          {columns.map((col) => (
            <div key={col.heading}>
              <h3 className="ed-eyebrow mb-4">{col.heading}</h3>
              <ul className="flex flex-col gap-2.5">
                {col.links.map((l) => (
                  <li key={l.label}>
                    <Link
                      href={l.href}
                      className="ed-ink-soft text-sm tracking-tight transition-colors hover:[color:var(--ink)]"
                    >
                      {l.label}
                    </Link>
                  </li>
                ))}
              </ul>
            </div>
          ))}
        </div>

        {/* Colophon */}
        <div className="ed-rule flex flex-col gap-3 border-t py-8 sm:flex-row sm:items-center sm:justify-between">
          <p className="ed-mono ed-ink-faint text-xs">
            © {new Date().getFullYear()} Notrelix — All rights reserved.
          </p>
          <p className="ed-mono ed-ink-faint text-xs uppercase tracking-wider">
            Designed in the Swiss editorial tradition
          </p>
        </div>
      </div>
    </footer>
  )
}
