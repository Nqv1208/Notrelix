import Link, { LinkProps } from "next/link"
import { ArrowRight } from "lucide-react"

import { cn } from "@/lib/utils"

/** Solid near-black block — the primary call to action. Sharp corners, Swiss. */
export function PrimaryAction({
  href,
  children,
  className,
}: {
  href: LinkProps<any>["href"]
  children: React.ReactNode
  className?: string
}) {
  return (
    <Link
      href={href}
      className={cn(
        "ed-ink-block group inline-flex items-center gap-2.5 px-6 py-3.5 text-sm font-medium tracking-tight transition-colors",
        "hover:[background-color:var(--accent)] hover:[color:var(--accent-ink)]",
        className
      )}
    >
      {children}
      <ArrowRight className="size-4 transition-transform duration-300 group-hover:translate-x-1" />
    </Link>
  )
}

/** Quiet text action with a drawing underline. */
export function GhostAction({
  href,
  children,
  className,
}: {
  href: LinkProps<any>["href"]
  children: React.ReactNode
  className?: string
}) {
  return (
    <Link
      href={href}
      className={cn(
        "ed-ink group inline-flex items-center gap-2 px-1 py-3.5 text-sm font-medium tracking-tight",
        className
      )}
    >
      <span className="ed-link">{children}</span>
      <ArrowRight className="size-4 transition-transform duration-300 group-hover:translate-x-1" />
    </Link>
  )
}
