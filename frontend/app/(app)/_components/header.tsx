"use client"

import * as React from "react"
import Link from "next/link"
import { Menu, X, Layers } from "lucide-react"

import { Button } from "@/registry/new-york-v4/ui/button"
import { ThemeToggle } from "@/app/(app)/_components/theme-toggle"
import { useAuthUser } from "@/features/auth/hooks/useAuthUser"
import { useLogout } from "@/features/auth/hooks/useLogout"
import { routes } from "@/lib/routes"
import { cn } from "@/lib/utils"

const navLinks = [
  { href: "#features", label: "Features" },
  { href: "#showcase", label: "Product" },
  { href: "#pricing", label: "Pricing" },
  { href: "#testimonials", label: "Customers" },
]

export function Header() {
  const { user, isAuthenticated } = useAuthUser()
  const logoutMutation = useLogout()
  const [mobileOpen, setMobileOpen] = React.useState(false)
  const [scrolled, setScrolled] = React.useState(false)

  React.useEffect(() => {
    const onScroll = () => setScrolled(window.scrollY > 16)
    window.addEventListener("scroll", onScroll, { passive: true })
    return () => window.removeEventListener("scroll", onScroll)
  }, [])

  return (
    <header
      className={cn(
        "sticky top-0 z-50 transition-all duration-300",
        scrolled
          ? "bg-background/80 backdrop-blur-xl border-b border-border/50 shadow-sm"
          : "bg-transparent"
      )}
    >
      <div className="container mx-auto px-4 sm:px-6 lg:px-8">
        <div className="flex items-center justify-between h-16">
          <Link href={routes.home} className="flex items-center gap-2.5 group">
            <div className="relative flex items-center justify-center size-9 rounded-xl bg-gradient-to-br from-violet-600 to-indigo-600 shadow-lg shadow-violet-500/20 group-hover:shadow-violet-500/40 transition-shadow">
              <Layers className="size-[18px] text-white" />
            </div>
            <span className="text-xl font-bold tracking-tight">
              Craft<span className="bg-gradient-to-r from-violet-600 to-indigo-600 bg-clip-text text-transparent">board</span>
            </span>
          </Link>

          <nav className="hidden md:flex items-center gap-1">
            {navLinks.map((link) => (
              <a
                key={link.href}
                href={link.href}
                className="px-3 py-2 text-sm font-medium text-muted-foreground hover:text-foreground rounded-lg hover:bg-accent/50 transition-colors"
              >
                {link.label}
              </a>
            ))}
          </nav>

          <div className="flex items-center gap-2">
            <ThemeToggle />
            {isAuthenticated ? (
              <>
                <Link href={routes.dashboard.root}>
                  <Button size="sm" variant="ghost">
                    Dashboard
                  </Button>
                </Link>
                <Button
                  size="sm"
                  variant="outline"
                  onClick={() => logoutMutation.mutate()}
                  disabled={logoutMutation.isPending}
                >
                  Sign out
                </Button>
              </>
            ) : (
              <>
                <Link href={routes.auth.signIn} className="hidden sm:block">
                  <Button variant="ghost" size="sm">
                    Sign in
                  </Button>
                </Link>
                <Link href={routes.auth.register}>
                  <Button
                    size="sm"
                    className="bg-gradient-to-r from-violet-600 to-indigo-600 hover:from-violet-700 hover:to-indigo-700 text-white border-0 shadow-lg shadow-violet-500/20"
                  >
                    Get started free
                  </Button>
                </Link>
              </>
            )}
            <button
              onClick={() => setMobileOpen(!mobileOpen)}
              className="md:hidden flex items-center justify-center size-9 rounded-lg hover:bg-accent transition-colors"
            >
              {mobileOpen ? <X className="size-5" /> : <Menu className="size-5" />}
            </button>
          </div>
        </div>
      </div>

      {mobileOpen && (
        <div className="md:hidden border-t bg-background/95 backdrop-blur-xl">
          <nav className="container mx-auto px-4 py-4 flex flex-col gap-1">
            {navLinks.map((link) => (
              <a
                key={link.href}
                href={link.href}
                onClick={() => setMobileOpen(false)}
                className="px-3 py-2.5 text-sm font-medium rounded-lg hover:bg-accent transition-colors"
              >
                {link.label}
              </a>
            ))}
          </nav>
        </div>
      )}
    </header>
  )
}
