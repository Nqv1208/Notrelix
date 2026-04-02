"use client"

import { useEffect } from "react"
import { useRouter } from "next/navigation"
import { TooltipProvider } from "@/registry/new-york-v4/ui/tooltip"
import { SidebarProvider, SidebarInset } from "@/registry/new-york-v4/ui/sidebar"
import { AppSidebar } from "@/app/(dashboard)/_components/app-sidebar"
import { DashboardHeader } from "@/app/(dashboard)/_components/dashboard-header"
import { useAuthUser } from "@/features/auth/hooks/useAuthUser"

export default function DashboardLayout({
  children,
}: {
  children: React.ReactNode
}) {
  const router = useRouter()
  const { isAuthenticated, isLoading, isReady } = useAuthUser()

  useEffect(() => {
    if (isReady && !isAuthenticated) {
      router.replace("/sign-in")
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
      <SidebarProvider>
        <AppSidebar />
        <SidebarInset>
          <DashboardHeader />
          <main className="flex-1 overflow-auto">
            {children}
          </main>
        </SidebarInset>
      </SidebarProvider>
    </TooltipProvider>
  )
}
