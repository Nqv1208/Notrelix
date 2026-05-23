"use client"

import { useEffect } from "react"
import { useRouter } from "next/navigation"
import { TooltipProvider } from "@/components/ui/tooltip"
import { SidebarProvider, SidebarInset } from "@/components/ui/sidebar"
import { AppSidebar } from "@/app/(dashboard)/_components/app-sidebar"
import { AppHeader } from "@/app/(dashboard)/_components/app-header"
import { useAuthUser } from "@/features/auth/hooks/useAuthUser"
import { routes } from "@/lib/routes"

export default function DashboardLayout({
  children,
}: {
  children: React.ReactNode
}) {
  const router = useRouter()
  const { isAuthenticated, isLoading, isReady } = useAuthUser()

  useEffect(() => {
    if (isReady && !isAuthenticated) {
      router.replace(routes.auth.signIn)
    }
  }, [isReady, isAuthenticated, router])

  if (!isReady || (isAuthenticated && isLoading)) {
    return (
      <div className="min-h-screen flex items-center justify-center text-sm text-muted-foreground">
        Đang kiểm tra phiên đăng nhập...
      </div>
    )
  }

  if (!isAuthenticated) {
    return null
  }

  return (
    <TooltipProvider delayDuration={0}>
      <SidebarProvider className="h-screen flex-col overflow-hidden bg-app-shell text-foreground">
        <AppHeader showSidebarTrigger={false} />
        <div className="flex flex-1 overflow-hidden px-2 pb-1">
          <div className="h-full ml-2" style={{ transform: "translateZ(0)" }}>
            <AppSidebar className="!h-full rounded-tl-xl rounded-bl-xl overflow-hidden" />
          </div>
          <SidebarInset className="!m-0 min-h-0 flex-1 overflow-hidden rounded-br-xl rounded-tr-xl bg-card shadow-sm">
            <main className="h-full overflow-auto bg-card p-8">
              {children}
            </main>
          </SidebarInset>
        </div>
      </SidebarProvider>
    </TooltipProvider>
  )
}
