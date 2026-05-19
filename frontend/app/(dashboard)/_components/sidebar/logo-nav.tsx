"use client"

import * as React from "react"
import Link from "next/link"
import { usePathname } from "next/navigation"

import {
  SidebarGroup,
  SidebarMenu,
  SidebarMenuItem,
  SidebarMenuButton,
} from "@/components/ui/sidebar"

export function LogoNav() {
  const pathname = usePathname()
  
  // Extract workspace slug for dynamic routing
  const segments = pathname.split("/").filter(Boolean)
  const workspaceSlug = segments[0] && segments[0] !== "home" ? segments[0] : "ws-1"

  return (
    <SidebarMenu>
      <SidebarMenuItem>
        <SidebarMenuButton size="lg" asChild className="hover:bg-transparent">
          <Link href={`/${workspaceSlug}`}>
            <div className="flex aspect-square size-8 items-center justify-center rounded-lg bg-[var(--color-brand-violet)] text-[var(--color-paper)]">
              {/* Icon placeholder */}
            </div>
            <div className="grid flex-1 text-left text-sm leading-tight ml-2 group-data-[collapsible=icon]:hidden">
              <span className="truncate font-semibold text-[16px]" style={{ fontFamily: "var(--font-display)", color: "var(--color-graphite)" }}>
                Notrelix
              </span>
            </div>
          </Link>
        </SidebarMenuButton>
      </SidebarMenuItem>
    </SidebarMenu>
  )
}
