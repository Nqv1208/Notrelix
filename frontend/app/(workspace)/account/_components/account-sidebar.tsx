"use client"

import Link from "next/link"
import { usePathname } from "next/navigation"
import { useTranslations } from "next-intl"
import { cn } from "@/lib/utils"
import { User, Palette, ShieldCheck, Bell, ChevronLeft } from "lucide-react"
import { buttonVariants } from "@/components/ui/button"

const sidebarNavItems = [
  {
    key: "profile" as const,
    href: "/account/profile",
    icon: User,
  },
  {
    key: "appearance" as const,
    href: "/account/appearance",
    icon: Palette,
  },
  {
    key: "security" as const,
    href: "/account/security",
    icon: ShieldCheck,
  },
  {
    key: "notifications" as const,
    href: "/account/notifications",
    icon: Bell,
  },
]

export function AccountSidebar() {
  const pathname = usePathname()
  const t = useTranslations("account.sidebar")

  return (
    <aside className="w-64 shrink-0 hidden md:block">
      <div className="flex flex-col gap-6">
        {/* Back Link */}
        <div>
          <Link
            href="/home"
            className={cn(
              buttonVariants({ variant: "ghost", size: "sm" }),
              "text-muted-foreground hover:text-foreground -ml-2 gap-1.5"
            )}
          >
            <ChevronLeft size={16} />
            <span>{t("backToHome")}</span>
          </Link>
        </div>

        {/* Navigation Section */}
        <div className="flex flex-col gap-1">
          <h2 className="px-3 text-xs font-semibold uppercase tracking-wider text-muted-foreground mb-2">
            {t("title")}
          </h2>
          <nav className="flex flex-col gap-1">
            {sidebarNavItems.map((item) => {
              const isActive = pathname === item.href
              const Icon = item.icon

              return (
                <Link
                  key={item.href}
                  href={item.href as any}
                  className={cn(
                    "flex items-center gap-3 rounded-lg px-3 py-2.5 text-sm font-medium transition-all duration-200",
                    isActive
                      ? "bg-primary text-primary-foreground shadow-sm shadow-primary/10"
                      : "text-muted-foreground hover:bg-muted hover:text-foreground"
                  )}
                >
                  <Icon size={18} className="shrink-0" />
                  <span>{t(item.key)}</span>
                </Link>
              )
            })}
          </nav>
        </div>
      </div>
    </aside>
  )
}
