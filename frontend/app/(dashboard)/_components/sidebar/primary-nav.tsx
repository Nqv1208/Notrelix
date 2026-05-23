"use client"

import {
  Home,
  CheckSquare,
  MoreHorizontal,
} from "lucide-react"
import Link from "next/link"
import { usePathname } from "next/navigation"

import {
  SidebarGroup,
  SidebarGroupContent,
  SidebarMenu,
  SidebarMenuButton,
  SidebarMenuItem,
} from "@/components/ui/sidebar"

const primaryItems = [
  { title: "Home", url: "/home", icon: Home },
  { title: "My work", url: "/my-work", icon: CheckSquare },
  { title: "More", url: "/more", icon: MoreHorizontal },
]

export function PrimaryNav() {
  const pathname = usePathname()

  return (
    <SidebarGroup>
      <SidebarGroupContent>
        <SidebarMenu>
          {primaryItems.map((item) => {
            const isActive = pathname.startsWith(item.url)
            return (
              <SidebarMenuItem key={item.title}>
                <SidebarMenuButton asChild isActive={isActive} tooltip={item.title} className="h-8 group-data-[collapsible=icon]:justify-center">
                  <Link
                    href={item.url as never}
                    style={{
                      fontFamily: "var(--font-display)",
                      color: isActive ? "var(--color-brand-violet)" : "var(--color-graphite)",
                      fontWeight: isActive ? 500 : 400,
                    }}
                  >
                    <item.icon style={{ color: isActive ? "var(--color-brand-violet)" : "var(--color-slate)" }} />
                    <span className="text-[14px] group-data-[collapsible=icon]:hidden">{item.title}</span>
                  </Link>
                </SidebarMenuButton>
              </SidebarMenuItem>
            )
          })}
        </SidebarMenu>
      </SidebarGroupContent>
    </SidebarGroup>
  )
}
