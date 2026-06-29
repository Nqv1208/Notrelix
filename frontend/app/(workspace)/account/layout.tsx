"use client"

import { useEffect } from "react"
import { useRouter, usePathname } from "next/navigation"
import { useTranslations } from "next-intl"
import { AppHeader } from "@/app/(dashboard)/_components/app-header"
import { useAuthUser } from "@/features/auth"
import { routes } from "@/lib/routes"
import { AccountSidebar } from "./_components/account-sidebar"

export default function AccountLayout({
  children,
}: {
  children: React.ReactNode
}) {
  const router = useRouter()
  const pathname = usePathname()
  const { isAuthenticated, isLoading, isReady } = useAuthUser()
  const t = useTranslations("account")

  useEffect(() => {
    if (isReady && !isAuthenticated) {
      router.replace(routes.auth.signIn)
    }
  }, [isReady, isAuthenticated, router])

  // Redirect /account → /account/profile
  useEffect(() => {
    if (pathname === "/account") {
      router.replace("/account/profile" as any)
    }
  }, [pathname, router])

  if (!isReady || (isAuthenticated && isLoading)) {
    return (
      <div className="min-h-screen flex items-center justify-center text-sm text-muted-foreground">
        {t("profile.loading")}
      </div>
    )
  }

  if (!isAuthenticated) {
    return null
  }

  return (
    <div className="min-h-svh bg-app-shell text-foreground">
      <AppHeader showSidebarTrigger={false} />
      <div className="mx-auto flex min-h-[calc(100svh-3.5rem)] max-w-5xl gap-0 px-4 py-8 sm:px-6 lg:px-8">
        <AccountSidebar />
        <main className="min-w-0 flex-1 pl-0 sm:pl-8">
          {children}
        </main>
      </div>
    </div>
  )
}
