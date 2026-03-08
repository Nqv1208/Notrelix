"use client"

import { TooltipProvider } from "@/registry/new-york-v4/ui/tooltip"
import { SidebarProvider, SidebarInset } from "@/registry/new-york-v4/ui/sidebar"
import { AppSidebar } from "@/app/(dashboard)/_components/app-sidebar"
import { DashboardHeader } from "@/app/(dashboard)/_components/dashboard-header"

export default function DashboardLayout({
  children,
}: {
  children: React.ReactNode
}) {
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
