"use client"

import * as React from "react"
import Image from "next/image"
import Link from "next/link"
import { Menu, X } from "lucide-react"

import { routes } from "@/lib/routes"
import { cn } from "@/lib/utils"

const navLinks = [
  { href: "#product", label: "Product" },
  { href: "#solutions", label: "Solutions" },
  { href: "#scale", label: "Customers" },
  { href: "#pricing", label: "Pricing" },
  { href: "#enterprise", label: "Enterprise" },
]

export function EditorialNav() {
  const [open, setOpen] = React.useState(false)
  const [scrolled, setScrolled] = React.useState(false)

  React.useEffect(() => {
    const onScroll = () => setScrolled(window.scrollY > 8)
    onScroll()
    window.addEventListener("scroll", onScroll, { passive: true })
    return () => window.removeEventListener("scroll", onScroll)
  }, [])

  return (
    <header
      className={cn(
        "sticky top-0 z-50 border-b transition-colors duration-300",
        scrolled
          ? "ed-rule bg-[color-mix(in_oklab,var(--paper)_80%,transparent)] backdrop-blur-md"
          : "border-transparent"
      )}
    >
      <div className="mx-auto flex h-16 max-w-[88rem] items-center justify-between gap-6 px-5 sm:px-8">
        {/* Wordmark */}
        <Link href={routes.home} className="flex items-center gap-2">
          <Image
            src="/logo_no_text.png"
            alt="Notrelix"
            width={28}
            height={28}
            className="size-7"
          />
        </Link>

        {/* Center nav */}
        <nav className="hidden items-center gap-7 md:flex">
          {navLinks.map((l) => (
            <a
              key={l.href}
              href={l.href}
              className="ed-ink-soft ed-link text-sm font-medium tracking-tight transition-colors hover:[color:var(--ink)]"
            >
              {l.label}
            </a>
          ))}
        </nav>

        {/* Right actions */}
        <div className="flex items-center gap-1 sm:gap-3">
          <Link
            href={routes.auth.signIn}
            className="ed-ink-soft hidden text-sm font-medium tracking-tight transition-colors hover:[color:var(--ink)] sm:inline"
          >
            Log in
          </Link>
          <Link
            href={routes.auth.register}
            className="ed-ink-block hidden items-center px-4 py-2 text-sm font-medium tracking-tight transition-colors hover:[background-color:var(--accent)] hover:[color:var(--accent-ink)] sm:inline-flex"
          >
            Start free
          </Link>
          <button
            type="button"
            aria-label={open ? "Close menu" : "Open menu"}
            onClick={() => setOpen((o) => !o)}
            className="ed-rule ed-ink inline-flex size-9 items-center justify-center border md:hidden"
          >
            {open ? <X className="size-4" /> : <Menu className="size-4" />}
          </button>
        </div>
      </div>

      {open ? (
        <div className="ed-rule ed-paper border-t md:hidden">
          <nav className="mx-auto flex max-w-[88rem] flex-col px-5 py-2 sm:px-8">
            {navLinks.map((l) => (
              <a
                key={l.href}
                href={l.href}
                onClick={() => setOpen(false)}
                className="ed-rule ed-ink border-b py-3 text-sm font-medium tracking-tight last:border-b-0"
              >
                {l.label}
              </a>
            ))}
            <Link
              href={routes.auth.signIn}
              onClick={() => setOpen(false)}
              className="ed-ink py-3 text-sm font-medium tracking-tight"
            >
              Log in
            </Link>
          </nav>
        </div>
      ) : null}
    </header>
  )
}
